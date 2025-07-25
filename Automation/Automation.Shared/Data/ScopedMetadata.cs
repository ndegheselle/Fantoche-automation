using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Automation.Shared.Data
{
    [Flags]
    public enum EnumScopedType
    {
        Scope,
        Workflow,
        Task
    }

    public class ScopedMetadata : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public EnumScopedType Type { get; set; }
        public string Name { get; set; } = "";
        public string? Color { get; set; }
        public string? Icon { get; set; }

        public bool IsReadOnly { get; set; }

        public ScopedMetadata()
        {
        }

        public ScopedMetadata(EnumScopedType type)
        {
            Type = type;
        }

        public ScopedMetadata(string name, EnumScopedType type)
        {
            Name = name;
            Type = type;
        }

        private void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
