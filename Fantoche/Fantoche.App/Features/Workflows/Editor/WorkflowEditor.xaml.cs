using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Fantoche.App.Features.Workflows.Editor.ViewModels;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;
using Joufflu.Toolkit;
using Nodify;
using Nodify.Events;

namespace Fantoche.App.Features.Workflows.Editor
{
    public partial class WorkflowEditor : UserControl
    {
        public WorkflowEditorViewModel? ViewModel => DataContext as WorkflowEditorViewModel;

        /// <summary>
        /// Drop of a node dragged out of the tree, added to the graph where it landed. Held by the
        /// view rather than by the view model : turning the drop position into graph coordinates
        /// needs the editor and its viewport.
        /// </summary>
        public IRelayCommand<IDataObject> DropCommand { get; }

        public WorkflowEditor()
        {
            NodifyEditor.AutoFocusFirstElement = false;
            InitializeComponent();
            DropCommand = new RelayCommand<IDataObject>(OnDrop, CanDrop);

            // Nodify aligns the containers itself, without raising its drag commands : the alignment
            // is intercepted on its way to the editor and applied here, so it goes through the history.
            CommandManager.AddPreviewCanExecuteHandler(Editor, OnEditorPreviewCanExecute);
            CommandManager.AddPreviewExecutedHandler(Editor, OnEditorPreviewExecuted);

            // The nodes a link or a drag can't reach are greyed out while it lasts. Routed events
            // rather than the editor commands : only they tell a link let go in the void or canceled.
            Editor.AddHandler(Connector.PendingConnectionStartedEvent, new PendingConnectionEventHandler(OnLinkStarted), handledEventsToo: true);
            Editor.AddHandler(Connector.PendingConnectionCompletedEvent, new PendingConnectionEventHandler(OnLinkEnded), handledEventsToo: true);
            DropArea.PreviewDragEnter += OnDragEntered;
            // Rather than DragLeave, raised whenever the pointer moves from one child to another :
            // the drop target counts them, and its flag drops on a leave, a drop or a cancel alike.
            DependencyPropertyDescriptor.FromProperty(DropTarget.IsDragOverProperty, typeof(Border))
                .AddValueChanged(DropArea, OnDragOverChanged);
        }

        #region Muting
        private void OnLinkStarted(object sender, PendingConnectionEventArgs e)
        {
            if (e.SourceConnector is ConnectorViewModel connector)
                ViewModel?.MuteIncompatibleWith(connector);
        }

        private void OnLinkEnded(object sender, PendingConnectionEventArgs e) => ViewModel?.Unmute();

        /// <summary>The page dragged from the tree the nodes are muted for, so it is only resolved once.</summary>
        private BaseAutomationTask? _draggedPage;

        private void OnDragEntered(object sender, DragEventArgs e)
        {
            // Not kept while the graph is read only : the drop being refused, the flag clearing it
            // would never rise.
            if (GetTask(e.Data) is not AutomationTask { IsUserInterface: true } page || page == _draggedPage || ViewModel?.IsEditable != true)
                return;

            _draggedPage = page;
            ViewModel?.MuteIncompatibleWith(BaseGraphTask.For(page));
        }

        private void OnDragOverChanged(object? sender, EventArgs e)
        {
            if (DropTarget.GetIsDragOver(DropArea) || _draggedPage == null)
                return;

            _draggedPage = null;
            ViewModel?.Unmute();
        }
        #endregion

        /// <summary>A graph that can't be edited can't be aligned either, Nodify knowing nothing of it.</summary>
        private void OnEditorPreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Command == EditorCommands.Align && ViewModel?.IsEditable != true)
            {
                e.CanExecute = false;
                e.Handled = true;
            }
        }

        private void OnEditorPreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command != EditorCommands.Align || ViewModel == null || GetAlignment(e.Parameter) is not Alignment alignment)
                return;

            e.Handled = true;
            ViewModel.Move("Align", () => Editor.AlignSelection(alignment));
        }

        /// <summary>The alignment Nodify accepts : the value itself or its name.</summary>
        private static Alignment? GetAlignment(object? parameter) => parameter switch
        {
            Alignment alignment => alignment,
            string name when Enum.TryParse(name, out Alignment alignment) => alignment,
            _ => null,
        };

        private void OnDrop(IDataObject? data)
        {
            if (GetTask(data) is not BaseAutomationTask task || data is not DropData drop)
                return;

            if (ViewModel == null)
                return;

            // The drop is placed on the border holding the target, which the editor translates to
            // the graph itself. Dropped on a node, a page is attached under it.
            ItemContainer? onto = ContainerAt(drop.Target.TranslatePoint(drop.Position, Editor));
            ViewModel.Add(
                task,
                Editor.GetLocationInsideEditor(drop.Position, drop.Target),
                onto?.DataContext as NodeViewModel);
        }

        /// <summary>The node container under [position] in the editor, null over its background.</summary>
        private ItemContainer? ContainerAt(Point position)
        {
            DependencyObject? hit = Editor.InputHitTest(position) as DependencyObject;
            while (hit != null && hit is not ItemContainer)
                hit = VisualTreeHelper.GetParent(hit);
            return hit as ItemContainer;
        }

        /// <summary>
        /// Only a task or a workflow can be dropped, a scope having nothing to run, and the workflow
        /// being edited can't contain itself.
        /// </summary>
        private bool CanDrop(IDataObject? data)
        {
            BaseAutomationTask? task = GetTask(data);
            // Nothing can be added to a running workflow, its graph being read only.
            return task != null && task.Id != ViewModel?.Workflow.Id && ViewModel?.IsEditable == true;
        }

        /// <summary>
        /// Hand the keyboard to the box as soon as renaming starts : the name is edited on the graph
        /// itself, so nothing else is going to focus it.
        /// </summary>
        private void OnRenameBoxVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is true && sender is TextBox box)
            {
                box.Focus();
                box.SelectAll();
            }
        }

        /// <summary>
        /// Leaving the box commits what was typed : a name edited in place has no button to press.
        /// </summary>
        private void OnRenameBoxLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement box && box.DataContext is NodeViewModel node)
                ViewModel?.CommitRenameCommand.Execute(node);
        }

        /// <summary>
        /// What the dragged node is worth to the editor, <see langword="null"/> for anything else
        /// than a task or a workflow coming from the tree.
        /// </summary>
        private static BaseAutomationTask? GetTask(IDataObject? data)
            => (data?.GetData(typeof(ScopedNode)) as ScopedNode)?.TaskElement;
    }
}
