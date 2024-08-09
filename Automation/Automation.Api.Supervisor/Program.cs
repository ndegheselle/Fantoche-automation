using DotNetEnv;
using MongoDB.Driver;

Env.Load("../.env");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region Services

builder.Services.AddSingleton((services) =>
{
    string? connectionString = Environment.GetEnvironmentVariable("MONGODB_URI") ??
        throw new ArgumentException("Missing MONGODB_URI in .env file");
    string? databaseName = Environment.GetEnvironmentVariable("MONGO_INITDB_DATABASE") ??
        throw new ArgumentException("Missing MONGO_INITDB_DATABASE in .env file");

    MongoClient client = new MongoClient(connectionString);
    return client.GetDatabase(databaseName);
});

#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
