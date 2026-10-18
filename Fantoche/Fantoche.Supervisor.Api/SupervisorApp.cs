using Fantoche.Shared.Remote;

namespace Fantoche.Supervisor.Api;

/// <summary>
/// The bridge between the callers and the workers : the callers send their tasks, the supervisor
/// hands them to the workers and relays what they report back. It stores nothing.
/// </summary>
public static class SupervisorApp
{
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<Dispatcher>();
        builder.Services.AddSignalR(options =>
        {
            // A workflow travels with its graph and every task it points at.
            options.MaximumReceiveMessageSize = 16 * 1024 * 1024;
        });

        var app = builder.Build();

        app.MapHub<CallerHub>($"/{SupervisorRoutes.Callers}");
        app.MapHub<WorkerHub>($"/{SupervisorRoutes.Workers}");
        app.MapGet("/workers", (Dispatcher dispatcher) => dispatcher.GetWorkers());

        return app;
    }
}
