// Helpers & Models
using ClientDesktop;
using ClientDesktop.HelperClass;   // Ensure these namespaces exist in your project
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
using System.Windows.Forms;    // For LoginPage
using TraderApps.Config;
using TraderApps.Helpers;
using WeifenLuo.WinFormsUI.Docking; // DockPanelSuite

namespace TraderApps
{
    public partial class Home : Form
    {
        #region Variables
        public static Home Instance; // Singleton access
        private DockPanel _dockPanel;
        private static readonly HttpClient _http = new HttpClient();
        public static bool isViewLocked = false;
        private static ClientDetails clientDetails { get; set; }

        // User Controls (Lazy Loaded)
        ////private UCMarketWatchControl _marketWatchUC;
        ////private UCNavigationControl _navigationUC;
        ////private UCDetailsControl _detailsUC;
        // private UCChartControl _chartUC; // Uncomment when you have the chart control

        private bool _isUserControlsPreloaded = false;
        private bool IsComeFromSocket = false;

        // DockContents (Wrappers for DockPanel)
        private DockContent _marketWatchDock;
        private DockContent _navigationDock;
        private DockContent _detailsDock;
        #endregion

        public Home()
        {
            Instance = this;
            InitializeComponent();

            // 1. Setup UI
            SetupDockPanel();
            ThemeManager.ApplyTheme(this);

            // 2. Setup Events
            this.FormClosing += Home_FormClosing;
        }

        private void SetupDockPanel()
        {
            _dockPanel = new DockPanel();
            _dockPanel.Dock = DockStyle.Fill;

            // Note: Ensure you have installed 'DockPanelSuite.ThemeVS2015' via NuGet
            _dockPanel.Theme = new VS2015LightTheme();
            _dockPanel.DocumentStyle = DocumentStyle.DockingMdi;

            this.Controls.Add(_dockPanel);
        }

        private void Home_Load(object sender, EventArgs e)
        {
            // Start the Authentication Flow
            InitializeHome();
        }

        private void Home_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Save layout logic can go here
        }

        #region Authentication & Startup Logic

        public async void InitializeHome()
        {
            // 1. Load Server List
            await LoadServerListAsync();

            // 2. Check for Cached Login ("Remember Me")
            string filePath = Path.Combine(AppConfig.dataFolder, $"{AESHelper.ToBase64UrlSafe("LoginData")}.dat");
            var loginInfoList = CommonHelper.LoadLoginDataFromCache(filePath);

            if (loginInfoList == null || !loginInfoList.Any(u => u.LastLogin))
            {
                // No saved user, show login
                ShowLoginForm();
            }
            else
            {
                // Auto-Login Logic
                var existingUser = loginInfoList.First(user => user.LastLogin);

                SessionManager.SetServerList(existingUser.ServerListData);
                SessionManager.SetSession(null, existingUser.UserId, existingUser.Username, existingUser.LicenseId, null, existingUser.Password);

                if (string.IsNullOrEmpty(existingUser.Password))
                {
                    ShowLoginForm();
                    return;
                }

                // Verify credentials silently
                LoginPage loginPage = new LoginPage();
                bool loginSuccessful = await loginPage.LoginAsync(existingUser.UserId, existingUser.Password, existingUser.LicenseId, existingUser.LastLogin);

                if (loginSuccessful)
                {
                    using (var popup = loginPage)
                    {
                        await PreloadUserControlsAsync();

                        // Disclaimer Check
                        bool disclaimerAcknowledged = await ShowDisclaimerAndCheckAsync();
                        if (!disclaimerAcknowledged)
                        {
                            ShowLoginForm();
                            return;
                        }

                        // Load Client Data
                        await LoadClientDataAsync();

                        InitializeAfterLogin();
                    }
                }
                else
                {
                    ShowLoginForm(); // Token expired or changed password
                }
            }
        }

        private async void ShowLoginForm()
        {
            // Hide main form or ensure it's empty before login
            if (_dockPanel.Contents.Count > 0)
            {
                // Optional: Clear panels if logging out
            }

            using (var popup = new LoginPage())
            {
                ThemeManager.AdjustLoginSize(popup, this);
                var result = popup.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    await PreloadUserControlsAsync();

                    bool disclaimerAcknowledged = await ShowDisclaimerAndCheckAsync();
                    if (!disclaimerAcknowledged)
                    {
                        ShowLoginForm(); // Rejected disclaimer
                        return;
                    }

                    await LoadClientDataAsync();

                    InitializeAfterLogin();
                }
                else
                {
                    // If user cancels login, close the app?
                    if (SessionManager.Token == null) Application.Exit();
                }
            }
        }

        private async Task LoadClientDataAsync()
        {
            var specificData = await GetSpecificClientListAsync();
            clientDetails = specificData.Clients;

            var result1 = await GetClientListAsync();
            var clients = result1.Clients;

            SessionManager.IsClientDataLoaded = true;
            SessionManager.SetClientList(clients);
        }

        private void InitializeAfterLogin()
        {
            // Update Title
            string title = "Home";
            if (SessionManager.ServerListData != null && SessionManager.ServerListData.Any())
            {
                var server = SessionManager.ServerListData.FirstOrDefault(q => q?.licenseId.ToString() == SessionManager.LicenseId);
                if (server != null) title = server.serverDisplayName;
            }
            this.Text = $"{title} - {SessionManager.UserId}";

            // Initialize Dock Panels
            ShowDockPanels();
        }

        #endregion

        #region UI & Docking Logic

        private async Task PreloadUserControlsAsync()
        {
            if (_isUserControlsPreloaded) return;

            ////_marketWatchUC = new UCMarketWatchControl();
            ////_navigationUC = new UCNavigationControl();
            ////_navigationUC.LoginSelected += OnNavigationLoginSelected;
            ////_detailsUC = new UCDetailsControl();

            // _chartUC = new UCChartControl();

            _isUserControlsPreloaded = true;
        }

        private void ShowDockPanels()
        {
            _dockPanel.SuspendLayout(true);

            //// 1. Market Watch (Left)
            //if (_marketWatchDock == null || _marketWatchDock.IsDisposed)
            //{
            //    _marketWatchDock = CreateDockContent("Market Watch", _marketWatchUC);
            //}
            //_marketWatchDock.Show(_dockPanel, DockState.DockLeft);

            //// 2. Details (Bottom)
            //if (_detailsDock == null || _detailsDock.IsDisposed)
            //{
            //    _detailsDock = CreateDockContent("Details", _detailsUC);
            //}
            //_detailsDock.Show(_dockPanel, DockState.DockBottom);

            //// 3. Navigation (Tabbed under Market Watch or Bottom)
            //if (_navigationDock == null || _navigationDock.IsDisposed)
            //{
            //    _navigationDock = CreateDockContent("Navigation", _navigationUC);
            //}

            // Dock Navigation at the bottom of the MarketWatch pane
            //if (_marketWatchDock.Pane != null)
            //    _navigationDock.Show(_marketWatchDock.Pane, DockAlignment.Bottom, 0.5);
            //else
            //    _navigationDock.Show(_dockPanel, DockState.DockLeft);

            _dockPanel.ResumeLayout(true, true);
        }

        private DockContent CreateDockContent(string title, UserControl control)
        {
            var doc = new DockContent();
            doc.Text = title;
            doc.CloseButtonVisible = false; // Prevent accidental closing
            control.Dock = DockStyle.Fill;
            doc.Controls.Add(control);
            return doc;
        }

        #endregion

        #region Helpers (API & Files)

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
            string encryptedFilePath = Path.Combine(AppConfig.dataFolder, folder, $"{file}.dat");

            // Only fetch if local file doesn't exist
            if (!File.Exists(encryptedFilePath))
            {
                await ServerService.GetServerListAsync();
            }
        }

        public static async Task<(bool Success, string ErrorMessage, ClientDetails Clients)> GetSpecificClientListAsync()
        {
            try
            {
                _http.AddAuthHeader();
                var stringURL = $"{AppConfig.ClientListURL.ToReplaceUrl()}/{SessionManager.UserId}";
                var response = await _http.GetAsync(stringURL).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var parsed = JsonConvert.DeserializeObject<ClientDetailsRootModel>(json);
                    return (true, null, parsed?.data);
                }
                return (false, "Failed to load specific client", null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public static async Task<(bool Success, string ErrorMessage, List<ClientDetails> Clients)> GetClientListAsync()
        {
            // (Simulated Logic based on your previous file)
            // In a real scenario, copy the full caching/decryption logic from the original file 
            // if you need offline support. Here is the direct API fetch:

            try
            {
                _http.AddAuthHeader();
                var response = await _http.GetAsync(AppConfig.MasterClientListURL.ToReplaceUrl()).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var parsed = JsonConvert.DeserializeObject<ClientDetailsRootModel>(json);

                    if (parsed?.data != null)
                    {
                        // Logic to merge specific client details (Balance/Credit)
                        if (clientDetails != null)
                        {
                            parsed.data.CreditAmount = clientDetails.CreditAmount;
                            parsed.data.Balance = clientDetails.Balance;
                            // ... map other fields
                        }
                        return (true, null, new List<ClientDetails> { parsed.data });
                    }
                }
                return (false, "Failed to load master client", null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        #endregion

        #region Logout & Disconnect

        private void SocketForceLogOut(string userId)
        {
            IsComeFromSocket = true;
            PerformDisconnect();
        }

        private void OnNavigationLoginSelected(LoginInfo login)
        {
            // User clicked a different account in Navigation -> Logout current, Login new
            this.BeginInvoke(new Action(() => PerformDisconnect()));
        }

        private void PerformDisconnect()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(PerformDisconnect));
                return;
            }

            ////// Dispose Controls safely
            ////if (_marketWatchUC != null) _marketWatchUC.Dispose();
            ////if (_navigationUC != null) _navigationUC.Dispose();
            ////if (_detailsUC != null) _detailsUC.Dispose();

            _isUserControlsPreloaded = false;
            SessionManager.ClearSession();

            // Close all DockContents
            foreach (var content in _dockPanel.Contents.ToList())
            {
                if (content is DockContent dc) dc.Close();
            }

            IsComeFromSocket = false;

            // Restart Login Process
            ShowLoginForm();
        }

        #endregion
    }
}