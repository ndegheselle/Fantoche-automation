using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Joufflu.Popups;
using NJsonSchema;

namespace Automation.App.Views.WorkPages.Workflows.Editor.Components;

public partial class SchemaDisplay : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public static readonly DependencyProperty SchemaJsonProperty = DependencyProperty.Register(
        nameof(SchemaJson), typeof(string), typeof(SchemaDisplay),
        new PropertyMetadata(null));

    public string SchemaJson
    {
        get => (string)GetValue(SchemaJsonProperty);
        set => SetValue(SchemaJsonProperty, value);
    }

    public string SampleJson { get; set; } = "";
    public bool WithError { get; private set; } = false;

    public bool IsReadOnly { get; set; } = false;

    public SchemaDisplay()
    {
        InitializeComponent();
    }
    
    private async void HandleSchemaJsonChanged(object sender, TextChangedEventArgs e)
    {
        WithError = false;
        if (string.IsNullOrEmpty(SchemaJson))
            return;

        try
        {
            var schema = await JsonSchema.FromJsonAsync(SchemaJson);
            SampleJson = schema.ToSampleJson().ToString();
        }
        catch
        {
            WithError = true;
        }
    }
    
    private void HandleSampleJsonChanged(object sender, TextChangedEventArgs e)
    {
        WithError = false;
        if (string.IsNullOrEmpty(SampleJson))
            return;

        try
        {
            SchemaJson = JsonSchema.FromSampleJson(SampleJson).ToJson();
        }
        catch
        {
            WithError = true;
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}