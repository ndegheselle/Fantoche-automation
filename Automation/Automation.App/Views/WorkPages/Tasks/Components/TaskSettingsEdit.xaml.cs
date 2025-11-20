using System.Windows;
using System.Windows.Controls;
using Automation.Models.Work;

namespace Automation.App.Views.WorkPages.Tasks.Components;

public partial class TaskSettingsEdit : UserControl
{
    public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
        nameof(Settings), typeof(TaskSettings), typeof(TaskSettingsEdit), new PropertyMetadata(default(TaskSettings)));

    public TaskSettings Settings
    {
        get { return (TaskSettings)GetValue(SettingsProperty); }
        set { SetValue(SettingsProperty, value); }
    }
    
    public TaskSettingsEdit()
    {
        InitializeComponent();
    }
}