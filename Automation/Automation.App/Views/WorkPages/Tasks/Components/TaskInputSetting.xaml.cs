using Automation.App.Shared.ApiClients;
using Automation.Models.Work;
using Joufflu.Popups;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Controls;
using Usuel.Shared;

namespace Automation.App.Views.WorkPages.Tasks.Components
{
    /// <summary>
    /// Logique d'interaction pour TaskInputSetting.xaml
    /// </summary>
    public partial class TaskInputSetting : UserControl, IModalContent
    {
        public ModalOptions Options { get; private set; } = new ModalOptions();
        public IModal? ParentLayout { get; set; }

        public GraphTask Task { get; private set; }

        public ICustomCommand CancelCommand { get; private set; }
        public ICustomCommand ValidateCommand { get; private set; }

        private IAlert _alert => this.GetCurrentAlert();
        private readonly string? _originalSettings;

        public TaskInputSetting(GraphTask task) {
            Task = task;
            _originalSettings = Task.InputJson;

            if (string.IsNullOrEmpty(Task.InputJson))
                Task.InputJson = Task.InputSchema?.ToSampleJson().ToString();

            Options.Title = $"{Task.Name} - settings";
            CancelCommand = new DelegateCommand(Cancel);
            ValidateCommand = new DelegateCommand(Validate);
            InitializeComponent();
        }

        public void Cancel()
        {
            Task.InputJson = _originalSettings;
            ParentLayout?.Hide();
        }

        public void Validate()
        {
            if (string.IsNullOrEmpty(Task.InputJson))
                return;

            Task.InputSchema?.Validate(Task.InputJson);
            _alert.Success("Settings changed !");
            ParentLayout?.Hide(true);
        }
    }
}