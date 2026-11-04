using System.Windows;

namespace Fantoche.App.Common
{
    /// <summary>
    /// The data context of a view, reached by the rows of its grids as a static resource. A row
    /// looking for it up the tree (RelativeSource) fails each time the grid builds or drops the row,
    /// detached meanwhile : the proxy is held by the resources of the view, never by a row.
    /// </summary>
    public class BindingProxy : Freezable
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy));

        public object? Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        protected override Freezable CreateInstanceCore() => new BindingProxy();
    }
}
