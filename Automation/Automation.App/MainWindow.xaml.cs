using AdonisUI.Controls;
using Automation.App.Base;
using Automation.App.Views.TasksPages;
using Joufflu.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;

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
    public partial class MainWindow : AdonisWindow, IWindowContainer, INavigationLayout
    {
        // XXX : if called before InitializeComponent, the property will be null
        public IModalContainer Modal => this.ModalContainer;
        public IAlert Alert => this.AlertContainer;

        public INavigationLayout? Layout { get; set; }

        private readonly App _app = (App)App.Current;

        public MainWindow()
        {
            InitializeComponent();
            Show(new TasksMainPage());
        }

        public void Show(IPage page)
        {
            page.Layout = this;
            NavigationContent.Content = page;
        }

        private void NavigationSideMenu_NavigationChanged(Views.Menus.EnumNavigationPages page)
        {
            switch(page)
            {
                case Views.Menus.EnumNavigationPages.Tasks:
                    Show(new TasksMainPage());
                    break;
            }
        }
    }
}