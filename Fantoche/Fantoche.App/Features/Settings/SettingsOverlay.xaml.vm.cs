using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Feedback;
using Joufflu.Navigation;
using Joufflu.Themes;
using AppSettings = Fantoche.App.Settings;

namespace Fantoche.App.Features.Settings
{
    public partial class SettingsViewModel : ObservableObject, IOverlayContent
    {
        private readonly AppSettings _settings;
        private readonly IToastService _toasts;
        private readonly IOverlayer _overlayer;

        public OverlayOptions Options { get; } = new() { Title = "Settings" };

        public ReadOnlyObservableCollection<string> Themes => ThemeManager.Instance.Themes;

        /// <summary>Applied and saved as soon as it is picked.</summary>
        public string Theme
        {
            get => ThemeManager.Instance.Theme;
            set
            {
                ThemeManager.Instance.Theme = value;
                _settings.Theme = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }

        [ObservableProperty]
        private bool _isStandalone;

        [ObservableProperty]
        private string _supervisorUrl;

        public SettingsViewModel(AppSettings settings, IToastService toasts, IOverlayer overlayer)
        {
            _settings = settings;
            _toasts = toasts;
            _overlayer = overlayer;
            _isStandalone = settings.IsStandalone;
            _supervisorUrl = settings.SupervisorUrl;
        }

        /// <summary>Built on each opening so unsaved edits of a cancelled one are dropped.</summary>
        public static Task<bool?> ShowAsync()
        {
            SpineViewModel shell = SpineViewModel.Instance;
            var settings = new SettingsViewModel(shell.Settings, shell.Toasting, shell.Overlayer);
            return shell.Overlayer.ShowAsync(settings);
        }

        [RelayCommand]
        private void Save()
        {
            if (!IsStandalone && !Uri.TryCreate(SupervisorUrl, UriKind.Absolute, out _))
            {
                _toasts.Error($"'{SupervisorUrl}' is not a valid address.", "The settings were not saved");
                return;
            }

            _settings.IsStandalone = IsStandalone;
            _settings.SupervisorUrl = SupervisorUrl;
            _settings.Save();
            _toasts.Success("They will be applied when the application starts again.", "Settings saved");
            _overlayer.Validate(this);
        }

        [RelayCommand]
        private void Cancel() => _overlayer.Cancel(this);
    }
}
