using Automation.Dal;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Worker.Service;
using DotNetEnv;

Env.Load("../.env");

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddSingleton<WorkerInstance>((services) => new WorkerInstance()
{
    Id = builder.Configuration["WORKER_ID"] ?? Guid.NewGuid().ToString(),
});
builder.Services.AddHostedService<Lifecycle>();
builder.Services.AddHostedService<Worker>();

#region Services
// Realtime com between supervisor and workers
string realtimeConnectionString = Environment.GetEnvironmentVariable("REDIS_URI") ??
    throw new ArgumentException("Missing REDIS_URI in .env file");
builder.Services.AddSingleton<RedisConnectionManager>(new RedisConnectionManager(realtimeConnectionString));
builder.Services.AddSingleton<RealtimeClients>((services) => new RealtimeClients(services.GetRequiredService<RedisConnectionManager>()));

// Package management
builder.Services.AddSingleton<Automation.Worker.Packages.IPackageManagement>(new Automation.Worker.Packages.LocalPackageManagement("/app/data/nuget"));

// DatabaseConnection
builder.Services.AddSingleton<DatabaseConnection>(
        (services) =>
        {
            string connectionString = Environment.GetEnvironmentVariable("MONGODB_URI") ??
                throw new ArgumentException("Missing MONGODB_URI in .env file");
            string databaseName = Environment.GetEnvironmentVariable("MONGO_INITDB_DATABASE") ??
                throw new ArgumentException("Missing MONGO_INITDB_DATABASE in .env file");

            return new DatabaseConnection(connectionString, databaseName);
        });
#endregion

var host = builder.Build();
host.Run();
