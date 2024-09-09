using System.ComponentModel;

namespace Automation.App.Base
{
    public enum EnumDialogType
    {
        Info,
        Warning,
        Error,
        Success
    }

    public interface IWindowContainer
    {
        public IModalContainer Modal { get; }
        public IAlert Alert { get; }
    }

    #region Modal
    public class ModalOptions
    {
        public EnumDialogType Type { get; set; } = EnumDialogType.Info;
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
        public Task<bool> Show(IModalContent content, ModalOptions? options = null);
        public void Close(bool result = false);
    }

    public interface IModalContent
    {
        public ModalOptions Options { get; }
        public IModalContainer? ModalContainer { get; set; }
    }

    public interface IModalContentValidate : IModalContent
    {
        /// <summary>
        /// Handle modal validation
        /// </summary>
        /// <returns>If the modal content is valid, if false will prevent closing successfully the modal</returns>
        public Task<bool> OnValidate();
    }

    #endregion

    #region Alert

    public class AlertOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public EnumDialogType Type { get; set; }
        public string Message { get; set; }
    }

    public interface IAlert
    {
        public void Show(EnumDialogType type, string message);
        public void Hide();

        public void Info(string message);
        public void Warning(string message);
        public void Error(string message);
        public void Success(string message);
    }

    #endregion
}
