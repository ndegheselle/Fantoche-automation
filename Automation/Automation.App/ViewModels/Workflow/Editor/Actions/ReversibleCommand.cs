using System.ComponentModel;
using System.DirectoryServices;
using System.Runtime.CompilerServices;
using System.Text;
using Usuel.Shared;

namespace Automation.App.ViewModels.Workflow.Editor.Actions
{
    public class HistoryAction
    {
        public IReversibleCommand Command { get; }

        public object? Parameter { get; }

        public HistoryAction(IReversibleCommand command, object? parameter = null)
        {
            Command = command;
            Parameter = parameter;
        }
    }

    public class HistoryHandler : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string? name = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }

        private Stack<HistoryAction> _undoStack = [];
        private Stack<HistoryAction> _redoStack = [];

        public bool IsUndoAvailable => _undoStack.Any();

        public bool IsRedoAvailable => _redoStack.Any();

        public void Add(IReversibleCommand command, object? parameter = null)
        {
            _undoStack.Push(new HistoryAction(command, parameter));
            _redoStack.Clear();

            NotifyPropertyChanged(nameof(IsUndoAvailable));
            NotifyPropertyChanged(nameof(IsRedoAvailable));
        }

        public void Undo()
        {
            if (IsUndoAvailable == false)
                return;

            var action = _undoStack.Pop();
            NotifyPropertyChanged(nameof(IsUndoAvailable));

            // Ignore the command that don't have any undo command
            if (action.Command.Reverse == null)
                return;
            action.Command.Reverse.Execute(action.Parameter, withHistory: false);
            _redoStack.Push(action);
            NotifyPropertyChanged(nameof(IsRedoAvailable));
        }

        public void Redo()
        {
            if (IsRedoAvailable == false)
                return;

            var action = _redoStack.Pop();
            NotifyPropertyChanged(nameof(IsRedoAvailable));

            // Ignore the command that don't have any undo command
            if (action.Command.Reverse == null)
                return;
            action.Command.Reverse.Execute(action.Parameter, withHistory: false);
            _undoStack.Push(action);
            NotifyPropertyChanged(nameof(IsUndoAvailable));
        }
    }

    public interface IReversibleCommand : ICustomCommand
    {
        /// <summary>
        /// Command that does the exact oposit of this command.
        /// </summary>
        public IReversibleCommand? Reverse { get; }

        void Execute(object? parameter, bool withHistory);
    }

    public class ReversibleCommand : DelegateCommand, IReversibleCommand
    {
        public IReversibleCommand? Reverse { get; set; }

        private readonly HistoryHandler _handler;

        public ReversibleCommand(HistoryHandler handler, Action action, Func<bool>? executeCondition = default) : base(
            action,
            executeCondition)
        { _handler = handler; }

        public override void Execute(object? parameter) { Execute(parameter, true); }

        public void Execute(object? parameter, bool withHistory)
        {
            base.Execute(parameter);
            if (withHistory == true)
                _handler.Add(this, parameter);
        }
    }

    public class ReversibleCommand<T> : DelegateCommand<T>, IReversibleCommand
    {
        public IReversibleCommand? Reverse { get; set; }

        private readonly HistoryHandler _handler;

        public ReversibleCommand(HistoryHandler handler, Action<T> action, Func<T, bool>? executeCondition = default) : base(
            action,
            executeCondition)
        { _handler = handler; }

        public override void Execute(object? parameter) { Execute(parameter, true); }

        public void Execute(object? parameter, bool withHistory)
        {
            base.Execute(parameter);
            if (withHistory == true)
                _handler.Add(this, parameter);
        }
    }
}
