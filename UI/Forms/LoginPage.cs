using ClientDesktop.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TraderApps.Helpers;
using TraderApps.Services;
using TraderApps.UI.Theme;

namespace TraderApps.Forms
{
    public partial class LoginPage : Form
    {
        #region Variables
        private readonly AuthService _authService;
        private bool isSelectedServer;
        private string isValidated = string.Empty;

        private List<ServerList> _allServers = new List<ServerList>();
        private bool _updating = false;
        private const int threshold = 3;
        #endregion

        #region Form Initialization
        public LoginPage()
        {
            InitializeComponent();

            _authService = new AuthService();

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

            this.AcceptButton = loginButton;
            this.cmbLogin.KeyPress += cmbLogin_KeyPress;

            this.cmbServerName.TextChanged += cmbServerName_TextChanged;
            this.cmbServerName.DropDown += CmbServerName_DropDown;
            this.cmbServerName.KeyDown += CmbServerName_KeyDown;
            this.cmbServerName.SelectionChangeCommitted += CmbServerName_SelectionChangeCommitted;

            this.cmbLogin.TextChanged += cmbLogin_TextChanged;
            this.txtpassword.TextChanged += txtpassword_TextChanged;
            this.cmbLogin.Enter += cmbLogin_Enter;
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

            if (!ValidateLoginInput(out userId, out password, out licenseId)) return;

            loginButton.Enabled = false;
            cancleButton.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                // ✅ Try Login. Even if fails, we proceed (Offline/Restricted Mode)
                await LoginAsync(userId, password, licenseId, checkBox1.Checked);

                // ✅ ALWAYS Return OK to close dialog and enter Home
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            finally
            {
                loginButton.Enabled = true;
                cancleButton.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        public async Task<bool> LoginAsync(string userId, string password, string licenseId, bool isRemember = false)
        {
            // Clear previous session first
            SessionManager.SetSession(string.Empty, userId, string.Empty ?? userId, licenseId, null, password);

            try
            {
                var result = await _authService.LoginAsync(userId, password, licenseId, isRemember);

                if (!result.Success)
                {
                    // 🛑 LOGIN FAILED (Wrong Pass/Server Logic): Log it but don't stop
                    isValidated = result.Message;
                    FileLogger.Log("Network", $"Login Failed: {result.Message}");

                    // Return TRUE so form closes, but Session Token remains EMPTY.
                    // Home.cs checks Token to decide if history should be enabled.
                    return true;
                }

                // ✅ LOGIN SUCCESS
                var data = result.Data;
                DateTime? exp = null;
                if (DateTime.TryParse(data.expiration, out var dt)) exp = dt;

                // Save Token (This enables Full Access in Home.cs)
                SessionManager.SetSession(data.token, userId, data.name ?? userId, licenseId, exp, password);
                FileLogger.Log("Network", $"User '{userId}' Authorized Successfully.");

                var profileResult = await _authService.GetUserProfileAsync();

                if (profileResult != null && profileResult.isSuccess && profileResult.data != null)
                {
                    SocketLoginInfo socketInfo = new SocketLoginInfo
                    {
                        UserSubId = profileResult.data.sub,
                        UserIss = profileResult.data.iss,
                        LicenseId = SessionManager.LicenseId,
                        Intime = profileResult.data.intime,
                        Role = profileResult.data.role,
                        IpAddress = profileResult.data.ip,
                        Device = "Windows"
                    };

                    SessionManager.socketLoginInfos = socketInfo;
                    SessionManager.IsPasswordReadOnly = profileResult.data.isreadonlypassword;
                }

                return true;
            }
            catch (Exception ex)
            {
                // 🛑 NETWORK/CRASH ERROR: Log it and Proceed in Restricted Mode
                FileLogger.Log("Network", $"Login Exception: {ex.Message}");
                return true; // Proceed to Home in Restricted Mode
            }
        }

        private bool ValidateLoginInput(out string username, out string password, out string licenseId)
        {
            username = SessionManager.UserId;
            password = SessionManager.Password;
            licenseId = SessionManager.LicenseId;

            bool isValid = true;

            if (string.IsNullOrEmpty(SessionManager.Username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(licenseId))
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

            if (string.IsNullOrEmpty(username)) { cmbLogin.BackColor = ThemeManager.LightRed; isValid = false; }
            if (string.IsNullOrEmpty(password)) { txtpassword.BackColor = ThemeManager.LightRed; isValid = false; }
            if (string.IsNullOrEmpty(licenseId) && (cmbServerName.SelectedItem == null)) { cmbServerName.BackColor = ThemeManager.LightRed; isValid = false; }

            return isValid;
        }
        #endregion

        #region Server List Management
        private async Task LoadServerListAsync()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                var serverList = await _authService.GetServerListAsync();

                if (serverList != null && serverList.Count > 0)
                {
                    SessionManager.SetServerList(serverList);
                    _allServers = serverList;
                }
                else
                {
                    _allServers = new List<ServerList>();
                }

                cmbServerName.DropDownStyle = ComboBoxStyle.DropDown;
                cmbServerName.AutoCompleteMode = AutoCompleteMode.None;
                cmbServerName.AutoCompleteSource = AutoCompleteSource.None;

                cmbServerName.DisplayMember = nameof(ServerList.companyName);
                cmbServerName.ValueMember = nameof(ServerList.licenseId);

                cmbServerName.Items.Clear();
                cmbServerName.SelectedIndex = -1;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private List<ServerList> Filter(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return new List<ServerList>();
            input = input.Trim();
            if (input.Length < threshold) return new List<ServerList>();

            return _allServers
                .Where(s => (s.companyName ?? string.Empty)
                    .IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(20)
                .ToList();
        }

        private void cmbServerName_TextChanged(object sender, EventArgs e)
        {
            if (isSelectedServer) return;

            if (_updating) return;
            _updating = true;

            try
            {
                var txt = cmbServerName.Text ?? string.Empty;
                var caret = cmbServerName.SelectionStart;
                var results = Filter(txt);

                if (!string.IsNullOrEmpty(txt))
                {
                    cmbServerName.BackColor = ThemeManager.White;
                    cmbLogin.Text = string.Empty;
                    SessionManager.LastSelectedLogin = (string.Empty, string.Empty, string.Empty);
                }
                else
                {
                    cmbServerName.BackColor = ThemeManager.LightRed;
                }

                cmbServerName.BeginUpdate();
                try
                {
                    cmbServerName.DroppedDown = false;
                    cmbServerName.Items.Clear();
                    if (results.Count > 0)
                    {
                        foreach (var item in results)
                            cmbServerName.Items.Add(item);

                        cmbServerName.BeginInvoke(new Action(() =>
                        {
                            if (!cmbServerName.IsDisposed && cmbServerName.IsHandleCreated && (cmbServerName.Text ?? "").Trim().Length >= threshold)
                                cmbServerName.DroppedDown = true;
                        }));
                    }
                }
                finally { cmbServerName.EndUpdate(); }

                cmbServerName.SelectedIndex = -1;
                cmbServerName.Text = txt;
                cmbServerName.SelectionStart = Math.Min(caret, cmbServerName.Text.Length);
                cmbServerName.SelectionLength = 0;
            }
            finally
            {
                _updating = false;
            }
        }

        private void CmbServerName_DropDown(object sender, EventArgs e)
        {
            var txt = (cmbServerName.Text ?? string.Empty).Trim();
            if (txt.Length < threshold)
                cmbServerName.DroppedDown = false;
        }

        private void CmbServerName_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cmbServerName.SelectedItem is ServerList sel)
                cmbServerName.Text = sel.companyName ?? string.Empty;
        }

        private void CmbServerName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                cmbServerName.DroppedDown = false;
                cmbServerName.Items.Clear();
                cmbServerName.SelectedIndex = -1;
                e.Handled = true;
            }
        }
        #endregion

        #region Form Events
        private async void LoginPage_Load(object sender, EventArgs e)
        {
            await LoadServerListAsync();

            string cLic = SessionManager.LastSelectedLogin.LicenseId;
            string cUser = SessionManager.LastSelectedLogin.UserId;

            if (string.IsNullOrEmpty(cLic)) cLic = SessionManager.LicenseId;
            if (string.IsNullOrEmpty(cUser)) cUser = SessionManager.UserId;

            if (!string.IsNullOrEmpty(cLic))
            {
                var server = SessionManager.ServerListData?.FirstOrDefault(s => s.licenseId.ToString() == cLic);
                if (server != null)
                {
                    isSelectedServer = true;

                    if (!cmbServerName.Items.Cast<ServerList>().Any(s => s.licenseId == server.licenseId))
                    {
                        cmbServerName.Items.Add(server);
                    }

                    foreach (var item in cmbServerName.Items)
                    {
                        if (item is ServerList s && s.licenseId == server.licenseId)
                        {
                            cmbServerName.SelectedItem = item;
                            break;
                        }
                    }

                    isSelectedServer = false;
                    cmbServerName.Text = server.companyName;

                    cmbServerName.BeginInvoke(new Action(() =>
                    {
                        if (!cmbServerName.IsDisposed && cmbServerName.IsHandleCreated)
                            cmbServerName.DroppedDown = false;
                    }));
                }
            }

            if (!string.IsNullOrEmpty(cUser)) cmbLogin.Text = cUser;
        }
        #endregion

        #region UI Event Handlers
        private void cmbLogin_Enter(object sender, EventArgs e)
        {
            var loginInfoList = _authService.GetLoginHistory();

            if (loginInfoList != null && loginInfoList.Count > 0)
            {
                cmbLogin.Items.Clear();
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

        private void cmbLogin_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(cmbLogin.Text) && !cmbLogin.Text.All(char.IsDigit))
            {
                cmbLogin.BackColor = ThemeManager.LightRed;
            }
            else
            {
                var history = _authService.GetLoginHistory();
                var loginInfo = history?.FirstOrDefault(s =>
                    s != null &&
                    string.Equals(s.UserId, cmbLogin.Text, StringComparison.Ordinal) &&
                    (cmbServerName.SelectedItem is ServerList sel && sel.licenseId.ToString() == s.LicenseId)
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
            txtpassword.BackColor = !string.IsNullOrEmpty(txtpassword.Text) ? ThemeManager.White : ThemeManager.LightRed;
        }

        private void eyePictureBox_Click(object sender, EventArgs e)
        {
            if (txtpassword.PasswordChar == '*')
            {
                txtpassword.PasswordChar = '\0';
                eyePictureBox.Image = TraderApp.Properties.Resources.eye_open;
            }
            else
            {
                txtpassword.PasswordChar = '*';
                eyePictureBox.Image = TraderApp.Properties.Resources.eye_close;
            }
        }

        private void cmbLogin_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back) e.Handled = true;
        }

        private void cancleButton_Click(object sender, EventArgs e) => this.Close();

        private void CenterControls()
        {
            int center = this.ClientSize.Width / 2;
            int totalW = loginButton.Width + 20 + cancleButton.Width;
            loginButton.Left = center - (totalW / 2);
            cancleButton.Left = loginButton.Right + 10;
        }
        #endregion
    }
}