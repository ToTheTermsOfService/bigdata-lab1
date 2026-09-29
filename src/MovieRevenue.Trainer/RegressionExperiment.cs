using System.Diagnostics;
using System.Text.Json;
using Microsoft.ML;
using Microsoft.ML.Data;
using MovieRevenue.Core;

namespace MovieRevenue.Trainer;

/// <summary>Одна регресійна задача: що прогнозуємо (Label) і за якими ознаками.</summary>
public sealed record ExperimentSpec(
    string Name,
    string Label,
    string[] NumericFeatures,
    string ModelFile,
    string InfoFile,
    bool LabelIsLogRevenue);

/// <summary>
/// Тренування → оцінювання → серіалізація для однієї задачі:
/// 1) ділимо дані 80/20; 2) для кожного тренера — 5-fold CV на train та оцінка на test;
/// 3) найкращий за CV R² зберігаємо в .zip + опис у .json.
/// </summary>
public sealed class RegressionExperiment(MLContext ml, ExperimentSpec spec, string modelsDir)
{
    private const int Seed = 42;

    public ModelInfo Run(List<(MovieRecord Movie, ModelInput Input)> rows)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 100));
        Console.WriteLine($" Модель: {spec.Name}   (мітка: {spec.Label}, рядків: {rows.Count})");
        Console.WriteLine(new string('=', 100));

        // Детермінований поділ 80/20 — щоб результати відтворювались і ми знали назви фільмів у test.
        var rng = new Random(Seed);
        var shuffled = rows.OrderBy(_ => rng.Next()).ToList();
        var testCount = rows.Count / 5;
        var test = shuffled.Take(testCount).ToList();
        var train = shuffled.Skip(testCount).ToList();

        var trainData = ml.Data.LoadFromEnumerable(train.Select(r => r.Input));
        var testData = ml.Data.LoadFromEnumerable(test.Select(r => r.Input));

        var results = new List<(TrainerResult Result, ITransformer Model)>();
        Console.WriteLine($"{"Тренер",-22} {"CV R² (mean±std)",18} {"Test R²",9} {"Test RMSE",10} {"Test MAE",9} {"MAE, $M",9} {"MedAPE",8} {"час, с",7}");

        foreach (var (name, create) in TrainerCatalog.All)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var pipeline = BuildPipeline().Append(create(ml, spec.Label));

                var cv = ml.Regression.CrossValidate(trainData, pipeline, numberOfFolds: 5, labelColumnName: spec.Label, seed: Seed);
                var cvR2 = cv.Select(f => f.Metrics.RSquared).ToArray();

                var model = pipeline.Fit(trainData);
                var predictions = model.Transform(testData);
                var metrics = ml.Regression.Evaluate(predictions, labelColumnName: spec.Label);
                sw.Stop();

                var result = new TrainerResult
                {
                    Trainer = name,
                    CvRSquaredMean = cvR2.Average(),
                    CvRSquaredStd = StdDev(cvR2),
                    TestRSquared = metrics.RSquared,
                    TestRmse = metrics.RootMeanSquaredError,
                    TestMae = metrics.MeanAbsoluteError,
                    TrainSeconds = sw.Elapsed.TotalSeconds,
                };

                if (spec.LabelIsLogRevenue)
                    AddDollarMetrics(result, predictions);

                results.Add((result, model));
                PrintRow(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{name,-22} ПОМИЛКА: {ex.GetType().Name}: {ex.Message}");
            }
        }

        var (best, bestModel) = results.OrderByDescending(r => r.Result.CvRSquaredMean).First();
        Console.WriteLine();
        Console.WriteLine($" Найкращий тренер за CV R²: {best.Trainer} (CV R² = {best.CvRSquaredMean:F3}, Test R² = {best.TestRSquared:F3})");

        var importance = PermutationImportance(bestModel, test.Select(r => r.Input).ToList());
        Console.WriteLine(" Важливість ознак (падіння R² на test при перемішуванні ознаки):");
        foreach (var fi in importance)
            Console.WriteLine($"   {fi.Feature,-18} {fi.RSquaredDrop,7:F3}  {new string('#', (int)Math.Clamp(fi.RSquaredDrop * 100, 0, 60))}");

        PrintSamplePredictions(bestModel, test);

        // Серіалізація: модель разом зі схемою вхідних даних → .zip
        Directory.CreateDirectory(modelsDir);
        var modelPath = Path.Combine(modelsDir, spec.ModelFile);
        ml.Model.Save(bestModel, trainData.Schema, modelPath);

        var info = new ModelInfo
        {
            Name = spec.Name,
            Label = spec.Label,
            BestTrainer = best.Trainer,
            Features = [.. spec.NumericFeatures, nameof(ModelInput.OriginalLanguage)],
            TrainedAtUtc = DateTime.UtcNow,
            TrainRows = train.Count,
            TestRows = test.Count,
            Leaderboard = [.. results.Select(r => r.Result).OrderByDescending(r => r.CvRSquaredMean)],
            FeatureImportance = importance,
        };
        File.WriteAllText(Path.Combine(modelsDir, spec.InfoFile), JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($" Модель збережено: {modelPath}");
        return info;
    }

    /// <summary>
    /// Спільна частина пайплайну: one-hot кодування мови → склеювання ознак у вектор → нормалізація.
    /// Нормалізація MinMax (у [0; 1]) потрібна лінійним моделям (Sdca, OGD, Ols, Poisson), деревам вона не шкодить.
    /// MeanVariance тут гірша: рідкісні one-hot колонки (мови) після неї отримують величезні значення і OGD розходиться.
    /// </summary>
    private IEstimator<ITransformer> BuildPipeline() =>
        ml.Transforms.Categorical.OneHotEncoding("LanguageEncoded", nameof(ModelInput.OriginalLanguage))
            .Append(ml.Transforms.Concatenate(TrainerCatalog.FeaturesColumn, [.. spec.NumericFeatures, "LanguageEncoded"]))
            .Append(ml.Transforms.NormalizeMinMax(TrainerCatalog.FeaturesColumn));

    private void AddDollarMetrics(TrainerResult result, IDataView predictions)
    {
        var actual = predictions.GetColumn<float>(spec.Label).Select(FeatureBuilder.Expm1).ToArray();
        var predicted = predictions.GetColumn<float>("Score").Select(FeatureBuilder.Expm1).ToArray();

        result.TestMaeUsd = actual.Zip(predicted, (a, p) => Math.Abs(a - p)).Average();
        var ape = actual.Zip(predicted, (a, p) => Math.Abs(a - p) / a * 100).Order().ToArray();
        result.TestMedianApePercent = ape[ape.Length / 2];
    }

    /// <summary>
    /// Permutation feature importance "вручну": перемішуємо одну ознаку в тестових даних і дивимось,
    /// наскільки погіршився R². Чим більше падіння — тим сильніше модель спирається на цю ознаку.
    /// </summary>
    private List<FeatureImportance> PermutationImportance(ITransformer model, List<ModelInput> test)
    {
        var baseline = Evaluate(model, test);
        var result = new List<FeatureImportance>();

        foreach (var feature in spec.NumericFeatures.Append(nameof(ModelInput.OriginalLanguage)))
        {
            var prop = typeof(ModelInput).GetProperty(feature)!;
            var rng = new Random(Seed);
            var permutedValues = test.Select(r => prop.GetValue(r)).OrderBy(_ => rng.Next()).ToList();

            var permuted = test.Select((r, i) =>
            {
                var copy = Clone(r);
                prop.SetValue(copy, permutedValues[i]);
                return copy;
            }).ToList();

            result.Add(new FeatureImportance { Feature = feature, RSquaredDrop = baseline - Evaluate(model, permuted) });
        }
        return [.. result.OrderByDescending(r => r.RSquaredDrop)];
    }

    private double Evaluate(ITransformer model, List<ModelInput> rows) =>
        ml.Regression.Evaluate(model.Transform(ml.Data.LoadFromEnumerable(rows)), labelColumnName: spec.Label).RSquared;

    private void PrintSamplePredictions(ITransformer model, List<(MovieRecord Movie, ModelInput Input)> test)
    {
        var engine = ml.Model.CreatePredictionEngine<ModelInput, ScorePrediction>(model);
        Console.WriteLine(" Приклади прогнозів на тестових фільмах:");

        foreach (var (movie, input) in test.Take(8))
        {
            var score = engine.Predict(input).Score;
            var line = spec.LabelIsLogRevenue
                ? $"факт {movie.Revenue / 1e6,8:F1} $M   прогноз {FeatureBuilder.Expm1(score) / 1e6,8:F1} $M   (бюджет {movie.Budget / 1e6:F1} $M)"
                : $"факт {movie.VoteAverage,4:F1}   прогноз {score,4:F1}";
            Console.WriteLine($"   {Truncate(movie.Title, 38),-38} {line}");
        }
    }

    private static void PrintRow(TrainerResult r) =>
        Console.WriteLine(
            $"{r.Trainer,-22} {$"{r.CvRSquaredMean:F3} ± {r.CvRSquaredStd:F3}",18} {r.TestRSquared,9:F3} {r.TestRmse,10:F3} {r.TestMae,9:F3} " +
            $"{(r.TestMaeUsd is { } mae ? (mae / 1e6).ToString("F1") : "-"),9} {(r.TestMedianApePercent is { } ape ? $"{ape:F0}%" : "-"),8} {r.TrainSeconds,7:F1}");

    private static ModelInput Clone(ModelInput r) => new()
    {
        LogBudget = r.LogBudget, LogPopularity = r.LogPopularity, Runtime = r.Runtime, VoteAverage = r.VoteAverage,
        LogVoteCount = r.LogVoteCount, ReleaseYear = r.ReleaseYear, ReleaseMonth = r.ReleaseMonth,
        Genres = r.Genres, OriginalLanguage = r.OriginalLanguage, LogRevenue = r.LogRevenue,
    };

    private static double StdDev(double[] values)
    {
        var mean = values.Average();
        return Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / values.Length);
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
