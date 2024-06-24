using Automation.App.Base;
using Automation.App.ViewModels;
using Automation.App.Views.TaskUI;
using Automation.App.Views.WorkflowUI;
using Automation.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.ScopeUI
{
    /// <summary>
    /// Logique d'interaction pour ScopePage.xaml
    /// </summary>
    public partial class ScopePage : UserControl
    {
        private readonly IModalContainer _modal;
        private readonly Scope _scope;
        private readonly SideMenuViewModel _sideMenu;

        public ScopePage(IModalContainer modal, Scope scope)
        {
            _scope = scope;
            _modal = modal;
            InitializeComponent();
            this.DataContext = _scope;
        }

        #region UI Events
        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            ContextMenu contextMenu = element.ContextMenu;
            contextMenu.PlacementTarget = element;
            contextMenu.IsOpen = true;
        }

        private async void MenuAddScope_Click(object sender, RoutedEventArgs e)
        {
            Scope newScope = new Scope();
            if (await _modal.Show(new ScopeEdit(newScope), new ModalOptions() { Title = "Add scope" }))
                _scope.AddChild(newScope);
        }

        private async void MenuAddTask_Click(object sender, RoutedEventArgs e)
        {
            TaskNode newScope = new TaskNode();
            if (await _modal.Show(new TaskEdit(newScope), new ModalOptions() { Title = "Add task" }))
                _scope.AddChild(newScope);
        }

        private async void MenuAddWorkflow_Click(object sender, RoutedEventArgs e)
        {
            WorkflowNode newScope = new WorkflowNode();
            if (await _modal.Show(new WorkflowEdit(newScope), new ModalOptions() { Title = "Add workflow" }))
                _scope.AddChild(newScope);
        }
        #endregion

        private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListBox listBox = (ListBox)sender;
            Node? selectedElement = listBox.SelectedItem as Node;

            if (selectedElement == null)
                return;

            selectedElement.IsSelected = true;
        }
    }
}
