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
}
