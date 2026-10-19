using Fantoche.Services.Local;
using Fantoche.Services.Local.Database;
using Fantoche.Shared.Remote;
using Fantoche.Supervisor.Api.Data;
using Fantoche.Supervisor.Api.Execution;

namespace Fantoche.Supervisor.Api;

/// <summary>
/// The entry point of the applications and the workers : it holds the database, hands the tasks to
/// the workers and relays what they report. It never runs a task itself.
/// <para>
/// Configured by : DATA_FOLDER (its database and the cache of the packages) and SHARED_FOLDER
/// (the "nuget" packages and the "storage", shared with the workers).
/// </para>
/// </summary>
public static class SupervisorApp
{
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        string dataFolder = builder.Configuration["DATA_FOLDER"] ?? "data";
        string sharedFolder = builder.Configuration["SHARED_FOLDER"] ?? Path.Combine(dataFolder, "shared");

        var database = new DatabaseFactory(Path.Combine(dataFolder, "fantoche.db"));
        builder.Services.AddSingleton(new LocalScopedService(database));
        builder.Services.AddSingleton(new LocalHistoryService(database));
        builder.Services.AddSingleton(new LocalPackagesService(Path.Combine(sharedFolder, "nuget"), Path.Combine(dataFolder, "cache")));
        builder.Services.AddSingleton<Dispatcher>();

        builder.Services.AddExceptionHandler<ErrorHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.ConfigureHttpJsonOptions(options => FantocheJson.Configure(options.SerializerOptions));
        builder.Services
            .AddSignalR(options =>
            {
                // A workflow travels with its graph and every task it points at.
                options.MaximumReceiveMessageSize = 16 * 1024 * 1024;
            })
            .AddJsonProtocol(options => FantocheJson.Configure(options.PayloadSerializerOptions));

        var app = builder.Build();
        app.UseExceptionHandler();

        ScopedApi.Map(app);
        HistoryApi.Map(app);
        PackagesApi.Map(app);

        app.MapHub<CallerHub>($"/{SupervisorRoutes.Callers}");
        app.MapHub<WorkerHub>($"/{SupervisorRoutes.Workers}");
        app.MapGet("/workers", (Dispatcher dispatcher) => dispatcher.GetWorkers());

        return app;
    }
}
