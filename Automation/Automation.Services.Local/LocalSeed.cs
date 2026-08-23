using Automation.Shared.Data.Scoped;

namespace Automation.Services.Local;

/// <summary>
/// Minimal content written to the SQLite database the first time it is created:
/// the built-in <see cref="AutomationControl.StartTask"/>/<see cref="AutomationControl.EndTask"/>
/// elements every graph relies on, plus a small demo hierarchy (and some finished history
/// against it) so the app isn't empty on first launch.
/// </summary>
internal static class LocalSeed
{
    public static void Seed(LocalDbContext db)
    {
        db.ScopedElements.AddRange(
            AutomationControl.StartTask,
            AutomationControl.EndTask,
            AutomationControl.ContextTask);

        db.SaveChanges();
    }
}
