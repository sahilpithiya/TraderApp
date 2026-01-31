using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TraderApp.Interfaces;
using TraderApps.Helpers;

namespace TraderApp.Utils.Network
{

    public class ApiService : IApiService
    {
        private readonly HttpClient _http;

        public ApiService()
        {
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<T> GetAsync<T>(string url)
        {
            try
            {
                _http.AddAuthHeader();
                var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode) return default;

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch { return default; }
        }

        public async Task<T> PostAsync<T>(string url, object data)
        {
            try
            {
                _http.AddAuthHeader();
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync(url, content);

                if (!response.IsSuccessStatusCode) return default;

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseJson);
            }
            catch
            {
                return default;
            }
        }

        public async Task<T> PostFormAsync<T>(string url, Dictionary<string, string> data)
        {
            try
            {
                _http.AddAuthHeader();
                var content = new FormUrlEncodedContent(data);
                var response = await _http.PostAsync(url, content);
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch { return default; }
        }
    }
}