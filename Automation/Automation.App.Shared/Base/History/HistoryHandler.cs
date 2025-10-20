using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Usuel.Shared;

namespace Automation.App.Shared.Base.History
{
    public interface IHistoryAction
    {
        public bool CanReverse { get; }
        public IReversibleCommand Reverse();
    }

    public class HistoryAction : IHistoryAction
    {
        public IReversibleCommand Command { get; }
        public object? Parameter { get; }

        public bool CanReverse => Command.Reverse != null;

        public HistoryAction(IReversibleCommand command, object? parameter = null)
        {
            Command = command;
            Parameter = parameter;
        }

        public IReversibleCommand Reverse()
        {
            if (Command.Reverse == null)
                throw new Exception("This action can't be reversed.");
            Command.Reverse.Execute(Parameter, withHistory: false);

            return Command.Reverse;
        }
    }

    public class HistoryActionBatch : IHistoryAction
    {
        public List<IHistoryAction> Actions { get; } = [];
        public bool CanReverse => Actions.Count > 0;

        public void Reverse()
        {
            foreach(var action  in Actions)
            {
                if (action.CanReverse)
                    action.Reverse();
            }
        }
    }

    public class HistoryHandler : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string? name = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }

        private bool _isUndoAvailable = false;
        public bool IsUndoAvailable
        {
            get => _isUndoAvailable;
            private set
            {
                if (_isUndoAvailable != value)
                {
                    _isUndoAvailable = value;
                    NotifyPropertyChanged();
                    UndoCommand.RaiseCanExecuteChanged();
                }
            }
        }
        private bool _isRedoAvailable = false;
        public bool IsRedoAvailable
        {
            get => _isRedoAvailable;
            private set
            {
                if (_isRedoAvailable != value)
                {
                    _isRedoAvailable = value;
                    NotifyPropertyChanged();
                    RedoCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ICustomCommand UndoCommand { get; }
        public ICustomCommand RedoCommand { get; }

        private Stack<IHistoryAction> _undoStack = [];
        private Stack<IHistoryAction> _redoStack = [];

        private Stack<HistoryActionBatch> _batch = [];

        public HistoryHandler()
        {
            UndoCommand = new DelegateCommand(Undo, () => IsUndoAvailable);
            RedoCommand = new DelegateCommand(Redo, () => IsRedoAvailable);
        }

        public void Add(IReversibleCommand command, object? parameter = null)
        {
            Add(new HistoryAction(command, parameter));
        }

        public void Add(IHistoryAction action)
        {
            _undoStack.Push(action);
            _redoStack.Clear();

            IsRedoAvailable = _redoStack.Count!=0;
            IsUndoAvailable = _undoStack.Count!=0;
        }

        public void BeginBatch()
        {
            _batch = new HistoryActionBatch();
        }

        public void EndBatch()
        {

        }

        public void Undo()
        {
            if (IsUndoAvailable == false)
                return;

            var action = _undoStack.Pop();
            IsUndoAvailable = _undoStack.Count!=0;

            // Ignore the command that don't have any undo command
            if (action.CanReverse == false)
                return;
            var reverseAction = action.Reverse();
            _redoStack.Push(reverseAction);
            IsRedoAvailable = _redoStack.Count!=0;
        }

        public void Redo()
        {
            if (IsRedoAvailable == false)
                return;

            var action = _redoStack.Pop();
            IsRedoAvailable = _redoStack.Count!=0;

            // Ignore the command that don't have any undo command
            if (action.CanReverse == false)
                return;
            action.Reverse();
            _undoStack.Push(action);
            IsUndoAvailable = _undoStack.Count!=0;
        }
    }
}
