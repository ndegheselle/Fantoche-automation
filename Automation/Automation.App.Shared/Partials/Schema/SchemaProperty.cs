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

        public uint Depth { get; set; }

        public bool IsHovered { get; set; }

        public bool IsSelected { get; set; }

        public SchemaObject? Parent { get; set; } = null;

        public SchemaProperty(string name)
        {
            RemoveCommand = new DelegateCommand(Remove);
            Name = name;
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
    }

    public partial class SchemaObjectProperty
    {
        public ICustomCommand AddValueCommand { get; set; }
        public ICustomCommand AddArrayCommand { get; set; }
        public ICustomCommand AddObjectCommand { get; set; }
        public ICustomCommand AddTableCommand { get; set; }
    }

    public partial class SchemaObject : ISchemaElement
    {
        #region Add & Remove properties
        /// <summary>
        /// Adds a property to the schema object.
        /// </summary>
        /// <param name="property">Property</param>
        /// <param name="index">Index of the property to add</param>
        /// <returns>This</returns>
        public SchemaObject Add(SchemaProperty property, uint depth, int index = -1)
        {
            property.Name = GetUniquePropertyName(property.Name);
            property.Depth = depth;
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

        public SchemaObject AddValue(string name, uint depth) 
        { return Add(new SchemaValueProperty(name, new SchemaValue(EnumDataType.String)), depth); }

        public SchemaObject AddArray(string name, uint depth)
        { return Add(new SchemaValueProperty(name, new SchemaArray(EnumDataType.String)), depth); }

        public SchemaObject AddObject(string name, uint depth)
        { return Add(new SchemaObjectProperty(name, new SchemaObject()), depth); }

        public SchemaObject AddTable(string name, uint depth)
        { return Add(new SchemaObjectProperty(name, new SchemaTable()), depth); }

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
}
