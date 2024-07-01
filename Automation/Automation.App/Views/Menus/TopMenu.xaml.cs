using Automation.App.Base;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.Menus
{
    /// <summary>
    /// Logique d'interaction pour TopMenu.xaml
    /// </summary>
    public partial class TopMenu : UserControl
    {
        private readonly App _app = (App)App.Current;

        public TopMenu()
        {
            InitializeComponent();
        }

        #region UI Events

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Parameters_Click(object sender, RoutedEventArgs e)
        {
            IModalContainer modal = _app.ServiceProvider.GetRequiredService<IModalContainer>();
            modal.Show(new ParametersUI());
        }

        #endregion
    }
}
