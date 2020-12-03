using System.Windows.Forms;

namespace Simego.DataSync.Providers.WinSCP
{
    public partial class ConnectionInterface : UserControl
    {
        public PropertyGrid PropertyGrid { get { return propertyGrid1; } }
        
        public ConnectionInterface()
        {
            InitializeComponent();
            Setup();
        }

        public void Setup()
        {
            PropertyGrid.LineColor = System.Drawing.Color.WhiteSmoke;
        }
    }
}
