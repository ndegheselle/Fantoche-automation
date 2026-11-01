using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;

namespace Fantoche.App.Features.Workflows.Editor
{
    /// <summary>
    /// The whole error a node of the followed run failed with, which a tooltip would cut.
    /// </summary>
    public partial class NodeErrorViewModel : ObservableObject, IOverlayContent
    {
        public OverlayOptions Options { get; }

        public string Message { get; }

        private readonly IOverlayer _overlayer;

        public NodeErrorViewModel(string nodeName, string message, IOverlayer overlayer)
        {
            Message = message;
            _overlayer = overlayer;
            Options = new OverlayOptions() { Title = $"Error - {nodeName}" };
        }

        public static Task ShowAsync(string nodeName, string message)
        {
            IOverlayer overlayer = SpineViewModel.Instance.Overlayer;
            return overlayer.ShowAsync(new NodeErrorViewModel(nodeName, message, overlayer));
        }

        [RelayCommand]
        private void Close() => _overlayer.Cancel(this);
    }
}
