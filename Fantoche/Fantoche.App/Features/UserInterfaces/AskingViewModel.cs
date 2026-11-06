using System.Windows;
using Fantoche.App.Features.Workflows.Controls;
using Fantoche.Plugins.Shared.Ui;
using Fantoche.Shared.Data.Execution;
using Fantoche.Worker.UserInterfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;

namespace Fantoche.App.Features.UserInterfaces
{
    /// <summary>
    /// A task asking the user, as its window shows it : the interface of the task (see
    /// <see cref="ITaskInterface"/>), or the form generated from its parameters when it has none, when
    /// it doesn't handle a run, or when it can't be loaded.
    /// </summary>
    public partial class AskingViewModel : ObservableObject
    {
        private readonly PageRequest _request;
        private readonly ITaskInterface? _interface;

        public string Title => _request.Instance.NodeName;

        /// <summary>The view of the interface of the task, null for the generated form.</summary>
        public FrameworkElement? View { get; }

        /// <summary>The generated form, null when the interface of the task is shown.</summary>
        public ParametersForm? Form { get; }

        /// <summary>Why the interface of the task isn't shown although it has one.</summary>
        public string? LoadError { get; }

        /// <summary>Why the last answer was refused.</summary>
        [ObservableProperty] private string? _error;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private bool _isAnswering;

        /// <summary>The task took the answer : the window has nothing left to show.</summary>
        public event Action? Answered;

        /// <summary>The user closes it unanswered : the task asks on, and it can be opened again.</summary>
        public event Action? Later;

        private AskingViewModel(PageRequest request, ITaskInterface? taskInterface, FrameworkElement? view, string? loadError)
        {
            _request = request;
            _interface = taskInterface;
            View = view;
            LoadError = loadError;
            if (view != null)
                return;

            Form = new ParametersForm(request.Task, request.Input);
            Form.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ParametersForm.HasErrors))
                    ConfirmCommand.NotifyCanExecuteChanged();
            };
        }

        /// <summary>What [request] shows, its interface downloaded if needed. On the UI thread : the view is built here.</summary>
        public static async Task<AskingViewModel> CreateAsync(PageRequest request, TaskInterfaces interfaces)
        {
            if (request.Task.Interface is not PackageClassTarget target)
                return new AskingViewModel(request, null, null, null);

            try
            {
                ITaskInterface taskInterface = await interfaces.CreateAsync(target);
                if (!taskInterface.Handles(EnumInterfaceMode.Execution))
                    return new AskingViewModel(request, null, null, null);

                TaskInterfaceContext context = new()
                {
                    Mode = EnumInterfaceMode.Execution,
                    Parameters = request.Input,
                    Runtime = request.Runtime,
                };
                FrameworkElement view = taskInterface.Show(context);
                return new AskingViewModel(request, taskInterface, view, null);
            }
            catch (Exception exception)
            {
                // The run isn't held up by an interface that can't be shown : its parameters can still
                // be answered.
                string error = $"The interface of the task can't be shown, the form of its parameters is instead : {exception.Message}";
                return new AskingViewModel(request, null, null, error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanConfirm))]
        private async Task Confirm()
        {
            Error = null;
            IsAnswering = true;
            try
            {
                JToken? answer = _interface == null ? Form!.ToToken() : await _interface.ReadAsync();
                string? refusal = _request.Answer(answer ?? JValue.CreateNull());
                if (refusal != null)
                {
                    Error = refusal;
                    return;
                }
                Answered?.Invoke();
            }
            catch (Exception exception)
            {
                // A TaskInterfaceException tells what is left to fix ; anything else is a bug of the
                // interface, shown the same way rather than closing what the user filled in.
                Error = exception.Message;
            }
            finally
            {
                IsAnswering = false;
            }
        }

        private bool CanConfirm() => !IsAnswering && Form?.HasErrors != true;

        [RelayCommand]
        private void AnswerLater() => Later?.Invoke();
    }
}
