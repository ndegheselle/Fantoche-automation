using Joufflu.Popups;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Workflows.Editor
{
    /// <summary>
    /// Logique d'interaction pour GraphEditorHelp.xaml
    /// </summary>
    public partial class GraphEditorHelp : UserControl, IModalContent
    {
        public Modal? ParentLayout { get; set; }
        public ModalOptions Options { get; } = new ModalOptions() { Title = "Editor help" };

        public GraphEditorHelp()
        {
            InitializeComponent();
        }
    }
}
