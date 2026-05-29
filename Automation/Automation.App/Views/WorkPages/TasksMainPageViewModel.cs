using Automation.App.Services.Abstractions;
using Automation.App.ViewModels.Scoped;
using Automation.App.Views.WorkPages.Scopes;
using Automation.Shared.Data.Scoped;
using Avalonia.Controls;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using ShadUI;

namespace Automation.App.Views.WorkPages
{
    /// <summary>
    /// MIGRATION: the "Work" page (was TasksMainPage : ILayout). Composes the scope selector with a
    /// content area that swaps in the page for the active element. Resolved to TasksMainPage by the
    /// ViewLocator and shown in the shell's content host.
    /// </summary>
    public partial class TasksMainPageViewModel : ObservableObject
    {
        private readonly DialogManager _dialogManager;
        private readonly ToastManager _toastManager;
        private readonly IScopesService _scopes;

        /// <summary>Right-pane content: a page view-model (resolved by the ViewLocator) or a placeholder.</summary>
        [ObservableProperty]
        private object? _content;

        /// <summary>The displayed scope, for the breadcrumb.</summary>
        [ObservableProperty]
        private Scope? _currentScope;

        public TasksMainPageViewModel(DialogManager dialogManager, ToastManager toastManager, IScopesService scopes)
        {
            _dialogManager = dialogManager;
            _toastManager = toastManager;
            _scopes = scopes;
        }

        /// <summary>Shows the page for the selected element (or the current scope when nothing is selected).</summary>
        public void ShowElement(ScopedElementViewModel? element)
        {
            switch (element)
            {
                case ScopeViewModel scope:
                    Content = new ScopePageViewModel(scope, _dialogManager, _toastManager, _scopes);
                    break;
                case null:
                    Content = null;
                    break;
                default:
                    // TODO: TaskPage / WorkflowPage not yet ported.
                    Content = new TextBlock
                    {
                        Margin = new Avalonia.Thickness(16),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = $"{element.Type} page — to be ported (Phase 4)."
                    };
                    break;
            }
        }
    }
}
