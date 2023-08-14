using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TwitchSongRequest.Helpers
{
    internal class Secure
    {
        private static readonly string password = Environment.MachineName;

        public static string? EncryptString(string? plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
            {
                return null;
            }

            byte[] salt = Encoding.UTF8.GetBytes(password);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            using (Aes aes = Aes.Create())
            {
                var pbkdf2 = new Rfc2898DeriveBytes(password, salt);
                aes.Key = pbkdf2.GetBytes(32); // Set a 256-bit key
                aes.IV = pbkdf2.GetBytes(16); // Set a 128-bit IV

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string? DecryptString(string? cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
            {
                return null;
            }

            byte[] salt = Encoding.UTF8.GetBytes(password);
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                var pbkdf2 = new Rfc2898DeriveBytes(password, salt);
                aes.Key = pbkdf2.GetBytes(32); // Set a 256-bit key
                aes.IV = pbkdf2.GetBytes(16); // Set a 128-bit IV

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                    }

                    byte[] decryptedBytes = ms.ToArray();
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }
    }
}
