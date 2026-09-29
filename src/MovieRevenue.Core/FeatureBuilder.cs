namespace MovieRevenue.Core;

public static class FeatureBuilder
{
    public static ModelInput Build(MovieRecord movie) => new()
    {
        LogBudget = Log1p(movie.Budget),
        LogPopularity = Log1p(movie.Popularity),
        Runtime = (float)movie.Runtime,
        VoteAverage = (float)movie.VoteAverage,
        LogVoteCount = Log1p(movie.VoteCount),
        ReleaseYear = movie.ReleaseDate.Year,
        ReleaseMonth = movie.ReleaseDate.Month,
        Genres = MovieGenres.Encode(movie.Genres),
        OriginalLanguage = string.IsNullOrWhiteSpace(movie.OriginalLanguage) ? "en" : movie.OriginalLanguage,
        LogRevenue = Log1p(movie.Revenue),
    };

    public static float Log1p(double value) => (float)Math.Log(1 + Math.Max(0, value));

    public static double Expm1(float value) => Math.Exp(value) - 1;
}
