using System.ComponentModel;

namespace Automation.App.Base
{
    public interface IWindowContainer
    {
        public IModalContainer Modal { get; }
        public IAlert Alert { get; }
    }

    #region Modal
    public struct ModalOptions
    {
        public string Title { get; set; }
        public string ValidButtonText { get; set; }
        public bool ShowFooter { get; set; }

        public ModalOptions()
        {
            ShowFooter = true;
            ValidButtonText = "Save";
        }
    }

    public interface IModalContainer
    {
        public event Action<bool>? OnClose;
        public Task<bool> Show(IModalContent content, ModalOptions options = default);
        public void Close(bool result = false);
    }

    public interface IModalContent
    {
        public IModalContainer? ModalParent { get; set; }
    }

    public interface IModalContentCallback : IModalContent
    {
        public void OnModalClose(bool result);
    }
    #endregion

    #region Alert

    public enum EnumAlertType
    {
        Info,
        Warning,
        Error,
        Success
    }

    public class AlertOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public EnumAlertType Type { get; set; }
        public string Message { get; set; }
    }

    public interface IAlert
    {
        public void Show(EnumAlertType type, string message);
        public void Hide();

        public void Info(string message);
        public void Warning(string message);
        public void Error(string message);
        public void Success(string message);
    }

    #endregion
}
