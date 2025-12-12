using System.ComponentModel;
using System.Windows;
using Automation.Models.Work;
using Automation.Shared.Data;
using Joufflu.Popups;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Newtonsoft.Json.Linq;
using Usuel.Shared;

namespace Automation.App.Views.WorkPages.Workflows.Editor.Components;

/// <summary>
/// Logique d'interaction pour TaskInputSettingModal.xaml
/// </summary>
public partial class WorkflowOutputModal : UserControl, IModalContent, INotifyPropertyChanged
{
    public ModalOptions Options { get; private set; } = new();
    public IModal? ParentLayout { get; set; }

    public List<string> ContextSamples { get; private set; }
    public AutomationWorkflow Workflow { get; private set; }
    public GraphControl EndTask { get; private set; }

    public ICustomCommand CancelCommand { get; private set; }
    public ICustomCommand ValidateCommand { get; private set; }

    private IAlert _alert => this.GetCurrentAlert();
    private readonly string? _originalSchema;
    private readonly string? _originalSettings;

    public WorkflowOutputModal(GraphControl task, AutomationWorkflow workflow)
    {
        EndTask = task;
        Workflow = workflow;
        _originalSchema = Workflow.OutputSchemaJson;
        _originalSettings = Workflow.OutputMappingJson;

        ContextSamples = Workflow.Graph.Execution.GetContextSampleForEnd().Select(x => x.ToString()).ToList();

        Options.Title = $"{Workflow.Metadata.Name} - output schema";
        CancelCommand = new DelegateCommand(Cancel);
        ValidateCommand = new DelegateCommand(Validate);
        InitializeComponent();
    }

    private void Cancel()
    {
        // Restore
        Workflow.OutputSchemaJson = _originalSchema;
        Workflow.OutputMappingJson = _originalSettings;

        ParentLayout?.Hide();
    }

    private void Validate()
    {
        if (ContextMappingElement.HasErrors)
            return;

        // Update all end task
        var endTasks = Workflow.Graph.GetEndNodes();
        foreach (var task in endTasks)
        {
            task.Settings.IsWaitingAllInputs = Workflow.WorkflowSettings.IsWaitingForAllEnd;
            task.InputJson = Workflow.OutputMappingJson;
        }

        _alert.Success("Settings changed !");
        ParentLayout?.Hide(true);
    }

    private void HandleWaitAllChanged(object sender, RoutedEventArgs e)
    {
        ContextSamples = Workflow.Graph.Execution.GetContextSampleForEnd().Select(x => x.ToString()).ToList();
    }
}