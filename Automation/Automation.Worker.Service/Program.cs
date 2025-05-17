using Automation.Realtime;
using Automation.Realtime.Models;
using Automation.Shared.Packages;
using Automation.Worker.Service;
using DotNetEnv;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

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

// Package management
builder.Services.AddSingleton<IPackageManagement>(new LocalPackageManagement("/app/data/nuget"));

// Database
builder.Services.AddSingleton<IMongoDatabase>(
        (services) =>
        {
            string connectionString = Environment.GetEnvironmentVariable("MONGODB_URI") ??
                throw new ArgumentException("Missing MONGODB_URI in .env file");
            string databaseName = Environment.GetEnvironmentVariable("MONGO_INITDB_DATABASE") ??
                throw new ArgumentException("Missing MONGO_INITDB_DATABASE in .env file");

            MongoClient client = new MongoClient(connectionString);

            // Allow find request on guid
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            // Using camelCase for property names
            ConventionRegistry.Register(
                "camelCase",
                new ConventionPack { new CamelCaseElementNameConvention() },
                t => true);

            return client.GetDatabase(databaseName);
        });
#endregion

var host = builder.Build();
host.Run();
