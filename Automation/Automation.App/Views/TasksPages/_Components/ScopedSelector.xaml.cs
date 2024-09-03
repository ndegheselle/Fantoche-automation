using Automation.App.Base;
using Automation.App.Components.Display;
using Automation.App.Shared;
using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Packaging;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Automation.App;
using Automation.App.Views.TasksPages.ScopeUI;
using Automation.App.Views.TasksPages.TaskUI;
using Automation.App.Views.TasksPages.WorkflowUI;
using AdonisUI.Converters;
using System.Threading.Tasks;

namespace Automation.App.Views.TasksPages.Components
{
    public class ScopedSelectorModal : ScopedSelector, IModalContent
    {
        public IModalContainer? ModalParent { get; set; }
        public ModalOptions? Options => new ModalOptions() { Title = "Add node", ValidButtonText = "Add" };
    }

    /// <summary>
    /// Logique d'interaction pour ScopedElementSelector.xaml
    /// </summary>
    public partial class ScopedSelector : UserControl
    {
        public event Action<ScopedElement?>? SelectedChanged;

        #region Dependency Properties
        // Dependency property Scope RootScope
        public static readonly DependencyProperty RootScopeProperty = DependencyProperty.Register(
            nameof(RootScope),
            typeof(Scope),
            typeof(ScopedSelector),
            new PropertyMetadata(null));

        public Scope RootScope
        {
            get { return (Scope)GetValue(RootScopeProperty); }
            set { SetValue(RootScopeProperty, value); }
        }

        // Dependency property ScopedElement Selected
        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
            nameof(Selected),
            typeof(ScopedElement),
            typeof(ScopedSelector),
            new PropertyMetadata(null));

        public ScopedElement? Selected
        {
            get { return (ScopedElement?)GetValue(SelectedProperty); }
            set { 
                SetValue(SelectedProperty, value);
                SelectedChanged?.Invoke(Selected);
            }
        }

        #endregion

        #region Props

        public EnumScopedType AllowedSelectedNodes { get; set; } = EnumScopedType.Scope | EnumScopedType.Workflow | EnumScopedType.Task;
        public ScopedElement SelectedOrDefault => Selected ?? RootScope;

        private readonly App _app = (App)App.Current;
        private readonly ScopeClient _scopeClient;
        private readonly WorkflowClient _workflowClient;
        private readonly TaskClient _taskClient;

        private readonly IModalContainer _modal;
        #endregion



        public ScopedSelector() {
            _modal = this.GetCurrentModalContainer();
            _scopeClient = _app.ServiceProvider.GetRequiredService<ScopeClient>();
            _taskClient = _app.ServiceProvider.GetRequiredService<TaskClient>();
            _workflowClient = _app.ServiceProvider.GetRequiredService<WorkflowClient>();
            InitializeComponent();

            RemoveSelectedCommand = new DelegateCommand(OnRemoveSelected,
            // Not the root element
            () => SelectedOrDefault.Parent != null);
            AddScopeCommand = new DelegateCommand(OnAddScope);
            AddTaskCommand = new DelegateCommand(OnAddTask);
            AddWorkflowCommand = new DelegateCommand(OnAddWorkflow);

        }

        // XXX : move to viewmodels ?
        #region Commands

        public ICommand RemoveSelectedCommand { get; set; }
        public ICommand AddScopeCommand { get; set; }
        public ICommand AddTaskCommand { get; set; }
        public ICommand AddWorkflowCommand { get; set; }

        private async void OnRemoveSelected()
        {
            if (!await _modal.Show(new ConfirmationModal("Are you sure you want to remove this element ?")))
                return;

            switch (SelectedOrDefault.Type)
            {
                case EnumScopedType.Workflow:
                    await _workflowClient.DeleteAsync(SelectedOrDefault.Id);
                    break;
                case EnumScopedType.Task:
                    await _taskClient.DeleteAsync(SelectedOrDefault.Id);
                    break;
                case EnumScopedType.Scope:
                    await _scopeClient.DeleteAsync(SelectedOrDefault.Id);
                    break;
            }
            SelectedOrDefault.Parent!.Childrens.Remove(SelectedOrDefault);
        }

        private async void OnAddScope()
        {
            if (SelectedOrDefault is not Scope parentScope)
                return;

            Scope newScope = new Scope();
            newScope.ParentId = parentScope.Id;
            if (await _modal.Show(new ScopeEditModal(newScope)))
            {
                newScope.Id = await _scopeClient.CreateAsync(newScope);
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    parentScope.AddChild(newScope);
                });
            }
        }

        private async void OnAddTask()
        {
            if (SelectedOrDefault is not Scope parentScope)
                return;

            var task = new TaskNode();
            task.ScopeId = parentScope.Id;
            if (await _modal.Show(new TaskEditModal(task)))
            {
                task.Id = await _taskClient.CreateAsync(task);
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    parentScope.AddChild(task);
                });
            }
        }

        private async void OnAddWorkflow()
        {
            if (SelectedOrDefault is not Scope parentScope)
                return;

            WorkflowNode workflow = new WorkflowNode();
            workflow.ScopeId = parentScope.Id;
            if (await _modal.Show(new WorkflowEditModal(workflow)))
            {
                workflow.Id = await _taskClient.CreateAsync(workflow);
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    parentScope.AddChild(workflow);
                });
            }
        }

        #endregion

        #region UI Events

        private async void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView = (TreeView)sender;
            ScopedElement? selected = treeView.SelectedItem as ScopedElement;

            // Load childrens if the selected element is a scope and its childrens are not loaded
            if (selected != null && selected is Scope scope && scope.Childrens.Count == 0)
            {
                Scope? fullScope = await _scopeClient.GetByIdAsync(scope.Id);

                if (fullScope == null)
                    return;
                scope.Childrens = fullScope.Childrens;
            }

            if (selected != null && !AllowedSelectedNodes.HasFlag(selected.Type))
            {
                selected.IsSelected = false;
                return;
            }
            Selected = selected;
        }

        private void TreeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var source = (FrameworkElement)e.Source;
            bool forceOpenning = source.ContextMenu == null;

            if (SelectedOrDefault.Type == EnumScopedType.Scope)
                source.ContextMenu = (ContextMenu)FindResource("ContextMenuScope");
            else
                source.ContextMenu = (ContextMenu)FindResource("ContextMenuElement");

            // During the first opening we force the opening (otherwise the ContextMenu is not ready at the time of display)
            if (forceOpenning)
            {
                source.ContextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        // Allow selection on right click
        private void TreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem? treeViewItem = VisualUpwardSearch((DependencyObject)e.OriginalSource);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }

        #endregion

        private TreeViewItem? VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }
    }
}
