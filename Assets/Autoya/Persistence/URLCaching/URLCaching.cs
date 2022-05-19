using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoyaFramework.Persistence.Files;
using UnityEngine;
using UnityEngine.Networking;

namespace AutoyaFramework.Persistence.URLCaching
{
    public class URLCache
    {
        private readonly FilePersistence filePersist;

        public URLCache(FilePersistence fp)
        {
            this.filePersist = fp;
        }

        const string CONST_VALUE = "_";
        private object writeLock = new object();

        private Hashtable cachingLock = new Hashtable();

        // オンメモリキャッシュ
        private Dictionary<string, UnityEngine.Object> pathObjectOnMemoryCache = new Dictionary<string, UnityEngine.Object>();

        /*
            urlに紐づく対象をファイルとオンメモリキャッシュから削除する
        */
        public void PurgeCache(string storePath, string urlWithoutHash, bool destroyLoadedObject = false)
        {
            var deleteTargetPath = Path.Combine(storePath, GenerateFolderAndFilePath(urlWithoutHash, string.Empty, storePath).url);

            var filePaths = filePersist.FileNamesInDomain(deleteTargetPath);
            if (!filePaths.Any())
            {
                return;
            }

            // remove from hard cache.
            filePersist.DeleteByDomain(deleteTargetPath);

            // remove from on memory cache.
            var filePath = Path.Combine(deleteTargetPath, Path.GetFileName(filePaths[0]));
            if (pathObjectOnMemoryCache.ContainsKey(filePath))
            {
                UnityEngine.Object obj = null;
                if (destroyLoadedObject)
                {
                    obj = pathObjectOnMemoryCache[filePath];
                }

                pathObjectOnMemoryCache.Remove(filePath);

                if (destroyLoadedObject)
                {
                    GameObject.Destroy(obj);
                }
            }
        }

        /*
            ドメイン単位で元ファイルとオンメモリーキャッシュを消す。
        */
        public void PurgeCacheByDomain(string storePath, bool destroyLoadedObject = false)
        {
            // 削除する前にonMemoryにloadされている可能性のあるpathを収集しておく
            var domainFullPath = Path.Combine(filePersist.basePath, storePath);
            if (!Directory.Exists(domainFullPath))
            {
                return;
            }
            var contentFolderPaths = Directory.GetDirectories(domainFullPath);

            // on memoryにcacheされている可能性がある
            foreach (var contentFolderPath in contentFolderPaths)
            {
                if (!Directory.Exists(contentFolderPath))
                {
                    continue;
                }

                var targetFileNameCandidates = Directory.GetFiles(contentFolderPath);
                if (targetFileNameCandidates.Length == 0)
                {
                    continue;
                }

                var dirPath = Path.GetDirectoryName(targetFileNameCandidates[0]);
                var dirName = dirPath.Split('/').Last();
                var targetFileName = Path.GetFileName(targetFileNameCandidates[0]);
                var fileUniquePath = Path.Combine(storePath, dirName, targetFileName);

                if (pathObjectOnMemoryCache.ContainsKey(fileUniquePath))
                {
                    UnityEngine.Object obj = null;
                    if (destroyLoadedObject)
                    {
                        obj = pathObjectOnMemoryCache[fileUniquePath];
                    }

                    pathObjectOnMemoryCache.Remove(fileUniquePath);

                    if (destroyLoadedObject)
                    {
                        GameObject.Destroy(obj);
                    }
                }
            }

            // 実ファイルを削除する
            filePersist.DeleteByDomain(storePath);
        }

        private UrlAndHash GenerateFolderAndFilePath(string urlWithoutHash, string hashSource, string storePath)
        {
            var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(storePath));

            var urlBase = hmac.ComputeHash(Encoding.UTF8.GetBytes(urlWithoutHash));
            var url = Convert.ToBase64String(urlBase).Replace("/", "_").Replace("+", "_").Replace("=", "_");

            var hashBase = hmac.ComputeHash(Encoding.UTF8.GetBytes(hashSource));
            var hash = Convert.ToBase64String(hashBase).Replace("/", "_").Replace("+", "_").Replace("=", "_");

            return new UrlAndHash(url, hash);
        }

        private struct UrlAndHash
        {
            public readonly string url;
            public readonly string hash;
            public UrlAndHash(string url, string hash)
            {
                this.url = url;
                this.hash = hash;
            }
        }

        /**
            T型と、保存してあるファイルのdomain、ファイルのDL元url、urlの差分更新のためのハッシュ値、byte列からT型を生成する式を渡すと、T型を返してくる。
         */
        public IEnumerator LoadFromURLAs<T>(string storePath, string url, Func<byte[], T> bytesToTConverter, Action<T> onLoaded, Action<int, string> onLoadFailed, Dictionary<string, string> requestHeader = null, int timeout = (int)BackyardSettings.HTTP_TIMEOUT_SEC) where T : UnityEngine.Object
        {
            var urlBase = new Uri(url);
            var urlWithoutHash = urlBase.Authority + urlBase.LocalPath;
            var hash = Convert.ToBase64String(Encoding.UTF8.GetBytes(urlBase.Query));

            var cor = Load<T>(url, urlWithoutHash, hash, storePath, bytesToTConverter, onLoaded, onLoadFailed, requestHeader, timeout);

            while (cor.MoveNext())
            {
                yield return null;
            }
        }

        // 指定したkeyパラメータをurlのqueryパラメータの代わりに使用する = urlで取得したものをkeyパラメータに依存したパスにキャッシュする。
        public IEnumerator LoadFromURLAs<T>(string storePath, string key, string url, Func<byte[], T> bytesToTConverter, Action<T> onLoaded, Action<int, string> onLoadFailed, Dictionary<string, string> requestHeader = null, int timeout = (int)BackyardSettings.HTTP_TIMEOUT_SEC) where T : UnityEngine.Object
        {
            // urlでない場合、urlとして扱うためのパラメータを足す。
            if (!key.StartsWith("https://"))
            {
                key = "https://" + key;
            }

            var keyBase = new Uri(key);

            var keyWithoutHash = keyBase.Authority + keyBase.LocalPath;
            var hash = Convert.ToBase64String(Encoding.UTF8.GetBytes(keyBase.Query));

            var cor = Load<T>(url, keyWithoutHash, hash, storePath, bytesToTConverter, onLoaded, onLoadFailed, requestHeader, timeout);

            while (cor.MoveNext())
            {
                yield return null;
            }
        }

        /*
            メモリにload済であればtrueを返す。
            loadされていなければfalseを返す。
        */
        public bool IsLoaded(string storePath, string url, out string fileUniquePath)
        {
            var urlBase = new Uri(url);
            var pathWithoutHash = urlBase.Authority + urlBase.LocalPath;
            var hash = Convert.ToBase64String(Encoding.UTF8.GetBytes(urlBase.Query));

            // ファイルパス、ファイル名を生成する
            var targetFolderNameAndHash = GenerateFolderAndFilePath(pathWithoutHash, hash, storePath);
            var targetFolderName = targetFolderNameAndHash.url;
            var targetFileName = targetFolderNameAndHash.url + "_" + targetFolderNameAndHash.hash;

            // フォルダ、ファイルがあるかどうかチェックする
            var folderPath = Path.Combine(storePath, targetFolderName);

            fileUniquePath = Path.Combine(folderPath, targetFileName);

            // オンメモリキャッシュに対象が存在するかどうか
            if (pathObjectOnMemoryCache.ContainsKey(fileUniquePath))
            {
                return true;
            }

            return false;
        }

        /*
            ロード済のオンメモリから削除する。
        */
        public void Unload(string storePath, string urlWithoutHash, bool destroyLoadedObject = false)
        {
            if (IsLoaded(storePath, urlWithoutHash, out var fileUniquePath))
            {
                // remove from on memory cache.
                UnityEngine.Object obj = null;
                if (destroyLoadedObject)
                {
                    obj = pathObjectOnMemoryCache[fileUniquePath];
                }

                pathObjectOnMemoryCache.Remove(fileUniquePath);

                if (destroyLoadedObject)
                {
                    GameObject.Destroy(obj);
                }
            }
        }

        /*
            ドメイン単位でオンメモリーキャッシュを消す。
        */
        public void UnloadByDomain(string storePath, bool destroyLoadedObject = false)
        {
            var domainFullPath = Path.Combine(filePersist.basePath, storePath);
            if (!Directory.Exists(domainFullPath))
            {
                return;
            }
            var contentFolderPaths = Directory.GetDirectories(domainFullPath);

            foreach (var contentFolderPath in contentFolderPaths)
            {
                if (!Directory.Exists(contentFolderPath))
                {
                    continue;
                }

                var targetFileNameCandidates = Directory.GetFiles(contentFolderPath);
                if (targetFileNameCandidates.Length == 0)
                {
                    continue;
                }

                var dirPath = Path.GetDirectoryName(targetFileNameCandidates[0]);
                var dirName = dirPath.Split('/').Last();
                var targetFileName = Path.GetFileName(targetFileNameCandidates[0]);
                var fileUniquePath = Path.Combine(storePath, dirName, targetFileName);

                if (pathObjectOnMemoryCache.ContainsKey(fileUniquePath))
                {
                    UnityEngine.Object obj = null;
                    if (destroyLoadedObject)
                    {
                        obj = pathObjectOnMemoryCache[fileUniquePath];
                    }

                    pathObjectOnMemoryCache.Remove(fileUniquePath);

                    if (destroyLoadedObject)
                    {
                        GameObject.Destroy(obj);
                    }
                }
            }
        }

        /*
            urlに対してキャッシュされるpathを返す
        */
        public string PathOf(string storePath, string url)
        {
            var urlBase = new Uri(url);
            var urlWithoutHash = urlBase.Authority + urlBase.LocalPath;
            var hash = Convert.ToBase64String(Encoding.UTF8.GetBytes(urlBase.Query));

            // ファイルパス、ファイル名を生成する
            var targetFolderNameAndHash = GenerateFolderAndFilePath(urlWithoutHash, hash, storePath);
            var targetFolderName = targetFolderNameAndHash.url;
            var targetFileName = targetFolderNameAndHash.url + "_" + targetFolderNameAndHash.hash;

            // フォルダ、ファイルがあるかどうかチェックする
            var folderPath = Path.Combine(storePath, targetFolderName);
            var existFolderPaths = filePersist.FileNamesInDomain(folderPath);

            var fileUniquePath = Path.Combine(folderPath, targetFileName);

            return Path.Combine(filePersist.basePath, fileUniquePath);
        }


        /*
            storePath以下に保存されているファイルに対して、expirationDayCount より長く使用されていないファイルを消す。
            並列数 parallelHandleFileCount で同時ファイルアクセス上限をセット可能。デフォルトは30
        */
        public IEnumerator ExecuteExpiration(string storePath, int expirationDayCount, int parallelHandleFileCount = 30)
        {
            // 特定のドメインに含まれるファイルたちに対して、最終readの日付がNより前のファイルを消す。
            Debug.Assert(0 < parallelHandleFileCount, "parallelHandleFileCount must be more than 0.");

            // domainがあっても一つもフォルダが無い場合、終了
            var targetDirectoryPaths = filePersist.DirectoryNamesInDomain(storePath);
            if (!targetDirectoryPaths.Any())
            {
                yield break;
            }

            // 対象の最終アクセス日付を確認し、N日以上経過しているものを削除する。

            var running = true;

            Action threadAct = () =>
            {
                var targetDirectoryPathList = targetDirectoryPaths.ToList();

            rest:
                var takeCount = Mathf.Min(targetDirectoryPathList.Count, parallelHandleFileCount);

                var currentTargetDirPaths = targetDirectoryPathList.GetRange(0, takeCount);
                targetDirectoryPathList.RemoveRange(0, takeCount);

                Parallel.ForEach(currentTargetDirPaths, currentTargetDirPath =>
                    {
                        var filePaths = Directory.GetFiles(currentTargetDirPath);
                        foreach (var filePath in filePaths)
                        {
                            // 最低0日からで、最後にreadした日からの経過日数を出す
                            // DaysはSecondsやMinutesと違って上限なしのday数(11111とか)を返してくるので、このまま使用する。TotalDaysを使うとdoubleになるため避けている。
                            var elapsedUnreadDayCount = (DateTime.UtcNow - File.GetLastAccessTimeUtc(filePath)).Days;

                            if (expirationDayCount < elapsedUnreadDayCount)
                            {
                                // 一つでも経過していたら、フォルダ自体を削除する
                                Directory.Delete(currentTargetDirPath, true);
                                break;
                            }
                        }
                    }
                );

                if (0 < targetDirectoryPathList.Count)
                {
                    Thread.Sleep(10);
                    goto rest;
                }

                running = false;
            };

            var thread = new Thread(new ThreadStart(threadAct));
            thread.Start();

            while (running)
            {
                yield return null;
            }
        }

        private IEnumerator Load<T>(string url, string pathWithoutHash, string hash, string storePath, Func<byte[], T> bytesToTConverter, Action<T> onLoaded, Action<int, string> onLoadFailed, Dictionary<string, string> requestHeader = null, double timeout = BackyardSettings.HTTP_TIMEOUT_SEC) where T : UnityEngine.Object
        {
            // ファイルパス、ファイル名を生成する
            var targetFolderNameAndHash = GenerateFolderAndFilePath(pathWithoutHash, hash, storePath);
            var targetFolderName = targetFolderNameAndHash.url;
            var targetFileName = targetFolderNameAndHash.url + "_" + targetFolderNameAndHash.hash;

            // フォルダ、ファイルがあるかどうかチェックする
            var folderPath = Path.Combine(storePath, targetFolderName);
            var existFolderPaths = filePersist.FileNamesInDomain(folderPath);

            var fileUniquePath = Path.Combine(folderPath, targetFileName);

            // キャッシュに対象が存在するかどうか
            if (pathObjectOnMemoryCache.ContainsKey(fileUniquePath))
            {
                var cached = pathObjectOnMemoryCache[fileUniquePath] as T;
                onLoaded(cached);
                yield break;
            }

            // もしすでにロード中だったら待機する
            if (cachingLock.ContainsKey(targetFolderName))
            {
                while (cachingLock.ContainsKey(targetFolderName))
                {
                    yield return null;
                }

                // 待機完了、オンメモリのキャッシュにあるかどうかチェックし、あれば返す。なければダウンロードに移行する。

                UnityEngine.Object _object;
                if (pathObjectOnMemoryCache.TryGetValue(fileUniquePath, out _object))
                {
                    onLoaded(_object as T);
                    yield break;
                }

                // ダウンロードに向かう。
            }

            // 処理中のロックをかける
            lock (writeLock)
            {
                cachingLock.Add(targetFolderName, CONST_VALUE);
            }

            /*
                ロック後に、オンメモリにロードする必要 or DLする必要のチェックを行う。
             */

            // 既にDL済みのファイルが一つ以上存在していて、hashもヒットした -> ファイルはまだオンメモリにはないので、ここでロードして返す。
            if (0 < existFolderPaths.Length && filePersist.IsExist(folderPath, targetFileName))
            {
                // byte列を非同期で読み出す
                filePersist.Load(
                    folderPath,
                    targetFileName,
                    bytes =>
                    {
                        // 読み出し成功したのでオンメモリに載せる。
                        var tObj = bytesToTConverter(bytes);

                        pathObjectOnMemoryCache[fileUniquePath] = tObj;
                        lock (writeLock)
                        {
                            cachingLock.Remove(targetFolderName);
                        }

                        onLoaded(tObj);
                    },
                    error =>
                    {
                        lock (writeLock)
                        {
                            cachingLock.Remove(targetFolderName);
                        }
                        onLoadFailed(0, error);
                    }
                );
                yield break;
            }

            // オンメモリ、ファイルとしても手元に存在しないので、DLして取得を行う。

            // 求めるhashに合致しない古いキャッシュファイルがある場合、消す。
            foreach (var path in existFolderPaths)
            {
                var fileName = Path.GetFileName(path);
                filePersist.Delete(folderPath, fileName);
            }

            // ダウンロードを行う。
            using (var request = UnityWebRequest.Get(url))
            {
                filePersist.CreateDirectory(Path.Combine(filePersist.basePath, folderPath));
                var fileSavePath = Path.Combine(filePersist.basePath, folderPath, targetFileName);

                if (requestHeader == null)
                {
                    requestHeader = new Dictionary<string, string>();
                }

                foreach (var item in requestHeader)
                {
                    request.SetRequestHeader(item.Key, item.Value);
                }

                var handler = new DownloadHandlerFile(fileSavePath);

                // 失敗時に中途半端なファイルを消す
                handler.removeFileOnAbort = true;
                request.downloadHandler = handler;

                request.timeout = (int)timeout;

                var p = request.SendWebRequest();

                while (!p.isDone)
                {
                    yield return null;
                }

                var responseCode = (int)request.responseCode;

                if (request.isNetworkError)
                {
                    filePersist.Delete(folderPath, targetFileName);
                    lock (writeLock)
                    {
                        cachingLock.Remove(targetFolderName);
                    }
                    onLoadFailed(responseCode, request.error);
                    yield break;
                }

                if (request.isHttpError)
                {
                    filePersist.Delete(folderPath, targetFileName);
                    lock (writeLock)
                    {
                        cachingLock.Remove(targetFolderName);
                    }
                    onLoadFailed(responseCode, request.error);
                    yield break;
                }
            }

            // ダウンロードが成功、保存も完了

            // 保存したデータのbyte列を非同期で読み出す
            filePersist.Load(
                folderPath,
                targetFileName,
                bytes =>
                {
                    // 型に対しての返還式を取り出し、byteからT型を生成する。
                    var obj = bytesToTConverter(bytes);

                    // オンメモリへのキャッシュを行う
                    pathObjectOnMemoryCache[fileUniquePath] = obj;

                    lock (writeLock)
                    {
                        cachingLock.Remove(targetFolderName);
                    }

                    // 取得したT型オブジェクトを返す
                    onLoaded(obj);
                },
                error =>
                {
                    lock (writeLock)
                    {
                        cachingLock.Remove(targetFolderName);
                    }
                    onLoadFailed(0, error);
                }
            );
        }
    }
}
