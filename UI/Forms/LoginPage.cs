using DesktopClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using TraderApp.Config;
using TraderApp.Helpers;
using TraderApp.Models;
using TraderApp.Services;
using TraderApp.UI.Theme;
using TraderApp.Utils.Security;

namespace TraderApp.UI.Forms
{
    public partial class LoginPage : Form
    {
        public LoginPage()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
            LoadSavedLogin(); // Auto-fill logic
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            PerformLogin();
        }

        private async void PerformLogin()
        {
            string user = txtUsername.Text;
            string pass = txtPassword.Text;
            string license = "1"; // Filhal hardcoded ya dropdown se le lena

            var result = await AuthService.AuthenticateAsync(user, pass, license);

            if (result.Success)
            {
                // 1. Session Set karo
                SessionManager.Token = result.Data.token;
                SessionManager.UserId = user;
                SessionManager.LicenseId = license;

                // 2. Data Save karo (Remember Me logic)
                SaveLoginInfo(user, pass, license);

                // 3. Disclaimer Dikhao
                this.Hide();
                using (var disclaimer = new DisclaimerForm())
                {
                    disclaimer.ShowDialog();
                    if (disclaimer.IsAccepted)
                    {
                        // 4. Open Blank Home
                        var home = new Home();
                        home.ShowDialog();
                    }
                }
                this.Close();
            }
            else
            {
                MessageBox.Show("Login Failed: " + result.Message);
            }
        }

        private void SaveLoginInfo(string user, string pass, string license)
        {
            // Prepare Data
            var loginData = new LoginInfo
            {
                UserId = user,
                Password = pass,
                LicenseId = license,
                LastLogin = true
            };

            var list = new List<LoginInfo> { loginData };
            var dict = new Dictionary<string, object> { { "LoginData", list } };

            // Encrypt and Save using CommonHelper
            string json = JsonConvert.SerializeObject(dict);
            string encrypted = AESHelper.CompressAndEncryptString(json);

            CommonHelper.SaveEncryptedData(AppConfig.AppDataPath, "LoginData", encrypted);
        }

        private void LoadSavedLogin()
        {
            string path = Path.Combine(AppConfig.AppDataPath, "LoginData.dat");
            var savedLogins = CommonHelper.LoadLoginDataFromCache(path);

            if (savedLogins != null && savedLogins.Count > 0)
            {
                var last = savedLogins[0];
                txtUsername.Text = last.UserId;
                txtPassword.Text = last.Password;
            }
        }
    }
}