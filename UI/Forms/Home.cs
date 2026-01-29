using System;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking; // DockPanelSuite reference
using TraderApp.UI.Theme;
using TraderApp.Helpers;

namespace TraderApp.UI.Forms
{
    public partial class Home : Form
    {
        private DockPanel _dockPanel;

        public Home()
        {
            InitializeComponent();
            SetupDockPanel();
            ThemeManager.ApplyTheme(this);

            this.Text = $"TraderApp - {SessionManager.UserId}";
            this.WindowState = FormWindowState.Maximized;
        }

        private void SetupDockPanel()
        {
            _dockPanel = new DockPanel();
            _dockPanel.Dock = DockStyle.Fill;
            _dockPanel.Theme = new VS2015BlueTheme(); // Ya jo bhi theme pasand ho

            // Background color fix for blank area
            _dockPanel.DocumentStyle = DocumentStyle.DockingMdi;

            this.Controls.Add(_dockPanel);
        }

        // Abhi MarketWatch aur Navigation load nahi kar rahe 
        // Jaisa tune bola: "home page abhi sifr bank rakhte hai"
        private void Home_Load(object sender, EventArgs e)
        {
            // Future logic:
            // _marketWatch.Show(_dockPanel, DockState.DockLeft);
        }
    }
}