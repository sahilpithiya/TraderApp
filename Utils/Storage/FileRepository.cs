using Newtonsoft.Json;
using System;
using System.IO;
using TraderApp.Interfaces;
using TraderApps.Config;

namespace TraderApps.Utils.Storage
{
    public class FileRepository<T> : IRepository<T>
    {
        private readonly string _baseFolder;

        public FileRepository()
        {
            _baseFolder = AppConfig.AppDataPath;
            if (!Directory.Exists(_baseFolder)) Directory.CreateDirectory(_baseFolder);
        }

        public void Save(string filename, T data)
        {
            try
            {
                string path = Path.Combine(_baseFolder, filename + ".dat");

                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize & Encrypt
                string json = JsonConvert.SerializeObject(data);
                string encrypted = AESHelper.CompressAndEncryptString(json);

                File.WriteAllText(path, encrypted);
            }
            catch (Exception ex) { Console.WriteLine("Save Error: " + ex.Message); }
        }

        public T Load(string filename)
        {
            try
            {
                string path = Path.Combine(_baseFolder, filename + ".dat");
                if (!File.Exists(path)) return default;

                string encrypted = File.ReadAllText(path);
                string json = AESHelper.DecompressAndDecryptString(encrypted);

                return JsonConvert.DeserializeObject<T>(json);
            }
            catch { return default; }
        }
    }
}