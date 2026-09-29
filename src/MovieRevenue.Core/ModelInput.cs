using Microsoft.ML.Data;

namespace MovieRevenue.Core;

/// <summary>
/// Рядок даних, який бачить ML.NET. Усі ознаки вже числові (крім мови — її кодує пайплайн).
/// Грошові величини та лічильники беремо в логарифмі: log(1 + x) —
/// розподіл бюджетів/зборів дуже скошений, логарифм робить його близьким до нормального.
/// </summary>
public sealed class ModelInput
{
    public float LogBudget { get; set; }
    public float LogPopularity { get; set; }
    public float Runtime { get; set; }
    public float VoteAverage { get; set; }
    public float LogVoteCount { get; set; }
    public float ReleaseYear { get; set; }
    public float ReleaseMonth { get; set; }

    [VectorType(MovieGenres.Count)]
    public float[] Genres { get; set; } = new float[MovieGenres.Count];

    public string OriginalLanguage { get; set; } = "en";

    /// <summary>Мітка для моделі доходу: log(1 + revenue).</summary>
    public float LogRevenue { get; set; }
}

/// <summary>Вихід регресійної моделі: ML.NET кладе прогноз у колонку Score.</summary>
public sealed class ScorePrediction
{
    [ColumnName("Score")]
    public float Score { get; set; }
}
