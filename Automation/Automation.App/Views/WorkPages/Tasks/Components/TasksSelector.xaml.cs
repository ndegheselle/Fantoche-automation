using Automation.App.Shared.ApiClients;
using Automation.Dal.Models;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace Automation.App.Views.WorkPages.Tasks.Components
{
    public class FlatenedTask
    {
        public BaseAutomationTask Task { get; set; }
        public string? Tag { get; set; }

        public FlatenedTask(BaseAutomationTask task, string? tag)
        {
            Task = task;
            Tag = tag;
        }
    }

    public partial class TasksSelector : UserControl, INotifyPropertyChanged
    {
        public IEnumerable<FlatenedTask> Tasks { get; set; } = [];
        public ICollectionView TasksView => CollectionViewSource.GetDefaultView(Tasks);

        private readonly TasksClient _client;

        public TasksSelector()
        {
            _client = Services.Provider.GetRequiredService<TasksClient>();
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // XXX : toto maybe load by tag once the expander is opened
            var tasks = await _client.GetAllAsync();
            Tasks = tasks.SelectMany(d => d.Metadata.Tags.Count() == 0 ? [new FlatenedTask(d, null)] : d.Metadata.Tags.Select(tag => new FlatenedTask(d, tag)));

            ICollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(Tasks);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(FlatenedTask.Tag)));
            view.SortDescriptions.Add(new SortDescription(nameof(FlatenedTask.Tag), ListSortDirection.Ascending));
        }
    }
}
