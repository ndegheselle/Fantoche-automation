using Automation.Models.Schema;
using Joufflu.Shared.Resources.Fonts;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Components.Schema
{
    /// <summary>
    /// Logique d'interaction pour SchemaValueEdit.xaml
    /// </summary>
    public partial class SchemaValueEdit : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string? name = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        #endregion

        #region DependencyProperty
        public static readonly DependencyProperty ValueElementProperty =
            DependencyProperty.Register(
            nameof(ValueElement),
            typeof(ISchemaValue),
            typeof(SchemaValueEdit),
            new PropertyMetadata(null));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(SchemaValueEdit), new PropertyMetadata(false));
        #endregion

        public ISchemaValue ValueElement
        {
            get { return (ISchemaValue)GetValue(ValueElementProperty); }
            set { SetValue(ValueElementProperty, value); }
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public string Icon { get; set; } = IconFont.Quotes;

        public SchemaValueEdit() { InitializeComponent(); }
    }
}