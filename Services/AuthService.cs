using ClientDesktop.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TraderApps.Config;
using TraderApps.Helpers;
using TraderApps.Models;

namespace TraderApps.Services
{
    public class AuthService
    {
        #region Variables And Configuration
        private static readonly HttpClient _http = new HttpClient();
        public static string AuthUrl => AppConfig.AuthURL;
        #endregion

        #region Authentication Method
        public static async Task<(bool Success, string ErrorMessage, AuthResponseData ResponseData)> AuthenticateAsync(
            string username, string password, string licenseId)
        {
            try
            {
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password),
                    new KeyValuePair<string, string>("licenseId", licenseId)
                });

                using (var resp = await _http.PostAsync(AuthUrl.ToReplaceUrl(), formContent).ConfigureAwait(false))
                {
                    var respString = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!resp.IsSuccessStatusCode)
                        return (false,
                            JsonConvert.DeserializeObject<dynamic>(respString)?
                            .exception?.message?.ToString()
                            ?? $"{(int)resp.StatusCode}: {resp.ReasonPhrase}", null);

                    var authResp = JsonConvert.DeserializeObject<AuthResponse>(respString);
                    if (authResp == null)
                        return (false, "Invalid response from server", null);

                    if (!authResp.isSuccess || authResp.data == null)
                        return (false, authResp.successMessage ?? "Login failed", null);

                    return (true, null, authResp.data);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public static async Task AuthenticateAsyncGET()
        {
            try
            {
                _http.AddAuthHeader();
                using (var resp = await _http.GetAsync(AuthUrl.ToReplaceUrl()).ConfigureAwait(false))
                {
                    var respString = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!resp.IsSuccessStatusCode)
                        return;

                    var authResp = JsonConvert.DeserializeObject<AuthResponseObj>(respString);
                    if (authResp == null)
                        return;

                    if (!authResp.isSuccess || authResp.data == null)
                        return;

                    if (authResp.data != null)
                    {
                        SocketLoginInfo socketInfo = new SocketLoginInfo();
                        socketInfo.UserSubId = authResp.data.sub;
                        socketInfo.UserIss = authResp.data.iss;
                        socketInfo.LicenseId = SessionManager.LicenseId;
                        socketInfo.Intime = authResp.data.intime;
                        socketInfo.Role = authResp.data.role;
                        socketInfo.IpAddress = authResp.data.ip;
                        socketInfo.Device = "Windows";

                        SessionManager.socketLoginInfos = socketInfo;
                        SessionManager.IsPasswordReadOnly = authResp.data.isreadonlypassword;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error From AuthenticateAsyncGET - " + ex.Message);
            }
        }
        #endregion
    }
}