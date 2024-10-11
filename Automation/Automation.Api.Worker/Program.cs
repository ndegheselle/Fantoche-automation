using Automation.Api.Shared;
using Automation.Api.Worker;
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
builder.Services.AddSingleton<IPackageManagement>(new LocalPackageManagement("/app/data/nuget"));
builder.Services
    .AddSingleton<IMongoDatabase>(
        (services) =>
        {
            string? connectionString = Environment.GetEnvironmentVariable("MONGODB_URI") ??
                throw new ArgumentException("Missing MONGODB_URI in .env file");
            string? databaseName = Environment.GetEnvironmentVariable("MONGO_INITDB_DATABASE") ??
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

Initialize.RegisterWorker(app.Services);
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
