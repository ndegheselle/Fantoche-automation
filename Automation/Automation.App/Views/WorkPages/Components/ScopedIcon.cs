using Automation.Shared.Data;
using Joufflu.Shared.Resources.Fonts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

            string lDefaultIcon = IconFont.Cube;
            switch (Metadata.Type)
            {
                case EnumScopedType.Scope:
                    lDefaultIcon = IconFont.Folder; break;
                case EnumScopedType.Workflow:
                    lDefaultIcon = IconFont.Cubes; break;
            }

            Binding iconBinding = new Binding(nameof(Metadata.Icon));
            iconBinding.Source = Metadata;
            iconBinding.TargetNullValue = lDefaultIcon;
            this.SetBinding(TextProperty, iconBinding);

            Binding colorBinding = new Binding(nameof(Metadata.Color));
            colorBinding.Source = Metadata;
            this.SetBinding(TextProperty, colorBinding);
        }
    }
}
