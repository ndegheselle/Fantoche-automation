using Automation.App.Views.WorkPages;
using Avalonia.Controls;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
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

        private readonly IServiceProvider _services;

        public MainWindowViewModel(DialogManager dialogManager, ToastManager toastManager, IServiceProvider services)
        {
            DialogManager = dialogManager;
            ToastManager = toastManager;
            _services = services;
            OpenWork();
        }

        // MIGRATION: Servers / Packages pages are ported later in Phase 4 (placeholders for now).
        [RelayCommand]
        private void OpenWork()
        {
            CurrentRoute = "work";
            SelectedPage = _services.GetRequiredService<TasksMainPageViewModel>();
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
            SelectedPage = _services.GetRequiredService<Views.PackagesPages.PackagesMainPageViewModel>();
        }

        private static Control Placeholder(string text) => new TextBlock
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }
}
