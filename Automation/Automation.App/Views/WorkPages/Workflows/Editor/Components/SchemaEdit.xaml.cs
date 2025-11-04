using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Joufflu.Popups;
using NJsonSchema;

namespace Automation.App.Views.WorkPages.Workflows.Editor.Components;

public partial class SchemaEdit : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public static readonly DependencyProperty SchemaJsonProperty = DependencyProperty.Register(
        nameof(SchemaJson), typeof(string), typeof(SchemaEdit),
        new PropertyMetadata(null, (o, args) => ((SchemaEdit)o).OnSchemaJsonChanged()));

    public string SchemaJson
    {
        get => (string)GetValue(SchemaJsonProperty);
        set => SetValue(SchemaJsonProperty, value);
    }

    public string SampleJson { get; set; }

    public bool IsReadOnly { get; set; } = false;

    private IAlert _alert => this.GetCurrentAlert();

    public SchemaEdit()
    {
        InitializeComponent();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async Task OnSchemaJsonChanged()
    {
        if (string.IsNullOrEmpty(SchemaJson))
            return;

        var schema = await JsonSchema.FromJsonAsync(SchemaJson);
        SampleJson = schema.ToSampleJson().ToString();
    }

    private void ConvertSampleToSchema(object sender, RoutedEventArgs e)
    {
        try
        {
            SchemaJson = JsonSchema.FromSampleJson(SampleJson).ToJson();
            TabControl.SelectedIndex = 0;
        }
        catch
        {
            _alert.Error("Cannot convert sample to schema.");
        }
    }
}