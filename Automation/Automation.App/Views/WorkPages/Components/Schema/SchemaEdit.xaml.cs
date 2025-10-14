using Automation.Models.Work;
using NuGet.Protocol;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Components.Schema
{
    /// <summary>
    /// Logique d'interaction pour SchemaEdit.xaml
    /// </summary>
    public partial class SchemaEdit : UserControl
    {
        public static readonly DependencyProperty TaskProperty =
            DependencyProperty.Register(
            nameof(Task),
            typeof(AutomationTask),
            typeof(SchemaEdit),
            new PropertyMetadata(null, (o, d) => ((SchemaEdit)o).OnTaskChanged()));

        public AutomationTask Task
        {
            get { return (AutomationTask)GetValue(TaskProperty); }
            set { SetValue(TaskProperty, value); }
        }

        public string InputJson { get; set; } = "";
        public string OutputJson { get; set; } = "";

        public bool IsReadOnly { get; set; }

        public SchemaEdit()
        {
            InitializeComponent();
            Task.Inputs.First().ToJson(Newtonsoft.Json.Formatting.Indented);
        }

        private void OnTaskChanged()
        {
            if (Task == null)
                return;

            InputJson = Task.Inputs.First().SchemaJson;
            OutputJson = Task.Outputs.First().SchemaJson;
        }
    }
}
