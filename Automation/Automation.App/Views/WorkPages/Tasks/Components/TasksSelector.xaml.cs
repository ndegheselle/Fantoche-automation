using Automation.App.Shared.ApiClients;
using Automation.Models.Work;
using Joufflu.Data.DnD;
using Joufflu.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
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

    public class TaskDragHandler : AdornerDragHandler
    {
        public TaskDragHandler(FrameworkElement parent) : base(parent)
        {}

        protected override object? GetSourceData(FrameworkElement? source)
        {
            if (source == null)
                return null;

            TaskTile? tile = MoreVisualTreeHelper.FindParent<TaskTile>(source);
            return tile?.Task;
        }

        protected override FrameworkElement? CreateAdornerContent(object data)
        {
            if (data is not BaseAutomationTask task)
                return null;
            return new TaskTile() { Task = task };
        }
    }

    public partial class TasksSelector : UserControl, INotifyPropertyChanged
    {
        public IEnumerable<FlatenedTask> Tasks { get; set; } = [];
        public ICollectionView TasksView => CollectionViewSource.GetDefaultView(Tasks);
        public TaskDragHandler DragHandler { get; }

        private readonly TasksClient _client;

        public TasksSelector()
        {
            _client = Services.Provider.GetRequiredService<TasksClient>();
            DragHandler = new TaskDragHandler(this);
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
