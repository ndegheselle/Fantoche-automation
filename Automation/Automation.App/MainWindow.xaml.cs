using AdonisUI.Controls;
using Automation.App.Base;
using Automation.Supervisor;
using Automation.Supervisor.Repositories;
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
    public partial class MainWindow : AdonisWindow, IWindowContainer
    {
        private readonly TaskSuperviser _supervisor = new TaskSuperviser();

        // XXX : if called before InitializeComponent, the property will be null
        public IModalContainer Modal => this.ModalContainer;

        public MainWindow()
        {
            InitializeComponent();

            ScopeRepository scopeRepository = new ScopeRepository();
            SideMenu.RootScope = scopeRepository.GetRootScope();
        }
    }
}