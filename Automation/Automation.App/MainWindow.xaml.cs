using AdonisUI.Controls;
using Automation.App.Views.PackagesPages;
using Automation.App.Views.WorkersPages;
using Automation.App.Views.WorkPages;
using Joufflu.Popups;
using Joufflu.Shared.Navigation;
using System.Windows;

namespace Automation.App
{
    public interface IWindowContainer
    {
        IModal Modal {  get; }
        IAlert Alert { get; }
        ILoading Loading { get; }
    }

    public static class DependencyObjectExtension
    {
        public static IModal GetCurrentModal(this DependencyObject d)
        {
            return ((IWindowContainer)Window.GetWindow(d)).Modal;
        }

        public static IAlert GetCurrentAlert(this DependencyObject d)
        {
            return ((IWindowContainer)Window.GetWindow(d)).Alert;
        }

        public static ILoading GetCurrentLoading(this DependencyObject d)
        {
            return ((IWindowContainer)Window.GetWindow(d)).Loading;
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
        public ILoading Loading => this.LoadingElement;
        public ILayout? ParentLayout { get; set; }

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
                case Views.Menus.EnumNavigationPages.Work:
                    Show(new TasksMainPage());
                    break;
                case Views.Menus.EnumNavigationPages.Servers:
                    Show(new WorkerMainPage());
                    break;
                case Views.Menus.EnumNavigationPages.Packages:
                    Show(new PackagesMainPage());
                    break;
            }
        }
    }
}