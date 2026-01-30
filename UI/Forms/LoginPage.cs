using ClientDesktop;
using ClientDesktop.HelperClass;
using ClientDesktop.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TraderApps.Config;
using TraderApps.Helpers;
using TraderApps.Models;
using TraderApps.Services;

namespace TraderApps
{
    public partial class LoginPage : Form
    {
        #region Variables
        private bool isSelectedServer;
        private string isValidated = string.Empty;
        string filePath = Path.Combine(Path.Combine(AppConfig.dataFolder, $"{AESHelper.ToBase64UrlSafe("LoginData")}.dat"));
        #endregion

        #region Form Initialization
        public LoginPage()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
            ThemeManager.ApplyTheme(loginButton);
            ThemeManager.ApplyTheme(cancleButton);
            cancleButton.BackColor = ThemeManager.Red;

            ApplyResponsiveLayout();
            CenterControls();

            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            // Attach event handlers
            this.AcceptButton = loginButton;
            this.cmbLogin.KeyPress -= cmbLogin_KeyPress;
            this.cmbLogin.KeyPress += cmbLogin_KeyPress;
            this.cmbServerName.TextChanged -= cmbServerName_TextChanged;
            this.cmbServerName.TextChanged += cmbServerName_TextChanged;
            this.cmbLogin.TextChanged -= cmbLogin_TextChanged;
            this.cmbLogin.TextChanged += cmbLogin_TextChanged;
            this.txtpassword.TextChanged -= txtpassword_TextChanged;
            this.txtpassword.TextChanged += txtpassword_TextChanged;
        }

        private void ApplyResponsiveLayout()
        {
            this.Width = CommonHelper.GetScaled(this.Width);
            this.Height = CommonHelper.GetScaled(this.Height);

            foreach (Control ctrl in this.Controls)
            {
                ctrl.Left = CommonHelper.GetScaled(ctrl.Left);
                ctrl.Top = CommonHelper.GetScaled(ctrl.Top);
                ctrl.Width = CommonHelper.GetScaled(ctrl.Width);
                ctrl.Height = CommonHelper.GetScaled(ctrl.Height);

                if (ctrl.Font != null)
                {
                    float originalSize = ctrl.Font.Size;
                    float scaledSize = CommonHelper.GetScaled(originalSize);

                    if (scaledSize < 9f) scaledSize = 9f;

                    ctrl.Font = new Font(ctrl.Font.FontFamily, scaledSize, ctrl.Font.Style, ctrl.Font.Unit);
                }
            }
        }
        #endregion

        #region Authentication And Login Handling
        public async void loginButton_Click(object sender, EventArgs e)
        {
            string userId, password, licenseId;

            // Validate user input
            if (!ValidateLoginInput(out userId, out password, out licenseId))
            {
                return; // If validation fails, stop further execution
            }

            // Disable UI while authenticating
            loginButton.Enabled = false;
            cancleButton.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                // Call the LoginAsync method
                bool loginSuccess = await LoginAsync(userId, password, licenseId, checkBox1.Checked);

                if (loginSuccess)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else if (string.IsNullOrEmpty(isValidated))
                {
                    //lblError.Text = "Login failed. Please check your credentials.";
                    //// Needs to log - MessagePopup.ShowPopup(CommonMessages.Loginfaild, false);
                }
            }
            finally
            {
                loginButton.Enabled = true;
                cancleButton.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        public async Task<bool> LoginAsync(string userId, string password, string licenseId, bool isLastLogin = false)
        {
            // Proceed with authentication
            SessionManager.SetSession(string.Empty, userId, string.Empty ?? userId, licenseId, null, password);

            // Disable UI during authentication
            loginButton.Enabled = false;
            cancleButton.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                var result = await AuthService.AuthenticateAsync(userId, password, licenseId);

                if (!result.Success)
                {
                    isValidated = result.ErrorMessage;
                    //// Needs to add into log MessagePopup.ShowPopup(result.ErrorMessage, false);
                    return false;
                }

                var data = result.ResponseData;
                // Save to session
                DateTime? exp = null;
                if (DateTime.TryParse(data.expiration, out var dt)) exp = dt;

                // Proceed with session management and storing login data
                SessionManager.SetSession(data.token, userId, data.name ?? userId, licenseId, exp, password);
                SetSessionAndStoreLoginData(userId, password, licenseId, isLastLogin);
                await AuthService.AuthenticateAsyncGET();

                return true;
            }
            finally
            {
                loginButton.Enabled = true;
                cancleButton.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void SetSessionAndStoreLoginData(string username, string password, string licenseId, bool isRemember)
        {
            // Remember me logic
            try
            {
                _ = CommonHelper.StoreLoginDataAsync(Path.Combine(AppConfig.dataFolder, $"{AESHelper.ToBase64UrlSafe("LoginData")}.dat"), password, isRemember);
            }
            catch { }

        }

        private bool ValidateLoginInput(out string username, out string password, out string licenseId)
        {
            username = SessionManager.UserId;
            password = SessionManager.Password;
            licenseId = SessionManager.LicenseId;

            bool isValid = true;

            if (string.IsNullOrEmpty(SessionManager.Username) || string.IsNullOrEmpty(SessionManager.Password) || string.IsNullOrEmpty(SessionManager.LicenseId))
            {
                username = cmbLogin.Text?.Trim();
                password = txtpassword.Text ?? string.Empty;

                if (cmbServerName.SelectedItem is ServerList sel)
                {
                    licenseId = sel.licenseId.ToString() ?? string.Empty ?? "0";
                }
            }
            else
            {
                checkBox1.Checked = true;
            }

            if (string.IsNullOrEmpty(username))
            {
                cmbLogin.BackColor = ThemeManager.LightRed;
                isValid = false;
            }

            if (string.IsNullOrEmpty(password))
            {
                txtpassword.BackColor = ThemeManager.LightRed;
                isValid = false;
            }

            if (string.IsNullOrEmpty(licenseId) && (cmbServerName.SelectedItem == null || string.IsNullOrEmpty(cmbServerName.Text)))
            {
                cmbServerName.BackColor = ThemeManager.LightRed;
                isValid = false;
            }

            return isValid;
        }

        #endregion

        #region Server List Management
        private async Task LoadServerListAsync()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                var serverList = SessionManager.ServerListData;

                string folder = AESHelper.ToBase64UrlSafe("Servers");
                string file = AESHelper.ToBase64UrlSafe("ServerList");
                string encryptedContent = null;

                // Try to read local encrypted file (if exists)
                string encryptedFilePath = Path.Combine(AppConfig.dataFolder, folder, $"{file}.dat");
                if (File.Exists(encryptedFilePath))
                {
                    encryptedContent = File.ReadAllText(encryptedFilePath);

                    string encrptedFilejson = AESHelper.DecompressAndDecryptString(encryptedContent);
                    var fallbackParsed = JsonConvert.DeserializeObject<ServerListResponse>(encrptedFilejson);

                    serverList = fallbackParsed?.data?.licenseDetail;
                    SessionManager.SetServerList(serverList);
                }
                else if (serverList == null)
                {
                    var result = await ServerService.GetServerListAsync();
                    if (!result.Success)
                    {
                        Console.WriteLine("Failed to load server list: " + result.ErrorMessage);
                        return;
                    }

                    serverList = result.Servers;
                    if (serverList == null || serverList.Count == 0)
                    {
                        //// Needs to add Log MessagePopup.ShowPopup(CommonMessages.NoServerAvailable);
                        return;
                    }

                    // Save globally
                    SessionManager.SetServerList(serverList);
                }

                int threshold = 3;
                var allServers = (serverList != null) ? serverList : new List<ServerList>();

                cmbServerName.DropDownStyle = ComboBoxStyle.DropDown;
                cmbServerName.AutoCompleteMode = AutoCompleteMode.None;   // avoid fights with Items updates
                cmbServerName.AutoCompleteSource = AutoCompleteSource.None;

                cmbServerName.DisplayMember = nameof(ServerList.companyName);
                cmbServerName.ValueMember = nameof(ServerList.licenseId);

                cmbServerName.Items.Clear();      // start empty
                cmbServerName.SelectedIndex = -1; // no preselect

                bool _updating = false;

                // Filter helper
                List<ServerList> Filter(string input)
                {
                    if (string.IsNullOrWhiteSpace(input)) return new List<ServerList>();
                    input = input.Trim();
                    if (input.Length < threshold) return new List<ServerList>();

                    return allServers
                        .Where(s => (s.companyName ?? string.Empty)
                            .IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
                        .Take(20) // cap results (security/UX)
                        .ToList();
                }

                // TextChanged: update Items only, preserve user typing
                cmbServerName.TextChanged += (s, e) =>
                {
                    if (!isSelectedServer)
                    {

                        if (_updating) return;
                        _updating = true;
                        try
                        {
                            var txt = cmbServerName.Text ?? string.Empty;
                            var caret = cmbServerName.SelectionStart;

                            var results = Filter(txt);

                            cmbServerName.BeginUpdate();
                            try
                            {
                                // Close before modifying list to avoid native glitches
                                cmbServerName.DroppedDown = false;

                                cmbServerName.Items.Clear();
                                if (results.Count > 0)
                                {
                                    // Add matching objects directly; DisplayMember handles text
                                    foreach (var item in results)
                                        cmbServerName.Items.Add(item);

                                    // Open dropdown AFTER items are ready
                                    // Do it with BeginInvoke to avoid re-entrancy
                                    cmbServerName.BeginInvoke(new Action(() =>
                                    {
                                        if (!cmbServerName.IsDisposed && cmbServerName.IsHandleCreated && (cmbServerName.Text ?? "").Trim().Length >= threshold)
                                            cmbServerName.DroppedDown = true;
                                    }));
                                }
                            }
                            finally { cmbServerName.EndUpdate(); }

                            // Restore what user typed + caret
                            cmbServerName.SelectedIndex = -1;   // prevent auto-select overriding text
                            cmbServerName.Text = txt;
                            cmbServerName.SelectionStart = Math.Min(caret, cmbServerName.Text.Length);
                            cmbServerName.SelectionLength = 0;
                        }
                        finally { _updating = false; }
                    }
                };

                // User tries to open dropdown manually before threshold? Block it.
                cmbServerName.DropDown += (s, e) =>
                {
                    var txt = (cmbServerName.Text ?? string.Empty).Trim();
                    if (txt.Length < threshold)
                        cmbServerName.DroppedDown = false;
                };

                // When user picks an item, set Text accordingly (optional, usually auto)
                cmbServerName.SelectionChangeCommitted += (s, e) =>
                {
                    if (cmbServerName.SelectedItem is ServerList sel)
                        cmbServerName.Text = sel.companyName ?? string.Empty;
                };

                // Optional: ESC clears local suggestions only (doesn't close your owner form)
                cmbServerName.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Escape)
                    {
                        cmbServerName.DroppedDown = false;
                        cmbServerName.Items.Clear();
                        cmbServerName.SelectedIndex = -1;
                        // don't forcibly clear Text unless you want to
                        e.Handled = true;
                    }
                };

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading server list: " + ex.Message);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
        #endregion

        #region Form Events
        private async void LoginPage_Load(object sender, EventArgs e)
        {
            // Load server list first
            await LoadServerListAsync();

            string currentLicenseId = string.Empty;
            string currentUserId = string.Empty;

            if (!string.IsNullOrEmpty(SessionManager.LastSelectedLogin.UserId) && !string.IsNullOrEmpty(SessionManager.LastSelectedLogin.LicenseId))
            {
                currentLicenseId = SessionManager.LastSelectedLogin.LicenseId;
                currentUserId = SessionManager.LastSelectedLogin.UserId;
            }
            else if (!string.IsNullOrEmpty(SessionManager.LicenseId) && cmbServerName != null && cmbServerName.SelectedValue == null)
            {
                currentLicenseId = SessionManager.LicenseId;
                currentUserId = SessionManager.UserId;
            }

            if (string.IsNullOrEmpty(currentLicenseId) && string.IsNullOrEmpty(currentUserId)) return;

            var selectedServer = SessionManager.ServerListData?
                .FirstOrDefault(s => s.licenseId.ToString() == currentLicenseId);

            if (selectedServer != null)
            {
                // 1️⃣ Ensure the combo knows how to display ServerList
                cmbServerName.DisplayMember = nameof(selectedServer.companyName);
                cmbServerName.ValueMember = nameof(selectedServer.licenseId);

                // 2️⃣ Ensure Items list contains that server (if not, add it)
                var existing = cmbServerName.Items
                    .OfType<ServerList>()
                    .FirstOrDefault(s => s.licenseId == selectedServer.licenseId);

                if (existing == null)
                {
                    cmbServerName.Items.Add(selectedServer);
                    existing = selectedServer;
                }

                cmbServerName.DroppedDown = false;

                isSelectedServer = true;
                // 3️⃣ Set the selected item *as object*, not text/value only
                cmbServerName.SelectedItem = existing;
                isSelectedServer = false;

                // 4️⃣ Ensure the visible text updates (it will, because DisplayMember is set)
                cmbServerName.Text = existing.companyName ?? string.Empty;

                cmbServerName.BeginInvoke(new Action(() =>
                {
                    if (!cmbServerName.IsDisposed && cmbServerName.IsHandleCreated)
                        cmbServerName.DroppedDown = false;
                }));

                if (!string.IsNullOrEmpty(currentUserId))
                {
                    cmbLogin.Text = currentUserId;
                }
            }
        }
        #endregion

        #region UI Event Handlers

        private void eyePictureBox_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.txtpassword.PasswordChar == '*')
                {
                    this.txtpassword.PasswordChar = '\0';
                    this.eyePictureBox.Image = TraderApp.Properties.Resources.eye_open;
                }
                else
                {
                    this.txtpassword.PasswordChar = '*';
                    this.eyePictureBox.Image = TraderApp.Properties.Resources.eye_close;
                }
            }
            catch { }
        }

        private void cmbLogin_Enter(object sender, EventArgs e)
        {
            var loginInfoList = CommonHelper.LoadLoginDataFromCache(filePath);
            if (loginInfoList != null && loginInfoList.Count > 0)
            {
                // Clear existing items in ComboBox
                cmbLogin.Items.Clear();

                // Populate ComboBox with UserId from each login information object
                foreach (var loginInfo in loginInfoList)
                {
                    if (cmbServerName.SelectedItem is ServerList sel)
                    {
                        if (sel?.licenseId.ToString() == loginInfo.LicenseId.ToString())
                        {
                            cmbLogin.Items.Add(loginInfo.UserId);
                        }
                    }

                }
            }
        }

        private void cancleButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cmbLogin_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow only numeric input (0-9), backspace, and other control characters
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back)
            {
                e.Handled = true;
            }
        }

        private void cmbServerName_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(cmbServerName.Text))
            {
                cmbServerName.BackColor = ThemeManager.White;
                cmbLogin.Text = string.Empty;
                SessionManager.LastSelectedLogin = (string.Empty, string.Empty, string.Empty);
            }
            else
            {
                cmbServerName.BackColor = ThemeManager.LightRed;
            }
        }

        private void cmbLogin_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(cmbLogin.Text) && !cmbLogin.Text.All(char.IsDigit))
            {
                cmbLogin.BackColor = ThemeManager.LightRed;
            }
            else
            {
                var loginInfo = CommonHelper.LoadLoginDataFromCache(filePath)?
                .FirstOrDefault(s =>
                    s != null &&
                    string.Equals(s.UserId, cmbLogin.Text, StringComparison.Ordinal) &&
                    s.ServerListData != null &&
                    s.ServerListData.Any(q =>
                        string.Equals(q.companyName, cmbServerName.Text, StringComparison.Ordinal) &&
                        string.Equals(q.licenseId.ToString(), s.LicenseId, StringComparison.Ordinal)
                    )
                );

                if (loginInfo != null && !string.IsNullOrEmpty(loginInfo.Password))
                {
                    txtpassword.Text = loginInfo.Password;
                    checkBox1.Checked = true;
                }
                else
                {
                    txtpassword.Text = string.Empty;
                    checkBox1.Checked = false;
                }

                cmbLogin.BackColor = ThemeManager.White;
            }
        }

        private void txtpassword_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtpassword.Text))
            {
                txtpassword.BackColor = ThemeManager.White;
            }
            else
            {
                txtpassword.BackColor = ThemeManager.LightRed;
            }
        }

        #endregion

        #region Utility Methods
        private void CenterControls()
        {
            int formCenter = this.ClientSize.Width / 2;
            // Group button set center
            int totalBtnWidth = loginButton.Width + 20 + cancleButton.Width;
            int leftStart = formCenter - (totalBtnWidth / 2);

            loginButton.Left = leftStart;
            cancleButton.Left = loginButton.Right + 10;
        }
        #endregion
    }
}