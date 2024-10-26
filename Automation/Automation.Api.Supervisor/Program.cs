using Automation.Realtime;
using Automation.Server.Shared.Packages;
using DotNetEnv;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

Env.Load("../.env");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

// XXX
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
