using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Automation.App.Components.Behaviors;

/// <summary>
/// Attached behavior that focuses a <see cref="TextBox"/> and selects all of its text as soon as
/// the box becomes visible. Used for the inline rename boxes that appear on demand in a tree, so the
/// user can start typing the new name straight away (explorer-style rename).
/// </summary>
public static class EditBoxAssist
{
    public static readonly DependencyProperty FocusOnVisibleProperty =
        DependencyProperty.RegisterAttached(
            "FocusOnVisible",
            typeof(bool),
            typeof(EditBoxAssist),
            new PropertyMetadata(false, OnFocusOnVisibleChanged));

    public static void SetFocusOnVisible(TextBox element, bool value) =>
        element.SetValue(FocusOnVisibleProperty, value);

    public static bool GetFocusOnVisible(TextBox element) =>
        (bool)element.GetValue(FocusOnVisibleProperty);

    private static void OnFocusOnVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox box)
            return;

        // The box may already be loaded and visible (a freshly created node that starts in edit
        // mode), or it may flip to visible later (renaming an existing node). Cover both cases.
        box.Loaded -= OnLoaded;
        box.IsVisibleChanged -= OnIsVisibleChanged;

        if (e.NewValue is true)
        {
            box.Loaded += OnLoaded;
            box.IsVisibleChanged += OnIsVisibleChanged;
            if (box.IsVisible)
                FocusAndSelect(box);
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box && box.IsVisible)
            FocusAndSelect(box);
    }

    private static void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is TextBox box && e.NewValue is true)
            FocusAndSelect(box);
    }

    private static void FocusAndSelect(TextBox box)
    {
        // Defer so the focus lands after the control is laid out and any selection has settled.
        box.Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!box.IsVisible)
                return;
            box.Focus();
            box.SelectAll();
        }), DispatcherPriority.Background);
    }
}
