using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Infrastructure
{
    /// <summary>
    /// Resolves a view-model instance to its view by convention: the view type is the view-model
    /// type name with the "ViewModel" suffix removed, in the same namespace and assembly
    /// (e.g. ...Inputs.TextBoxDialogViewModel -> ...Inputs.TextBoxDialog).
    ///
    /// Registered in App.axaml &lt;Application.DataTemplates&gt;. Used wherever a view-model is shown as
    /// content: ShadUI dialogs (DialogManager.CreateDialog(vm)) and the shell content host.
    /// Views whose DataContext is assigned directly (e.g. a UserControl that news up its own VM)
    /// are not affected, since the locator only runs when a VM is used as Content.
    /// </summary>
    public class ViewLocator : IDataTemplate
    {
        public Control Build(object? data)
        {
            if (data is null)
                return new TextBlock { Text = "(null view-model)" };

            string vmName = data.GetType().FullName!;
            string viewName = vmName.EndsWith("ViewModel", StringComparison.Ordinal)
                ? vmName[..^"ViewModel".Length]
                : vmName;

            Type? viewType = data.GetType().Assembly.GetType(viewName);
            if (viewType is null)
                return new TextBlock { Text = $"View not found: {viewName}" };

            return (Control)Activator.CreateInstance(viewType)!;
        }

        public bool Match(object? data) => data is ObservableObject;
    }
}
