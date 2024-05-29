using AdonisUI.Controls;
using Automation.Supervisor;
using Automation.Worker;
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
        private readonly TaskSuperviser _supervisor = new TaskSuperviser();
        public TaskContext TaskContext { get; set; } = new TaskContext();

        public MainWindow()
        {
            TaskContext.PropertyChanged += TaskContext_PropertyChanged;
            // Get the executable path
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            TaskContext.DllFolderPath = path;

            InitializeComponent();
        }

        private void TaskContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskContext.DllFolderPath))
            {
                TaskContext.AvailableClasses.Clear();
                foreach (var task in _supervisor.RefreshClassesFromFolderDlls(TaskContext.DllFolderPath))
                    TaskContext.AvailableClasses.Add(task);
            }
        }

        private void StartTask_Click(object sender, RoutedEventArgs e)
        {
            TaskWorker worker = new TaskWorker(TaskContext.SelectedClass, TaskContext.JsonContext);
            worker.ExecuteTask();
        }
    }
}