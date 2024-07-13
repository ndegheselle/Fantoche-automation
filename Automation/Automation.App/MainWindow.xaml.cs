using AdonisUI.Controls;
using Automation.App.Base;
using Automation.Shared.Supervisor;
using Automation.Supervisor;
using Automation.Supervisor.Repositories;
using Automation.Worker;
using Microsoft.Extensions.DependencyInjection;
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
        // XXX : if called before InitializeComponent, the property will be null
        public IModalContainer Modal => this.ModalContainer;
        public IAlert Alert => this.AlertContainer;

        private readonly App _app = (App)App.Current;
        private readonly INodeRepository _repository;

        public MainWindow()
        {
            _repository = _app.ServiceProvider.GetRequiredService<INodeRepository>();
            InitializeComponent();
            OnLoaded();
        }

        protected async void OnLoaded()
        {
            SideMenu.RootScope = await _repository.GetRootScopeAsync();
        }
    }
}