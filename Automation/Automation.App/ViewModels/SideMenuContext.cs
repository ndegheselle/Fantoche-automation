using Automation.Base;
using Automation.Plugins.Flow;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Automation.App.ViewModels
{
    public class SideMenuContext : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName]string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TaskScope RootScope { get; set; }

        private IContextElement? _selectedElement;
        public IContextElement? SelectedElement
        {
            get => _selectedElement;
            set
            {
                if (_selectedElement == value)
                    return;

                _selectedElement = value;
                OnPropertyChanged();
            }
        }

        public SideMenuContext()
        {
            RootScope = new TaskScope();
            RootScope.Childrens.Add(new TaskScope()
            {
                Name = "Scope 1",
                Childrens = new List<IContextElement>()
                {
                    new WaitAllTasks() { Name = "Wait all"},
                    new WaitDelay() { Name = "Delay" },
                }
            });
        }
    }
}
