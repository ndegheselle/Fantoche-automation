using Fantoche.Services.Local.Models;
using Fantoche.Shared.Data.Scoped;
using Dapper;

namespace Fantoche.Services.Local.Database;

/// <summary>
/// Minimal content written to the SQLite database the first time it is created: the
/// <see cref="Scope.Root"/> holding every other element and the built-in controls every graph relies on
/// (<see cref="AutomationControl.All"/>), the form generated from the parameters of a task among them
/// (<see cref="AutomationInterface.Form"/>). The samples come with the packages they run, see
/// <c>ExampleSamples</c>.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Write the starting content, unless the tree already holds something : the seed runs once, on
    /// a database that has never been written to, so whatever was removed since stays removed.
    /// </summary>
    public static void Seed(DatabaseFactory factory)
    {
        using var connection = factory.Create();

        if (connection.ExecuteScalar<bool>("SELECT EXISTS(SELECT 1 FROM Scoped);"))
        {
            AddMissingControls(connection);
            return;
        }

        // The elements are written in the order they hang under each other : a parent is there
        // before its children.
        List<ScopedElement> elements =
        [
            Scope.Root,
            Scope.Controls,
            .. AutomationControl.All,
            AutomationInterface.Form,
        ];

        using var transaction = connection.BeginTransaction();

        connection.Execute(
            ScopedModel.InsertQuery,
            elements.Select(ScopedModel.From).ToList(),
            transaction);

        transaction.Commit();
    }

    /// <summary>
    /// The built-in elements a database written by an older version lacks : the controls are
    /// hard coded, and every graph and task relies on them being in the tree.
    /// </summary>
    private static void AddMissingControls(System.Data.IDbConnection connection)
    {
        if (connection.ExecuteScalar<bool>("SELECT EXISTS(SELECT 1 FROM Scoped WHERE Id = @Id);", new { Id = AutomationInterface.Form.Id }))
            return;
        connection.Execute(ScopedModel.InsertQuery, ScopedModel.From(AutomationInterface.Form));
    }
}
