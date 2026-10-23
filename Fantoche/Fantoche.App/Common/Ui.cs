using System.Windows;

namespace Fantoche.App.Common
{
    public static class Ui
    {
        /// <summary>
        /// Run [action] on the UI thread. The history service reports its instances as plain
        /// <see cref="Action{T}"/> events raised by the thread executing them, so whoever follows a
        /// run has to come back on its own.
        /// </summary>
        public static void Dispatch(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.Invoke(action);
        }

        /// <summary>
        /// Queue [action] on the UI thread without waiting for it, in the order it was posted : for
        /// an event raised while its source holds a lock the UI thread may be waiting for.
        /// </summary>
        public static void Post(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
                action();
            else
                dispatcher.BeginInvoke(action);
        }
    }
}
