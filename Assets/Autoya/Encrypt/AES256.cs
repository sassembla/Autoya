using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AutoyaFramework.Encrypt.AES256
{
    public class AES256
    {
        private readonly UTF8Encoding utf8Enc;

        private readonly byte[] keyBytes, IVBytes;
        private readonly RijndaelManaged rijndael;

        public AES256(string key, string initalizationVector)
        {
            this.utf8Enc = new UTF8Encoding();

            this.keyBytes = utf8Enc.GetBytes(key);
            this.IVBytes = utf8Enc.GetBytes(initalizationVector);

            this.rijndael = new RijndaelManaged();

            rijndael.Padding = PaddingMode.PKCS7;
            rijndael.Mode = CipherMode.CBC;
            rijndael.KeySize = 256;
            rijndael.BlockSize = 256;
        }

        public string Encrypt(string baseStr)
        {
            var baseBytes = utf8Enc.GetBytes(baseStr);

            using (var encryptor = rijndael.CreateEncryptor(keyBytes, IVBytes))
            using (var ms = new MemoryStream())
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(baseBytes, 0, baseBytes.Length);
                cs.FlushFinalBlock();
                var encrypted = ms.ToArray();
                return Convert.ToBase64String(encrypted);
            }
        }

        public string Decrypt(string encStr)
        {
            var convertedEncBytes = Convert.FromBase64String(encStr);

            using (var decryptor = rijndael.CreateDecryptor(keyBytes, IVBytes))
            using (var ms = new MemoryStream(convertedEncBytes))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            {
                var decryptedBuffer = new byte[convertedEncBytes.Length];
                var len = cs.Read(decryptedBuffer, 0, convertedEncBytes.Length);

                var decrypted = new byte[len];
                Buffer.BlockCopy(decryptedBuffer, 0, decrypted, 0, len);

                return utf8Enc.GetString(decrypted);
            }
        }

        public byte[] Encrypt(byte[] baseBytes)
        {
            using (var encryptor = rijndael.CreateEncryptor(keyBytes, IVBytes))
            using (var ms = new MemoryStream())
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(baseBytes, 0, baseBytes.Length);
                cs.FlushFinalBlock();
                return ms.ToArray();
            }
        }

        public byte[] Decrypt(byte[] baseBytes)
        {
            using (var decryptor = rijndael.CreateDecryptor(keyBytes, IVBytes))
            using (var ms = new MemoryStream(baseBytes))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            {
                var decryptedBuffer = new byte[baseBytes.Length];
                var len = cs.Read(decryptedBuffer, 0, baseBytes.Length);

                var decrypted = new byte[len];
                Buffer.BlockCopy(decryptedBuffer, 0, decrypted, 0, len);

                return decrypted;
            }
        }
    }
}