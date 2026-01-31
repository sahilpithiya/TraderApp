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

        private bool _isUserControlsPreloaded = false;
        private bool IsComeFromSocket = false;
        private static ClientDetails clientDetails { get; set; }

        // Track panels
        private Dictionary<string, DockContent> allPanels = new Dictionary<string, DockContent>();
        private Dictionary<string, DockState> lastDockStates = new Dictionary<string, DockState>();

        private DesignTimeHelper layoutHelper;
        #endregion

        #region Nested Classes
        public class MessageItem
        {
            public string Form { get; set; }
            public string Title { get; set; }
            public string Message { get; set; }
            public DateTime DateTime { get; set; }
        }
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
            // Apply theme to dock panel
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
            ShowEmptyColoredLayout();

            this.toolStripSeparator6.Visible = false;

            await _authService.GetServerListAsync();

            var loginInfoList = _authService.GetLoginHistory();

            if (loginInfoList == null || !loginInfoList.Any())
            {
                // No saved user -> Show Login -> Background will be Empty
                ShowLoginForm();
            }
            else
            {
                var existingUser = loginInfoList.FirstOrDefault(user => user.LastLogin == true);
                if (existingUser != null)
                {
                    SessionManager.SetServerList(existingUser.ServerListData);
                    SessionManager.SetSession(null, existingUser.UserId, existingUser.Username, existingUser.LicenseId, null, existingUser.Password);

                    if (string.IsNullOrEmpty(existingUser.Password))
                    {
                        ShowLoginForm();
                        return;
                    }
                    else
                    {
                        // Silent Login for "Remember Me"
                        LoginPage loginPage = new LoginPage();
                        bool loginSuccessful = await loginPage.LoginAsync(existingUser.UserId, existingUser.Password, existingUser.LicenseId, existingUser.LastLogin);

                        if (loginSuccessful)
                        {
                            using (var popup = loginPage)
                            {
                                await PreloadUserControlsAsync();

                                bool disclaimerAcknowledged = await ShowDisclaimerAndCheckAsync();
                                if (disclaimerAcknowledged)
                                {
                                   
                                    var specificData = await _clientService.GetSpecificClientListAsync();
                                    clientDetails = specificData.Clients;

                                    var result1 = await _clientService.GetClientListAsync(clientDetails);
                                    var clients = result1.Clients;
                                    SessionManager.IsClientDataLoaded = true;
                                    SessionManager.SetClientList(clients);
                                }
                                else
                                {
                                    ShowLoginForm();
                                    return;
                                }

                                // SUCCESS: Now we show the panels
                                InitializeAfterLogin(popup);
                            }
                        }
                        else
                        {
                            ShowLoginForm();
                        }
                    }
                }
                else
                {
                    ShowLoginForm();
                }
            }
        }

        private void ConvertLoadedPanelsToEmpty()
        {
            foreach (var kv in allPanels)
            {
                var panel = kv.Value;
                if (panel != null && !panel.IsDisposed)
                {
                    panel.Controls.Clear();
                    panel.Controls.Add(CreateEmptyBorderedControl());
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


        private async void ShowLoginForm()
        {
            using (var popup = new LoginPage())
            {
                ThemeManager.AdjustLoginSize(popup, this);
                var result = popup.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    await PreloadUserControlsAsync();

                    bool disclaimerAcknowledged = await ShowDisclaimerAndCheckAsync();
                    if (disclaimerAcknowledged)
                    {
                        var clientResposne = await _clientService.GetSpecificClientListAsync();
                        clientDetails = clientResposne.Clients;

                        var result1 = await _clientService.GetClientListAsync(clientDetails);
                        var clients = result1.Clients;
                        // Save globally
                        SessionManager.IsClientDataLoaded = true;
                        SessionManager.SetClientList(clients);
                    }
                    else
                    {
                        ShowLoginForm();
                        return;
                    }

                    // Login successful - initialize the UI with user controls
                    InitializeAfterLogin(popup);
                }
            }
        }
        #endregion

        #region Post Login Initialization
        private async void InitializeAfterLogin(LoginPage popup)
        {
            toolStripDropDownUserButton.Text = SessionManager.UserId;
            toolStripDropDownUserButton.Font = new Font(toolStripDropDownUserButton.Font, FontStyle.Bold);
            toolStripDropDownUserButton.DropDown.Font = new Font(toolStripDropDownUserButton.Font, FontStyle.Regular);
            disconnectToolStripMenuItem.Text = "Disconnect";
            disconnectToolStripMenuItem.Image = TraderApp.Properties.Resources.disconnectednew;
            string title = (SessionManager.ServerListData != null
                && SessionManager.ServerListData.Any())
                ? (SessionManager.ServerListData
                    .FirstOrDefault(q => q?.licenseId.ToString() == SessionManager.LicenseId)?
                    .serverDisplayName ?? "Home")
                : "Home";
            this.Text = title;

            if (!SessionManager.IsPasswordReadOnly)
            {
                this.toolStripSeparator6.Visible = true;
            }

            this.SuspendLayout();
            dockPanel.SuspendLayout(true);
            try
            {
                dockPanel.DockBottomPortion = this.Height * 0.30;

                Label lblMarket = new Label();
                lblMarket.Text = "Market Watch";
                lblMarket.TextAlign = ContentAlignment.MiddleCenter;
                lblMarket.BackColor = Color.AliceBlue;
                lblMarket.Dock = DockStyle.Fill;
                lblMarket.Font = new Font("Segoe UI", 14, FontStyle.Bold);


                if (_detailsUC == null || _detailsUC.IsDisposed)
                {
                    _detailsUC = new DetailsControl();
                }
                UpdatePanelContent("Details", _detailsUC);


                // Update Panels
                UpdatePanelContent("Market Watch", lblMarket);

                EnsurePanelsVisible();
            }
            finally
            {
                dockPanel.ResumeLayout(true, true);
                this.ResumeLayout(true);
            }

            this.Show();
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
                // TrackPanel(panel); // Add this if you have the tracking method
                allPanels[key] = panel;
            }

            newContent.Dock = DockStyle.Fill;
            panel.Controls.Add(newContent);
            panel.Text = key;

            // --- CRITICAL CHANGE FOR LAYOUT ---
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
                    // Restore to last known state or default
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

            //_detailsUC = new DetailsControl();

            _isUserControlsPreloaded = true;
        }

        private DockContent CreateEmptyDockContentWithBorder(string title)
        {
            var borderedPanel = new Panel
            {
                BackColor = Color.White, // Changed to White for cleaner look
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill
            };

            var label = new Label
            {
                Text = title, // <--- FIXED: Now uses the title ("Market Watch", etc.)
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 12, FontStyle.Bold) // Made it visible
            };

            borderedPanel.Controls.Add(label);

            var dockContent = new DesignTimeHelper.DynamicDockContent(title, borderedPanel);
            dockContent.FormClosing += Dock_FormClosing;
            TrackPanel(dockContent);
            return dockContent;
        }

        private void ShowEmptyColoredLayout()
        {
            this.SuspendLayout();
            dockPanel.SuspendLayout(true);
            try
            {
                // 1. Set 30% Height for Bottom Panel
                dockPanel.DockBottomPortion = this.Height * 0.30;

                // 2. Create EMPTY colored panels (No Text)
                Panel emptyBluePanel = new Panel { BackColor = Color.AliceBlue, Dock = DockStyle.Fill };
                Panel emptyPinkPanel = new Panel { BackColor = Color.MistyRose, Dock = DockStyle.Fill };

                // 3. Add them to Dock
                UpdatePanelContent("Market Watch", emptyBluePanel);
                UpdatePanelContent("Details", emptyPinkPanel);
            }
            finally
            {
                dockPanel.ResumeLayout(true, true);
                this.ResumeLayout(true);
            }
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
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                var panel = sender as DockContent;
                if (panel != null)
                {
                    panel.Hide();

                    // mark as hidden in dropdown
                    foreach (ToolStripMenuItem item in panelsDropdown.DropDownItems)
                    {
                        if (item.Text == panel.Text)
                        {
                            item.Checked = false;
                        }
                    }
                }
            }
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
                item.Checked = kv.Value.Visible; // checked if hidden
                item.CheckOnClick = false; // prevent auto toggle, we manage manually
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
            EnsurePanelsCreated(isLoggedIn); // defined below

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
            // if any missing/disposed, (re)create it ONCE
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

        #region Disconnect Handling

        private void SocketForceLogOut(string userId)
        {
            IsComeFromSocket = true;
            disconnectToolStripMenuItem_Click(this, EventArgs.Empty);
        }

        public void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => disconnectToolStripMenuItem_Click(sender, e)));
                return;
            }

            // ---------- FROM HERE ↓ WE ARE 100% ON UI THREAD ----------

            if (disconnectToolStripMenuItem.Text != "Connect" || IsComeFromSocket)
            {
                //_safeDispose(_marketWatchUC);
                //_safeDispose(_navigationUC);
                //_safeDispose(_chartUC);
                //_safeDispose(_detailsUC);

                //_marketWatchUC = null;
                //_navigationUC = null;
                //_chartUC = null;
                //_detailsUC = null;

                _isUserControlsPreloaded = false;

                this.SuspendLayout();
                dockPanel.SuspendLayout(true);

                foreach (var kv in allPanels)
                {
                    var panel = kv.Value;
                    if (panel != null && !panel.IsDisposed)
                    {
                        panel.Controls.Clear();
                        var emptyControl = CreateEmptyBorderedControl();
                        panel.Controls.Add(emptyControl);
                    }
                }

                dockPanel.ResumeLayout(true, true);
                this.ResumeLayout(true);
                IsComeFromSocket = false;
                this.Text = string.Empty;
                toolStripDropDownUserButton.Text = "";
                disconnectToolStripMenuItem.Text = "Connect";
                disconnectToolStripMenuItem.Image = TraderApp.Properties.Resources.connected;
                this.changePasswordToolStripMenuItem.Visible = false;
                this.trade.Visible = false;
                this.panelsDropdown.Visible = false;
                this.toolStripSeparator6.Visible = false;

                //ShowEmptyDockLayout();
                SessionManager.ClearSession();

            }
            ShowLoginForm();
        }

        private Control CreateEmptyBorderedControl()
        {
            var borderedPanel = new Panel
            {
                //BackColor = ThemeManager.Gray,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill
            };
            var label = new Label
            {
                Text = string.Empty,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = ThemeManager.Black
            };
            borderedPanel.Controls.Add(label);
            return borderedPanel;
        }

        private void OnNavigationLoginSelected(LoginInfo login)
        {
            if (this.IsDisposed) return;

            BeginInvoke(new Action(() =>
            {
                if (!this.IsDisposed)
                {
                    disconnectToolStripMenuItem_Click(this, EventArgs.Empty);
                }
            }));
        }

        private void CloseNonPanelForms()
        {
            var openForms = Application.OpenForms.Cast<Form>().ToList();
            foreach (Form form in openForms)
            {
                if (form != this)
                {
                    try
                    {
                        form.Hide();
                        form.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error closing form: {ex.Message}");
                    }
                }
            }
        }

        private void _safeDispose(Control c)
        {
            if (c != null && !c.IsDisposed)
                c.Dispose();
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

        //public void OpenChart(string displaySymbol)
        //{
        //    // Get master symbol from SessionManager
        //    var symbolInfo = SessionManager.SymbolNameList
        //        .FirstOrDefault(q => q.symbolName.Equals(displaySymbol));

        //    if (symbolInfo == null)
        //    {
        //        MessageBox.Show($"Symbol '{displaySymbol}' not found!", "Error",
        //                        MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        return;
        //    }

        //    string masterSymbol = symbolInfo.masterSymbolName;

        //    //foreach (IDockContent document in dockPanel.Contents)
        //    //{
        //    //    if (document is ChartWindow existingChart &&
        //    //        existingChart.Tag?.ToString() == displaySymbol)
        //    //    {
        //    //        existingChart.Activate();
        //    //        return;
        //    //    }
        //    //}

        //    // Create new chart with display symbol
        //    var newChart = new ChartWindow(displaySymbol);
        //    newChart.Tag = displaySymbol; // Track by master symbol
        //    newChart.Show(this.dockPanel, DockState.Document);
        //}

        #endregion
    }
}
