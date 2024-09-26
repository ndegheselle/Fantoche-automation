using AdonisUI.Controls;
using Automation.App.Views.PackagesPages;
using Automation.App.Views.TasksPages;
using Automation.App.Views.WorkersPages;
using Joufflu.Popups;
using Joufflu.Shared.Layouts;
using System.Windows;

namespace Automation.App
{
    public interface IWindowContainer
    {
        IModal Modal {  get; }
        IAlert Alert { get; }
    }

    public static class DependencyObjectExtension
    {
        public static IModal GetCurrentModalContainer(this DependencyObject d)
        {
            return ((IWindowContainer)Window.GetWindow(d)).Modal;
        }

        public static IAlert GetCurrentAlertContainer(this DependencyObject d)
        {
            return ((IWindowContainer)Window.GetWindow(d)).Alert;
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AdonisWindow, IWindowContainer, ILayout
    {
        // XXX : if called before InitializeComponent, the property will be null
        public IModal Modal => this.ModalElement;
        public IAlert Alert => this.AlertElement;
        public ILayout? ParentLayout { get; set; }

        private readonly App _app = (App)App.Current;

        public MainWindow()
        {
            InitializeComponent();
            Show(new TasksMainPage());
        }

        public void Show(IPage page)
        {
            NavigationContent.Content = page;
        }

        private void NavigationSideMenu_NavigationChanged(Views.Menus.EnumNavigationPages page)
        {
            switch(page)
            {
                case Views.Menus.EnumNavigationPages.Tasks:
                    Show(new TasksMainPage());
                    break;
                case Views.Menus.EnumNavigationPages.Workers:
                    Show(new WorkerMainPage());
                    break;
                case Views.Menus.EnumNavigationPages.Packages:
                    Show(new PackagesMainPage());
                    break;
            }
        }
    }
}