using System.Data;
using System.Windows.Controls;
using System.Windows.Data;

namespace Fantoche.Plugins.Example.Ui.Tables;

/// <summary>Shows the <see cref="MapColumnsViewModel"/> set as its DataContext.</summary>
public partial class MapColumnsView : UserControl
{
    public MapColumnsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Each column of the preview headed by the name it is read by, and bound by its stored name as
    /// an index : a name holding a dot or a space would break a binding path.
    /// </summary>
    private void OnAutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (sender is not DataGrid { ItemsSource: DataView view } || e.Column is not DataGridTextColumn column)
            return;

        column.Header = view.Table!.Columns[e.PropertyName]!.Caption;
        column.Binding = new Binding($"[{e.PropertyName}]");
    }
}
