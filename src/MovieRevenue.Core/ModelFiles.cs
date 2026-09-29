namespace MovieRevenue.Core;

public static class ModelFiles
{
    public const string RevenueModel = "revenue-model.zip";
    public const string RevenueInfo = "revenue-model.json";
    public const string RatingModel = "rating-model.zip";
    public const string RatingInfo = "rating-model.json";

    /// <summary>
    /// Шукає корінь солюшну (папку з *.slnx), піднімаючись від заданої директорії.
    /// Потрібно, щоб і Trainer, і Api знаходили спільні папки data/ та models/ незалежно від того, звідки їх запустили.
    /// </summary>
    public static string FindSolutionRoot(string startDirectory)
    {
        for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
        {
            if (dir.EnumerateFiles("*.slnx").Any() || dir.EnumerateFiles("*.sln").Any())
                return dir.FullName;
        }
        throw new DirectoryNotFoundException($"Не знайдено корінь солюшну вище за {startDirectory}");
    }
}
