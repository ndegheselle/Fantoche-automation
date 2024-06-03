using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Automation.App.Base
{
    public interface IWindowContainer
    {
        public IModalContainer Modal { get; }
    }

    public interface IAlertContainer
    {
        public void Show(string message);
    }

    public interface IModalContainer
    {
        public void Show(FrameworkElement content);
        public void Close();
    }
}
