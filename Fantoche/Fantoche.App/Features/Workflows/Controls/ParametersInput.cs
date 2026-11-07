using System.Windows;
using Fantoche.App.Features.UserInterfaces;
using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Fantoche.App.Features.Workflows.Controls
{
    /// <summary>What the user confirmed : the parameters, or why they are refused.</summary>
    public sealed record ParametersAnswer(JToken? Parameters, string? Refusal)
    {
        public bool IsRefused => Refusal != null;
    }

    /// <summary>
    /// The parameters a task or workflow is started or scheduled with, as the user fills them in : with
    /// its interface when it has one handling <see cref="EnumInterfaceMode.Setup"/>, with the
    /// <see cref="ParametersForm"/> generated from its schema otherwise.
    /// </summary>
    public partial class ParametersInput : ObservableObject
    {
        private readonly BaseAutomationTask _task;
        private readonly ITaskInterface? _interface;

        /// <summary>The generated form, null when the interface of the element is shown.</summary>
        public ParametersForm? Form { get; }

        /// <summary>The view of the interface of the element, null for the generated form.</summary>
        public FrameworkElement? View { get; }

        /// <summary>Why the interface of the element isn't shown although it has one.</summary>
        public string? LoadError { get; }

        /// <summary>Why the interface's last answer was refused.</summary>
        [ObservableProperty] private string? _refusal;

        private ParametersInput(BaseAutomationTask task, JToken? values, ITaskInterface? taskInterface, FrameworkElement? view, string? loadError)
        {
            _task = task;
            _interface = taskInterface;
            View = view;
            LoadError = loadError;
            if (view == null)
                Form = new ParametersForm(task, values);
        }

        /// <summary>
        /// What [task] is filled in with, starting from [values] : the interface its settings pick
        /// (read from the tree), downloaded if needed. On the UI thread : the view is built here.
        /// </summary>
        public static async Task<ParametersInput> CreateAsync(BaseAutomationTask task, JToken? values)
        {
            try
            {
                await SpineViewModel.Instance.Scoped.ResolveInterfacesAsync([task]);
                if (task.Interface is not PackageClassTarget target)
                    return new ParametersInput(task, values, null, null, null);

                ITaskInterface taskInterface = await SpineViewModel.Instance.Interfaces.CreateAsync(target);
                if (!taskInterface.Handles(EnumInterfaceMode.Setup))
                    return new ParametersInput(task, values, null, null, null);

                TaskInterfaceContext context = new()
                {
                    Mode = EnumInterfaceMode.Setup,
                    Parameters = Prefilled(task, values),
                };
                FrameworkElement view = TaskInterfaces.ViewOf(taskInterface, context);
                return new ParametersInput(task, values, taskInterface, view, null);
            }
            catch (Exception exception)
            {
                // The element can still be started : the generated form fills in the same parameters.
                string error = $"The interface can't be shown, the form of the parameters is instead : {exception.Message}";
                return new ParametersInput(task, values, null, null, error);
            }
        }

        /// <summary>Whether the generated form already knows the parameters are wrong.</summary>
        public bool HasFormErrors => Form?.HasErrors == true;

        /// <summary>The parameters to start with, refused when they don't match the schema of the element.</summary>
        public async Task<ParametersAnswer> ReadAsync()
        {
            if (_interface == null)
                return new ParametersAnswer(Form!.ToToken(), null);

            Refusal = null;
            try
            {
                JToken answer = await _interface.ReadAsync() ?? JValue.CreateNull();
                Refusal = RefusalOf(answer);
                return new ParametersAnswer(answer, Refusal);
            }
            catch (Exception exception)
            {
                // A TaskInterfaceException tells what is left to fix ; anything else is a bug of the
                // interface, shown the same way rather than losing what the user set.
                Refusal = exception.Message;
                return new ParametersAnswer(null, Refusal);
            }
        }

        /// <summary>What the parameters of [task] start from : the defaults of a workflow, then [values].</summary>
        private static JToken? Prefilled(BaseAutomationTask task, JToken? values)
        {
            if (ParametersForm.Defaults(task) is not JObject defaults)
                return values;
            if (values is not JObject given)
                return defaults;

            JObject merged = (JObject)defaults.DeepClone();
            merged.Merge(given, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
            return merged;
        }

        private string? RefusalOf(JToken answer)
        {
            JsonSchema? schema = _task.InputSchema;
            if (schema == null)
                return null;

            List<string> errors = [.. schema.Validate(answer).Select(Schemas.Describe)];
            return errors.Count == 0 ? null : string.Join("\n", errors);
        }
    }
}
