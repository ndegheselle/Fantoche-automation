using Fantoche.App.Common;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Fantoche.Worker;
using Fantoche.Worker.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Fantoche.App.Features.Storage
{
    /// <summary>The storage a task or workflow keeps, see <see cref="StorageUsage"/>.</summary>
    public record StorageRow(string Name, EnumScopedType Type, int Executions, long Bytes)
    {
        public string Size => StorageViewModel.FormatSize(Bytes);
    }

    /// <summary>
    /// The storage of the worker of the application : each worker only reaches its own (see
    /// architecture.md, "Storage and retention").
    /// </summary>
    public partial class StorageViewModel : ObservableObject
    {
        [ObservableProperty] private List<StorageRow> _rows = [];
        [ObservableProperty] private string _total = "";

        private readonly LocalWorker _worker;
        private readonly IScopedService _scoped;

        public StorageViewModel(LocalWorker worker, IScopedService scoped)
        {
            _worker = worker;
            _scoped = scoped;
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            // Walks every file kept, off the interface.
            List<StorageUsage> usages = await Task.Run(_worker.GetStorageUsage);
            var names = await TaskNames.ResolveAsync(_scoped, usages.Select(x => x.TaskId));

            Rows = [.. usages
                .Select(x => new StorageRow(names[x.TaskId].Name, names[x.TaskId].Type, x.Executions, x.Bytes))
                .OrderByDescending(x => x.Bytes)];
            long totalBytes = usages.Sum(x => x.Bytes);
            Total = FormatSize(totalBytes);
        }

        public static string FormatSize(long bytes)
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
