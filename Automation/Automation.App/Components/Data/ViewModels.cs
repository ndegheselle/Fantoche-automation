using Automation.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Automation.App.Components.Data
{
    public class DataNode : BaseNotifyDataError, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public DataTree? Parent { get; set; }

        private string _key;
        public string Key
        {
            get => _key;
            set
            {
                this.ClearErrors();
                if (String.IsNullOrEmpty(value))
                    this.AddError("Key cannot be empty.");
                if (Parent?.Childrens.Any(x => x.Key == value) == true)
                    this.AddError($"'{value}' is already used in parent.");

                if (this.HasErrors)
                    return;
                _key = value;
                OnPropertyChanged();
            }
        }

        public string Path
        {
            get
            {
                if (Parent == null)
                    return Key.ToString() ?? "<unknow>";

                return $"{Parent.Path}.{Key}";
            }
        }
    }

    public class DataValue : DataNode
    {
        // TODO : handle multiples types (string, Guid, decimal, bool, datetime, timespan) with different controls + serialization
        // May use these classes for a filter system
        public string? Value { get; set; }
        public DataValue()
        {
        }

        public DataValue(string key, string? value)
        {
            Key = key;
            Value = value;
        }
    }

    public enum EnumTreeType
    {
        Dictionnary,
        List
    }

    public class DataTree : DataNode
    {
        public EnumTreeType Type { get; set; }
        public ObservableCollection<DataNode> Childrens { get; set; } = [];
        public ListCollectionView SortedChildrens { get; set; }

        public DataTree()
        {
            SortedChildrens = (ListCollectionView)CollectionViewSource.GetDefaultView(Childrens);
            SortedChildrens.SortDescriptions.Add(new SortDescription(nameof(Key), ListSortDirection.Ascending));
        }
    }
}
