using Automation.App.Base;
using Automation.Shared.ViewModels;
using System.Windows.Controls;

namespace Automation.App.Components.Data
{
    public class ContextEditModal : ContextEdit, IModalContent
    {
        public IModalContainer? ModalParent { get; set; }
        public ModalOptions Options => new ModalOptions() { Title = "Edit key", ValidButtonText = "Save" };

        public ContextEditModal(DataNode node) : base(node)
        {}
    }

    /// <summary>
    /// Logique d'interaction pour DataNodeEdit.xaml
    /// </summary>
    public partial class ContextEdit : UserControl
    {
        private readonly DataNode _node;
        public ContextEdit(DataNode node)
        {
            this._node = node;
            this.DataContext = _node;
            InitializeComponent();
        }
    }
}
