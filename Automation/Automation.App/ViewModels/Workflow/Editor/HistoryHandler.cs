using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Usuel.Shared;

namespace Automation.App.ViewModels.Workflow.Editor
{
    public class HistoryHandler
    {
        private Stack<Action> UndoStack = [];
        private Stack<Action> RedoStack = [];

        private bool _isCanceling = false;

        public void Add(Action action)
        {
            if (_isCanceling)
            {
                CanceledActions.Push(action);
            }
            else
            {
                PreviousActions.Push(action);
                // Can't roll back after a new action have been added
                CanceledActions.Clear();
            }
        }

        public void Cancel()
        {
            var action = PreviousActions.Pop();
            _isCanceling = true;
            action.Invoke();
            _isCanceling = false;
        }

        public void Restore()
        {
            var action = CanceledActions.Pop();
            action.Invoke();
        }
    }
}
