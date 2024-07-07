using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Automation.App.Base
{
    public class BaseNotifyDataError : INotifyDataErrorInfo
    {
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        public bool HasErrors => _errorsByPropertyName.Any();
        private readonly Dictionary<string, List<string>> _errorsByPropertyName = new Dictionary<string, List<string>>();

        public IEnumerable GetErrors(string? propertyName)
        {
            return !String.IsNullOrEmpty(propertyName) && _errorsByPropertyName.ContainsKey(propertyName) ?
            _errorsByPropertyName[propertyName] : new List<string>();
        }

        private void OnErrorsChanged(string? propertyName = null)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public void AddError(string error, [CallerMemberName] string? propertyName = null)
        {
            if (propertyName == null)
                return;

            if (!_errorsByPropertyName.ContainsKey(propertyName))
                _errorsByPropertyName[propertyName] = new List<string>();

            if (!_errorsByPropertyName[propertyName].Contains(error))
            {
                _errorsByPropertyName[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        public void ClearAllErrors()
        {
            _errorsByPropertyName.Clear();
            OnErrorsChanged();
        }

        public void ClearErrors([CallerMemberName] string? propertyName = null)
        {
            if (!String.IsNullOrEmpty(propertyName) && _errorsByPropertyName.ContainsKey(propertyName))
            {
                _errorsByPropertyName.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }
        }
    }
}
