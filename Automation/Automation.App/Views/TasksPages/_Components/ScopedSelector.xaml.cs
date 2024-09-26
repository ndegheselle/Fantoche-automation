using AdonisUI.Controls;
using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Automation.App.Views.TasksPages.ScopeUI;
using Automation.App.Views.TasksPages.TaskUI;
using Automation.App.Views.TasksPages.WorkflowUI;
using Automation.Shared.Data;
using Joufflu.Popups;
using Joufflu.Shared;
using Joufflu.Shared.Layouts;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Usuel.Shared;
using MessageBox = AdonisUI.Controls.MessageBox;

namespace Automation.App.Views.TasksPages.Components
{
    public class ScopedSelectorModal : ScopedSelector, IModalContent
    {
        public ILayout? ParentLayout { get; set; }

        public ModalOptions Options => new ModalOptions() { Title = "Add node" };
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
            set
            {
                SetValue(SelectedProperty, value);
                SelectedChanged?.Invoke(Selected);
            }
        }
        #endregion

        #region Props
        public EnumScopedType AllowedSelectedNodes
        {
            get;
            set;
        } = EnumScopedType.Scope | EnumScopedType.Workflow | EnumScopedType.Task;

        public ScopedElement SelectedOrDefault => Selected ?? RootScope;

        private readonly App _app = (App)App.Current;
        private readonly ScopesClient _scopeClient;
        private readonly WorkflowsClient _workflowClient;
        private readonly TasksClient _taskClient;

        private IModal _modal => this.GetCurrentModalContainer();
        #endregion

        public ScopedSelector()
        {
            _scopeClient = _app.ServiceProvider.GetRequiredService<ScopesClient>();
            _taskClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            _workflowClient = _app.ServiceProvider.GetRequiredService<WorkflowsClient>();
            InitializeComponent();

            RemoveSelectedCommand = new DelegateCommand(
                OnRemoveSelected,
                // Not the root element
                () => SelectedOrDefault.Parent != null);
            AddScopeCommand = new DelegateCommand(OnAddScope);
            AddTaskCommand = new DelegateCommand(OnAddTask);
            AddWorkflowCommand = new DelegateCommand(OnAddWorkflow);
        }

        // XXX : move to viewmodels ?
        #region Commands
        public ICustomCommand RemoveSelectedCommand { get; set; }

        public ICommand AddScopeCommand { get; set; }

        public ICommand AddTaskCommand { get; set; }

        public ICommand AddWorkflowCommand { get; set; }

        private async void OnRemoveSelected()
        {
            if (MessageBox.Show(
                    "Are you sure you want to remove this element ?",
                    "Confirmation",
                    AdonisUI.Controls.MessageBoxButton.YesNo) !=
                AdonisUI.Controls.MessageBoxResult.Yes)
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
            if (await _modal.Show(new ScopeCreateModal(newScope)))
            {
                Dispatcher.Invoke(
                    () =>
                    {
                        parentScope.AddChild(newScope);
                        newScope.FocusOn = EnumScopedTabs.Settings;
                        newScope.IsSelected = true;
                    });
            }
        }

        private async void OnAddTask()
        {
            if (SelectedOrDefault is not Scope parentScope)
                return;

            var task = new TaskNode();
            task.ScopeId = parentScope.Id;
            if (await _modal.Show(new TaskCreateModal(task)))
            {
                parentScope.AddChild(task);
                task.FocusOn = EnumScopedTabs.Settings;
                task.IsSelected = true;
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
                workflow.Id = await _workflowClient.CreateAsync(workflow);
                parentScope.AddChild(workflow);
            }
        }
        #endregion

        #region UI Events
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView = (TreeView)sender;
            ScopedElement? selected = treeView.SelectedItem as ScopedElement;
            if (selected != null && !AllowedSelectedNodes.HasFlag(selected.Type))
            {
                selected.IsSelected = false;
                return;
            }
            Selected = selected;
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
            // Unselect the current if not null
            else if (Selected != null)
            {
                Selected.IsSelected = false;
            }
        }
        #endregion

        private TreeViewItem? VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement frameworkElement = (FrameworkElement)sender;
            frameworkElement.ContextMenu.PlacementTarget = frameworkElement;
            frameworkElement.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            frameworkElement.ContextMenu.IsOpen = true;
        }
    }
}
