using Fantoche.Services.Local;

namespace Fantoche.Supervisor.Api.Data;

/// <summary>
/// The packages the tasks are loaded from, kept in the shared folder the workers read them from,
/// see <see cref="Shared.Services.IPackagesService"/>.
/// </summary>
public static class PackagesApi
{
    public static void Map(WebApplication app)
    {
        var packages = app.MapGroup("/packages");

        packages.MapGet("/", (LocalPackagesService service, string? search, int? page, int? pageSize)
            => service.SearchAsync(search ?? "", ScopedApi.Paging(page, pageSize)));
        packages.MapPost("/", AddAsync).DisableAntiforgery();
        packages.MapDelete("/{id}", (LocalPackagesService service, string id, Version? version) => service.RemoveAsync(id, version));
        packages.MapGet("/{id}/versions", (LocalPackagesService service, string id) => service.GetVersionsAsync(id));
        packages.MapGet("/{id}/{version}/classes", (LocalPackagesService service, string id, Version version)
            => service.GetClassesAsync(id, version));
    }

    /// <summary>
    /// The package is added from a file : the uploaded one is kept under its own name the time it
    /// takes, its extension telling a symbol package apart.
    /// </summary>
    private static async Task<IResult> AddAsync(LocalPackagesService service, IFormFile file)
    {
        string folder = Path.Combine(Path.GetTempPath(), "fantoche-uploads", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(folder, Path.GetFileName(file.FileName));
        Directory.CreateDirectory(folder);
        try
        {
            await using (FileStream stream = File.Create(path))
                await file.CopyToAsync(stream);

            return TypedResults.Ok(await service.AddAsync(path));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
