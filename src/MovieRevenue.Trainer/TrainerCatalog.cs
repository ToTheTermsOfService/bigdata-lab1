using Microsoft.ML;
using Microsoft.ML.Trainers;

namespace MovieRevenue.Trainer;

/// <summary>
/// Регресійні тренери ML.NET з методички. Кожен отримує колонку "Features" та назву колонки-мітки.
/// </summary>
public static class TrainerCatalog
{
    public const string FeaturesColumn = "Features";

    public static IReadOnlyList<(string Name, Func<MLContext, string, IEstimator<ITransformer>> Create)> All { get; } =
    [
        ("Sdca", (ml, label) => ml.Regression.Trainers.Sdca(labelColumnName: label, featureColumnName: FeaturesColumn)),
        ("LbfgsPoisson", (ml, label) => ml.Regression.Trainers.LbfgsPoissonRegression(labelColumnName: label, featureColumnName: FeaturesColumn)),
        ("OnlineGradientDescent", (ml, label) => ml.Regression.Trainers.OnlineGradientDescent(labelColumnName: label, featureColumnName: FeaturesColumn)),
        // L2-регуляризація: без неї матриця XᵀX вироджена (one-hot колонки мов/жанрів лінійно залежні).
        ("Ols", (ml, label) => ml.Regression.Trainers.Ols(new OlsTrainer.Options { LabelColumnName = label, FeatureColumnName = FeaturesColumn, L2Regularization = 0.1f })),
        ("Gam", (ml, label) => ml.Regression.Trainers.Gam(labelColumnName: label, featureColumnName: FeaturesColumn)),
        ("FastTree", (ml, label) => ml.Regression.Trainers.FastTree(labelColumnName: label, featureColumnName: FeaturesColumn)),
        ("FastTreeTweedie", (ml, label) => ml.Regression.Trainers.FastTreeTweedie(labelColumnName: label, featureColumnName: FeaturesColumn)),
        ("FastForest", (ml, label) => ml.Regression.Trainers.FastForest(labelColumnName: label, featureColumnName: FeaturesColumn)),
        ("LightGbm", (ml, label) => ml.Regression.Trainers.LightGbm(labelColumnName: label, featureColumnName: FeaturesColumn)),
    ];
}
