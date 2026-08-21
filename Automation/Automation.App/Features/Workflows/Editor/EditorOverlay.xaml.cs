using System.Windows.Controls;

namespace Automation.App.Features.Workflows.Editor
{
    /// <summary>
    /// Layer displayed on top of the editor, holding its buttons.The root is not hit testable so
    /// that the empty areas keep panning and selecting on the editor below, every panel meant to
    /// hold buttons has to re-enable it.
    /// </summary>
    public partial class EditorOverlay : UserControl
    {
        public EditorOverlay()
        {
            InitializeComponent();
        }
    }
}
