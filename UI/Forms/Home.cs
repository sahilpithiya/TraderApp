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
using TraderApps.Config;
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

        // Dockable panels
        private DockContent MarketwatchDock;
        private DockContent NavigationDock;
        private DockContent ChartDock;
        private DockContent DetailsDock;

        //private UCMarketWatchControl _marketWatchUC;
        //private UCNavigationControl _navigationUC;
        //private UCChartControl _chartUC;
        //private UCDetailsControl _detailsUC;
        private bool _isUserControlsPreloaded = false;
        private bool IsComeFromSocket = false;
        private static ClientDetails clientDetails { get; set; }

        // Track panels
        private Dictionary<string, DockContent> allPanels = new Dictionary<string, DockContent>();
        private Dictionary<string, DockState> lastDockStates = new Dictionary<string, DockState>();
        private static readonly HttpClient _http = new HttpClient();

        private DesignTimeHelper layoutHelper;
        public static bool isViewLocked = false;
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
            ThemeManager.ApplyTheme(this);
            //SocketManager.OnForceLogout += SocketForceLogOut;
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
            string layoutFile = Path.Combine(AppConfig.dataFolder, "layout.xml");

            bool isLayoutLoaded = false;

            if (File.Exists(layoutFile))
            {
                try
                {
                    dockPanel.LoadFromXml(layoutFile, DeserializeDockContent);
                    isLayoutLoaded = true;

                    ConvertLoadedPanelsToEmpty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Layout load failed: " + ex.Message);
                }
            }

            if (!isLayoutLoaded)
            {
                ShowEmptyDockLayout();
            }

            //this.changePasswordToolStripMenuItem.Visible = false;
            //this.trade.Visible = false;
            //this.panelsDropdown.Visible = false;
            this.toolStripSeparator6.Visible = false;

            await LoadServerListAsync();
            string filePath = Path.Combine(Path.Combine(AppConfig.dataFolder, $"{AESHelper.ToBase64UrlSafe("LoginData")}.dat"));
            var loginInfoList = CommonHelper.LoadLoginDataFromCache(filePath);

            if (loginInfoList == null)
            {
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
                                    var specificData = await GetSpecificClientListAsync();
                                    clientDetails = specificData.Clients;
                                    var result1 = await GetClientListAsync();
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

                                InitializeAfterLogin(popup);
                                //SocketManager.Start();
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

        private async Task LoadServerListAsync()
        {
            string folder = AESHelper.ToBase64UrlSafe("Servers");
            string file = AESHelper.ToBase64UrlSafe("ServerList");
            string encryptedContent = null;

            // Try to read local encrypted file (if exists)
            string encryptedFilePath = Path.Combine(AppConfig.dataFolder, folder, $"{file}.dat");
            if (!File.Exists(encryptedFilePath))
            {
                var result = await ServerService.GetServerListAsync();
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
                        var clientResposne = await GetSpecificClientListAsync();
                        clientDetails = clientResposne.Clients;
                        var result1 = await GetClientListAsync();
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
                    //SocketManager.Start();
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

            //this.changePasswordToolStripMenuItem.Visible = true;
            if (!SessionManager.IsPasswordReadOnly)
            {
                //this.trade.Visible = true;
                this.toolStripSeparator6.Visible = true;
            }
            //this.panelsDropdown.Visible = true;


            //if (!_isUserControlsPreloaded || _marketWatchUC == null || _marketWatchUC.IsDisposed)
            //    await PreloadUserControlsAsync();

            this.SuspendLayout();
            dockPanel.SuspendLayout(true);

            try
            {
                //// Panels ko destroy karne ke bajaye, content swap kar rahe hain
                //UpdatePanelContent("Market Watch", _marketWatchUC);

                //// Navigation panel ke liye special case (dock position check)
                //UpdatePanelContent("Navigation", _navigationUC);

                //UpdatePanelContent("Chart", _chartUC);
                //UpdatePanelContent("Details", _detailsUC);

                //EnsurePanelsVisible();
            }
            finally
            {
                // UI Updates resume kar
                dockPanel.ResumeLayout(true, true);
                this.ResumeLayout(true);
            }

            // Dropdown update
            InitializePanelsDropdown();

            // Re-attach closing events just in case
            foreach (var kv in allPanels)
            {
                var panel = kv.Value;
                panel.FormClosing -= Dock_FormClosing;
                panel.FormClosing += Dock_FormClosing;
                TrackPanel(panel);
            }

            this.Show();
        }

        private void UpdatePanelContent(string key, UserControl newContent)
        {
            if (allPanels.TryGetValue(key, out DockContent panel) && !panel.IsDisposed)
            {
                panel.Controls.Clear();

                newContent.Dock = DockStyle.Fill;
                panel.Controls.Add(newContent);

                panel.Text = key;
            }
            else
            {
                var newPanel = CreateDockContentWithUserControl(key, newContent);
                allPanels[key] = newPanel;

                // Default showing logic
                if (key == "Market Watch") newPanel.Show(dockPanel, DockState.DockLeft);
                else if (key == "Chart") newPanel.Show(dockPanel, DockState.Document);
                else if (key == "Details") newPanel.Show(dockPanel, DockState.DockBottom);
                else if (key == "Navigation" && allPanels.ContainsKey("Market Watch"))
                    newPanel.Show(allPanels["Market Watch"].Pane, DockAlignment.Bottom, 0.5);
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
        private IDockContent DeserializeDockContent(string persistString)
        {
            if (string.IsNullOrEmpty(persistString))
                return null;

            if (!allPanels.TryGetValue(persistString, out DockContent panel))
            {
                switch (persistString)
                {
                    //case "Market Watch":
                    //    if (_marketWatchUC == null || _marketWatchUC.IsDisposed)
                    //        _marketWatchUC = new UCMarketWatchControl();
                    //    panel = CreateDockContentWithUserControl("Market Watch", _marketWatchUC);
                    //    break;
                    //case "Navigation":
                    //    if (_navigationUC == null || _navigationUC.IsDisposed)
                    //    {
                    //        _navigationUC = new UCNavigationControl();
                    //        _navigationUC.LoginSelected -= OnNavigationLoginSelected;
                    //        _navigationUC.LoginSelected += OnNavigationLoginSelected;
                    //    }
                    //    panel = CreateDockContentWithUserControl("Navigation", _navigationUC);
                    //    break;
                    //case "Chart":
                    //    if (_chartUC == null || _chartUC.IsDisposed)
                    //        _chartUC = new UCChartControl();
                    //    panel = CreateDockContentWithUserControl("Chart", _chartUC);
                    //    break;
                    //case "Details":
                    //    if (_detailsUC == null || _detailsUC.IsDisposed)
                    //        _detailsUC = new UCDetailsControl();
                    //    panel = CreateDockContentWithUserControl("Details", _detailsUC);
                    //    break;
                    //default:
                    //    return null;
                }

                allPanels[persistString] = panel;
            }

            return panel;
        }

        private DockContent CreateDockContentWithUserControl(string title, UserControl userControl)
        {
            var dockContent = new DesignTimeHelper.DynamicDockContent(title, userControl);
            dockContent.FormClosing += Dock_FormClosing;
            TrackPanel(dockContent);
            return dockContent;
        }

        private async Task PreloadUserControlsAsync()
        {
            if (_isUserControlsPreloaded) return;

            //_marketWatchUC = new UCMarketWatchControl();
            //_navigationUC = new UCNavigationControl();
            //_navigationUC.LoginSelected -= OnNavigationLoginSelected;
            //_navigationUC.LoginSelected += OnNavigationLoginSelected;
            //_chartUC = new UCChartControl();
            //_detailsUC = new UCDetailsControl();

            _isUserControlsPreloaded = true;
        }

        //private void ShowDockPanelsWithUserControls()
        //{
        //    MarketwatchDock = new DesignTimeHelper.DynamicDockContent("Market Watch", _marketWatchUC);
        //    allPanels["Market Watch"] = MarketwatchDock;
        //    MarketwatchDock.Show(dockPanel, DockState.DockLeft);

        //    NavigationDock = new DesignTimeHelper.DynamicDockContent("Navigation", _navigationUC);
        //    allPanels["Navigation"] = NavigationDock;
        //    NavigationDock.Show(MarketwatchDock.Pane, DockAlignment.Bottom, 0.5);

        //    ChartDock = new DesignTimeHelper.DynamicDockContent("Chart", _chartUC);
        //    allPanels["Chart"] = ChartDock;
        //    ChartDock.Show(dockPanel, DockState.Document);

        //    DetailsDock = new DesignTimeHelper.DynamicDockContent("Details", _detailsUC);
        //    allPanels["Details"] = DetailsDock;
        //    DetailsDock.Show(dockPanel, DockState.DockBottom);
        //}

        private void ShowEmptyDockLayout()
        {
            // Create empty dock contents with bordered panels
            MarketwatchDock = CreateEmptyDockContentWithBorder("Market Watch");
            allPanels["Market Watch"] = MarketwatchDock;
            MarketwatchDock.Show(dockPanel, DockState.DockLeft);

            NavigationDock = CreateEmptyDockContentWithBorder("Navigation");
            allPanels["Navigation"] = NavigationDock;
            NavigationDock.Show(MarketwatchDock.Pane, DockAlignment.Bottom, 0.5);

            ChartDock = CreateEmptyDockContentWithBorder("Chart");
            allPanels["Chart"] = ChartDock;
            ChartDock.Show(dockPanel, DockState.Document);

            DetailsDock = CreateEmptyDockContentWithBorder("Details");
            allPanels["Details"] = DetailsDock;
            DetailsDock.Show(dockPanel, DockState.DockBottom);
        }

        private DockContent CreateEmptyDockContentWithBorder(string title)
        {
            // Create a panel with visible border
            var borderedPanel = new Panel
            {
                BackColor = ThemeManager.Gray,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill
            };

            // Add a label to indicate this area needs login
            var label = new Label
            {
                Text = string.Empty,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = ThemeManager.Black
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

                if (allPanels.TryGetValue("Chart", out var chart))
                    chart.Show(dockPanel, DockState.Document);

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

            //if (isLoggedIn)
            //{
            //    GetOrCreate("Market Watch", () => CreateDockContentWithUserControl("Market Watch", _marketWatchUC));
            //    GetOrCreate("Navigation", () => CreateDockContentWithUserControl("Navigation", _navigationUC));
            //    GetOrCreate("Chart", () => CreateDockContentWithUserControl("Chart", _chartUC));
            //    GetOrCreate("Details", () => CreateDockContentWithUserControl("Details", _detailsUC));
            //}
            //else
            //{
                GetOrCreate("Market Watch", () => CreateEmptyDockContentWithBorder("Market Watch"));
                GetOrCreate("Navigation", () => CreateEmptyDockContentWithBorder("Navigation"));
                GetOrCreate("Chart", () => CreateEmptyDockContentWithBorder("Chart"));
                GetOrCreate("Details", () => CreateEmptyDockContentWithBorder("Details"));
            //}
        }

        #endregion

        #region Client List Management
        public static async Task<(bool Success, string ErrorMessage, List<ClientDetails> Clients)> GetClientListAsync()
        {
            string domain = SessionManager.ServerListData
                .FirstOrDefault(w => w.licenseId.ToString() == SessionManager.LicenseId)?
                .serverDisplayName;

            string folder = Path.Combine(AppConfig.dataFolder, AESHelper.ToBase64UrlSafe(domain));
            string fileName = $"{AESHelper.ToBase64UrlSafe(SessionManager.UserId)}.dat";
            string filePath = Path.Combine(folder, fileName);

            var encryptedFileData = new List<ClientDetails>();

            if (File.Exists(filePath))
            {
                // Await the async method to get cached client data
                encryptedFileData = await CommonHelper.LoadClientDataFromCacheAsync(filePath);
            }

            try
            {
                _http.AddAuthHeader();
                var response = await _http.GetAsync(AppConfig.MasterClientListURL.ToReplaceUrl()).ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    // If the response fails, return cached data if available
                    return (true, $"Failed to get client details: {(int)response.StatusCode} {response.ReasonPhrase}\n{json}", encryptedFileData);
                }

                // Deserialize the API response using the updated model
                var parsed = JsonConvert.DeserializeObject<ClientDetailsRootModel>(json);

                if (encryptedFileData != null && encryptedFileData.Count != 0 && (parsed == null || parsed.data == null || parsed.data == null))
                {
                    // If the data from API is invalid, return the cached data
                    return (true, "Invalid or empty client details response", encryptedFileData);
                }
                else if (encryptedFileData != null && encryptedFileData.Count == 0 && (parsed == null || parsed.data == null || parsed.data == null))
                {
                    // If no data is available, return null and error message
                    return (true, "Invalid or empty client details response", null);
                }

                // Load existing data from file if it exists or initialize a new dictionary
                var existingData = File.Exists(filePath)
                    ? JsonConvert.DeserializeObject<Dictionary<string, object>>(AESHelper.DecompressAndDecryptString(File.ReadAllText(filePath)))
                    : new Dictionary<string, object>();

                var ClientObj = parsed.data;
                if (clientDetails != null)
                {
                    ClientObj.CreditAmount = clientDetails.CreditAmount;
                    ClientObj.UplineAmount = clientDetails.UplineAmount;
                    ClientObj.Balance = clientDetails.Balance;
                    ClientObj.OccupiedMarginAmount = clientDetails.OccupiedMarginAmount;
                    ClientObj.UplineCommission = clientDetails.UplineCommission;
                }

                // Add or update the "client" data in the dictionary
                existingData["client"] = ClientObj;
                isViewLocked = ClientObj.IsViewLocked;

                // Serialize and encrypt the updated data before saving it back to the file
                string updatedJson = JsonConvert.SerializeObject(existingData);
                string encryptedUpdatedJson = AESHelper.CompressAndEncryptString(updatedJson);
                string decryptedUpdateJson = AESHelper.DecompressAndDecryptString(encryptedUpdatedJson);
                string reencryptedUpdatedJson = AESHelper.CompressAndEncryptString(decryptedUpdateJson);

                // Save the encrypted data back to the file
                CommonHelper.SaveEncryptedData(folder, AESHelper.ToBase64UrlSafe(SessionManager.UserId), reencryptedUpdatedJson);

                // Return the list of client details
                return (true, null, new List<ClientDetails> { ClientObj });
            }
            catch (Exception ex)
            {
                // Return the error message and cached data if available
                return (true, ex.Message, encryptedFileData);
            }
        }

        public static async Task<(bool Success, string ErrorMessage, ClientDetails Clients)> GetSpecificClientListAsync()
        {
            string domain = SessionManager.ServerListData
                .FirstOrDefault(w => w.licenseId.ToString() == SessionManager.LicenseId)?
                .serverDisplayName;

            try
            {
                _http.AddAuthHeader();

                var stringURL = $"{AppConfig.ClientListURL.ToReplaceUrl()}/{SessionManager.UserId}";
                var response = await _http.GetAsync(stringURL).ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    // If the response fails, return cached data if available
                    return (true, $"Failed to get client details: {(int)response.StatusCode} {response.ReasonPhrase}\n{json}", null);
                }

                // Deserialize the API response using the updated model
                var parsed = JsonConvert.DeserializeObject<ClientDetailsRootModel>(json);

                if (parsed == null || parsed.data == null || parsed.data == null)
                {
                    // If the data from API is invalid, return the cached data
                    return (true, "Invalid or empty client details response", null);
                }
                else if (parsed == null || parsed.data == null || parsed.data == null)
                {
                    // If no data is available, return null and error message
                    return (true, "Invalid or empty client details response", null);
                }


                // Return the list of client details
                return (true, null, parsed.data);
            }
            catch (Exception ex)
            {
                // Return the error message and cached data if available
                return (true, ex.Message, null);
            }
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
