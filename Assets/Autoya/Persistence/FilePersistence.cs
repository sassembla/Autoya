using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/**
	features for control data storage.
	do not use severe data without encryption.
*/
namespace AutoyaFramework.Persistence.Files {
    public class FilePersistence {
		private readonly string basePath;

		public FilePersistence (string basePathSource) {
			this.basePath = basePathSource;
		}
		
		/*
			sync series.
			run on Unity's MainThread.
		*/

		/**
			append data to end of domain/fileName file.
		*/
		public bool Append (string domain, string fileName, string data) {
			var domainPath = Path.Combine(basePath, domain);
			if (Directory.Exists(domainPath)) {

				var filePath = Path.Combine(domainPath, fileName);
				using (var sw = new StreamWriter(filePath, true))	{
					sw.Write(data);
				}
				
				return true;
			} else {// no directory = domain exists.
				var created = Directory.CreateDirectory(domainPath);
				
				if (created.Exists) {
					
					#if UNITY_IOS
					{
						UnityEngine.iOS.Device.SetNoBackupFlag(domainPath);
					}
					#endif

					var filePath = Path.Combine(domainPath, fileName);
					using (var sw = new StreamWriter(filePath, true)) {
						sw.Write(data);
					}
					return true;
				} 
			}
			return false;
		}


		/**
			update data of domain/fileName file.
		*/
		public bool Update (string domain, string fileName, string data) {
			try {
			Debug.LogError("Update 1");
			var domainPath = Path.Combine(basePath, domain);
			if (Directory.Exists(domainPath)) {
				Debug.LogError("Update 2");
				var filePath = Path.Combine(domainPath, fileName);
				Debug.LogError("Update 3");
				using (var sw = new StreamWriter(filePath, false))	{
					Debug.LogError("Update 4");
					sw.Write(data);
				}
				Debug.LogError("Update 5");
				return true;
			} else {// no directory = domain exists.
				Debug.LogError("Update 6　domainPath:" + domainPath);
				var created = Directory.CreateDirectory(domainPath);
				Debug.LogError("Update 7");
				if (created.Exists) {
					Debug.Log("Update 8");
					#if UNITY_IOS
					{
						Debug.LogError("Update 9");
						UnityEngine.iOS.Device.SetNoBackupFlag(domainPath);
					}
					#endif
					Debug.LogError("Update 10");
					var filePath = Path.Combine(domainPath, fileName);
					Debug.LogError("Update 11");
					using (var sw = new StreamWriter(filePath, false)) {
						Debug.LogError("Update 12");
						sw.Write(data);
					}
					Debug.LogError("Update 13");
					return true;
				} 
			}
			Debug.LogError("Update 14");
			return false;
			} catch (Exception e) {
				Debug.LogError("e:" + e);
				return false;
			}
		}

		/**
			returns all file names in domain/.
			
			ignore .(dot) start named file.
			e.g. 
				.dat will be ignored.
		*/
		public string[] FileNamesInDomain (string domain) {
			var domainPath = Path.Combine(basePath, domain);
			var fileNames = new List<string>();
			
			if (Directory.Exists(domainPath)) {
				var filePaths = Directory.GetFiles(domainPath);
				
				if (filePaths.Length == 0) {
					return new string[]{};
				}

				foreach (var filePath in filePaths) {
					if (filePath.StartsWith(".")) continue;
					fileNames.Add(filePath);
				}

				return fileNames.ToArray();
			}
			return new string[]{};
		}

		/**
			load data from domain/fileName if file is exists.
			else return empty.
		*/
		public string Load (string domain, string fileName) {
			var domainPath = Path.Combine(basePath, domain);
			if (Directory.Exists(domainPath)) {
				var filePath = Path.Combine(domainPath, fileName);
				if (File.Exists(filePath)) {
					using (var sr = new StreamReader(filePath))	{
						return sr.ReadToEnd();
					}
				}
			}
			return string.Empty;
		}

		/**
			delete domain/fileName if exists. then return true.
			else return false.
		*/
		public bool Delete (string domain, string fileName) {
			var domainPath = Path.Combine(basePath, domain);
			if (Directory.Exists(domainPath)) {
				var filePath = Path.Combine(domainPath, fileName);
				if (File.Exists(filePath)) {
					File.Delete(filePath);
					return true;
				}
			}
			return false;
		}
		
		/**
			delete all files in domain/.
		*/
		public bool DeleteByDomain (string domain) {
			var domainPath = Path.Combine(basePath, domain);
			if (Directory.Exists(domainPath)) {
				Directory.Delete(domainPath, true);
				return true;
			}
			return false;
		}

		/*
			async series.
		*/

		public void AppendAsync (string domain, string fileName, string data, Action<bool> result) {
			var domainPath = Path.Combine(basePath, domain);
			if (Directory.Exists(domainPath)) {

				var filePath = Path.Combine(domainPath, fileName);
				using (var sw = new StreamWriter(filePath, false))	{
					sw.WriteLine(data);
				}

				result(true);
			} else {// no directory = domain exists.
				var created = Directory.CreateDirectory(domainPath);
				
				if (created.Exists) {

					#if UNITY_IOS
					{
                        UnityEngine.iOS.Device.SetNoBackupFlag(domainPath);
					}
					#endif

					var filePath = Path.Combine(domainPath, fileName);
					using (var sw = new StreamWriter(filePath, false))	{
						sw.WriteLine(data);
					}
					result(true);
				} 
			}
			result(false);
		}

		// public void UpdateAsync (string domain, string fileName, string data, Action<bool> result) {
		// 	var domainPath = Path.Combine(basePath, domain);
		// 	if (Directory.Exists(domainPath)) {

		// 		var filePath = Path.Combine(domainPath, fileName);
		// 		using (var sw = new StreamWriter(filePath, false))	{
		// 			sw.WriteLine(data);
		// 		}

		// 		result(true);
		// 	} else {// no directory = domain exists.
		// 		var created = Directory.CreateDirectory(domainPath);
				
		// 		if (created.Exists) {

		// 			#if UNITY_IOS
		// 			{
        //                 UnityEngine.iOS.Device.SetNoBackupFlag(domainPath);
		// 			}
		// 			#endif

		// 			var filePath = Path.Combine(domainPath, fileName);
		// 			using (var sw = new StreamWriter(filePath, false))	{
		// 				sw.WriteLine(data);
		// 			}
		// 			result(true);
		// 		} 
		// 	}
		// 	result(false);
		// }

		// public void LoadAsync (string domain, string fileName, Action<string> resultData) {
		// 	var domainPath = Path.Combine(basePath, domain);
		// 	if (Directory.Exists(domainPath)) {
		// 		var filePath = Path.Combine(domainPath, fileName);
		// 		if (File.Exists(filePath)) {
		// 			using (var sr = new StreamReader(filePath))	{
		// 				resultData(sr.ReadLine());
		// 			}
		// 		}
		// 	}
		// 	resultData(string.Empty);
		// }

		// public void DeleteAsync (string domain, string fileName, Action<bool> result) {
		// 	var domainPath = Path.Combine(basePath, domain);
		// 	if (Directory.Exists(domainPath)) {
		// 		var filePath = Path.Combine(domainPath, fileName);
		// 		if (File.Exists(filePath)) {
		// 			File.Delete(filePath);
		// 			result(true);
		// 		}
		// 	}
		// 	result(false);
		// }

		// public void DeleteByDomainAsync (string domain, Action<bool> result) {
		// 	var domainPath = Path.Combine(basePath, domain);
		// 	if (Directory.Exists(domainPath)) {
		// 		Directory.Delete(domainPath, true);
		// 		result(true);
		// 	}
		// 	result(false);
		// }
	}
}


	/*
		iOS-Keychain Persistence is under consideration.
	*/