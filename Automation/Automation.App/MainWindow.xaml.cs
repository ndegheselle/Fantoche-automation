using AdonisUI.Controls;
using Automation.Supervisor;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Automation.App
{
    public class TaskContext : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string DllFolderPath { get; set; }
        public ObservableCollection<Type> AvailableClasses { get; set; } = new ObservableCollection<Type>();
        public Type SelectedClass { get; set; }
        public string JsonContext { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AdonisWindow
    {
        public TaskContext TaskContext { get; set; } = new TaskContext();

        public MainWindow()
        {
            InitializeComponent();
            TaskContext.PropertyChanged += TaskContext_PropertyChanged;
        }

        private void TaskContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskContext.DllFolderPath))
            {
                TaskContext.AvailableClasses.Clear();
                TaskSuperviser taskSuperviser = new TaskSuperviser(new TaskSupervisorParams
                {
                    DllsFolderPaths = new List<string> { TaskContext.DllFolderPath }
                });

                foreach (var task in taskSuperviser.AvailableClasses)
                    TaskContext.AvailableClasses.Add(task);
            }
        }
    }
}