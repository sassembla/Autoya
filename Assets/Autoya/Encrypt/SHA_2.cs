using System.Security.Cryptography;
using System.Text;

namespace AutoyaFramework.Encrypt.SHA_2 {
	public static class SHA_2 {
		private static UTF8Encoding utf8Enc = new UTF8Encoding();

		public static string Sha256 (string baseStr, string key) {
			var baseStrBytes = utf8Enc.GetBytes(baseStr);
			var keyBytes = utf8Enc.GetBytes(key);
			
			var sha256 = new HMACSHA256(keyBytes);

			var hashBytes = sha256.ComputeHash(baseStrBytes);
			var hashStr = string.Empty;

			foreach (var hashByte in hashBytes) {
				hashStr += string.Format("{0,0:x2}", hashByte);
			}

			return hashStr;
		}

		public static string Sha512 (string baseStr, string key) {
			var baseStrBytes = utf8Enc.GetBytes(baseStr);
			var keyBytes = utf8Enc.GetBytes(key);
			
			var sha512 = new HMACSHA512(keyBytes);
			
			var hashBytes = sha512.ComputeHash(baseStrBytes);
			var hashStr = string.Empty;

			foreach (var hashByte in hashBytes) {
				hashStr += string.Format("{0,0:x2}", hashByte);
			}
			
			return hashStr;
		}
	}
}