using System.Collections.ObjectModel;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Joufflu.Feedback;

namespace Fantoche.App.Features.Workflows.Details.Controls
{
    /// <summary>An interface a task or workflow can pick, see <see cref="TaskSettings.InterfaceId"/>.</summary>
    public record InterfaceOption(Guid? Id, string Name);

    /// <summary>
    /// The interface a task or workflow is set up, started and asked with, picked among the default,
    /// the generated form and the interfaces hanging under it (see <see cref="AutomationInterface.Pick"/>).
    /// </summary>
    public partial class InterfaceChoiceViewModel : ObservableObject
    {
        private readonly BaseAutomationTask _element;
        private readonly Action _markChanged;

        public ObservableCollection<InterfaceOption> Options { get; } = [];

        private InterfaceOption? _selected;

        /// <summary>Picking one records it in the settings of the element, saved with it.</summary>
        public InterfaceOption? Selected
        {
            get => _selected;
            set
            {
                if (!SetProperty(ref _selected, value) || value == null)
                    return;

                _element.Settings.InterfaceId = value.Id;
                _markChanged();
            }
        }

        /// <param name="markChanged">Tell the page the element has a change to save.</param>
        public InterfaceChoiceViewModel(BaseAutomationTask element, Action markChanged)
        {
            _element = element;
            _markChanged = markChanged;
        }

        /// <summary>Read the interfaces hanging under the element, the ones it can pick.</summary>
        public async Task LoadAsync(IScopedService scoped, IToastService toasts)
        {
            List<AutomationInterface> linked;
            try
            {
                linked = [.. (await scoped.GetChildrensAsync(_element.Id)).OfType<AutomationInterface>().Where(x => x.Target != null)];
            }
            catch (Exception exception)
            {
                toasts.Error(exception.Message, $"The interfaces of '{_element.Metadata.Name}' could not be read");
                linked = [];
            }

            string defaultName = AutomationInterface.Pick(new TaskSettings(), linked)?.Metadata.Name ?? AutomationInterface.Form.Metadata.Name;
            Options.Add(new InterfaceOption(null, $"Default ({defaultName})"));
            Options.Add(new InterfaceOption(AutomationInterface.Form.Id, AutomationInterface.Form.Metadata.Name));
            foreach (AutomationInterface face in linked)
                Options.Add(new InterfaceOption(face.Id, face.Metadata.Name));

            // Shown as it is stored, without counting as a change. A pick no longer linked shows as
            // the default, which is what it falls back on.
            _selected = Options.FirstOrDefault(x => x.Id == _element.Settings.InterfaceId) ?? Options[0];
            OnPropertyChanged(nameof(Selected));
        }
    }
}
