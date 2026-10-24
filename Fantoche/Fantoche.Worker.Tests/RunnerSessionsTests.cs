namespace Fantoche.Worker.Tests;

/// <summary>
/// How a process tells whether a run another one started is still running : several copies of the
/// application share the same runs.
/// </summary>
[TestFixture]
internal sealed class RunnerSessionsTests
{
    private string _folder = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    [Test]
    public void RunningSession_IsAlive()
    {
        using RunnerSessions running = new(_folder);
        using RunnerSessions other = new(_folder);

        Assert.That(other.IsAlive(running.CurrentId), Is.True);
    }

    [Test]
    public void SessionLeftByACrash_IsNotAlive()
    {
        using RunnerSessions sessions = new(_folder);
        // What a process dying leaves : its lock file, held by nobody.
        File.WriteAllText(Path.Combine(_folder, "crashed.lock"), "");

        Assert.That(sessions.IsAlive("crashed"), Is.False);
    }

    [Test]
    public void StoppedSession_IsNotAlive()
    {
        using RunnerSessions sessions = new(_folder);
        RunnerSessions stopped = new(_folder);
        stopped.Dispose();

        Assert.That(sessions.IsAlive(stopped.CurrentId), Is.False);
    }
}
