using Automation.App.Base;
using Automation.App.Views.TaskUI;
using Automation.App.Views.WorkflowUI;
using Automation.Base.ViewModels;
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

        public ScopePage(IModalContainer modal, Scope scope)
        {
            _scope = scope;
            _modal = modal;
            this.DataContext = _scope;
            InitializeComponent();
        }

        #region UI Events
        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            ContextMenu contextMenu = element.ContextMenu;
            contextMenu.PlacementTarget = element;
            contextMenu.IsOpen = true;
        }

        private void ButtonParam_Click(object sender, RoutedEventArgs e)
        {
            _modal.Show(new ScopeEditModal(_scope));
        }

        private async void MenuAddScope_Click(object sender, RoutedEventArgs e)
        {
            Scope newScope = new Scope();
            if (await _modal.Show(new ScopeEditModal(newScope)))
                _scope.AddChild(newScope);
        }

        private async void MenuAddTask_Click(object sender, RoutedEventArgs e)
        {
            TaskNode newTask = new TaskNode();
            if (await _modal.Show(new TaskEditModal(newTask)))
                _scope.AddChild(new ScopedNode(newTask));
        }

        private async void MenuAddWorkflow_Click(object sender, RoutedEventArgs e)
        {
            WorkflowNode newWorkflow = new WorkflowNode();
            if (await _modal.Show(new WorkflowEditModal(newWorkflow)))
                _scope.AddChild(new ScopedNode(newWorkflow));
        }
        #endregion

        private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListBox listBox = (ListBox)sender;
            ScopedElement? selectedElement = listBox.SelectedItem as ScopedElement;

            if (selectedElement == null)
                return;

            selectedElement.IsSelected = true;
        }
    }
}
