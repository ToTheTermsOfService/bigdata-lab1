namespace MovieRevenue.Core;

public static class MovieGenres
{
    public const int Count = 20;

    public static readonly string[] All =
    [
        "Action", "Adventure", "Animation", "Comedy", "Crime",
        "Documentary", "Drama", "Family", "Fantasy", "Foreign",
        "History", "Horror", "Music", "Mystery", "Romance",
        "Science Fiction", "TV Movie", "Thriller", "War", "Western",
    ];

    public static bool IsKnown(string genre) =>
        All.Contains(genre, StringComparer.OrdinalIgnoreCase);

    public static float[] Encode(IEnumerable<string> genres)
    {
        var vector = new float[Count];
        foreach (var genre in genres)
        {
            var index = Array.FindIndex(All, g => g.Equals(genre, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                vector[index] = 1f;
        }
        return vector;
    }
}
