using Fantoche.Services.Local;
using Fantoche.Shared.Base;
using Fantoche.Shared.Data.Scoped;

namespace Fantoche.Supervisor.Api.Data;

/// <summary>
/// The scopes, tasks and workflows, see <see cref="Shared.Services.IScopedService"/>.
/// </summary>
public static class ScopedApi
{
    public static void Map(WebApplication app)
    {
        var elements = app.MapGroup("/elements");

        elements.MapGet("/tree", (LocalScopedService scoped) => scoped.GetTreeAsync());
        elements.MapGet("/search", (LocalScopedService scoped, string? search, int? page, int? pageSize)
            => scoped.SearchAsync(search ?? "", Paging(page, pageSize)));
        elements.MapGet("/search-tree", (LocalScopedService scoped, string? search) => scoped.SearchTreeAsync(search ?? ""));
        // A graph may point at many tasks : the ids go in the body rather than the url.
        elements.MapPost("/query", (LocalScopedService scoped, List<Guid> ids) => scoped.GetAsync(ids));

        elements.MapGet("/{id:guid}", async (LocalScopedService scoped, Guid id) =>
        {
            ScopedElement? element = await scoped.GetAsync(id);
            return element == null ? Results.NotFound() : TypedResults.Ok(element);
        });
        elements.MapGet("/{id:guid}/versions/{version:int}", async (LocalScopedService scoped, Guid id, int version) =>
        {
            AutomationWorkflow? workflow = await scoped.GetVersionAsync(id, version);
            // Written as the base type the client reads, with the name of its concrete type.
            return workflow == null ? Results.NotFound() : TypedResults.Ok<ScopedElement>(workflow);
        });
        elements.MapPost("/", (LocalScopedService scoped, ScopedElement element) => scoped.CreateAsync(element));
        elements.MapPut("/{id:guid}", (LocalScopedService scoped, Guid id, ScopedElement element) =>
        {
            element.Id = id;
            return scoped.EditAsync(element);
        });
        elements.MapDelete("/{id:guid}", async (LocalScopedService scoped, Guid id) =>
        {
            ScopedElement element = await scoped.GetAsync(id) ?? throw new KeyNotFoundException();
            return await scoped.RemoveAsync(element);
        });

        elements.MapGet("/{id:guid}/children", (LocalScopedService scoped, Guid id) => scoped.GetChildrensAsync(id));
        elements.MapGet("/{id:guid}/graph-task-ids", (LocalScopedService scoped, Guid id) => scoped.GetGraphTaskIdsAsync(id));
        elements.MapGet("/{id:guid}/usages", (LocalScopedService scoped, Guid id) => scoped.GetUsagesAsync(id));
        elements.MapGet("/{id:guid}/context", (LocalScopedService scoped, Guid id) => scoped.GetContextAsync(id));
        elements.MapGet("/{id:guid}/name-unique", (LocalScopedService scoped, Guid id, string name, Guid? excludeId)
            => scoped.IsNameUniqueAsync(id, name, excludeId));
    }

    public static PaginationOptions Paging(int? page, int? pageSize)
    {
        PaginationOptions options = new();
        if (page != null)
            options.Page = page.Value;
        if (pageSize != null)
            options.PageSize = pageSize.Value;
        return options;
    }
}
