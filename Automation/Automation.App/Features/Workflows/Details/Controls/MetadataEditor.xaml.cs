using System.Windows.Controls;

namespace Automation.App.Features.Workflows.Details.Controls
{
    /// <summary>
    /// Edits the name, tags, icon and color of the <see cref="ScopedDetailsViewModel{TElement}"/> set as
    /// its DataContext.
    /// </summary>
    public partial class MetadataEditor : UserControl
    {
        public MetadataEditor()
        {
            InitializeComponent();
        }
    }
}
