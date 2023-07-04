using System.Linq;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TwitchSongRequest.Helpers
{
    internal class Secure
    {
        private byte[] keyAndIvBytes;

        internal Secure(string password)
        {
            keyAndIvBytes = Encoding.UTF8.GetBytes(password);
        }

        internal string DecodeAndDecrypt(string cipherText)
        {
            string DecodeAndDecrypt = AesDecrypt(StringToByteArray(cipherText));
            return DecodeAndDecrypt;
        }

        internal string EncryptAndEncode(string plaintext)
        {
            return ByteArrayToHexString(AesEncrypt(plaintext));
        }

        private static Aes GetCryptoAlgorithm()
        {
            var algorithm = Aes.Create();

            algorithm.Padding = PaddingMode.PKCS7;
            algorithm.Mode = CipherMode.CBC;
            algorithm.KeySize = 128;
            algorithm.BlockSize = 128;

            return algorithm;
        }

        private string ByteArrayToHexString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }

        private byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private byte[] AesEncrypt(string inputText)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputText);
            byte[]? result = null;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, GetCryptoAlgorithm().CreateEncryptor(keyAndIvBytes, keyAndIvBytes), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(inputBytes, 0, inputBytes.Length);
                    cryptoStream.FlushFinalBlock();

                    result = memoryStream.ToArray();
                }
            }

            return result;
        }


        private string AesDecrypt(byte[] inputBytes)
        {
            byte[] outputBytes = inputBytes;

            string plaintext = string.Empty;

            using (MemoryStream memoryStream = new MemoryStream(outputBytes))
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, GetCryptoAlgorithm().CreateDecryptor(keyAndIvBytes, keyAndIvBytes), CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(cryptoStream))
                    {
                        plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }

            return plaintext;
        }
    }
}
