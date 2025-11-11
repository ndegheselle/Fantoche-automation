using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Automation.Shared.Data;
using NJsonSchema;
using Usuel.Shared;

namespace Automation.App.Views.WorkPages.Workflows.Editor.Components;

public partial class ContextMapping : UserControl, INotifyPropertyChanged, INotifyDataErrorInfo
{
    public static readonly DependencyProperty SchemaJsonProperty = DependencyProperty.Register(
        nameof(SchemaJson), typeof(string), typeof(ContextMapping), 
        new PropertyMetadata(null, (o, args) => ((ContextMapping)o).HandleSchemaChanged()));
    
    public static readonly DependencyProperty SettingsJsonProperty = DependencyProperty.Register(
        nameof(SettingsJson), typeof(string), typeof(ContextMapping),
        new PropertyMetadata(null, (o, args) => ((ContextMapping)o).HandleSettingsChanged()));

    public static readonly DependencyProperty InputsProperty = DependencyProperty.Register(
        nameof(Inputs), typeof(List<string>), typeof(ContextMapping), new PropertyMetadata(default(List<string>)));

    public string SchemaJson
    {
        get => (string)GetValue(SchemaJsonProperty);
        set => SetValue(SchemaJsonProperty, value);
    }

    public string SettingsJson
    {
        get => (string)GetValue(SettingsJsonProperty);
        set => SetValue(SettingsJsonProperty, value);
    }

    public List<string> Inputs
    {
        get => (List<string>)GetValue(InputsProperty);
        set => SetValue(InputsProperty, value);
    }

    public bool DoesSettingUpdateSchema { get; set; } = true;

    public ContextMapping()
    {
        InitializeComponent();
    }

    private async Task HandleSchemaChanged()
    {
        if (DoesSettingUpdateSchema == false)
            return;
        
        Errors.Clear(nameof(SchemaJson));
        try
        {
            await JsonSchema.FromJsonAsync(SchemaJson);
        }
        catch
        {
            Errors.Add("The schema is not valid.", nameof(SchemaJson));
        }
    }
    
    private async Task HandleSettingsChanged()
    {
        Errors.Clear(nameof(SettingsJson));
        
        try
        {
            var result = ReferencesHandler.ReplaceReferences(SettingsJson, Inputs ?? []);
            if (result.InconsistentReferenceErrors.Count > 0)
            {
                Errors.Add(result.InconsistentReferenceErrors.Select(x => x.ToString()), nameof(SettingsJson));
                return;
            }
            
            if (DoesSettingUpdateSchema)
            {
                SchemaJson = JsonSchema.FromSampleJson(SettingsJson).ToJson();
            }
            else
            {
                // Check if replaced references pass schema
                var schema = await JsonSchema.FromJsonAsync(SchemaJson);
                foreach (var context in result.Contexts)
                {
                    var errors = schema.Validate(context.ReplacedSetting);
                    if (errors.Count > 0)
                        Errors.Add(errors.Select(x => x.ToString()), nameof(SettingsJson));
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Errors.Add(ex.Message, nameof(SettingsJson));
        }
    }

    #region Errors

    public ErrorValidation Errors { get; } = new();

    public IEnumerable GetErrors(string? propertyName)
    {
        return Errors.GetErrors(propertyName);
    }

    public bool HasErrors => Errors.HasErrors;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged
    {
        add => Errors.ErrorsChanged += value;
        remove => Errors.ErrorsChanged -= value;
    }

    #endregion
}