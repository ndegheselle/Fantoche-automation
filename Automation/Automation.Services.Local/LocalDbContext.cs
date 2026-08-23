using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Automation.Shared.Data;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json.Linq;

namespace Automation.Services.Local;

public class LocalDbContext : DbContext
{
    private readonly string _connectionString;

    public DbSet<TaskInstance> TaskInstances => Set<TaskInstance>();
    public DbSet<ScopedElement> ScopedElements => Set<ScopedElement>();

    public LocalDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(_connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureTaskInstances(modelBuilder);
        ConfigureScopedElements(modelBuilder);
    }

    private static void ConfigureTaskInstances(ModelBuilder modelBuilder)
    {
        var task = modelBuilder.Entity<TaskInstance>();

        task.HasKey(x => x.Id);

        // Execution-only graph/runtime references: never persisted.
        task.Ignore(x => x.Previous);
        task.Ignore(x => x.Nexts);
        task.Ignore(x => x.Node);
        task.Ignore(x => x.ParentWorkflow);
        task.Ignore(x => x.Context);

        // TaskInstance.State's setter stamps FinishedAt as a side effect of the *transition*
        // out of a non-finished state. Materializing a row from the database isn't a real
        // transition, so read/write through the backing field to avoid clobbering the
        // FinishedAt value that was actually stored.
        task.Property(x => x.State)
            .HasField("_state")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        task.Property(x => x.Parameters).HasConversion(JTokenConverter).Metadata.SetValueComparer(JTokenComparer);
        task.Property(x => x.Output).HasConversion(JTokenConverter).Metadata.SetValueComparer(JTokenComparer);

        task.HasIndex(x => x.TaskId);
        task.HasIndex(x => x.CreatedAt);
    }

    private static void ConfigureScopedElements(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScopedElement>(element =>
        {
            element.HasKey(x => x.Id);
            element.HasDiscriminator<string>("ElementKind")
                .HasValue<Scope>("scope")
                .HasValue<AutomationTask>("task")
                .HasValue<AutomationControl>("control")
                .HasValue<AutomationWorkflow>("workflow");

            element.OwnsOne(x => x.Metadata, metadata =>
            {
                metadata.Property(x => x.Tags)
                    .HasConversion(TagsConverter)
                    .Metadata.SetValueComparer(TagsComparer);
            });
        });

        modelBuilder.Entity<BaseAutomationTask>(task =>
        {
            // Computed from *SchemaJson below; not independently storable.
            task.Ignore(x => x.InputSchema);
            task.Ignore(x => x.OutputSchema);

            task.OwnsOne(x => x.Settings);

            task.Property(x => x.Schedules)
                .HasConversion(SchedulesConverter)
                .Metadata.SetValueComparer(SchedulesComparer);
        });

        modelBuilder.Entity<AutomationTask>(task =>
        {
            task.Property(x => x.Target)
                .HasConversion(TargetConverter)
                .Metadata.SetValueComparer(CreateJsonValueComparer<PackageClassTarget>());
        });

        modelBuilder.Entity<AutomationControl>(control =>
        {
            control.Ignore(x => x.Type);
        });

        modelBuilder.Entity<AutomationWorkflow>(workflow =>
        {
            workflow.Property(x => x.Graph)
                .HasConversion(GraphConverter)
                .Metadata.SetValueComparer(CreateJsonValueComparer<TasksGraph>());
            workflow.OwnsOne(x => x.WorkflowSettings);
        });
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new VersionJsonConverter() },
    };

    private static readonly ValueConverter<JToken?, string?> JTokenConverter = new(
        v => v == null ? null : v.ToString(Newtonsoft.Json.Formatting.None),
        v => v == null ? null : JToken.Parse(v));

    // JToken is a mutable reference type that can be edited in place (e.g. instance.Parameters["x"] = ...);
    // without a comparer with a cloning snapshot, EF's change tracker would compare the live object against
    // itself and never see a change.
    private static readonly ValueComparer<JToken?> JTokenComparer = new(
        (a, b) => JToken.DeepEquals(a, b),
        v => v == null ? 0 : v.ToString(Newtonsoft.Json.Formatting.None).GetHashCode(),
        v => v == null ? null : v.DeepClone());

    private static readonly ValueConverter<ObservableCollection<string>, string> TagsConverter = new(
        v => JsonSerializer.Serialize(v, JsonOptions),
        v => JsonSerializer.Deserialize<ObservableCollection<string>>(v, JsonOptions) ?? new ObservableCollection<string>());

    private static readonly ValueComparer<ObservableCollection<string>> TagsComparer = new(
        (a, b) => (a ?? new ObservableCollection<string>()).SequenceEqual(b ?? new ObservableCollection<string>()),
        v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
        v => new ObservableCollection<string>(v));

    private static readonly ValueConverter<IEnumerable<Schedule>, string> SchedulesConverter = new(
        v => JsonSerializer.Serialize(v, JsonOptions),
        v => JsonSerializer.Deserialize<List<Schedule>>(v, JsonOptions) ?? new List<Schedule>());

    private static readonly ValueComparer<IEnumerable<Schedule>> SchedulesComparer = new(
        (a, b) => SchedulesEqual(a, b),
        v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.CronExpression, item.JsonSettings)),
        v => CloneSchedules(v));

    private static bool SchedulesEqual(IEnumerable<Schedule>? a, IEnumerable<Schedule>? b)
    {
        return (a ?? Enumerable.Empty<Schedule>())
            .Select(x => new { x.CronExpression, x.JsonSettings })
            .SequenceEqual((b ?? Enumerable.Empty<Schedule>()).Select(x => new { x.CronExpression, x.JsonSettings }));
    }

    private static List<Schedule> CloneSchedules(IEnumerable<Schedule> schedules)
    {
        return schedules.Select(s => new Schedule { CronExpression = s.CronExpression, JsonSettings = s.JsonSettings }).ToList();
    }

    private static readonly ValueConverter<PackageClassTarget?, string?> TargetConverter = new(
        v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
        v => v == null ? null : JsonSerializer.Deserialize<PackageClassTarget>(v, JsonOptions));

    private static readonly ValueConverter<TasksGraph, string> GraphConverter = new(
        v => JsonSerializer.Serialize(v, JsonOptions),
        v => JsonSerializer.Deserialize<TasksGraph>(v, JsonOptions) ?? new TasksGraph());

    // Reference types stored as an opaque JSON column can be mutated in place by application code
    // (e.g. adding a node to a workflow's graph), so the comparer's snapshot must deep-clone via a
    // JSON round-trip rather than keep the live reference, or in-place edits would go undetected.
    private static ValueComparer<T?> CreateJsonValueComparer<T>() where T : class
    {
        return new ValueComparer<T?>(
            (a, b) => JsonValueEquals(a, b),
            v => v == null ? 0 : JsonSerializer.Serialize(v, JsonOptions).GetHashCode(),
            v => v == null ? null : JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, JsonOptions), JsonOptions));
    }

    private static bool JsonValueEquals<T>(T? a, T? b) where T : class
    {
        if (a == null || b == null)
            return a == null && b == null;
        return JsonSerializer.Serialize(a, JsonOptions) == JsonSerializer.Serialize(b, JsonOptions);
    }

    private sealed class VersionJsonConverter : JsonConverter<Version>
    {
        public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Version.Parse(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }
}
