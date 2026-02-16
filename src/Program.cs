using System.Reflection;
using FluentValidation;
using Listenfy.Application.Middleware;
using Listenfy.Infrastructure.Configuration;
using Listenfy.Infrastructure.Persistence;
using Listenfy.Shared;
using Listenfy.Shared.Behaviors;
using NetCord.Hosting.Services;
using Serilog;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.Refit.Destructurers;
using Serilog.Sinks.SystemConsole.Themes;

try
{
    var builder = WebApplication.CreateBuilder(args);
    var environment = builder.Environment.EnvironmentName;
    Log.Information("Starting application in {Environment} environment", environment);

    builder.Services.AddSerilog(
        (services, lc) =>
            lc
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails(
                    new DestructuringOptionsBuilder().WithDefaultDestructurers().WithDestructurers(new[] { new ApiExceptionDestructurer() })
                )
                .WriteTo.Console(
                    theme: AnsiConsoleTheme.Code,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] - {Message:lj}{NewLine}{Exception}"
                )
    );

    // load after setting up logger
    DotEnv.Load(builder.Services, environment);
    builder.Configuration.AddEnvironmentVariables().Build();

    builder.Services.SetupConfigFiles();
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    });
    builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    builder.Services.SetupControllers();

    builder.Services.RegisterServices();
    builder.Services.SetupDiscord();
    builder.Services.SetupSpotify();
    builder.Services.SetupDatabase<ApplicationDbContext>();
    builder.Services.SetupHangfire(environment);
    builder.Services.SetupCors();

    var app = builder.Build();
    app.UseCors("DefaultCors");
    app.UseSerilogRequestLogging();

    // Add Discord commands from modules
    app.AddModules(typeof(Program).Assembly);

    await app.ApplyMigrationsAsync<ApplicationDbContext>(environment);

    app.RegisterOpenApiAndScalar();
    app.UseHttpsRedirection();
    app.MapControllers();
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseHangfireDashboard();
    app.SetupRecurringJobs();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException && ex.Source != "Microsoft.EntityFrameworkCore.Design") // see https://github.com/dotnet/efcore/issues/29923
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
