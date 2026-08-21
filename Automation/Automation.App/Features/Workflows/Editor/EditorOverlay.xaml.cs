using System.Windows;
using System.Windows.Controls;
using Nodify;

namespace Automation.App.Features.Workflows.Editor
{
    /// <summary>
    /// Logique d'interaction pour EditorOverlay.xaml
    /// </summary>
    public partial class EditorOverlay : UserControl
    {
        public static readonly DependencyProperty EditorProperty = DependencyProperty.Register(
            nameof(Editor),
            typeof(NodifyEditor),
            typeof(EditorOverlay));

        /// <summary>
        /// Editor the overlay acts on, the buttons using the <see cref="EditorCommands"/> needing
        /// it as their command target since the overlay is a sibling of the editor.
        /// </summary>
        public NodifyEditor? Editor
        {
            get => (NodifyEditor?)GetValue(EditorProperty);
            set => SetValue(EditorProperty, value);
        }

        public EditorOverlay()
        {
            InitializeComponent();
        }
    }
}
