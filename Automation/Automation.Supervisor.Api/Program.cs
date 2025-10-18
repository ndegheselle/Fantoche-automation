using Automation.Dal;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Supervisor.Api.Business;
using Automation.Supervisor.Api.Hubs;
using Automation.Worker.Control;
using DotNetEnv;

Env.Load("../.env");

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddSignalR();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();


builder.Services.AddHostedService<WorkerCleaner>();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

// Resolve DatabaseSeeder and call Seed method
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var connection = services.GetRequiredService<DatabaseConnection>();
    DatabaseSeeder databaseSeeder = new DatabaseSeeder(connection);
    await databaseSeeder.Seed(ControlTasks.ControlScope);
}

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<TaskProgressHub>("/tasks/progress");

app.Run();
