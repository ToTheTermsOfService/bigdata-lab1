using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MovieRevenue.Api;

public sealed class MovieRequestExampleFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(MovieRequest) || schema is not OpenApiSchema concrete)
            return;

        concrete.Example = new JsonObject
        {
            ["title"] = "Dune: Part Three",
            ["budget"] = 190_000_000,
            ["runtime"] = 155,
            ["releaseDate"] = "2026-12-18",
            ["genres"] = new JsonArray("Science Fiction", "Adventure"),
            ["originalLanguage"] = "en",
            ["popularity"] = 80,
            ["voteAverage"] = null,
            ["voteCount"] = 4000,
        };
    }
}
