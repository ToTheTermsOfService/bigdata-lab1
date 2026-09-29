namespace MovieRevenue.Core;

/// <summary>
/// Опис натренованої моделі. Тренер зберігає його поруч із .zip, API показує в /api/model/info.
/// </summary>
public sealed class ModelInfo
{
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public string BestTrainer { get; set; } = "";
    public string[] Features { get; set; } = [];
    public DateTime TrainedAtUtc { get; set; }
    public int TrainRows { get; set; }
    public int TestRows { get; set; }
    public List<TrainerResult> Leaderboard { get; set; } = [];

    /// <summary>Permutation feature importance найкращої моделі: наскільки падає R², якщо перемішати ознаку.</summary>
    public List<FeatureImportance> FeatureImportance { get; set; } = [];
}

public sealed class FeatureImportance
{
    public string Feature { get; set; } = "";
    public double RSquaredDrop { get; set; }
}

public sealed class TrainerResult
{
    public string Trainer { get; set; } = "";

    /// <summary>Середній R² на 5-fold крос-валідації (на тренувальній вибірці).</summary>
    public double CvRSquaredMean { get; set; }
    public double CvRSquaredStd { get; set; }

    /// <summary>Метрики на відкладеній тестовій вибірці (у тій шкалі, в якій вчилась модель).</summary>
    public double TestRSquared { get; set; }
    public double TestRmse { get; set; }
    public double TestMae { get; set; }

    /// <summary>Лише для моделі доходу: похибки у доларах після зворотного перетворення expm1.</summary>
    public double? TestMaeUsd { get; set; }
    public double? TestMedianApePercent { get; set; }

    public double TrainSeconds { get; set; }
}
