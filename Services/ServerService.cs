using ClientDesktop.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TraderApps.Config;

namespace TraderApps.Services
{
    public static class ServerService
    {
        #region Variables And Configuration
        private static readonly HttpClient _http = new HttpClient();
        public static string ServerListUrl => AppConfig.ServerListURL;
        #endregion

        #region Server List Fetch And Cache Logic
        public static async Task<(bool Success, string ErrorMessage, List<ServerList> Servers)> GetServerListAsync()
        {
            try
            {

                string folder = AESHelper.ToBase64UrlSafe("Servers");
                string file = AESHelper.ToBase64UrlSafe("ServerList");
                string encryptedContent = null;

                // Try to read local encrypted file (if exists)
                string encryptedFilePath = Path.Combine(AppConfig.dataFolder, folder, $"{file}.dat");
                if (File.Exists(encryptedFilePath))
                {
                    encryptedContent = File.ReadAllText(encryptedFilePath);
                }

                // Call server
                HttpResponseMessage response = null;
                string json = null;

                try
                {
                    response = await _http.GetAsync(ServerListUrl).ConfigureAwait(false);
                    json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Ignore exception here, will handle below
                }

                // Case 1: No file and no server — show error
                if (string.IsNullOrEmpty(encryptedContent) && (response == null || !response.IsSuccessStatusCode))
                {
                    return (false, "❌ Unable to connect to the server and no cached data found.", null);
                }

                // Case 2: Server failed, but file exists — use local cache
                if (!string.IsNullOrEmpty(encryptedContent) && (response == null || !response.IsSuccessStatusCode))
                {
                    string encrptedFilejson = AESHelper.DecompressAndDecryptString(encryptedContent);
                    var fallbackParsed = JsonConvert.DeserializeObject<ServerListResponse>(encrptedFilejson);

                    return (true, null, fallbackParsed?.data?.licenseDetail);
                }

                // Case 3: Server succeeded, but response is empty — fallback to file if exists
                var parsed = JsonConvert.DeserializeObject<ServerListResponse>(json);
                if (parsed == null || parsed.data?.licenseDetail == null)
                {
                    if (!string.IsNullOrEmpty(encryptedContent))
                    {
                        var fallbackParsed = JsonConvert.DeserializeObject<ServerListResponse>(encryptedContent);
                        return (true, null, fallbackParsed?.data?.licenseDetail);
                    }

                    return (false, "❌ Server returned empty data and no cached file found.", null);
                }

                // Case 4: Valid response — save it if it’s new or not already cached
                string jsonString = JsonConvert.SerializeObject(parsed);
                string encrypted = AESHelper.CompressAndEncryptString(jsonString);

                if (encrypted != encryptedContent)
                {
                    string decrypted = AESHelper.DecompressAndDecryptString(encrypted);
                    string reEncrypted = AESHelper.CompressAndEncryptString(decrypted);

                    CommonHelper.SaveEncryptedData(folder, file, reEncrypted);
                }

                return (true, null, parsed.data.licenseDetail);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }
        #endregion
    }
}