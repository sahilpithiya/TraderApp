using ClientDesktop.Models;
using DesktopClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using TraderApp.UI.Usercontrol;
using TraderApp.Services;
using TraderApps.Config;
using TraderApps.Forms;
using TraderApps.Helpers;
using TraderApps.Services;
using TraderApps.UI.Theme;
using WeifenLuo.WinFormsUI.Docking;

namespace TraderApps.UI.Forms
{
    public partial class Home : Form
    {
        #region Variables
        public static Home Instance;
        private readonly AuthService _authService; // Auth Service
        private readonly ClientService _clientService; // Client Service

        // Dockable panels
        private DockContent MarketwatchDock;
        private DockContent DetailsDock;

        private DetailsControl _detailsUC;
        private MarketWatchControl _marketWatchControl;

        private bool _isUserControlsPreloaded = false;
        private bool IsComeFromSocket = false;
        private static ClientDetails clientDetails { get; set; }

        // Track panels
        private Dictionary<string, DockContent> allPanels = new Dictionary<string, DockContent>();
        private Dictionary<string, DockState> lastDockStates = new Dictionary<string, DockState>();

        private DesignTimeHelper layoutHelper;

        #endregion

        #region Form Initialization
        public Home()
        {
            Instance = this;
            InitializeComponent();

            // Initialize Service
            _authService = new AuthService();
            _clientService = new ClientService();

            ThemeManager.ApplyTheme(this);
            dockPanel.Theme = new VS2015LightTheme();
            var whiteTheme = new DesignTimeHelper.DynamicColorTheme(ThemeManager.White);
            dockPanel.Theme = whiteTheme;
            layoutHelper = new DesignTimeHelper(dockPanel);
            dockPanel.BackColor = ThemeManager.White;
            this.FormClosing += (s, e) => layoutHelper.SaveLayout();

        }

        private void Home_Load(object sender, EventArgs e)
        {
            InitializeHome();
            ThemeManager.ApplyTheme(this);
        }
        #endregion

        #region Authentication And Login Handling
        public async void InitializeHome()
        {
            this.toolStripSeparator6.Visible = false;

            await _authService.GetServerListAsync();

            var loginInfoList = _authService.GetLoginHistory();
            var existingUser = loginInfoList?.FirstOrDefault(user => user.LastLogin == true);

            if (existingUser != null)
            {
                SessionManager.SetServerList(existingUser.ServerListData);
                SessionManager.SetSession(null, existingUser.UserId, existingUser.Username, existingUser.LicenseId, null, existingUser.Password);
            }

            ShowPreLoginLayout();

            await _authService.GetServerListAsync();

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                FileLogger.Log("Network", "No Internet Connection detected at startup.");
                ShowLoginForm();
                return;
            }

            if (loginInfoList == null || !loginInfoList.Any())
            {
                ShowLoginForm(); // First time install case
            }
            else
            {
                if (existingUser != null)
                {
                    // Case 2: Remember Me (Auto Login)
                    if (!string.IsNullOrEmpty(existingUser.Password))
                    {
                        LoginPage loginPage = new LoginPage();
                        bool loginSuccessful = await loginPage.LoginAsync(existingUser.UserId, existingUser.Password, existingUser.LicenseId, existingUser.LastLogin);

                        if (loginSuccessful)
                        {
                            // Login validate ho gaya -> Finalize setup
                            using (var popup = loginPage)
                            {
                                await PerformPostLoginSetup(popup);
                            }
                        }
                        else
                        {
                            ShowLoginForm();
                        }
                    }
                    else
                    {
                        // Case 1: Not Remembered -> Show Login Form (MarketWatch already visible in bg)
                        ShowLoginForm();
                    }
                }
                else
                {
                    ShowLoginForm();
                }
            }
        }

        private async Task PerformPostLoginSetup(LoginPage popup = null)
        {
            await PreloadUserControlsAsync();

            bool disclaimerAcknowledged = await ShowDisclaimerAndCheckAsync();
            if (disclaimerAcknowledged)
            {
                if (!string.IsNullOrEmpty(SessionManager.Token))
                {
                    try
                    {
                        var specificData = await _clientService.GetSpecificClientListAsync();
                        clientDetails = specificData.Clients;
                        var result1 = await _clientService.GetClientListAsync(clientDetails);
                        SessionManager.IsClientDataLoaded = true;
                        SessionManager.SetClientList(result1.Clients);
                    }
                    catch (Exception ex)
                    {
                        FileLogger.Log("Home", "Client Data Load Error: " + ex.Message);
                    }
                }
            }
            else
            {
                ShowLoginForm();
                return;
            }

            // 6. UI Update: Login Validate ho gaya, ab API se naya data leke silently update karo
            InitializeAfterLogin(popup);
        }

        private async void ShowLoginForm()
        {
            using (var popup = new LoginPage())
            {
                ThemeManager.AdjustLoginSize(popup, this);
                var result = popup.ShowDialog(this); // Modal Dialog

                if (result == DialogResult.OK)
                {
                    // User ne credentials dale aur LoginAsync success hua
                    await PerformPostLoginSetup(popup);
                }
            }
        }

        private async Task<bool> ShowDisclaimerAndCheckAsync()
        {
            using (var disclaimerForm = new DisclaimerForm())
            {
                return disclaimerForm.ShowDialog() == DialogResult.OK;
            }
        }
        #endregion

        #region Post Login Initialization
        private async void InitializeAfterLogin(LoginPage popup)
        {
            // UI Header updates
            toolStripDropDownUserButton.Text = SessionManager.UserId;
            disconnectToolStripMenuItem.Text = "Disconnect";
            disconnectToolStripMenuItem.Image = TraderApp.Properties.Resources.disconnectednew;

            string title = (SessionManager.ServerListData?
                .FirstOrDefault(q => q?.licenseId.ToString() == SessionManager.LicenseId)?
                .serverDisplayName ?? "Home");
            this.Text = title;

            if (!SessionManager.IsPasswordReadOnly)
                this.toolStripSeparator6.Visible = true;

            this.SuspendLayout();
            dockPanel.SuspendLayout(true);
            try
            {
                // Ensure controls exist
                if (_detailsUC == null || _detailsUC.IsDisposed) _detailsUC = new DetailsControl();
                if (_marketWatchControl == null || _marketWatchControl.IsDisposed) _marketWatchControl = new MarketWatchControl();

                // Token check for Full Access
                if (!string.IsNullOrEmpty(SessionManager.Token))
                {
                    _detailsUC.EnableFullAccess();
                    _detailsUC.LoadData();
                    FileLogger.Log("System", "Login Successful. Full Access Enabled.");

                    // ✅ CRITICAL: Sync Market Watch with API (Silent Update)
                    // Abhi tak local data dikh raha tha, ab API se fresh data aayega
                    await _marketWatchControl.LoadDataAsync(true); // forceApiSync = true
                }
                else
                {
                    _detailsUC.SetupPreLoginMode();
                    FileLogger.Log("System", "Restricted Mode.");
                }

                UpdatePanelContent("Details", _detailsUC);
                UpdatePanelContent("Market Watch", _marketWatchControl);

                EnsurePanelsVisible();
            }
            finally
            {
                dockPanel.ResumeLayout(true, true);
                this.ResumeLayout(true);
            }

            this.Show();
        }

        private void ShowPreLoginLayout()
        {
            this.SuspendLayout();
            dockPanel.SuspendLayout(true);
            try
            {
                dockPanel.DockBottomPortion = this.Height * 0.30;

                if (_detailsUC == null || _detailsUC.IsDisposed) _detailsUC = new DetailsControl();
                if (_marketWatchControl == null || _marketWatchControl.IsDisposed) _marketWatchControl = new MarketWatchControl();

                // Pre-Login Mode (Hide history)
                _detailsUC.SetupPreLoginMode();

                // ✅ Initial Load: Only Local Data (No API call yet)
                // MarketWatchControl constructor does NOT auto-load anymore to give us control.
                // Call LoadDataAsync(false) -> Loads from FileRepository ("symbol" key)
                _ = _marketWatchControl.LoadDataAsync(false);

                UpdatePanelContent("Market Watch", _marketWatchControl);
                UpdatePanelContent("Details", _detailsUC);
            }
            finally
            {
                dockPanel.ResumeLayout(true, true);
                this.ResumeLayout(true);
            }
        }

        private void UpdatePanelContent(string key, Control newContent)
        {
            DockContent panel;

            if (allPanels.TryGetValue(key, out DockContent existingPanel) && !existingPanel.IsDisposed)
            {
                panel = existingPanel;
                panel.Controls.Clear();
            }
            else
            {
                panel = new DesignTimeHelper.DynamicDockContent(key, null);
                panel.FormClosing += Dock_FormClosing;
                allPanels[key] = panel;
            }

            newContent.Dock = DockStyle.Fill;
            panel.Controls.Add(newContent);
            panel.Text = key;

            if (key == "Market Watch")
            {
                panel.Show(dockPanel, DockState.Document);
            }
            else if (key == "Details")
            {
                panel.Show(dockPanel, DockState.DockBottom);
            }
        }

        private void EnsurePanelsVisible()
        {
            foreach (var kv in allPanels)
            {
                if (kv.Value.DockState == DockState.Hidden)
                {
                    var state = lastDockStates.TryGetValue(kv.Key, out var s) ? s : DockState.Document;
                    kv.Value.Show(dockPanel, state);
                }
            }
        }
        #endregion

        #region Dock Content Management

        private async Task PreloadUserControlsAsync()
        {
            if (_isUserControlsPreloaded) return;
            _isUserControlsPreloaded = true;
        }

        private DockContent CreateEmptyDockContentWithBorder(string title)
        {
            var borderedPanel = new Panel
            {
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill
            };

            var label = new Label
            {
                Text = title,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };

            borderedPanel.Controls.Add(label);

            var dockContent = new DesignTimeHelper.DynamicDockContent(title, borderedPanel);
            dockContent.FormClosing += Dock_FormClosing;
            TrackPanel(dockContent);
            return dockContent;
        }

        #endregion

        #region Panel Tracking And Dropdown
        private void TrackPanel(DockContent panel)
        {
            panel.DockStateChanged += (s, e) =>
            {
                if (panel.DockHandler.DockState != DockState.Unknown &&
                    panel.DockHandler.DockState != DockState.Hidden)
                {
                    lastDockStates[panel.Text] = panel.DockHandler.DockState;
                }
            };
        }

        private void Dock_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; (sender as DockContent)?.Hide(); }
        }

        private void InitializePanelsDropdown()
        {
            panelsDropdown.DropDownItems.Clear();

            var defaultLayoutItem = new ToolStripMenuItem("Default Layout");
            defaultLayoutItem.Click += HiddenPanel_Click;
            defaultLayoutItem.Tag = "Default Layout";
            panelsDropdown.DropDownItems.Add(defaultLayoutItem);

            foreach (var kv in allPanels)
            {
                var item = new ToolStripMenuItem(kv.Key);
                item.Tag = kv.Value;
                item.Checked = kv.Value.Visible;
                item.CheckOnClick = false;
                item.Click += HiddenPanel_Click;
                panelsDropdown.DropDownItems.Add(item);
            }
        }

        private void HiddenPanel_Click(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;

            if (item?.Tag is DockContent panel)
            {
                var state = lastDockStates.TryGetValue(panel.Text, out var s) ? s : DockState.Document;
                if (!panel.Visible)
                {
                    panel.Show(dockPanel, state);
                    item.Checked = true;
                }
                else
                {
                    panel.Hide();
                    item.Checked = false;
                }
                return;
            }

            if (item?.Tag != null && item.Tag.ToString().Equals("Default Layout", StringComparison.OrdinalIgnoreCase))
            {
                ApplyDefaultLayout();
            }
        }

        private void ApplyDefaultLayout()
        {
            bool isLoggedIn = !string.IsNullOrEmpty(toolStripDropDownUserButton.Text);
            EnsurePanelsCreated(isLoggedIn);

            this.SuspendLayout();
            dockPanel.SuspendLayout(true);

            try
            {
                if (allPanels.TryGetValue("Market Watch", out var market))
                    market.Show(dockPanel, DockState.DockLeft);

                if (allPanels.TryGetValue("Navigation", out var nav) && market?.Pane != null)
                    nav.Show(market.Pane, DockAlignment.Bottom, 0.5);

                if (allPanels.TryGetValue("Details", out var details))
                    details.Show(dockPanel, DockState.DockBottom);

                foreach (var kv in allPanels)
                {
                    var p = kv.Value;
                    var st = p.DockHandler.DockState;
                    if (st != DockState.Unknown && st != DockState.Hidden)
                        lastDockStates[p.Text] = st;
                }

                InitializePanelsDropdown();
            }
            finally
            {
                dockPanel.ResumeLayout(true, true);
                this.ResumeLayout(true);
            }
        }

        private void EnsurePanelsCreated(bool isLoggedIn)
        {
            DockContent GetOrCreate(string key, Func<DockContent> factory)
            {
                if (allPanels.TryGetValue(key, out var pane) && !pane.IsDisposed) return pane;
                var created = factory();
                allPanels[key] = created;
                created.FormClosing -= Dock_FormClosing;
                created.FormClosing += Dock_FormClosing;
                TrackPanel(created);
                return created;
            }

            GetOrCreate("Market Watch", () => CreateEmptyDockContentWithBorder("Market Watch"));
            GetOrCreate("Details", () => CreateEmptyDockContentWithBorder("Details"));
        }

        #endregion

        #region Disconnect
        public void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => disconnectToolStripMenuItem_Click(sender, e)));
                return;
            }

            if (disconnectToolStripMenuItem.Text != "Connect" || IsComeFromSocket)
            {
                _isUserControlsPreloaded = false;
                SessionManager.ClearSession();
                FileLogger.Log("System", "User Disconnected.");

                ShowPreLoginLayout();
            }
            ShowLoginForm();
        }
        #endregion

        #region UI Event Handlers

        private void changePasswordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //ChangePassword changePassword = new ChangePassword();
            //changePassword.ShowDialog();
        }

        private void trade_Click(object sender, EventArgs e)
        {
            //TradeOrder tradeOrderForm = new TradeOrder(IsFromMarketWatch: true);
            //tradeOrderForm.ShowDialog();
        }

        #endregion
    }
}