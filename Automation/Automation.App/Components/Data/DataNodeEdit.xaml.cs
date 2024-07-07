using Automation.App.Base;
using System.Windows.Controls;

namespace Automation.App.Components.Data
{
    public class  DataNodeEditModal : DataNodeEdit, IModalContent
    {
        public IModalContainer? ModalParent { get; set; }
        public ModalOptions Options => new ModalOptions() { Title = "Edit key", ValidButtonText = "Save" };

        public DataNodeEditModal(DataNode node) : base(node)
        {}
    }

    /// <summary>
    /// Logique d'interaction pour DataNodeEdit.xaml
    /// </summary>
    public partial class DataNodeEdit : UserControl
    {
        private readonly DataNode _node;
        public DataNodeEdit(DataNode node)
        {
            this._node = node;
            this.DataContext = _node;
            InitializeComponent();
        }
    }
}
