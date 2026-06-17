using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace Automation.App.Components;

/// <summary>
/// A Lucide glyph rendered as a WPF UI <see cref="FontIcon"/> (an <c>IconElement</c>), so it can be
/// used wherever an icon element is required (e.g. <c>ui:Button.Icon</c>, <c>ui:TextBox.Icon</c>).
/// Set <see cref="FontIcon.Glyph"/> to a constant from <c>Assets.Fonts.LucideIconFont</c>.
/// </summary>
public class LucideIcon : FontIcon
{
    public LucideIcon()
    {
        if (Application.Current?.TryFindResource("LucideIconFamily") is FontFamily family)
            FontFamily = family;
        FontSize = 16;
    }
}
