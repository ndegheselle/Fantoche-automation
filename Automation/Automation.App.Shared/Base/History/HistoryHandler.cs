using System.ComponentModel;
using System.Runtime.CompilerServices;
using Usuel.Shared;

namespace Automation.App.Shared.Base.History
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

        private Stack<HistoryAction> _undoStack = [];
        private Stack<HistoryAction> _redoStack = [];

        public HistoryHandler()
        {
            UndoCommand = new DelegateCommand(Undo, () => IsUndoAvailable);
            RedoCommand = new DelegateCommand(Redo, () => IsRedoAvailable);
        }

        public void Add(IReversibleCommand command, object? parameter = null)
        {
            _undoStack.Push(new HistoryAction(command, parameter));
            _redoStack.Clear();

            IsRedoAvailable = _redoStack.Count!=0;
            IsUndoAvailable = _undoStack.Count!=0;
        }

        public void Undo()
        {
            if (IsUndoAvailable == false)
                return;

            var action = _undoStack.Pop();
            IsUndoAvailable = _undoStack.Count!=0;

            // Ignore the command that don't have any undo command
            if (action.Command.Reverse == null)
                return;
            action.Command.Reverse.Execute(action.Parameter, withHistory: false);
            _redoStack.Push(action);
            IsRedoAvailable = _redoStack.Count!=0;
        }

        public void Redo()
        {
            if (IsRedoAvailable == false)
                return;

            var action = _redoStack.Pop();
            IsRedoAvailable = _redoStack.Count!=0;

            // Ignore the command that don't have any undo command
            if (action.Command.Reverse == null)
                return;
            action.Command.Reverse.Execute(action.Parameter, withHistory: false);
            _undoStack.Push(action);
            IsUndoAvailable = _undoStack.Count!=0;
        }
    }
}
