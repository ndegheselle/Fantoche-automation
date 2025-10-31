using Automation.Models.Work;
using Automation.Shared.Data;
using Joufflu.Popups;
using System.Windows.Controls;
using Usuel.Shared;

namespace Automation.App.Views.WorkPages.Workflows.Editor.Components
{
    /// <summary>
    /// Logique d'interaction pour TaskInputSettingModal.xaml
    /// </summary>
    public partial class TaskInputSettingModal : UserControl, IModalContent
    {
        public ModalOptions Options { get; private set; } = new ModalOptions();
        public IModal? ParentLayout { get; set; }

        public BaseGraphTask Task { get; private set; }

        public ICustomCommand CancelCommand { get; private set; }
        public ICustomCommand ValidateCommand { get; private set; }

        private IAlert _alert => this.GetCurrentAlert();
        private readonly string? _originalSettings;

        public TaskInputSettingModal(BaseGraphTask task) {
            Task = task;
            _originalSettings = Task.InputJson;

            if (string.IsNullOrEmpty(Task.InputJson))
                Task.InputJson = Task.InputSchema?.ToSampleJson().ToString();

            Options.Title = $"{Task.Name} - settings";
            CancelCommand = new DelegateCommand(Cancel);
            ValidateCommand = new DelegateCommand(Validate, () => string.IsNullOrEmpty(Task.InputJson) == false);
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

            // TODO : before validate the context references should be modified

            string contextualizedInput = ContextHandler.ReplaceContext(Task.InputJson, "");
            var errors = Task.InputSchema?.Validate(contextualizedInput);
            if (errors?.Count > 0)
            {
                _alert.Error("The task settings doesn't correspond to the task schema.");
                return;
            }

            _alert.Success("Settings changed !");
            ParentLayout?.Hide(true);
        }

        #region UI events
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateCommand.RaiseCanExecuteChanged();
        }
        #endregion
    }
}