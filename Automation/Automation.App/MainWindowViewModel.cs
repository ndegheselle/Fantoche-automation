using Avalonia.Controls;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App
{
    /// <summary>
    /// Shell view-model. Hosts the ShadUI overlay managers (bound by the window's DialogHost /
    /// ToastHost) and drives navigation via the ShadUI Sidebar (CurrentRoute + SelectedPage).
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        // Bound by <shadui:DialogHost Manager="{Binding DialogManager}"/> and
        // <shadui:ToastHost Manager="{Binding ToastManager}"/> in MainWindow.
        public DialogManager DialogManager { get; }
        public ToastManager ToastManager { get; }

        [ObservableProperty]
        private object? _selectedPage;

        [ObservableProperty]
        private string _currentRoute = "work";

        public MainWindowViewModel(DialogManager dialogManager, ToastManager toastManager)
        {
            DialogManager = dialogManager;
            ToastManager = toastManager;
            OpenWork();
        }

        // MIGRATION: real pages (TasksMainPage / WorkerMainPage / PackagesMainPage) are ported in
        // Phase 4. Until then each route shows a placeholder so the shell + navigation are testable.
        [RelayCommand]
        private void OpenWork()
        {
            CurrentRoute = "work";
            SelectedPage = Placeholder("Work (Tasks) — to be ported in Phase 4");
        }

        [RelayCommand]
        private void OpenServers()
        {
            CurrentRoute = "servers";
            SelectedPage = Placeholder("Servers / Workers — to be ported in Phase 4");
        }

        [RelayCommand]
        private void OpenPackages()
        {
            CurrentRoute = "packages";
            SelectedPage = Placeholder("Packages — to be ported in Phase 4");
        }

        private static Control Placeholder(string text) => new TextBlock
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }
}
