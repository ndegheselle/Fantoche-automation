using System.Windows.Controls;

namespace Automation.App.Features.Workflows.Details.Controls
{
    /// <summary>
    /// Edits the name, tags, icon and color of the <see cref="Shared.Data.Scoped.ScopedMetadata"/> set
    /// as its DataContext. Its inputs are disabled for a read-only one.
    /// </summary>
    public partial class MetadataEditor : UserControl
    {
        public MetadataEditor()
        {
            InitializeComponent();
        }
    }
}
