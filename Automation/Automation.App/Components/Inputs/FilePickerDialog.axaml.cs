using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace Automation.App.Components.Inputs
{
    public partial class FilePickerDialog : UserControl
    {
        public FilePickerDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        // Browse uses Avalonia's StorageProvider (needs a TopLevel), so it lives in the view.
        private async void Browse_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not FilePickerDialogViewModel viewModel)
                return;

            TopLevel? topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
                return;

            IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions { AllowMultiple = false });

            if (files.Count > 0)
                viewModel.FilePath = files[0].TryGetLocalPath() ?? files[0].Name;
        }
    }
}
