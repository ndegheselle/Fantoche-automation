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
        public event Action<bool>? OnClose;
        public Task<bool> Show(string title, IModalContent content);
        public void Close(bool result = false);
    }

    public interface IModalContent
    {
        public IModalContainer? ModalParent { get; set; }
    }
}
