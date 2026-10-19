using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Feedback;
using Joufflu.Themes;
using AppSettings = Fantoche.App.Settings;

namespace Fantoche.App.Features.Settings
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly AppSettings _settings;
        private readonly IToastService _toasts;

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

        [ObservableProperty]
        private string _sharedFolder;

        public SettingsViewModel(AppSettings settings, IToastService toasts)
        {
            _settings = settings;
            _toasts = toasts;
            _isStandalone = settings.IsStandalone;
            _supervisorUrl = settings.SupervisorUrl;
            _sharedFolder = settings.SharedFolder;
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
            _settings.SharedFolder = SharedFolder;
            _settings.Save();
            _toasts.Success("They will be applied when the application starts again.", "Settings saved");
        }
    }
}
