using System.ComponentModel;

namespace MovieRevenue.Api;

/// <summary>Параметри фільму, для якого робимо прогноз.</summary>
public sealed class MovieRequest
{
    /// <summary>Назва (лише для відповіді, на прогноз не впливає).</summary>
    [DefaultValue("Dune: Part Three")]
    public string? Title { get; set; }

    /// <summary>Бюджет виробництва у доларах США.</summary>
    [DefaultValue(190_000_000)]
    public double Budget { get; set; }

    /// <summary>Тривалість у хвилинах.</summary>
    [DefaultValue(155)]
    public double Runtime { get; set; }

    /// <summary>Дата релізу (впливають рік та місяць).</summary>
    [DefaultValue("2026-12-18")]
    public DateOnly? ReleaseDate { get; set; }

    /// <summary>Жанри з переліку TMDB (див. GET /api/genres).</summary>
    public List<string>? Genres { get; set; }

    /// <summary>Мова оригіналу, код ISO 639-1 (en, fr, ja, ...).</summary>
    [DefaultValue("en")]
    public string OriginalLanguage { get; set; } = "en";

    /// <summary>Популярність TMDB (у датасеті медіана ≈ 13, блокбастери — 50–200).</summary>
    [DefaultValue(80)]
    public double Popularity { get; set; }

    /// <summary>Середня оцінка 0–10. Якщо не задано — підставляється прогноз моделі рейтингу.</summary>
    [DefaultValue(null)]
    public double? VoteAverage { get; set; }

    /// <summary>Кількість голосів на TMDB (у датасеті медіана ≈ 500, хіти — 5000+).</summary>
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
