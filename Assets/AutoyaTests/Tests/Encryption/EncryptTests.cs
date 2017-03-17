// using AutoyaFramework.Encrypt.AES256;
// using AutoyaFramework.Encrypt.RIPEMD;
// using AutoyaFramework.Encrypt.SHA_2;
// using Miyamasu;

// public class EncryptTests : MiyamasuTestRunner {
// 	[MTest] public void AESEncrypt () {
// 		var sample = "something";

// 		string key	= "z,mv--342krnsdrfJDSf33dq2423nsda";
// 		string iv	= "12325346457462343654867843523421";

// 		var aes = new AES256(key, iv);

// 		var encryptedStr = aes.Encrypt(sample);
// 		var	decryptedStr = aes.Decrypt(encryptedStr);

// 		var len1 = sample.Length;
// 		var len2 = decryptedStr.Length;

// 		Assert(sample.Length == decryptedStr.Length, "not match, dec:" + decryptedStr + " len1:" + len1 + " len2:" + len2);
// 	}

// 	[MTest] public void AESEncryptLong () {
// 		var sample = "somethingsomethingsomethingsomethingsomethingsomethingsomethingsomethingsomethingsomethingsomethingsomethingsomething";

// 		string key	= "z,mv--342krnsdrfJDSf33dq2423nsda";
// 		string iv	= "12325346457462343654867843523421";

// 		var aes = new AES256(key, iv);

// 		var encryptedStr = aes.Encrypt(sample);
// 		var	decryptedStr = aes.Decrypt(encryptedStr);

// 		var len1 = sample.Length;
// 		var len2 = decryptedStr.Length;

// 		Assert(sample.Length == decryptedStr.Length, "not match, dec:" + decryptedStr + " len1:" + len1 + " len2:" + len2);
// 	}

// 	[MTest] public void Sha256Hash () {
// 		var sample = "yetrwfnkiofaj039tq23rkekfnaksodnfawefsq4y2up1rk";
// 		var key = "oeo9ur2jowiefapwfpawkefwe0-e-0je";
// 		var hashed = SHA_2.Sha256Hex(sample, key);
// 		Assert(hashed == "e5345801e661de0bcf572e0dcf3cea9ac3328c7a2b8daded70ce4b5e8cb54186", "not match, hashed:" + hashed);
// 	}

// 	[MTest] public void Sha512Hash () {
// 		var sample = "yetrwfnkiofaj039tq23rkekfnaksodnfawefsq4y2up1rk";
// 		var key = "oeo9ur2jowiefapwfpawkefwe0-e-0je";
// 		var hashed = SHA_2.Sha512Hex(sample, key);
// 		Assert(hashed == "671ea03f5c1eb5e5a9a595f862fc9af3b4686178323c7b246af5b7320912ac2599a7bf8d01b3efa1f0d63ba9ecc0e5a5214eda4f7b048be661b61692629969e7", "not match, hashed:" + hashed);
// 	}
	
// 	[MTest] public void RIPEMD160Hash () {
// 		var sample = "yetrwfnkiofaj039tq23rkekfnaksodnfawefsq4y2up1rk";
// 		var key = "oeo9ur2jowiefapwfpawkefwe0-e-0je";
// 		var hashed = RIPEMD.RIPEMD160(sample, key);
// 		Assert(hashed == "a2202061c178084ac8b0c700c118162aae4d6834", "not match, hashed:" + hashed);
// 	}
// }
