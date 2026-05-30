using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Automation.App.Components.Inputs
{
    /// <summary>
    /// View for <see cref="TextBoxDialogViewModel"/>. Instantiated by the ViewLocator; its
    /// DataContext is the view-model supplied to DialogManager.CreateDialog(vm).
    /// </summary>
    public partial class TextBoxDialog : UserControl
    {
        public TextBoxDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
