using Automation.Dal.Models;
using System.Windows.Input;
using Usuel.Shared;

namespace Automation.App.ViewModels.Workflow.Editor.Actions
{
    public class HistoryCommand<T> : DelegateCommand<T>
    {
        public ICommand? Undo { get; set; }
        public HistoryCommand(Action<T> action, Func<T, bool>? executeCondition = null, ICommand? undo = null) : base(action, executeCondition)
        {
            Undo = undo;
        }
    }

    internal class ConnectionsAddAction : SimpleTargetedAction<IEnumerable<GraphConnection>>, IAction
    {
        public ConnectionsAddAction(IEnumerable<GraphConnection> target) : base(target)
        {}

        public void Execute(GraphEditorViewModel editor) => editor.Connect(_target);
        public IAction UndoAction => new ConnectionsRemoveAction(_target);
    }

    internal class ConnectionsRemoveAction : SimpleTargetedAction<IEnumerable<GraphConnection>>, IAction
    {
        public ConnectionsRemoveAction(IEnumerable<GraphConnection> target) : base(target)
        {}

        public void Execute(GraphEditorViewModel editor) => editor.Disconnect(_target);
        public IAction UndoAction => new ConnectionsAddAction(_target);
    }
}
