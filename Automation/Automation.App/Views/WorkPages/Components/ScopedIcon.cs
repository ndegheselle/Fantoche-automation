using Automation.Shared.Data;
using Joufflu.Shared.Resources.Fonts;
using System.Windows;
using System.Windows.Media;

namespace Automation.App.Views.WorkPages.Components
{
    internal class ScopedIcon : Icon
    {
        #region Dependency Properties
        public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
            nameof(Type),
            typeof(EnumScopedType),
            typeof(ScopedIcon),
            new PropertyMetadata(EnumScopedType.Task, (d, e) => ((ScopedIcon)d).OnScopedChanged()));

        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            nameof(Color),
            typeof(string),
            typeof(ScopedIcon),
            new PropertyMetadata(null));

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(string),
            typeof(ScopedIcon),
            new PropertyMetadata(null));

        #endregion

        public EnumScopedType Type
        {
            get { return (EnumScopedType)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        public string Color
        {
            get { return (string)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        public string Icon
        {
            get { return (string)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public void OnScopedChanged()
        {
            // Use specific icon if provided
            if (string.IsNullOrEmpty(Icon) == false)
            {
                Text = Icon;
            }
            // No specific icon, use default based on type
            else
            {
                switch (Type)
                {
                    case EnumScopedType.Scope:
                        Text = IconFont.Folder; break;
                    case EnumScopedType.Workflow:
                        Text = IconFont.Cubes; break;
                    default:
                        Text = IconFont.Cube; break;
                }
            }

            // Default 
            if (string.IsNullOrEmpty(Color) == false)
                Foreground = new BrushConverter().ConvertFrom(Color) as SolidColorBrush;
        }
    }
}
