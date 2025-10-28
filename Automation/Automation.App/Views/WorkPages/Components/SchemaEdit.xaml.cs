using Automation.App.Shared.ApiClients;
using Automation.Models.Work;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Components
{
    /// <summary>
    /// Logique d'interaction pour SchemaEdit.xaml
    /// </summary>
    public partial class SchemaEdit : UserControl
    {
        public static readonly DependencyProperty TaskProperty =
            DependencyProperty.Register(
            nameof(Task),
            typeof(BaseAutomationTask),
            typeof(SchemaEdit),
            new PropertyMetadata(null));

        public BaseAutomationTask Task
        {
            get { return (BaseAutomationTask)GetValue(TaskProperty); }
            set { SetValue(TaskProperty, value); }
        }

        private readonly TasksClient _taskClient;
        public SchemaEdit()
        {
            _taskClient = Services.Provider.GetRequiredService<TasksClient>();
            InitializeComponent();
        }

        private async void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            await _taskClient.UpdateAsync(Task.Id, Task);
        }
    }
}
