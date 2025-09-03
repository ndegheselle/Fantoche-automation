using Automation.Models.Schema;
using Joufflu.Data.DnD;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Automation.App.Components.Schema
{
    /// <summary>
    /// Handle schema property dragging. 
    /// </summary>
    public class SchemaDragHandler : DragHandler
    {
        private readonly Schema.SchemaDropHandler _dropHandler;
        public SchemaDragHandler(FrameworkElement parent, Schema.SchemaDropHandler dropHandler) : base(parent)
        {
            _dropHandler = dropHandler;
        }

        protected override void OnDragFinished()
        {
            _dropHandler.StopHovering();
        }
    }

    /// <summary>
    /// Handle schema property droping.
    /// </summary>
    public class SchemaDropHandler : DropHandler<SchemaValueProperty>
    {
        private SchemaValueProperty? _hoveredProperty = null;
        private readonly Schema.SchemaEdit _schema;
        public SchemaDropHandler(Schema.SchemaEdit schema)
        {
            _schema = schema;
        }

        public void StopHovering()
        {
            if (_hoveredProperty == null)
                return;
            _hoveredProperty.IsHovered = false;
            _hoveredProperty = null;
        }

        protected override bool IsDropAuthorized(DragEventArgs e)
        {
            if (_schema.IsReadOnly)
                return false;

            var source = GetDropData<SchemaValueProperty>(e.Data);
            var target = ((FrameworkElement)e.OriginalSource).DataContext as SchemaValueProperty;
            return base.IsDropAuthorized(e) && source != target;
        }

        protected override void OnPassingOver(DragEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is not SchemaValueProperty property)
                return;

            if (_hoveredProperty != null)
                _hoveredProperty.IsHovered = false;

            _hoveredProperty = property;
            _hoveredProperty.IsHovered = true;
        }

        /// <summary>
        /// Move the property to the target property position.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="e"></param>
        protected override void ApplyDrop(SchemaValueProperty? data, DragEventArgs e)
        {
            if (data == null)
                return;
            if (e.OriginalSource is not FrameworkElement target)
                return;
            if (target.DataContext is not SchemaValueProperty property)
                return;

            StopHovering();

            data.Parent?.Remove(data);
            property.Parent?.Add(data, property.Parent?.Properties.IndexOf(property) ?? 0);
            data.IsSelected = true;
        }
    }

    /// <summary>
    /// Logique d'interaction pour SchemaEdit.xaml
    /// </summary>
    public partial class SchemaEdit : Control
    {

        #region Dependency properties
        public static readonly DependencyProperty RootProperty =
            DependencyProperty.Register(
            nameof(Root),
            typeof(SchemaObject),
            typeof(SchemaEdit),
            new PropertyMetadata(null));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
            nameof(IsReadOnly),
            typeof(bool),
            typeof(SchemaEdit),
            new PropertyMetadata(false));
        #endregion

        public SchemaObject Root
        {
            get { return (SchemaObject)GetValue(RootProperty); }
            set { SetValue(RootProperty, value); }
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public SchemaDragHandler DragHandler { get; }
        public SchemaDropHandler DropHandler { get; }

        public SchemaEdit()
        {
            DropHandler = new Schema.SchemaDropHandler(this);
            DragHandler = new Schema.SchemaDragHandler(this, DropHandler);
        }
    }
}
