using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Workflows.Editor.History
{
    /// <summary>
    /// Undo / redo stack of the editor. It also tells whether the current work needs to be saved,
    /// the save point being the action that was the last applied one when the work got saved.
    /// </summary>
    public partial class EditorHistory : ObservableObject
    {
        private readonly Stack<IReversibleAction> _applied = new();
        private readonly Stack<IReversibleAction> _reverted = new();

        /// <summary>
        /// Last applied action when the work was saved, null when saved with nothing applied.
        /// </summary>
        private IReversibleAction? _savePoint;

        /// <summary>
        /// Raised when an action that changes what the graph would run into was applied, reverted or
        /// redone. Whoever reads the graph as a whole — the preview of what it would run into —
        /// listens to this rather than to every property of the history, moving a node around being
        /// a modification the graph itself knows nothing about.
        /// </summary>
        public event Action? ExecutionChanged;

        /// <summary>
        /// Whether the history can be used at all : the editor disables it while the graph is read
        /// only, an undo being a modification like any other.
        /// </summary>
        [ObservableProperty] private bool _isEnabled = true;

        public bool CanUndo => IsEnabled && _applied.Count > 0;

        public bool CanRedo => IsEnabled && _reverted.Count > 0;

        /// <summary>
        /// True when actions have been applied or reverted since the last <see cref="MarkSaved"/>.
        /// </summary>
        public bool HasUnsavedChanges => _savePoint != Current;

        private IReversibleAction? Current => _applied.Count > 0 ? _applied.Peek() : null;

        /// <summary>
        /// Apply an action and make it undoable, dropping whatever could be redone.
        /// </summary>
        public void Apply(IReversibleAction action)
        {
            action.Execute();
            _applied.Push(action);
            _reverted.Clear();
            NotifyChanged();
            Raise(action);
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            IReversibleAction action = _applied.Pop();
            action.Revert();
            _reverted.Push(action);
            NotifyChanged();
            Raise(action);
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            IReversibleAction action = _reverted.Pop();
            action.Execute();
            _applied.Push(action);
            NotifyChanged();
            Raise(action);
        }

        public void MarkSaved()
        {
            _savePoint = Current;
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }

        private void Raise(IReversibleAction action)
        {
            if (action.ChangesExecution)
                ExecutionChanged?.Invoke();
        }

        partial void OnIsEnabledChanged(bool value) => NotifyChanged();

        private void NotifyChanged()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }
    }
}
