using System.Windows.Controls;
using System.Windows.Input;

namespace Automation.App.Features.Workflows.Editor
{
    /// <summary>
    /// Logique d'interaction pour TaskSettingsOverlay.xaml
    /// </summary>
    public partial class TaskSettingsOverlay : UserControl
    {
        public TaskSettingsOverlay()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Write the reference of the selected value where the mapping is being typed : what a node
        /// reads is a list to pick from rather than a path to remember.
        /// </summary>
        private void OnContextDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ContextTree.SelectedItem is not ContextEntry entry
                || string.IsNullOrEmpty(entry.Reference)
                || MappingTextBox.IsReadOnly)
                return;

            // Written as JSON : a reference stands where a value stands, quotes included.
            string reference = $"\"{entry.Reference}\"";
            int caret = MappingTextBox.SelectionStart;

            MappingTextBox.Text = MappingTextBox.Text
                .Remove(caret, MappingTextBox.SelectionLength)
                .Insert(caret, reference);

            MappingTextBox.SelectionStart = caret + reference.Length;
            MappingTextBox.SelectionLength = 0;
            MappingTextBox.Focus();
        }
    }
}
