using System.Globalization;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;

namespace MovieRevenue.Core;

/// <summary>
/// Читає tmdb_5000_movies.csv. ML.NET TextLoader тут не підходить: колонка genres містить JSON,
/// а overview/tagline — коми, лапки та переноси рядків. TextFieldParser коректно обробляє все це.
/// </summary>
public static class TmdbCsvReader
{
    public static List<MovieRecord> Read(string path)
    {
        using var parser = new TextFieldParser(path) { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true };
        parser.SetDelimiters(",");

        var header = parser.ReadFields() ?? throw new InvalidDataException("CSV без заголовка");
        var col = header.Select((name, i) => (name, i)).ToDictionary(x => x.name, x => x.i);

        var movies = new List<MovieRecord>();
        while (!parser.EndOfData)
        {
            var f = parser.ReadFields();
            if (f is null || f.Length < header.Length)
                continue;

            if (!DateTime.TryParse(f[col["release_date"]], CultureInfo.InvariantCulture, out var releaseDate))
                continue;

            movies.Add(new MovieRecord
            {
                Title = f[col["title"]],
                Budget = ParseDouble(f[col["budget"]]),
                Revenue = ParseDouble(f[col["revenue"]]),
                Popularity = ParseDouble(f[col["popularity"]]),
                Runtime = ParseDouble(f[col["runtime"]]),
                VoteAverage = ParseDouble(f[col["vote_average"]]),
                VoteCount = ParseDouble(f[col["vote_count"]]),
                ReleaseDate = releaseDate,
                OriginalLanguage = f[col["original_language"]],
                Genres = ParseGenres(f[col["genres"]]),
            });
        }
        return movies;
    }

    private static double ParseDouble(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;

    // Формат: [{"id": 28, "name": "Action"}, {"id": 12, "name": "Adventure"}]
    private static List<string> ParseGenres(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("name").GetString() ?? "")
            .Where(n => n.Length > 0)
            .ToList();
    }
}
