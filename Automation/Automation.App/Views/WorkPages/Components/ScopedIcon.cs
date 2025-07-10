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

        private ScopedMetadata? _previousMetadata;

        public void OnMetadataChanged()
        {
            Text = null;
            if (_previousMetadata != null)
                _previousMetadata.PropertyChanged -= Metadata_PropertyChanged;

            if (Metadata == null)
                return;


            Metadata.PropertyChanged +=Metadata_PropertyChanged;
            if (Metadata.Icon == null)
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
            else
            {
                Text = Metadata.Icon;
            }


            if (string.IsNullOrEmpty(Metadata.Color) == false)
                Foreground = new BrushConverter().ConvertFrom(Metadata.Color) as SolidColorBrush;

            _previousMetadata = Metadata;
        }

        private void Metadata_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnMetadataChanged();
        }
    }
}
