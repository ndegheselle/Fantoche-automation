using Automation.Base;
using Automation.Plugins.Flow;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Formats.Asn1;
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

        public Scope RootScope { get; set; }

        private ScopedElement? _selectedElement;
        public ScopedElement? SelectedElement
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
            RootScope = new Scope();
            RootScope.Childrens.Add(new Scope()
            {
                Name = "Scope 1",
                Childrens = new ObservableCollection<ScopedElement>()
                {
                    new ScopedElement() { Name = "Wait all", Type = EnumTaskType.Task, TaskClass = typeof(WaitAllTasks)},
                    new ScopedElement() { Name = "Delay", Type = EnumTaskType.Task, TaskClass = typeof(WaitDelay) },
                }
            });
        }
    }
}
