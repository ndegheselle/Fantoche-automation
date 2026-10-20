using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Fantoche.Worker.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Fantoche.App.Features.Storage
{
    /// <summary>
    /// What a workflow or a task takes in the storage, its runs together.
    /// </summary>
    /// <param name="Metadata">Null when the storage holds something that isn't, or no longer is, a stored task.</param>
    /// <param name="Share">Its part of what the storage takes, from 0 to 1.</param>
    public record StoredTaskItem(string Name, ScopedMetadata? Metadata, int Runs, string Size, double Share);

    public partial class StorageViewModel : ObservableObject
    {
        [ObservableProperty] private IReadOnlyList<StoredTaskItem> _tasks = [];
        [ObservableProperty] private string _used = "";
        [ObservableProperty] private string _free = "";
        [ObservableProperty] private string _diskSize = "";

        /// <summary>The part of the disk taken, by the storage and everything else, from 0 to 1.</summary>
        [ObservableProperty] private double _diskShare;

        /// <summary>The storage as this application reaches it : the shared folder of the workers outside of the standalone mode.</summary>
        public string Folder => _storage.Path;

        private readonly StorageFolder _storage;
        private readonly IScopedService _scoped;

        public StorageViewModel(StorageFolder storage, IScopedService scoped)
        {
            _storage = storage;
            _scoped = scoped;
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            // Walking a share holding many runs takes a while.
            IReadOnlyList<TaskStorageUsage> usages = await Task.Run(_storage.MeasureUsage);
            DiskSpace? disk = await Task.Run(() => DiskSpace.Of(_storage.Path));

            List<Guid> taskIds = [.. usages.Where(x => x.TaskId != null).Select(x => x.TaskId!.Value)];
            List<ScopedElement> elements = await _scoped.GetAsync(taskIds);
            Dictionary<Guid, ScopedMetadata> metadataById = elements.ToDictionary(x => x.Id, x => x.Metadata);

            long usedBytes = usages.Sum(x => x.Bytes);
            Tasks = [.. usages.Select(x => ItemOf(x, metadataById, usedBytes))];
            Used = FormatSize(usedBytes);
            Free = disk == null ? "Unknown" : FormatSize(disk.FreeBytes);
            DiskSize = disk == null ? "Unknown" : FormatSize(disk.TotalBytes);
            DiskShare = disk == null || disk.TotalBytes == 0 ? 0 : 1 - (double)disk.FreeBytes / disk.TotalBytes;
        }

        private static StoredTaskItem ItemOf(TaskStorageUsage usage, Dictionary<Guid, ScopedMetadata> metadataById, long usedBytes)
        {
            ScopedMetadata? metadata = usage.TaskId == null ? null : metadataById.GetValueOrDefault(usage.TaskId.Value);
            string name = metadata?.Name ?? (usage.TaskId == null ? usage.Folder : $"Removed task ({usage.Folder})");
            double share = usedBytes == 0 ? 0 : (double)usage.Bytes / usedBytes;
            return new StoredTaskItem(name, metadata, usage.Runs, FormatSize(usage.Bytes), share);
        }

        private static string FormatSize(long bytes)
        {
            string[] units = ["B", "KB", "MB", "GB", "TB"];
            double size = bytes;
            int unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return unit == 0 ? $"{bytes} B" : $"{size:0.#} {units[unit]}";
        }
    }
}
