using Automation.Shared.Data;
using Joufflu.Shared.Resources.Fonts;
using System.Windows;
using System.Windows.Media;

namespace Automation.App.Views.WorkPages.Components
{
    internal class ScopedIcon : Icon
    {
        #region Dependency Properties
        public static readonly DependencyProperty MetadataProperty = DependencyProperty.Register(
            nameof(Metadata),
            typeof(ScopedMetadata),
            typeof(ScopedIcon),
            new PropertyMetadata(null, (d, e) => ((ScopedIcon)d).OnMetadataChanged()));

        #endregion

        public ScopedMetadata Metadata
        {
            get { return (ScopedMetadata)GetValue(MetadataProperty); }
            set { SetValue(MetadataProperty, value); }
        }

        public void OnMetadataChanged()
        {
            if (Metadata == null)
                return;

            // Use specific icon if provided
            if (string.IsNullOrEmpty(Metadata.Icon) == false)
            {
                Text = Metadata.Icon;
            }
            // No specific icon, use default based on type
            else
            {
                switch (Metadata.Type)
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
            if (string.IsNullOrEmpty(Metadata.Color) == false)
                Foreground = new BrushConverter().ConvertFrom(Metadata.Color) as SolidColorBrush;
        }
    }
}
