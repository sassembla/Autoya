using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace Encrypt.AES {
	class AES {
		private static UTF8Encoding utf8Enc = new UTF8Encoding();

		public void Action () {
			//'Shared 256 bit Key and IV here
			string sKy = "lkirwf897+22#bbtrm8814z5qq=498j5"; //'32 chr shared ascii string (32 * 8 = 256 bit)
			string sIV = "741952hheeyy66#cs!9hjv887mxx7@8y";  //'32 chr shared ascii string (32 * 8 = 256 bit)

			string sTextVal = "暗号化するテキスト!!!";
			string eText;
			string dText;

			eText = Encrypt(sKy, sIV, sTextVal);
			dText = Decrypt(sKy, sIV, eText);

			string msg = "";
			msg += "元のテキスト=" + sTextVal;
			msg += "\n";
			msg += "暗号化=" + eText;
			msg += "\n";
			msg += "復号化=" + dText;
			msg += "\n";

			Debug.Log(msg);
		}

		public string Encrypt (string prm_key, string prm_iv, string prm_text_to_encrypt) {
			var rijndael = new RijndaelManaged();

			rijndael.Padding = PaddingMode.Zeros;
			rijndael.Mode = CipherMode.CBC;
			rijndael.KeySize = 256;
			rijndael.BlockSize = 256;

			var key = new byte[0];
			var IV = new byte[0];

			key = System.Text.Encoding.UTF8.GetBytes(prm_key);
			IV = System.Text.Encoding.UTF8.GetBytes(prm_iv);

			ICryptoTransform encryptor = rijndael.CreateEncryptor(key, IV);

			var msEncrypt = new MemoryStream();
			var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

			var toEncrypt = utf8Enc.GetBytes(prm_text_to_encrypt);

			csEncrypt.Write(toEncrypt, 0, toEncrypt.Length);
			csEncrypt.FlushFinalBlock();
			// encrypted = msEncrypt.Length;

			// return (Convert.ToBase64String(encrypted));
			return null;
		}
		
		public string Decrypt (string prm_key, string prm_iv, string prm_text_to_decrypt) {
			string sEncryptedString = prm_text_to_decrypt;
			
			/*
				この辺が共通設定、なんかで沈められると良いんだけど。
			*/
			var rijndael = new RijndaelManaged();

			rijndael.Padding = PaddingMode.Zeros;
			rijndael.Mode = CipherMode.CBC;
			rijndael.KeySize = 256;
			rijndael.BlockSize = 256;

			var key = new byte[0];
			var IV = new byte[0];

			key = System.Text.Encoding.UTF8.GetBytes(prm_key);
			IV = System.Text.Encoding.UTF8.GetBytes(prm_iv);

			ICryptoTransform decryptor = rijndael.CreateDecryptor(key, IV);

			var sEncrypted = Convert.FromBase64String(sEncryptedString);
			var fromEncrypt = new byte[sEncrypted.Length];

			using (var msDecrypt = new MemoryStream(sEncrypted)) {
				using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
					csDecrypt.Read(fromEncrypt, 0, fromEncrypt.Length);
					return (utf8Enc.GetString(fromEncrypt));
				}
			}
		}
	}
}