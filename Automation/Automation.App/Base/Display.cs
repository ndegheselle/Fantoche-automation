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
        public void Show<T>(string title, IModalContent<T> content);
        public void Close();
    }

    public interface IModalContent<T>
    {
        public event Action<T>? OnFinish;
    }
    /// <summary>
    /// For modal content that need to know when it's closed
    /// </summary>
    public interface IModalContentFeedback
    {
        public void OnClose();
    }
}
