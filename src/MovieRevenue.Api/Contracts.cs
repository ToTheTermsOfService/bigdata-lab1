using System.ComponentModel;

namespace MovieRevenue.Api;

/// <summary>Movie to predict for.</summary>
public sealed class MovieRequest
{
    /// <summary>Title. Echoed back in the response, does not affect the prediction.</summary>
    [DefaultValue("Dune: Part Three")]
    public string? Title { get; set; }

    /// <summary>Production budget, USD.</summary>
    [DefaultValue(190_000_000)]
    public double Budget { get; set; }

    /// <summary>Runtime in minutes.</summary>
    [DefaultValue(155)]
    public double Runtime { get; set; }

    /// <summary>Release date; year and month are used as features.</summary>
    [DefaultValue("2026-12-18")]
    public DateOnly? ReleaseDate { get; set; }

    /// <summary>TMDB genres, see GET /api/genres.</summary>
    public List<string>? Genres { get; set; }

    /// <summary>Original language, ISO 639-1 code (en, fr, ja, ...).</summary>
    [DefaultValue("en")]
    public string OriginalLanguage { get; set; } = "en";

    /// <summary>TMDB popularity score (dataset median is about 13, blockbusters 50-200).</summary>
    [DefaultValue(80)]
    public double Popularity { get; set; }

    /// <summary>Average vote, 0-10. When omitted, the rating model's prediction is used instead.</summary>
    [DefaultValue(null)]
    public double? VoteAverage { get; set; }

    /// <summary>TMDB vote count (dataset median is about 500, hits 5000+).</summary>
    [DefaultValue(4000)]
    public double VoteCount { get; set; }
}

public sealed record RevenueResponse(
    string? Title,
    double PredictedRevenueUsd,
    double PredictedProfitUsd,
    double ReturnOnInvestment,
    double VoteAverageUsed,
    string VoteAverageSource,
    string Model);

public sealed record RatingResponse(string? Title, double PredictedVoteAverage, string Model);
