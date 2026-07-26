using System.Collections.ObjectModel;
using System.Windows;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.History
{
    /// <summary>
    /// Observable wrapper around a <see cref="TaskInstance"/> so the grid reflects state changes
    /// pushed by <see cref="IHistoryService.InstanceUpdated"/> in real time.
    /// </summary>
    public class HistoryRowViewModel : ObservableObject
    {
        public Guid Id { get; }

        private string _name = string.Empty;
        public string Name { get => _name; private set => SetProperty(ref _name, value); }

        private EnumTaskState _state;
        public EnumTaskState State { get => _state; private set => SetProperty(ref _state, value); }

        private DateTime _createdAt;
        public DateTime CreatedAt { get => _createdAt; private set => SetProperty(ref _createdAt, value); }

        private string _duration = string.Empty;
        public string Duration { get => _duration; private set => SetProperty(ref _duration, value); }

        public HistoryRowViewModel(TaskInstance instance)
        {
            Id = instance.Id;
            Update(instance);
        }

        public void Update(TaskInstance instance)
        {
            Name = string.IsNullOrEmpty(instance.NodeName) ? "(unnamed)" : instance.NodeName;
            State = instance.State;
            CreatedAt = instance.CreatedAt.ToLocalTime();

            if ((instance.State & EnumTaskState.Finished) != 0 && instance.FinishedAt is { } finished)
            {
                var elapsed = finished - instance.CreatedAt;
                Duration = elapsed.TotalSeconds < 60
                    ? $"{elapsed.TotalSeconds:0.#} s"
                    : $"{elapsed.TotalMinutes:0.#} min";
            }
            else
            {
                Duration = "—";
            }
        }
    }

    public class HistoryViewModel : ObservableObject
    {
        private readonly IHistoryService _history;

        public ObservableCollection<HistoryRowViewModel> Instances { get; } = [];

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }

        public IAsyncRelayCommand RefreshCommand { get; }

        public HistoryViewModel(IHistoryService history)
        {
            _history = history;
            RefreshCommand = new AsyncRelayCommand(LoadAsync);

            _history.InstanceAdded += OnInstanceAdded;
            _history.InstanceUpdated += OnInstanceUpdated;

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var page = await _history.SearchAsync(new PaginationOptions { Page = 1, PageSize = 50 });
                Instances.Clear();
                foreach (var instance in page.Items)
                    Instances.Add(new HistoryRowViewModel(instance));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnInstanceAdded(TaskInstance instance)
        {
            Dispatch(() =>
            {
                if (Instances.Any(x => x.Id == instance.Id))
                    return;
                // Newest first.
                Instances.Insert(0, new HistoryRowViewModel(instance));
            });
        }

        private void OnInstanceUpdated(TaskInstance instance)
        {
            Dispatch(() =>
            {
                var row = Instances.FirstOrDefault(x => x.Id == instance.Id);
                if (row != null)
                    row.Update(instance);
                else
                    Instances.Insert(0, new HistoryRowViewModel(instance));
            });
        }

        private static void Dispatch(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.Invoke(action);
        }
    }
}
