using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;

namespace Fantoche.App.Common
{
    /// <summary>The task or workflow an id points at, as a page lists it.</summary>
    public record TaskName(string Name, EnumScopedType Type);

    public static class TaskNames
    {
        /// <summary>
        /// The names of [taskIds], read in one go. An execution can outlive its task : a removed one
        /// is still listed, under its id.
        /// </summary>
        public static async Task<Dictionary<Guid, TaskName>> ResolveAsync(IScopedService scoped, IEnumerable<Guid> taskIds)
        {
            List<Guid> ids = [.. taskIds.Distinct()];
            List<ScopedElement> elements = await scoped.GetAsync(ids);
            Dictionary<Guid, ScopedElement> found = elements.ToDictionary(x => x.Id);

            return ids.ToDictionary(
                id => id,
                id => found.TryGetValue(id, out var element)
                    ? new TaskName(element.Metadata.Name, element.Metadata.Type)
                    : new TaskName($"Removed ({id:N})", EnumScopedType.Task));
        }
    }
}
