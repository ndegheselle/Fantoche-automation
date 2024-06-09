using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Automation.App.ViewModels
{
    public enum EnumTheme
    {
        Dark,
        Light
    }

    public class ParametersContext : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        private EnumTheme theme = EnumTheme.Dark;
        public EnumTheme Theme
        {
            get => theme;
            set
            {
                if (theme == value)
                    return;

                theme = value;
                _themesHandler.LoadTheme(theme);
                OnPropertyChanged();
            }
        }

        private readonly ThemesHandler _themesHandler = new ThemesHandler();

    }

    public class ThemesHandler
    {
        public Dictionary<EnumTheme, List<string>> ThemesDictionnaries
        {
            get;
            set;
        } = new Dictionary<EnumTheme, List<string>>()
        {
            {
                EnumTheme.Dark,
                new List<string>()
                {
                    "pack://application:,,,/Nodify;component/Themes/Dark.xaml",
                    "pack://application:,,,/AdonisUI;component/ColorSchemes/Dark.xaml"
                }
            },
            {
                EnumTheme.Light,
                new List<string>()
                {
                    "pack://application:,,,/Nodify;component/Themes/Light.xaml",
                    "pack://application:,,,/AdonisUI;component/ColorSchemes/Light.xaml"
                }
            }
        };

        private void ClearThemes()
        {
            for (int i = App.Current.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                var dictionary = App.Current.Resources.MergedDictionaries[i];
                // If contained in the themes dictionnaries, remove it
                if (ThemesDictionnaries.Values.SelectMany(x => x).Contains(dictionary.Source?.ToString()))
                    App.Current.Resources.MergedDictionaries.Remove(dictionary);
            }
        }

        public void LoadTheme(EnumTheme theme)
        {
            ClearThemes();

            foreach (var dictionary in ThemesDictionnaries[theme])
            {
                App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(dictionary) });
            }
        }
    }
}
