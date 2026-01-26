using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

namespace Listenfy.Infrastructure.Configuration;

internal static class StartupConfiguration
{
    public static void AddOpenApiAndScalar(this IServiceCollection services)
    {
        // This dictionary will track how many times we've generated an ID for a given type.
        var processedTypeCounts = new ConcurrentDictionary<Type, int>();

        services.AddOpenApi(options =>
        {
            // THE HACK:
            // Force unique schema IDs for types that the generator buggily processes multiple times.
            // THE BUG: https://github.com/dotnet/aspnetcore/issues/58213
            options.CreateSchemaReferenceId = (context) =>
            {
                var type = context.Type;
                if (type.FullName is null)
                {
                    return OpenApiOptions.CreateDefaultSchemaReferenceId(context);
                }

                // Increment the count for this type. The new value is returned.
                var count = processedTypeCounts.AddOrUpdate(type, 1, (key, currentCount) => currentCount + 1);

                // Generate the base ID from the full name.
                var baseId = type.FullName.Replace("+", ".");

                // If this is the first time we're seeing this type, use its normal ID.
                // If it's a subsequent time, append a version suffix to make the key unique.
                return count == 1 ? baseId : $"{baseId}.v{count}";
            };
        });
    }

    public static void RegisterOpenApiAndScalar(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.DarkMode = false;
            options.HideModels = true;
            options.WithTitle("Listenfy Api");
        });
    }

    public static void SetupControllers(this IServiceCollection services)
    {
        // Add ASP.NET Core services for OAuth callback endpoint
        services.AddControllers();
        services.AddOpenApiAndScalar();
        services.AddRouting(options => options.LowercaseUrls = true);
        services.Configure<JsonOptions>(jsonOptions =>
        {
            jsonOptions.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            jsonOptions.JsonSerializerOptions.WriteIndented = false;
            jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            jsonOptions.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
            jsonOptions.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });
    }
}
