using Automation.App.Components.Inputs;
using Automation.Shared.Data;
using Joufflu.Popups;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Components
{
    /// <summary>
    /// Logique d'interaction pour MetadataEdit.xaml
    /// </summary>
    public partial class MetadataEdit : UserControl
    {
        public static readonly DependencyProperty MetadataProperty = DependencyProperty.Register(
            nameof(Metadata),
            typeof(ScopedMetadata),
            typeof(MetadataEdit),
            new PropertyMetadata(null));

        public ScopedMetadata Metadata
        {
            get { return (ScopedMetadata)GetValue(MetadataProperty); }
            set { SetValue(MetadataProperty, value); }
        }

        private IModal _modal => this.GetCurrentModal();

        public MetadataEdit() { InitializeComponent(); }

        #region UI events
        private async void ButtonEditIcon_Click(object sender, RoutedEventArgs e)
        {
            SelectIconModal modal = new SelectIconModal();
            if (await _modal.Show(modal) && modal.Selected != null)
            {
                Metadata.Icon = modal.Selected.Icon;
            }
        }

        private async void ButtonEditColor_Click(object sender, RoutedEventArgs e)
        {
            SelectColorModal modal = new SelectColorModal();
            if (await _modal.Show(modal))
            {
                Metadata.Color = modal.Selected;
            }
        }
        #endregion
    }
}
