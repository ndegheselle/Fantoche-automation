using Automation.App.Components.Inputs;
using Automation.Shared.Data.Scoped;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ShadUI;

namespace Automation.App.Views.WorkPages.Components
{
    /// <summary>
    /// MIGRATION: metadata editor (name / icon / color / tags) for a ScopedMetadata. The icon and
    /// color buttons open the ShadUI SelectIconDialog / SelectColorDialog. Reused by the
    /// create/edit modals. ScopedMetadata is Fody-woven, so edits propagate to the ScopedIcon preview.
    /// </summary>
    public partial class MetadataEdit : UserControl
    {
        public static readonly StyledProperty<ScopedMetadata?> MetadataProperty =
            AvaloniaProperty.Register<MetadataEdit, ScopedMetadata?>(nameof(Metadata));

        public ScopedMetadata? Metadata
        {
            get => GetValue(MetadataProperty);
            set => SetValue(MetadataProperty, value);
        }

        private readonly DialogManager _dialogManager;

        public MetadataEdit()
        {
            _dialogManager = AppServices.Provider.GetRequiredService<DialogManager>();
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void ButtonEditColor_Click(object? sender, RoutedEventArgs e)
        {
            if (Metadata is null)
                return;

            var vm = new SelectColorDialogViewModel(_dialogManager, Metadata.Color);
            _dialogManager.CreateDialog(vm)
                .Dismissible()
                .WithSuccessCallback(v => Metadata.Color = v.Hex)
                .Show();
        }

        private void ButtonEditIcon_Click(object? sender, RoutedEventArgs e)
        {
            if (Metadata is null)
                return;

            var vm = new SelectIconDialogViewModel(_dialogManager, Metadata.Icon);
            _dialogManager.CreateDialog(vm)
                .Dismissible()
                .WithSuccessCallback(v => Metadata.Icon = v.Selected?.Kind.ToString())
                .Show();
        }

        private void AddTag_Click(object? sender, RoutedEventArgs e)
        {
            string tag = NewTagBox.Text?.Trim() ?? string.Empty;
            if (Metadata is null || string.IsNullOrEmpty(tag) || Metadata.Tags.Contains(tag))
                return;

            Metadata.Tags.Add(tag);
            NewTagBox.Text = string.Empty;
        }

        private void RemoveTag_Click(object? sender, RoutedEventArgs e)
        {
            if (Metadata is not null && sender is Control { DataContext: string tag })
                Metadata.Tags.Remove(tag);
        }
    }
}
