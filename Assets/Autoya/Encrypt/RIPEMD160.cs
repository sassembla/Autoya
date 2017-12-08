using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AutoyaFramework.Encrypt.RIPEMD
{
    public static class RIPEMD
    {
        private static UTF8Encoding utf8Enc = new UTF8Encoding();

        public static string RIPEMD160(string baseStr, string key)
        {
            var sourceBytes = utf8Enc.GetBytes(baseStr);
            var keyBytes = utf8Enc.GetBytes(key);

            var ripemd160 = new HMACRIPEMD160(keyBytes);

            var hashBytes = ripemd160.ComputeHash(sourceBytes);
            ripemd160.Clear();

            var hashStr = string.Empty;
            foreach (var hashByte in hashBytes)
            {
                hashStr += string.Format("{0,0:x2}", hashByte);
            }
            return hashStr;
        }
    }
}