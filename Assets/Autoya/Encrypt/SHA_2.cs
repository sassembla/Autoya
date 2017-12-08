using System.Security.Cryptography;
using System.Text;

namespace AutoyaFramework.Encrypt.SHA_2
{
    public static class SHA_2
    {
        private static UTF8Encoding utf8Enc = new UTF8Encoding();

        public static byte[] Sha256Bytes(string baseStr, string key)
        {
            var baseStrBytes = utf8Enc.GetBytes(baseStr);
            var keyBytes = utf8Enc.GetBytes(key);

            var sha256 = new HMACSHA256(keyBytes);

            return sha256.ComputeHash(baseStrBytes);
        }
        public static string Sha256Hex(string baseStr, string key)
        {
            var hashBytes = Sha256Bytes(baseStr, key);
            var hashStr = string.Empty;

            foreach (var hashByte in hashBytes)
            {
                hashStr += string.Format("{0,0:x2}", hashByte);
            }

            return hashStr;
        }

        public static byte[] Sha512Bytes(string baseStr, string key)
        {
            var baseStrBytes = utf8Enc.GetBytes(baseStr);
            var keyBytes = utf8Enc.GetBytes(key);

            var sha512 = new HMACSHA512(keyBytes);

            return sha512.ComputeHash(baseStrBytes);
        }
        public static string Sha512Hex(string baseStr, string key)
        {
            var hashBytes = Sha512Bytes(baseStr, key);
            var hashStr = string.Empty;

            foreach (var hashByte in hashBytes)
            {
                hashStr += string.Format("{0,0:x2}", hashByte);
            }

            return hashStr;
        }
    }
}