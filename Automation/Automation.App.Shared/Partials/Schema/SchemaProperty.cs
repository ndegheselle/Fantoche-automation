using System.ComponentModel;
using System.Runtime.CompilerServices;
using Usuel.Shared;

namespace Automation.Models.Schema
{
    public partial class SchemaProperty : ErrorValidationModel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string? name = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }

        public ICustomCommand RemoveCommand { get; set; }

        private string _name = "";
        public partial string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                    return;
                ClearErrors();
                if (_name != value && Parent?.IsPropertyNameUnique(value) == false)
                {
                    AddError($"The property name '{value}' is already used.");
                }
                _name = value;
                NotifyPropertyChanged();
            }
        }

        private EnumPropertyKind _kind;
        public partial EnumPropertyKind Kind
        {
            get => _kind;
            set
            {
                if (_kind == value)
                    return;
                _kind = value;
                ChangeKind(value);
            }
        }

        public uint Depth { get; set; }

        public bool IsHovered { get; set; }

        public bool IsSelected { get; set; }

        public SchemaObject? Parent { get; set; } = null;

        public SchemaProperty(string name, EnumPropertyKind kind)
        {
            RemoveCommand = new DelegateCommand(Remove);
            Name = name;
            Kind = kind;
        }

        /// <summary>
        /// Removes this property from its parent schema object.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Remove()
        {
            if (Parent == null)
                throw new Exception("Can't remove a property that doesn't have a parent.");

            Parent.Remove(this);
        }

        public void ChangeKind(EnumPropertyKind kind)
        {
            if (Parent == null)
                throw new Exception("Can't change kind without a parent.");

            Parent.ChangeChildrenKind(this, kind);
        }
    }

    public partial class SchemaValue : SchemaProperty
    {
        public SchemaValue(string name, EnumDataType type = EnumDataType.String, dynamic? value = null) : base(name, EnumPropertyKind.Value)
        {
            DataType = type;
            Value = value;
        }
    }

    public partial class SchemaArray : SchemaProperty
    {
        public SchemaArray(string name, EnumDataType type = EnumDataType.String) : base(name, EnumPropertyKind.Array) { DataType = type; }
    }

    public partial class SchemaObject : SchemaProperty
    {
        public ICustomCommand AddPropertyCommand { get; set; }

        public SchemaObject(string name) : base(name, EnumPropertyKind.Object)
        { AddPropertyCommand = new DelegateCommand(() => AddValue("default")); }

        #region Add & Remove properties
        /// <summary>
        /// Adds a property to the schema object.
        /// </summary>
        /// <param name="property">Property</param>
        /// <param name="index">Index of the property to add</param>
        /// <returns>This</returns>
        public SchemaObject Add(SchemaProperty property, int index = -1)
        {
            property.Name = GetUniquePropertyName(property.Name);
            property.Depth = Depth + 1;
            property.Parent = this;
            if (index >= 0 && index < Properties.Count)
            {
                Properties.Insert(index, property);
            }
            else if (index == -1 || index >= Properties.Count)
            {
                Properties.Add(property);
            }
            return this;
        }

        /// <summary>
        /// Adds a new object to the schema with the specified name.
        /// </summary>
        /// <param name="name">Property name</param>
        /// <returns>This</returns>
        public SchemaObject AddObject(string name) { return Add(new SchemaObject(name)); }

        /// <summary>
        /// Adds a new value to the schema with the specified name and type.
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="type">Type of the value</param>
        /// <param name="value">Value</param>
        /// <returns>This</returns>
        public SchemaObject AddValue(string name) { return Add(new SchemaValue(name)); }

        /// <summary>
        /// Removes a property from the schema object.
        /// </summary>
        /// <param name="property"></param>
        /// <exception cref="Exception"></exception>
        public void Remove(SchemaProperty property)
        {
            if (Properties.Remove(property))
            {
                property.Parent = null; // Clear parent reference
            }
            else
            {
                throw new Exception("Property not found in the collection.");
            }
        }
        #endregion

        public void ChangeChildrenKind(SchemaProperty children, EnumPropertyKind kind)
        {
            int index = Properties.IndexOf(children);
            if (index == -1)
                return;

            // Remove old property
            Properties.RemoveAt(index);
            SchemaProperty newProperty = kind switch
            {
                EnumPropertyKind.Value => new SchemaValue(children.Name)
                {
                    DataType = (children as SchemaArray)?.DataType ?? EnumDataType.String,
                    Value = (children as SchemaArray)?.Values.FirstOrDefault()
                },
                EnumPropertyKind.Array => new SchemaArray(children.Name)
                {
                    DataType = (children as SchemaValue)?.DataType ?? EnumDataType.String
                },
                EnumPropertyKind.Object => new SchemaObject(children.Name)
                {
                    Properties = (children as SchemaTable)?.Properties ?? []
                },
                EnumPropertyKind.Table => new SchemaTable(children.Name)
                {
                    Properties = (children as SchemaObject)?.Properties ?? []
                },
                _ => throw new ArgumentOutOfRangeException(nameof(kind), $"Unexpected kind value: {kind}"),
            };
        }

        public bool IsPropertyNameUnique(string name)
        {
            return !Properties.Any(
                p => p.Name.ToLowerInvariant().Equals(name.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a unique property name by appending a numeric extension if the name already exists.
        /// </summary>
        /// <param name="baseName">The base name to make unique</param>
        /// <returns>A unique property name</returns>
        private string GetUniquePropertyName(string baseName)
        {
            if (IsPropertyNameUnique(baseName))
                return baseName;

            int counter = 1;
            string uniqueName;
            do
            {
                uniqueName = $"{baseName} {counter}";
                counter++;
            } while (Properties.Any(p => p.Name.Equals(uniqueName, StringComparison.OrdinalIgnoreCase)));

            return uniqueName;
        }
    }

    public partial class SchemaTable : SchemaObject
    {
        public SchemaTable(string name) : base(name)
        {
            Kind = EnumPropertyKind.Table;
        }
    }
}
