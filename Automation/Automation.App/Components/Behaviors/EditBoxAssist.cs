using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Automation.App.Components.Behaviors;

/// <summary>
/// Attached behavior that focuses a <see cref="TextBox"/> and selects all of its text as soon as
/// the box becomes visible. Used for the inline rename boxes that appear on demand in a tree, so the
/// user can start typing the new name straight away (explorer-style rename).
/// </summary>
public class EditBoxAssist
{
    private EditBoxAssist() { }

    public static readonly AttachedProperty<bool> FocusOnVisibleProperty =
        AvaloniaProperty.RegisterAttached<EditBoxAssist, TextBox, bool>("FocusOnVisible");

    public static void SetFocusOnVisible(TextBox element, bool value) =>
        element.SetValue(FocusOnVisibleProperty, value);

    public static bool GetFocusOnVisible(TextBox element) =>
        element.GetValue(FocusOnVisibleProperty);

    static EditBoxAssist()
    {
        FocusOnVisibleProperty.Changed.AddClassHandler<TextBox>((box, e) =>
        {
            // The box may already be attached and visible (a freshly created node that starts in edit
            // mode), or it may flip to visible later (renaming an existing node). Cover both cases.
            box.AttachedToVisualTree -= OnAttached;
            box.PropertyChanged -= OnBoxPropertyChanged;

            if (e.GetNewValue<bool>())
            {
                box.AttachedToVisualTree += OnAttached;
                box.PropertyChanged += OnBoxPropertyChanged;
                if (box.IsVisible)
                    FocusAndSelect(box);
            }
        });
    }

    private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is TextBox box && box.IsVisible)
            FocusAndSelect(box);
    }

    private static void OnBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is TextBox box && e.Property == Visual.IsVisibleProperty && e.GetNewValue<bool>())
            FocusAndSelect(box);
    }

    private static void FocusAndSelect(TextBox box)
    {
        // Defer so the focus lands after the control is laid out and any selection has settled.
        Dispatcher.UIThread.Post(() =>
        {
            if (!box.IsVisible)
                return;
            box.Focus();
            box.SelectAll();
        }, DispatcherPriority.Background);
    }
}
