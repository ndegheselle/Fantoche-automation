using Automation.Models.Schema;
using Joufflu.Shared.Resources.Fonts;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Components.Schema
{
    /// <summary>
    /// Logique d'interaction pour DataTypeIcon.xaml
    /// </summary>
    public partial class IconPropertyKind : Control, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(
            nameof(Type),
            typeof(EnumPropertyKind),
            typeof(IconPropertyKind),
            new PropertyMetadata(EnumDataType.String, (d, e) => ((IconPropertyKind)d).OnTypeChanged()));

        /// <summary>
        /// Type of the value represented by this icon.
        /// </summary>
        public EnumPropertyKind Type
        {
            get { return (EnumPropertyKind)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        public string Icon { get; set; } = IconFont.Quotes;

        /// <summary>
        /// Change the icon based on the type of value.
        /// </summary>
        private void OnTypeChanged()
        {
            Icon = Type switch
            {
                EnumPropertyKind.Value => IconFont.Quotes,
                EnumPropertyKind.Array => IconFont.BracketsSquare,
                EnumPropertyKind.Object => IconFont.BracketsCurly,
                EnumPropertyKind.Table => IconFont.Table,
                _ => IconFont.QuestionMark
            };
        }
    }
}
