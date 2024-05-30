using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Automation.App.Layout
{
    public interface IPageContainer
    {
        void Show(FrameworkElement page);
    }

    public class Navigation
    {
        // Lazy singleton
        private static readonly Lazy<Navigation> _instance = new Lazy<Navigation>(() => new Navigation());
        public static Navigation Instance => _instance.Value;

        public IPageContainer? CurrentContainer { get; set; }

        private Navigation()
        {}

        public void Show(FrameworkElement page)
        {
            if (CurrentContainer == null)
                throw new Exception("No container set");
            CurrentContainer.Show(page);
        }
    }
}
