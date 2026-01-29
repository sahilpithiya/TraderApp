using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TraderApp.Models;
using TraderApp.Config;
using TraderApp.Utils.Storage;

namespace TraderApp.Services
{
    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly FileRepository<List<LoginModel>> _loginRepo; // Login History Save karne ke liye
        private readonly FileRepository<List<ServerList>> _serverRepo; // Server List Cache ke liye

        public AuthService()
        {
            _http = new HttpClient();
            _loginRepo = new FileRepository<List<LoginModel>>("LoginData");
            _serverRepo = new FileRepository<List<ServerList>>("ServerList");
        }

        // 1. Get Server List (API -> Cache -> Return)
        public async Task<List<ServerList>> GetServersAsync()
        {
            try
            {
                // Try API
                var response = await _http.GetAsync(AppConfig.ServerListURL.ToReplaceUrl());
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ServerListResponse>(json);

                    if (result?.isSuccess == true && result.data?.licenseDetail != null)
                    {
                        // Save to Local Cache
                        _serverRepo.Save(result.data.licenseDetail);
                        return result.data.licenseDetail;
                    }
                }
            }
            catch { /* Internet issue? Fallback to cache */ }

            // Fallback: Load from Cache
            return _serverRepo.Load() ?? new List<ServerList>();
        }

        // 2. Login Logic
        public async Task<(bool Success, string Message, AuthResponseData Data)> LoginAsync(string userId, string password, string licenseId)
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("username", userId),
                    new KeyValuePair<string, string>("password", password),
                    new KeyValuePair<string, string>("licenseId", licenseId)
                });

                var response = await _http.PostAsync(AppConfig.AuthURL.ToReplaceUrl(), content);
                var json = await response.Content.ReadAsStringAsync();

                // Tera existing AuthResponse model use karenge (namespace check karlena)
                var authResult = JsonConvert.DeserializeObject<AuthResponse>(json);

                if (authResult?.isSuccess == true)
                {
                    // 3. Login Successful - Save Data Locally
                    SaveLoginDataLocally(userId, password, licenseId);
                    return (true, "Success", authResult.data);
                }

                return (false, authResult?.successMessage ?? "Login Failed", null);
            }
            catch (System.Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        // 4. Helper: Save Login Data (Remember Logic)
        private void SaveLoginDataLocally(string userId, string password, string licenseId)
        {
            var list = _loginRepo.Load() ?? new List<LoginModel>();

            // Remove existing entry for same user
            list.RemoveAll(x => x.UserId == userId && x.LicenseId == licenseId);

            // Add new entry
            list.Add(new LoginModel
            {
                UserId = userId,
                Password = password,
                LicenseId = licenseId,
                LastLoginTime = System.DateTime.Now,
                IsRememberMe = true // Logic badal sakte hain checkbox ke hisab se
            });

            _loginRepo.Save(list);
        }

        // 5. Get Last Login User (Auto-fill ke liye)
        public LoginModel GetLastLoginUser()
        {
            var list = _loginRepo.Load();
            return list?.OrderByDescending(x => x.LastLoginTime).FirstOrDefault();
        }
    }
}