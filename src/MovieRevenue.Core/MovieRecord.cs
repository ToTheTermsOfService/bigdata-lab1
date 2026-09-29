namespace MovieRevenue.Core;

/// <summary>
/// Фільм у "людському" вигляді: те, що ми прочитали з CSV або отримали від користувача API.
/// </summary>
public sealed class MovieRecord
{
    public string Title { get; init; } = "";
    public double Budget { get; init; }
    public double Revenue { get; init; }
    public double Popularity { get; init; }
    public double Runtime { get; init; }
    public double VoteAverage { get; init; }
    public double VoteCount { get; init; }
    public DateTime ReleaseDate { get; init; }
    public string OriginalLanguage { get; init; } = "en";
    public IReadOnlyList<string> Genres { get; init; } = [];
}
