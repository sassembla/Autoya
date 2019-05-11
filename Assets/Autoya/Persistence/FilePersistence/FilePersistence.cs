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
        public readonly string basePath;


        public FilePersistence(string basePathSource)
        {
            this.basePath = basePathSource;
        }

        /*
			sync series.
			run on Unity's MainThread.
		*/
        public bool IsExist(string domain, string fileName)
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

        public bool IsDirectoryExists(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                return true;
            }
            return false;
        }

        public bool CreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                return true;
            }
            else
            {// no directory = domain exists.
                var created = Directory.CreateDirectory(path);

                if (created.Exists)
                {

#if UNITY_IOS
                    {
                        UnityEngine.iOS.Device.SetNoBackupFlag(path);
                    }
#endif
                    return true;
                }
            }
            return false;
        }

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

        public bool Append(string domain, string fileName, byte[] data)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {

                var filePath = Path.Combine(domainPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Append))
                {
                    stream.Write(data, 0, data.Length);
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
                    using (var stream = new FileStream(filePath, FileMode.Append))
                    {
                        stream.Write(data, 0, data.Length);
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

        public bool Update(string domain, string fileName, byte[] data)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {
                var filePath = Path.Combine(domainPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    stream.Write(data, 0, data.Length);
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
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        stream.Write(data, 0, data.Length);
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
			returns all directory names in domain/.
		*/
        public string[] DirectoryNamesInDomain(string domain)
        {
            var domainPath = Path.Combine(basePath, domain);

            if (Directory.Exists(domainPath))
            {
                var dirPaths = Directory.GetDirectories(domainPath);

                if (dirPaths.Length == 0)
                {
                    return new string[] { };
                }

                return dirPaths;
            }
            return new string[] { };
        }

        /**
			load data from domain/fileName if file is exists.
			else return empty.
		*/
        public string Load(string domain, string fileName)
        {
            return Encoding.UTF8.GetString(LoadAsBytes(domain, fileName));
        }

        public byte[] LoadAsBytes(string domain, string fileName)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {
                var filePath = Path.Combine(domainPath, fileName);
                if (File.Exists(filePath))
                {
                    var result = new byte[0];
                    using (var stream = new FileStream(filePath, FileMode.Open))
                    {
                        result = new byte[stream.Length];
                        stream.Read(result, 0, result.Length);
                    }
                    return result;
                }
            }

            return new byte[0];
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

        /**
			delete all files in specific path.
		*/
        public bool DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                return true;
            }
            return false;
        }



        /*
			async series.
		*/

        /**
            append data to end of domain/fileName file.
        */
        public void Append(string domain, string fileName, byte[] data, Action onSuceeded, Action<string> onFailed)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {
                var filePath = Path.Combine(domainPath, fileName);
                ProcessAppend(filePath, data, onSuceeded, onFailed);
                return;
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
                    ProcessAppend(filePath, data, onSuceeded, onFailed);
                    return;
                }
            }
        }

        private Task ProcessAppend(string path, byte[] data, Action onSuceeded, Action<string> onFailed)
        {
            return FileManuipulateTask(FileMode.Append, FileAccess.Write, path, data, onSuceeded, onFailed);
        }

        private async Task FileManuipulateTask(FileMode mode, FileAccess access, string filePath, byte[] data, Action onSuceeded, Action<string> onFailed)
        {
            var message = string.Empty;
            using (
                var sourceStream = new FileStream(
                    filePath,
                    mode,
                    access,
                    FileShare.None,
                    4096,
                    true
                )
            )
            {
                try
                {
                    await sourceStream.WriteAsync(data, 0, data.Length);
                }
                catch (Exception e)
                {
                    message = e.Message;
                    return;
                }
            };

            if (string.IsNullOrEmpty(message))
            {
                onSuceeded();
                return;
            }

            onFailed(message);
        }



        /**
			update data of domain/fileName file.
		*/
        public void Update(string domain, string fileName, byte[] data, Action onSuceeded, Action<string> onFailed)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {
                var filePath = Path.Combine(domainPath, fileName);
                ProcessUpdate(filePath, data, onSuceeded, onFailed);
                return;
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
                    ProcessUpdate(filePath, data, onSuceeded, onFailed);
                    return;
                }
            }
        }

        private Task ProcessUpdate(string path, byte[] data, Action onSuceeded, Action<string> onFailed)
        {
            return FileManuipulateTask(FileMode.CreateNew, FileAccess.Write, path, data, onSuceeded, onFailed);
        }

        /**
			load data from domain/fileName if file is exists.
			else return empty.
		*/
        public void Load(string domain, string fileName, Action<byte[]> onSuceeded, Action<string> onFailed)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {
                var filePath = Path.Combine(domainPath, fileName);
                if (File.Exists(filePath))
                {
                    ProcessLoad(filePath, onSuceeded, onFailed);
                    return;
                }
            }

            onFailed("file not found:" + basePath);
        }

        private Task ProcessLoad(string path, Action<byte[]> onSuceeded, Action<string> onFailed)
        {
            return FileReadTask(FileMode.Open, FileAccess.Read, path, onSuceeded, onFailed);
        }

        private async Task FileReadTask(FileMode mode, FileAccess access, string filePath, Action<byte[]> onSuceeded, Action<string> onFailed)
        {
            var buffer = new byte[0];
            var message = string.Empty;
            using (
                var sourceStream = new FileStream(
                    filePath,
                    mode,
                    access,
                    FileShare.None,
                    4096,
                    true
                )
            )
            {
                try
                {
                    buffer = new byte[sourceStream.Length];
                    await sourceStream.ReadAsync(buffer, 0, buffer.Length);
                }
                catch (Exception e)
                {
                    message = e.Message;
                    return;
                }
            };

            if (string.IsNullOrEmpty(message))
            {
                onSuceeded(buffer);
                return;
            }

            onFailed(message);
        }

        /**
			delete domain/fileName if exists. then return true.
			else return false.
		*/
        public void Delete(string domain, string fileName, Action onSuceeded, Action<string> onFailed)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {
                var filePath = Path.Combine(domainPath, fileName);
                ProcessDeleteFile(filePath, onSuceeded, onFailed);
                return;
            }

            onFailed("domain not found:" + basePath);
        }

        /**
			delete all files in domain/.
		*/
        public void DeleteByDomain(string domain, Action onSuceeded, Action<string> onFailed)
        {
            var domainPath = Path.Combine(basePath, domain);
            if (Directory.Exists(domainPath))
            {
                ProcessDeleteDirectory(domainPath, onSuceeded, onFailed);
                return;
            }

            onFailed("domain not found:" + basePath);
        }


        private Task ProcessDeleteFile(string path, Action onSuceeded, Action<string> onFailed)
        {
            return FileDeleteTask(path, onSuceeded, onFailed);
        }

        private Task ProcessDeleteDirectory(string path, Action onSuceeded, Action<string> onFailed)
        {
            return DirectoryDeleteTask(path, onSuceeded, onFailed);
        }

        private async Task FileDeleteTask(string filePath, Action onSuceeded, Action<string> onFailed)
        {
            await Task.Factory.StartNew(
                path =>
                {
                    try
                    {
                        File.Delete((string)path);
                        onSuceeded();
                    }
                    catch (Exception e)
                    {
                        onFailed(e.Message);
                    }
                },
                filePath
            );
        }
        private async Task DirectoryDeleteTask(string filePath, Action onSuceeded, Action<string> onFailed)
        {
            await Task.Factory.StartNew(
                path =>
                {
                    try
                    {
                        Directory.Delete((string)path, true);
                        onSuceeded();
                    }
                    catch (Exception e)
                    {
                        onFailed(e.Message);
                    }
                },
                filePath
            );
        }

    }
}
