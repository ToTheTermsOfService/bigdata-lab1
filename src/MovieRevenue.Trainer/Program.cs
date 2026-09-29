// Частина 1: тренування, оцінювання та серіалізація моделей.
// Запуск: dotnet run --project src/MovieRevenue.Trainer [шлях_до_csv]

using System.Text;
using Microsoft.ML;
using MovieRevenue.Core;
using MovieRevenue.Trainer;

Console.OutputEncoding = Encoding.UTF8;

var root = ModelFiles.FindSolutionRoot(AppContext.BaseDirectory);
var csvPath = args.Length > 0 ? args[0] : Path.Combine(root, "data", "tmdb_5000_movies.csv");
var modelsDir = Path.Combine(root, "models");

Console.WriteLine($"Читаю датасет: {csvPath}");
var movies = TmdbCsvReader.Read(csvPath);
Console.WriteLine($"Всього фільмів з датою релізу: {movies.Count}");

// Очищення даних. У TMDB багато фільмів з budget/revenue = 0 (невідомо) або з "1", "12" (записано в мільйонах) —
// такі рядки лише шум для регресії, тому залишаємо фільми з бюджетом і зборами від $10 000.
var revenueRows = movies
    .Where(m => m.Budget >= 10_000 && m.Revenue >= 10_000 && m.Runtime > 0)
    .Select(m => (m, FeatureBuilder.Build(m)))
    .ToList();

// Для рейтингу потрібна достатня кількість голосів, інакше середня оцінка випадкова.
var ratingRows = movies
    .Where(m => m.VoteCount >= 20 && m.Runtime > 0)
    .Select(m => (m, FeatureBuilder.Build(m)))
    .ToList();

Console.WriteLine($"Після очищення: для моделі доходу — {revenueRows.Count}, для моделі рейтингу — {ratingRows.Count}");

var ml = new MLContext(seed: 42);

// Модель 1: дохід (касові збори). Мітка — log(1 + revenue).
var revenueSpec = new ExperimentSpec(
    Name: "Прогноз доходу фільму",
    Label: nameof(ModelInput.LogRevenue),
    NumericFeatures:
    [
        nameof(ModelInput.LogBudget), nameof(ModelInput.LogPopularity), nameof(ModelInput.Runtime),
        nameof(ModelInput.VoteAverage), nameof(ModelInput.LogVoteCount),
        nameof(ModelInput.ReleaseYear), nameof(ModelInput.ReleaseMonth), nameof(ModelInput.Genres),
    ],
    ModelFile: ModelFiles.RevenueModel,
    InfoFile: ModelFiles.RevenueInfo,
    LabelIsLogRevenue: true);

// Модель 2: рейтинг (vote_average, 0–10). Голоси/рейтинг не використовуємо як ознаки — це була б "підказка відповіді".
var ratingSpec = new ExperimentSpec(
    Name: "Прогноз рейтингу фільму",
    Label: nameof(ModelInput.VoteAverage),
    NumericFeatures:
    [
        nameof(ModelInput.LogBudget), nameof(ModelInput.LogPopularity), nameof(ModelInput.Runtime),
        nameof(ModelInput.ReleaseYear), nameof(ModelInput.ReleaseMonth), nameof(ModelInput.Genres),
    ],
    ModelFile: ModelFiles.RatingModel,
    InfoFile: ModelFiles.RatingInfo,
    LabelIsLogRevenue: false);

new RegressionExperiment(ml, revenueSpec, modelsDir).Run(revenueRows);
new RegressionExperiment(ml, ratingSpec, modelsDir).Run(ratingRows);

Console.WriteLine();
Console.WriteLine($"Готово. Моделі та метрики лежать у {modelsDir}");
