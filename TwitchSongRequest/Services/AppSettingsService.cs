using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal class AppSettingsService : IAppSettingsService
    {
        private readonly string filePath = "appsettings.json";

        AppSettings IAppSettingsService.GetAppSettings()
        {
            if (!File.Exists(filePath))
            {
                return new AppSettings();
            }
            string json = File.ReadAllText(filePath);
            AppSettings? appSettings = JsonConvert.DeserializeObject<AppSettings>(json);
            if (appSettings == null)
            {
                return new AppSettings();
            }
            appSettings.TwitchClientSecret = DecryptString(appSettings.TwitchClientSecret, Environment.MachineName);
            return appSettings;
        }

        void IAppSettingsService.SaveAppSettings(AppSettings appSettings)
        {
            appSettings.TwitchClientSecret = EncryptString(appSettings.TwitchClientSecret, Environment.MachineName);
            string json = JsonConvert.SerializeObject(appSettings, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        private static string EncryptString(string plaintext, string password)
        {
            // Convert the plaintext string to a byte array
            byte[] plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);

            // Derive a new password using the PBKDF2 algorithm and a random salt
            Rfc2898DeriveBytes passwordBytes = new Rfc2898DeriveBytes(password, 20);

            // Use the password to encrypt the plaintext
            Aes encryptor = Aes.Create();
            encryptor.Key = passwordBytes.GetBytes(32);
            encryptor.IV = passwordBytes.GetBytes(16);
            encryptor.Padding = PaddingMode.PKCS7;
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(plaintextBytes, 0, plaintextBytes.Length);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private static string DecryptString(string encrypted, string password)
        {
            // Convert the encrypted string to a byte array
            byte[] encryptedBytes = Convert.FromBase64String(encrypted);

            // Derive the password using the PBKDF2 algorithm
            Rfc2898DeriveBytes passwordBytes = new Rfc2898DeriveBytes(password, 20);

            // Use the password to decrypt the encrypted string
            Aes encryptor = Aes.Create();
            encryptor.Key = passwordBytes.GetBytes(32);
            encryptor.IV = passwordBytes.GetBytes(16);
            encryptor.Padding = PaddingMode.PKCS7;
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(encryptedBytes, 0, encryptedBytes.Length);
                }
                return System.Text.Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}
