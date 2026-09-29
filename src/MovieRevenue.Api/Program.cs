using System.Reflection;
using Microsoft.Extensions.ML;
using MovieRevenue.Api;
using MovieRevenue.Core;

const string RevenueModelName = "Revenue";
const string RatingModelName = "Rating";

var builder = WebApplication.CreateBuilder(args);

var modelsDir = builder.Configuration["ModelsDirectory"]
    ?? Path.Combine(ModelFiles.FindSolutionRoot(builder.Environment.ContentRootPath), "models");

var revenueModelPath = Path.Combine(modelsDir, ModelFiles.RevenueModel);
var ratingModelPath = Path.Combine(modelsDir, ModelFiles.RatingModel);
if (!File.Exists(revenueModelPath) || !File.Exists(ratingModelPath))
    throw new FileNotFoundException($"Моделі не знайдено в {modelsDir}. Спочатку запустіть MovieRevenue.Trainer.");

// PredictionEngine is not thread-safe, hence the pool. watchForChanges reloads a retrained model.
builder.Services.AddPredictionEnginePool<ModelInput, ScorePrediction>()
    .FromFile(modelName: RevenueModelName, filePath: revenueModelPath, watchForChanges: true)
    .FromFile(modelName: RatingModelName, filePath: ratingModelPath, watchForChanges: true);

builder.Services.AddSingleton(new ModelInfoCache(modelsDir));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Movie Revenue Prediction API", Version = "v1",
        Description = "ЛР1, варіант 6: прогноз доходу та рейтингу фільму (ML.NET, датасет TMDB 5000)." });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
    options.SchemaFilter<MovieRequestExampleFilter>();
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

var api = app.MapGroup("/api");

api.MapPost("/predict/revenue", (MovieRequest request, PredictionEnginePool<ModelInput, ScorePrediction> pool, ModelInfoCache infos) =>
{
    if (Validate(request) is { } error)
        return Results.ValidationProblem(error);

    var ratingSource = "input";
    var voteAverage = request.VoteAverage;
    if (voteAverage is null)
    {
        voteAverage = PredictRating(pool, request);
        ratingSource = "rating-model";
    }

    var input = FeatureBuilder.Build(ToRecord(request, voteAverage.Value));
    var logRevenue = pool.Predict(RevenueModelName, input).Score;
    var revenue = FeatureBuilder.Expm1(logRevenue);

    return Results.Ok(new RevenueResponse(
        request.Title,
        PredictedRevenueUsd: Math.Round(revenue),
        PredictedProfitUsd: Math.Round(revenue - request.Budget),
        ReturnOnInvestment: Math.Round(revenue / request.Budget, 2),
        VoteAverageUsed: Math.Round(voteAverage.Value, 2),
        VoteAverageSource: ratingSource,
        Model: infos.Get(ModelFiles.RevenueInfo).BestTrainer));
})
.WithSummary("Прогноз касових зборів фільму")
.WithDescription("Повертає прогноз доходу, прибуток (дохід − бюджет) та ROI (дохід / бюджет). " +
                 "Якщо voteAverage не задано, спершу рахується прогноз моделі рейтингу.")
.Produces<RevenueResponse>()
.ProducesValidationProblem();

api.MapPost("/predict/rating", (MovieRequest request, PredictionEnginePool<ModelInput, ScorePrediction> pool, ModelInfoCache infos) =>
{
    if (Validate(request) is { } error)
        return Results.ValidationProblem(error);

    return Results.Ok(new RatingResponse(
        request.Title,
        Math.Round(PredictRating(pool, request), 2),
        infos.Get(ModelFiles.RatingInfo).BestTrainer));
})
.WithSummary("Прогноз середньої оцінки фільму (0–10)")
.WithDescription("Ознаки: бюджет, тривалість, дата релізу, жанри, мова та популярність. Поля voteAverage і voteCount ігноруються. " +
                 "Увага: популярність TMDB накопичується після релізу і є найсильнішою ознакою моделі, " +
                 "тому для ще не випущеного фільму її доводиться оцінювати самостійно (для блокбастера ≈ 50–200, медіана ≈ 13).")
.Produces<RatingResponse>()
.ProducesValidationProblem();

api.MapGet("/model/info", (ModelInfoCache infos) => Results.Ok(new
{
    Revenue = infos.Get(ModelFiles.RevenueInfo),
    Rating = infos.Get(ModelFiles.RatingInfo),
}))
.WithSummary("Інформація про завантажені моделі")
.WithDescription("Обраний тренер, порівняння всіх тренерів (CV та test метрики) і важливість ознак.");

api.MapGet("/genres", () => MovieGenres.All).WithSummary("Список допустимих жанрів");

app.Run();

double PredictRating(PredictionEnginePool<ModelInput, ScorePrediction> pool, MovieRequest request)
{
    var input = FeatureBuilder.Build(ToRecord(request, voteAverage: 0));
    return Math.Clamp(pool.Predict(RatingModelName, input).Score, 0, 10);
}

static MovieRecord ToRecord(MovieRequest r, double voteAverage) => new()
{
    Title = r.Title ?? "",
    Budget = r.Budget,
    Runtime = r.Runtime,
    ReleaseDate = r.ReleaseDate!.Value.ToDateTime(TimeOnly.MinValue),
    Genres = r.Genres!,
    OriginalLanguage = r.OriginalLanguage,
    Popularity = r.Popularity,
    VoteAverage = voteAverage,
    VoteCount = r.VoteCount,
};

static Dictionary<string, string[]>? Validate(MovieRequest r)
{
    var errors = new Dictionary<string, string[]>();
    if (r.Budget <= 0) errors["budget"] = ["Бюджет має бути більшим за 0."];
    if (r.Runtime <= 0) errors["runtime"] = ["Тривалість має бути більшою за 0."];
    if (r.Popularity < 0) errors["popularity"] = ["Популярність не може бути від'ємною."];
    if (r.VoteCount < 0) errors["voteCount"] = ["Кількість голосів не може бути від'ємною."];
    if (r.VoteAverage is < 0 or > 10) errors["voteAverage"] = ["Оцінка має бути в межах 0–10."];
    if (r.ReleaseDate is null) errors["releaseDate"] = ["Вкажіть дату релізу (yyyy-MM-dd)."];
    else if (r.ReleaseDate.Value.Year is < 1900 or > 2100) errors["releaseDate"] = ["Рік релізу має бути в межах 1900–2100."];

    if (r.Genres is not { Count: > 0 }) errors["genres"] = ["Вкажіть хоча б один жанр."];
    else if (r.Genres.Where(g => !MovieGenres.IsKnown(g)).ToArray() is { Length: > 0 } unknown)
        errors["genres"] = [$"Невідомі жанри: {string.Join(", ", unknown)}. Див. GET /api/genres."];
    return errors.Count > 0 ? errors : null;
}
