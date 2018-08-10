using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/**
	features for control data storage.
	do not use severe data without encryption.
*/
namespace AutoyaFramework.Persistence.Files
{
    public class FilePersistence
    {
        private readonly string basePath;

        public FilePersistence(string basePathSource)
        {
            this.basePath = basePathSource;
        }

        /*
			sync series.
			run on Unity's MainThread.
		*/

        /**
			append data to end of domain/fileName file.
		*/
        public bool Append(string domain, string fileName, string data)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {

                var filePath = Path.Combine(domainPath, fileName);
                using (var sw = new StreamWriter(filePath, true))
                {
                    sw.Write(data);
                }

                return true;
            }
            else
            {// no directory = domain exists.
                var created = Directory.CreateDirectory(domainPath);

                if (created.Exists)
                {

#if UNITY_IOS
                    {
                        UnityEngine.iOS.Device.SetNoBackupFlag(domainPath);
                    }
#endif

                    var filePath = Path.Combine(domainPath, fileName);
                    using (var sw = new StreamWriter(filePath, true))
                    {
                        sw.Write(data);
                    }
                    return true;
                }
            }
            return false;
        }

        public bool Append(string domain, string fileName, byte[] data, Action onAppended, Action<string> onAppendFailed)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {

                var filePath = Path.Combine(domainPath, fileName);
                ProcessWrite(filePath, data, onAppended, onAppendFailed);
                return true;
            }
            else
            {// no directory = domain exists.
                var created = Directory.CreateDirectory(domainPath);

                if (created.Exists)
                {

#if UNITY_IOS
                    {
                        UnityEngine.iOS.Device.SetNoBackupFlag(domainPath);
                    }
#endif

                    var filePath = Path.Combine(domainPath, fileName);
                    ProcessWrite(filePath, data, onAppended, onAppendFailed);
                    return true;
                }
            }
            return false;
        }

        static Task ProcessWrite(string path, byte[] data, Action onAppended, Action<string> onAppendFailed)
        {
            return WriteTextAsync(path, data, onAppended, onAppendFailed);
        }

        static async Task WriteTextAsync(string filePath, byte[] data, Action onAppended, Action<string> onAppendFailed)
        {
            using (
                var sourceStream = new FileStream(
                    filePath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    useAsync: true
                )
            )
            {
                try
                {
                    await sourceStream.WriteAsync(data, 0, data.Length);
                    Debug.Log("吐き出し完了した");
                    onAppended();
                }
                catch (Exception e)
                {
                    onAppendFailed(e.Message);
                }
            };
        }


        /**
			update data of domain/fileName file.
		*/
        public bool Update(string domain, string fileName, string data)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {
                var filePath = Path.Combine(domainPath, fileName);
                using (var sw = new StreamWriter(filePath, false))
                {
                    sw.Write(data);
                }
                return true;
            }
            else
            {// no directory = domain exists.
                var created = Directory.CreateDirectory(domainPath);
                if (created.Exists)
                {
#if UNITY_IOS
                    {
                        UnityEngine.iOS.Device.SetNoBackupFlag(domainPath);
                    }
#endif

                    var filePath = Path.Combine(domainPath, fileName);
                    using (var sw = new StreamWriter(filePath, false))
                    {
                        sw.Write(data);
                    }

                    return true;
                }
            }
            return false;
        }

        /**
			returns all file names in domain/.
			
			ignore .(dot) start named file.
			e.g. 
				.dat will be ignored.
		*/
        public string[] FileNamesInDomain(string domain)
        {
            var domainPath = Path.Combine(basePath, domain);
            var fileNames = new List<string>();

            if (Directory.Exists(domainPath))
            {
                var filePaths = Directory.GetFiles(domainPath);

                if (filePaths.Length == 0)
                {
                    return new string[] { };
                }

                foreach (var filePath in filePaths)
                {
                    var fileName = Path.GetFileName(filePath);
                    if (fileName.StartsWith("."))
                    {
                        continue;
                    }
                    fileNames.Add(filePath);
                }

                return fileNames.ToArray();
            }
            return new string[] { };
        }

        /**
			load data from domain/fileName if file is exists.
			else return empty.
		*/
        public string Load(string domain, string fileName)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {
                var filePath = Path.Combine(domainPath, fileName);
                if (File.Exists(filePath))
                {
                    using (var sr = new StreamReader(filePath))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            return string.Empty;
        }

        public void Load(string domain, string fileName, Action<byte[]> onLoaded, Action<string> onLoadFailed)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {
                var filePath = Path.Combine(domainPath, fileName);
                Debug.Log("filePath:" + filePath);
                if (File.Exists(filePath))
                {
                    ProcessRead(filePath, onLoaded, onLoadFailed);
                    return;
                }

                onLoadFailed("file not found.");
                return;
            }

            onLoadFailed("domain not found.");
        }

        static Task ProcessRead(string path, Action<byte[]> onLoaded, Action<string> onLoadFailed)
        {
            return WriteTextAsync(path, onLoaded, onLoadFailed);
        }

        static async Task WriteTextAsync(string filePath, Action<byte[]> onLoaded, Action<string> onLoadFailed)
        {
            using (
                var sourceStream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None,
                    bufferSize: 4096,
                    useAsync: true
                )
            )
            {
                try
                {
                    var buffer = new byte[sourceStream.Length];// これキッツくない？
                    await sourceStream.ReadAsync(buffer, 0, buffer.Length);
                    onLoaded(buffer);
                }
                catch (Exception e)
                {
                    onLoadFailed(e.Message);
                }
            };
        }


        /**
			delete domain/fileName if exists. then return true.
			else return false.
		*/
        public bool Delete(string domain, string fileName)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {
                var filePath = Path.Combine(domainPath, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
            }
            return false;
        }

        /**
			delete all files in domain/.
		*/
        public bool DeleteByDomain(string domain)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {
                Directory.Delete(domainPath, true);
                return true;
            }
            return false;
        }

        /*
			async series.
		*/

        public void AppendAsync(string domain, string fileName, string data, Action<bool> result)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {

                var filePath = Path.Combine(domainPath, fileName);
                using (var sw = new StreamWriter(filePath, false))
                {
                    sw.WriteLine(data);
                }

                result(true);
            }
            else
            {// no directory = domain exists.
                var created = Directory.CreateDirectory(domainPath);

                if (created.Exists)
                {

#if UNITY_IOS
                    {
                        UnityEngine.iOS.Device.SetNoBackupFlag(domainPath);
                    }
#endif

                    var filePath = Path.Combine(domainPath, fileName);
                    using (var sw = new StreamWriter(filePath, false))
                    {
                        sw.WriteLine(data);
                    }
                    result(true);
                }
            }
            result(false);
        }

        public bool Persist_IsExist(string domain, string fileName)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {
                var filePath = Path.Combine(domainPath, fileName);
                if (File.Exists(filePath))
                {
                    return true;
                }
            }
            return false;
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
