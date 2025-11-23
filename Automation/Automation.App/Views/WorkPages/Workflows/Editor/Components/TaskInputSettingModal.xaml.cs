using System.ComponentModel;
using System.Windows;
using Automation.Models.Work;
using Automation.Shared.Data;
using Joufflu.Popups;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using Usuel.Shared;

namespace Automation.App.Views.WorkPages.Workflows.Editor.Components;

/// <summary>
/// Logique d'interaction pour TaskInputSettingModal.xaml
/// </summary>
public partial class TaskInputSettingModal : UserControl, IModalContent, INotifyPropertyChanged
{
    public ModalOptions Options { get; private set; } = new();
    public IModal? ParentLayout { get; set; }

    public BaseGraphTask Task { get; private set; }
    public Graph Graph { get; private set; }
    public List<string> ContextSamples { get; private set; }

    public ICustomCommand CancelCommand { get; private set; }
    public ICustomCommand ValidateCommand { get; private set; }

    private IAlert _alert => this.GetCurrentAlert();
    private readonly string? _originalSettings;

    public TaskInputSettingModal(BaseGraphTask task, Graph graph)
    {
        Task = task;
        Graph = graph;
        ContextSamples = Graph.Execution.GetContextSampleJsonFor(Task).Select(x => x.ToString()).ToList();
        _originalSettings = Task.InputJson;

        if (string.IsNullOrWhiteSpace(Task.InputJson))
            Task.InputJson = Task.InputSchema?.ToSampleJson().ToString();
        
        Options.Title = $"{Task.Name} - settings";
        CancelCommand = new DelegateCommand(Cancel);
        ValidateCommand = new DelegateCommand(Validate, () => string.IsNullOrEmpty(Task.InputJson) == false);
        InitializeComponent();
    }

    private void Cancel()
    {
        Task.InputJson = _originalSettings;
        ParentLayout?.Hide();
    }

    // ReSharper disable once AsyncVoidMethod
    private async void Validate()
    {
        await ContextMappingElement.HandleSettingsChanged();
        if (ContextMappingElement.HasErrors)
            return;

        _alert.Success("Settings changed !");
        ParentLayout?.Hide(true);
    }

    private void HandleWaitAllChanged(object sender, RoutedEventArgs e)
    {
        ContextSamples = Graph.Execution.GetContextSampleJsonFor(Task).Select(x => x.ToString()).ToList();
    }
}