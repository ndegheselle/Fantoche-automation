using Fantoche.Services.Local;
using Fantoche.Shared.Remote;
using Fantoche.Supervisor.Api.Data;
using Fantoche.Supervisor.Api.Execution;

namespace Fantoche.Supervisor.Api;

/// <summary>
/// The entry point of the applications and the workers : the HTTP API and the hubs carrying the
/// calls to a <see cref="LocalSupervisor"/>, which holds the database, hands the tasks to the
/// workers, starts the scheduled ones and relays what they report. It never runs a task itself.
/// <para>
/// Configured by : DATA_FOLDER (the folder of the supervisor, see <see cref="LocalSupervisor"/>) and
/// STORAGE_FOLDER (the storage the workers share, cleaned with the history and served to the
/// applications running the tasks asking the user, left alone when unset).
/// </para>
/// </summary>
public static class SupervisorApp
{
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        string dataFolder = builder.Configuration["DATA_FOLDER"] ?? "data";
        string? storageFolder = builder.Configuration["STORAGE_FOLDER"];

        var supervisor = new LocalSupervisor(dataFolder, storageFolder);
        builder.Services.AddSingleton(supervisor);
        builder.Services.AddSingleton(supervisor.Scoped);
        builder.Services.AddSingleton(supervisor.History);
        builder.Services.AddSingleton(supervisor.Packages);
        builder.Services.AddSingleton(supervisor.Dispatcher);

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
        DispatchApi.Map(app);
        StorageApi.Map(app);

        app.MapHub<CallerHub>($"/{SupervisorRoutes.Callers}");
        app.MapHub<WorkerHub>($"/{SupervisorRoutes.Workers}");

        app.Lifetime.ApplicationStarted.Register(() => _ = supervisor.RunCleanupAsync(app.Lifetime.ApplicationStopping));
        app.Lifetime.ApplicationStarted.Register(() => _ = supervisor.Scheduler.RunAsync(app.Lifetime.ApplicationStopping));

        return app;
    }
}
