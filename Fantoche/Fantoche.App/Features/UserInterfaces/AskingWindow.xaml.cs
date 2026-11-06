using System.Windows;
using Fantoche.Worker.UserInterfaces;
using Joufflu.Controls;

namespace Fantoche.App.Features.UserInterfaces
{
    /// <summary>A task asking the user, until the user answers it or closes it (see <see cref="AskingViewModel"/>).</summary>
    public partial class AskingWindow : ThemedWindow
    {
        private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Completed once the window is closed, answered or not.</summary>
        public Task Dismissed => _closed.Task;

        private AskingWindow(AskingViewModel asking)
        {
            InitializeComponent();
            DataContext = asking;
            asking.Answered += Close;
            asking.Later += Close;
            Closed += (_, _) => _closed.TrySetResult();
        }

        /// <summary>Show [request] until it is answered or closed, see <see cref="Dismissed"/>.</summary>
        public static async Task<AskingWindow> OpenAsync(PageRequest request, TaskInterfaces interfaces)
        {
            AskingViewModel asking = await AskingViewModel.CreateAsync(request, interfaces);
            AskingWindow window = new(asking)
            {
                Owner = Application.Current.MainWindow,
            };
            window.Show();
            return window;
        }
    }
}
