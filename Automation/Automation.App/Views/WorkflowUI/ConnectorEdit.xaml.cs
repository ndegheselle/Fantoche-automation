using Automation.App.Base;
using Automation.Base.ViewModels;
using System.Windows.Controls;

namespace Automation.App.Views.WorkflowUI
{
    public class ConnectorEditModal : ConnectorEdit, IModalContentCallback
    {
        public IModalContainer? ModalParent { get; set; }

        public ModalOptions Options => new ModalOptions() { Title = "Edit connector", ValidButtonText = "Save" };

        public ConnectorEditModal(NodeConnector connector) : base(connector)
        {
            if (connector.Id == Guid.Empty)
                Options.Title = "New connector";
        }

        public void OnModalClose(bool result)
        {
            // New connector
            if (Connector.Id == Guid.Empty)
                Connector.Id = Guid.NewGuid();
        }
    }

    /// <summary>
    /// Logique d'interaction pour ConnectorEdit.xaml
    /// </summary>
    public partial class ConnectorEdit : UserControl
    {
        public NodeConnector Connector { get; set; }
        public ConnectorEdit(NodeConnector connector)
        {
            Connector = connector;
            this.DataContext = Connector;
            InitializeComponent();
        }
    }
}
