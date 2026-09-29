using Microsoft.ML.Data;

namespace MovieRevenue.Core;

// Money and count features are stored as log(1 + x): their raw distribution is heavily skewed.
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

    public float LogRevenue { get; set; }
}

public sealed class ScorePrediction
{
    [ColumnName("Score")]
    public float Score { get; set; }
}
