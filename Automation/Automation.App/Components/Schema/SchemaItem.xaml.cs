using Automation.Models.Schema;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Components.Schema
{
    /// <summary>
    /// Template selector to selection an action template from a property type.
    /// Overkill for this kind of use case but nice example of how this work.
    /// </summary>
    public class ActionTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? SchemaPropertyTemplate { get; set; }
        public DataTemplate? SchemaObjectTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is SchemaObject)
                return SchemaObjectTemplate;
            if (item is SchemaProperty)
                return SchemaPropertyTemplate;

            return base.SelectTemplate(item, container);
        }
    }

    /// <summary>
    /// Logique d'interaction pour SchemaItem.xaml
    /// </summary>
    public partial class SchemaItem : UserControl
    {
        #region Dependency properties
        public static readonly DependencyProperty PropertyProperty =
            DependencyProperty.Register(
            nameof(Property),
            typeof(SchemaProperty),
            typeof(SchemaItem),
            new PropertyMetadata(null));

        public static readonly DependencyProperty IsActionsVisibleProperty =
            DependencyProperty.Register(
            nameof(IsActionsVisible),
            typeof(bool),
            typeof(SchemaItem),
            new PropertyMetadata(false));

        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register(
            nameof(IsEditing),
            typeof(bool),
            typeof(SchemaItem),
            new PropertyMetadata(false));
        #endregion

        public SchemaProperty Property
        {
            get { return (SchemaProperty)GetValue(PropertyProperty); }
            set { SetValue(PropertyProperty, value); }
        }

        public bool IsActionsVisible
        {
            get { return (bool)GetValue(IsActionsVisibleProperty); }
            set { SetValue(IsActionsVisibleProperty, value); }
        }

        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            set { SetValue(IsEditingProperty, value); }
        }

        public SchemaItem()
        {
            InitializeComponent();
        }
    }
}
