using Automation.Realtime;
using Automation.Worker.Service;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using Automation.Api.Shared;
using DotNetEnv;

Env.Load("../.env");

var builder = Host.CreateApplicationBuilder(args);
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
#pragma warning disable 618
            BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
#pragma warning restore

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
