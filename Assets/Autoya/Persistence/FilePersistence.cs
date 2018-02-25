using System;
using System.Collections.Generic;
using System.IO;

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
