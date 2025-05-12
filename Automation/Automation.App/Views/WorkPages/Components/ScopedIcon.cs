using Automation.App.Shared.ViewModels.Work;
using Automation.Shared.Data;
using Joufflu.Shared.Resources.Fonts;
using System.Windows;
using System.Windows.Media;

namespace Automation.App.Views.WorkPages.Components
{
    internal class ScopedIcon : Icon
    {
        public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
            nameof(Scoped),
            typeof(ScopedElement),
            typeof(ScopedIcon),
            new PropertyMetadata(null, (d, e) => ((ScopedIcon)d).OnScopedChanged()));

        public ScopedElement Scoped
        {
            get { return (ScopedElement)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        public string Icon
        {
            get
            {
                if (string.IsNullOrEmpty(Scoped.Icon) == false)
                    return Scoped.Icon;
                switch (Scoped.Type)
                {
                    case EnumScopedType.Scope:
                        return IconFont.Folder;
                    case EnumScopedType.Workflow:
                        return IconFont.Cubes;
                }
                return IconFont.Cube;
            }
        }

        public SolidColorBrush? Color 
        {
            get
            {
                if (string.IsNullOrEmpty(Scoped.Color) == false)
                    return new BrushConverter().ConvertFrom(Scoped.Color) as SolidColorBrush;
                return null;
            }
        }

        private void OnScopedChanged()
        {
            Text = Icon;
            if (Color != null)
                Foreground = Color;
        }
    }
}
