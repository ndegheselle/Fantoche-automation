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

string? connectionString = Environment.GetEnvironmentVariable("MONGODB_URI") ??
    throw new ArgumentException("Missing MONGODB_URI in .env file");
builder.Services.AddSingleton((services) => new MongoClient(connectionString));

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
