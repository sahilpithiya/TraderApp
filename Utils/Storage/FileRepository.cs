using Newtonsoft.Json;
using System;
using System.IO;
using TraderApps.Config;

namespace TraderApps.Utils.Storage
{
    public class FileRepository<T>
    {
        private readonly string _filePath;

        public FileRepository(string fileName)
        {
            // Folder create karo agar nahi hai
            if (!Directory.Exists(AppConfig.AppDataPath))
                Directory.CreateDirectory(AppConfig.AppDataPath);

            _filePath = Path.Combine(AppConfig.AppDataPath, fileName + ".dat");
        }

        public void Save(T data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data);
                string encrypted = AESHelper.CompressAndEncryptString(json);
                File.WriteAllText(_filePath, encrypted);
            }
            catch (Exception ex)
            {
                // Logging kar sakte ho
                Console.WriteLine($"Save Error: {ex.Message}");
            }
        }

        public T Load()
        {
            if (!File.Exists(_filePath)) return default;

            try
            {
                string encrypted = File.ReadAllText(_filePath);
                string json = AESHelper.DecompressAndDecryptString(encrypted);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default;
            }
        }
    }
}