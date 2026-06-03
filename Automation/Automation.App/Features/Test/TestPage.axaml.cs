using System;
using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace Automation.App.Features.Test;

public partial class TestPage : UserControl
{
    [ModuleInitializer]
    internal static void Init()
    {
        Console.WriteLine("Runs when the module loads");
    }

    public TestPage()
    {
        InitializeComponent();
    }
}