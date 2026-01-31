using System.Collections.Generic;
using System.Threading.Tasks;

namespace TraderApp.Interfaces
{
    public interface IApiService
    {
        Task<T> GetAsync<T>(string url);

        Task<T> PostAsync<T>(string url, object data);

        Task<T> PostFormAsync<T>(string url, Dictionary<string, string> data);
    }
}