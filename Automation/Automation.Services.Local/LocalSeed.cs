using Automation.Shared.Data.Scoped;

namespace Automation.Services.Local;

/// <summary>
/// Minimal content written to the SQLite database the first time it is created:
/// the <see cref="Scope.Root"/> holding every other element, and the built-in
/// <see cref="AutomationControl.StartTask"/>/<see cref="AutomationControl.EndTask"/> elements
/// every graph relies on.
/// </summary>
internal static class LocalSeed
{
    public static void Seed(LocalDbContext db)
    {
        db.ScopedElements.AddRange(
            Scope.Root,
            AutomationControl.StartTask,
            AutomationControl.EndTask,
            AutomationControl.ShareTask,
            AutomationControl.JoinTask);

        db.SaveChanges();
    }
}
