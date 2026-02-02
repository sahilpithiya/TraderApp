using ClientDesktop.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TraderApp.Interfaces;
using TraderApp.Utils.Network;
using TraderApps.Config;
using TraderApps.Helpers;
using TraderApps.Utils.Storage;

namespace TraderApp.Services
{
    public class DetailService
    {
        private readonly IApiService _apiService;

        public DetailService()
        {
            _apiService = new ApiService();
        }

        #region Core Data Loading Logic (Cache + API)

        public async Task<List<HistoryModel>> GetDealsOrOrdersDataAsync(string userId, string licenseId, string domain)
        {
            string filePath = GetUserFilePath(domain, userId);
            List<HistoryModel> historyList = CommonHelper.LoadHistoryDataFromCache(filePath);

            bool needFetch = false;
            DateTime fromDate = (licenseId == "1") ? new DateTime(2025, 6, 1) : new DateTime(1970, 1, 1);
            DateTime toDate = DateTime.Today;

            if (historyList == null || historyList.Count == 0)
            {
                needFetch = true;
            }
            else
            {
                var lastDate = historyList.Max(h => h.createdOn);
                if (lastDate.Date <= DateTime.Today)
                {
                    fromDate = lastDate;
                    toDate = DateTime.Today.AddDays(1);
                    needFetch = true;
                }
            }

            if (needFetch)
            {
                var dealerId = SessionManager.ClientListData.FirstOrDefault()?.DealerId;

                var (success, error, apiData) = await FetchHistoryFromApiAsync(userId, dealerId, fromDate, toDate, licenseId);

                if (success && apiData?.Count > 0)
                {
                    if (historyList == null) historyList = new List<HistoryModel>();

                    var dataToRemove = historyList.Where(h => h.createdOn >= fromDate && h.createdOn <= toDate).ToList();
                    foreach (var item in dataToRemove) historyList.Remove(item);

                    historyList.AddRange(apiData);

                    await SaveHistoryDataToCacheAsync(filePath, historyList);
                }
            }

            return historyList;
        }

        public async Task<List<PositionHistoryModel>> GetPositionHistoryDataAsync(string userId, string licenseId, string domain)
        {
            string filePath = GetUserFilePath(domain, userId);
            List<PositionHistoryModel> posList = CommonHelper.LoadPositionHistoryDataFromCache(filePath);

            bool needFetch = false;
            DateTime fromDate = (licenseId == "1") ? new DateTime(2025, 6, 1) : new DateTime(1970, 1, 1);
            DateTime toDate = DateTime.Today;

            if (posList == null || posList.Count == 0)
            {
                needFetch = true;
            }
            else
            {
                var lastDate = posList.Max(h => h.UpdatedAt);
                if (lastDate.Date <= DateTime.Today)
                {
                    fromDate = lastDate;
                    toDate = DateTime.Today.AddDays(1);
                    needFetch = true;
                }
            }

            if (needFetch)
            {
                var (success, error, apiData) = await FetchPositionHistoryFromApiAsync(userId, fromDate, toDate, licenseId);

                if (success && apiData?.Count > 0)
                {
                    if (posList == null) posList = new List<PositionHistoryModel>();

                    var dataToRemove = posList.Where(h => h.LastOutAt == null || (h.UpdatedAt >= fromDate && h.UpdatedAt <= toDate)).ToList();
                    foreach (var item in dataToRemove) posList.Remove(item);

                    posList.AddRange(apiData);

                    await SavePositionHistoryDataToCacheAsync(filePath, posList);
                }
            }

            return posList;
        }

        #endregion

        #region Private Helpers (File & Path)

        private string GetUserFilePath(string domain, string userId)
        {
            return Path.Combine(
                Path.Combine(AppConfig.dataFolder, AESHelper.ToBase64UrlSafe(domain)),
                $"{AESHelper.ToBase64UrlSafe(userId)}.dat"
            );
        }

        private async Task SaveHistoryDataToCacheAsync(string filePath, List<HistoryModel> historyList)
        {
            await Task.Run(() =>
            {
                try
                {
                    var existingData = File.Exists(filePath)
                        ? JsonConvert.DeserializeObject<Dictionary<string, object>>(AESHelper.DecompressAndDecryptString(File.ReadAllText(filePath)))
                        : new Dictionary<string, object>();

                    existingData["History"] = historyList;
                    string updatedJson = JsonConvert.SerializeObject(existingData);
                    string encryptedUpdatedJson = AESHelper.CompressAndEncryptString(updatedJson);

                    string folder = Path.GetDirectoryName(filePath);
                    CommonHelper.SaveEncryptedData(folder, AESHelper.ToBase64UrlSafe(SessionManager.UserId), encryptedUpdatedJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error saving history cache: " + ex.Message);
                }
            });
        }

        private async Task SavePositionHistoryDataToCacheAsync(string filePath, List<PositionHistoryModel> positionHistoryList)
        {
            await Task.Run(() =>
            {
                try
                {
                    var existingData = File.Exists(filePath)
                        ? JsonConvert.DeserializeObject<Dictionary<string, object>>(AESHelper.DecompressAndDecryptString(File.ReadAllText(filePath)))
                        : new Dictionary<string, object>();

                    existingData["PositionHistory"] = positionHistoryList;
                    string updatedJson = JsonConvert.SerializeObject(existingData);
                    string encryptedUpdatedJson = AESHelper.CompressAndEncryptString(updatedJson);

                    string folder = Path.GetDirectoryName(filePath);
                    CommonHelper.SaveEncryptedData(folder, AESHelper.ToBase64UrlSafe(SessionManager.UserId), encryptedUpdatedJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error saving position cache: " + ex.Message);
                }
            });
        }

        #endregion

        #region API Calls

        public async Task<(bool Success, string ErrorMessage, List<HistoryModel> ResponseData)> FetchHistoryFromApiAsync(
             string clientId, string dealerId, DateTime fromDate, DateTime toDate, string licenseId)
        {
            try
            {
                var payload = new
                {
                    clientID = clientId,
                    dealerID = dealerId,
                    fromDate = fromDate.ToString("yyyy-MM-dd"),
                    toDate = toDate.ToString("yyyy-MM-dd")
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var response = await _apiService.PostRawAsync(AppConfig.GetHistoryForClient.ToReplaceUrl(), content))
                {
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = JsonConvert.DeserializeObject<dynamic>(responseString);
                        return (false, error?.exception?.message?.ToString() ??
                                $"{(int)response.StatusCode}: {response.ReasonPhrase}", null);
                    }

                    var result = JsonConvert.DeserializeObject<HistoryResponse>(responseString);

                    if (result == null) return (false, "Invalid response from server", null);
                    if (!result.isSuccess || result.data == null)
                        return (false, result.successMessage ?? "Failed to retrieve history data", null);

                    return (true, null, result.data);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Success, string ErrorMessage, List<PositionHistoryModel> ResponseData)> FetchPositionHistoryFromApiAsync(
             string clientId, DateTime fromDate, DateTime toDate, string licenseId)
        {
            try
            {
                var payload = new
                {
                    clientID = clientId,
                    fromDate = fromDate.ToString("yyyy-MM-dd"),
                    toDate = toDate.ToString("yyyy-MM-dd")
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // ✅ Updated: Uses _apiService.PostRawAsync
                using (var response = await _apiService.PostRawAsync(AppConfig.GetPositionHistoryForClient.ToReplaceUrl(), content).ConfigureAwait(false))
                {
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = JsonConvert.DeserializeObject<dynamic>(responseString);
                        return (false, error?.exception?.message?.ToString() ??
                                $"{(int)response.StatusCode}: {response.ReasonPhrase}", null);
                    }

                    var result = JsonConvert.DeserializeObject<PositionHistoryResponse>(responseString);

                    if (result == null) return (false, "Invalid response from server", null);
                    if (!result.IsSuccess || result.Data == null)
                        return (false, result.SuccessMessage ?? "Failed to retrieve position history data", null);

                    return (true, null, result.Data);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        #endregion
    }
}