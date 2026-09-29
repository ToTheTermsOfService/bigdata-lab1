using System.Globalization;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;

namespace MovieRevenue.Core;

// ML.NET's TextLoader cannot read this file: the genres column holds JSON and free-text columns
// contain commas, quotes and newlines. TextFieldParser handles all of that.
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
