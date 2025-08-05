using Automation.App.Components.Inputs;
using Automation.App.Shared.ApiClients;
using Automation.Shared.Data;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Components
{
    /// <summary>
    /// Logique d'interaction pour MetadataEdit.xaml
    /// </summary>
    public partial class MetadataEdit : UserControl, INotifyPropertyChanged
    {
        #region Dependency Properties

        public static readonly DependencyProperty MetadataProperty = DependencyProperty.Register(
            nameof(Metadata),
            typeof(ScopedMetadata),
            typeof(MetadataEdit),
            new PropertyMetadata(null));

        #endregion

        public ScopedMetadata Metadata
        {
            get { return (ScopedMetadata)GetValue(MetadataProperty); }
            set { SetValue(MetadataProperty, value); }
        }

        public IEnumerable<string> Tags { get; private set; } = ["fafa"];

        private IModal _modal => this.GetCurrentModal();
        private readonly TasksClient _tasksClient;

        public MetadataEdit() {
            _tasksClient = Services.Provider.GetRequiredService<TasksClient>();
            this.Loaded += OnLoaded;
            InitializeComponent(); 
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Tags = await _tasksClient.GetTagsAsync();
        }

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
