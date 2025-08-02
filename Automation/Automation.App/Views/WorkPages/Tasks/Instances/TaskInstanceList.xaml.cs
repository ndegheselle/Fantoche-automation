using Automation.Dal.Models;
using Joufflu.Popups;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Tasks.Instances
{
    /// <summary>
    /// Logique d'interaction pour TaskInstanceTable.xaml
    /// </summary>
    public partial class TaskInstanceList : UserControl, INotifyPropertyChanged
    {
        // Dependency property for the task id
        public static readonly DependencyProperty InstancesProperty = DependencyProperty.Register(
            nameof(Instances),
            typeof(IEnumerable<TaskInstance>),
            typeof(TaskInstanceList),
            new PropertyMetadata(null));

        public IEnumerable<TaskInstance>? Instances { get { return (IEnumerable<TaskInstance>?)GetValue(InstancesProperty); } set { SetValue(InstancesProperty, value); } }
        private IModal _modal => this.GetCurrentModal();

        public TaskInstanceList() {
            InitializeComponent();
        }

        #region UI events
        private void DataGridDetail_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)sender;
            var instance = dataGrid.SelectedItem as TaskInstance;

            if (instance == null)
                return;
            _modal.Show(new InstanceDetailModal(instance));
        }
        #endregion
    }
}
