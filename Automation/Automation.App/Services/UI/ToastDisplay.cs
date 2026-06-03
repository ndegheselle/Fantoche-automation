using ShadUI;

namespace Automation.App.Services.UI
{
    internal class ToastDisplay
    {
        private const int DEFAULT_DELAY_SECONDS = 10;
        private readonly ToastManager _manager;
        public ToastDisplay(ToastManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// Show a neutral toast
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        public void Show(string title, string message)
        {
            _manager.CreateToast(title)
                .WithContent(message)
                .WithDelay(DEFAULT_DELAY_SECONDS)
                .Show();
        }

        /// <summary>
        /// Show an info toast
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        public void Info(string title, string message)
        {
            _manager.CreateToast(title)
                .WithContent(message)
                .WithDelay(DEFAULT_DELAY_SECONDS)
                .ShowInfo();
        }

        /// <summary>
        /// Show a warning toast
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        public void Warning(string title, string message)
        {
            _manager.CreateToast(title)
                .WithContent(message)
                .WithDelay(DEFAULT_DELAY_SECONDS)
                .ShowWarning();
        }

        /// <summary>
        /// Show an error toast
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        public void Error(string title, string message)
        {
            _manager.CreateToast(title)
                .WithContent(message)
                .WithDelay(DEFAULT_DELAY_SECONDS)
                .ShowError();
        }

        /// <summary>
        /// Show a success toast
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        public void Success(string title, string message)
        {
            _manager.CreateToast(title)
                .WithContent(message)
                .WithDelay(DEFAULT_DELAY_SECONDS)
                .ShowSuccess();
        }
    }
}
