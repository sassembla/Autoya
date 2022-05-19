using System;
using System.Collections.Generic;
using System.Text;
using AutoyaFramework.Persistence.HashHit;
using AutoyaFramework.Persistence.Files;
using AutoyaFramework.Persistence.URLCaching;
using UnityEngine;
using System.Collections;

namespace AutoyaFramework
{
    public partial class Autoya
    {
        /*
			persistence.
				privides  sync/async persistent operation.

				File persistence and URLCaching.
		*/

        private FilePersistence _autoyaFilePersistence;
        private URLCache _autoyaURLCache;
        private HashHit _autoyaChacheHit;

        public static bool Persist_IsExist(string domain, string filePath)
        {
            return autoya._autoyaFilePersistence.IsExist(domain, filePath);
        }

        public static bool Persist_Update(string domain, string filePath, string data)
        {
            var isEnough = false;
            if (isEnough)
            {// estimate size over
                Debug.LogError("Persist_Update save size overed.");
                return false;
            }
            return autoya._autoyaFilePersistence.Update(domain, filePath, data);
        }

        public static bool Persist_Append(string domain, string filePath, string data)
        {
            var isEnough = false;
            if (isEnough)
            {// estimate size over
                Debug.LogError("Persist_Update save size overed.");
                return false;
            }
            return autoya._autoyaFilePersistence.Append(domain, filePath, data);
        }

        public static string Persist_Load(string domain, string filePath)
        {
            return autoya._autoyaFilePersistence.Load(domain, filePath);
        }

        public static byte[] Persist_LoadAsBytes(string domain, string filePath)
        {
            return autoya._autoyaFilePersistence.LoadAsBytes(domain, filePath);
        }

        public static bool Persist_Delete(string domain, string filePath)
        {
            return autoya._autoyaFilePersistence.Delete(domain, filePath);
        }

        public static bool Persist_DeleteByDomain(string domain)
        {
            return autoya._autoyaFilePersistence.DeleteByDomain(domain);
        }

        public static string[] Persist_FileNamesInDomain(string domain)
        {
            return autoya._autoyaFilePersistence.FileNamesInDomain(domain);
        }

        public static string[] Persist_DirectoryNamesInDomain(string domain)
        {
            return autoya._autoyaFilePersistence.DirectoryNamesInDomain(domain);
        }


        /*
            async series
         */

        public static void Persist_Update(string domain, string filePath, string data, Action onSucceeded, Action<string> onFailed)
        {
            Persist_Update(domain, filePath, Encoding.UTF8.GetBytes(data), onSucceeded, onFailed);
        }

        public static void Persist_Update(string domain, string filePath, byte[] data, Action onSucceeded, Action<string> onFailed)
        {
            var isEnough = false;
            if (isEnough)
            {// estimate size over
                Debug.LogError("Persist_Update save size overed.");
                onFailed("no empty space.");
                return;
            }

            autoya._autoyaFilePersistence.Update(domain, filePath, data, onSucceeded, onFailed);
        }

        public static void Persist_Append(string domain, string filePath, string data, Action onSucceeded, Action<string> onFailed)
        {
            Persist_Append(domain, filePath, Encoding.UTF8.GetBytes(data), onSucceeded, onFailed);
        }

        public static void Persist_Append(string domain, string filePath, byte[] data, Action onSucceeded, Action<string> onFailed)
        {
            var isEnough = false;
            if (isEnough)
            {// estimate size over
                Debug.LogError("Persist_Update save size overed.");
                onFailed("no empty space.");
                return;
            }

            autoya._autoyaFilePersistence.Append(domain, filePath, data, onSucceeded, onFailed);
        }

        public static void Persist_Load(string domain, string filePath, Action<string> onSucceeded, Action<string> onFailed)
        {
            Persist_Load(domain, filePath, bytes => { onSucceeded(Encoding.UTF8.GetString(bytes)); }, onFailed);
        }

        public static void Persist_Load(string domain, string filePath, Action<byte[]> onSucceeded, Action<string> onFailed)
        {
            autoya._autoyaFilePersistence.Load(domain, filePath, onSucceeded, onFailed);
        }

        public static void Persist_Delete(string domain, string filePath, Action onSucceeded, Action<string> onFailed)
        {
            autoya._autoyaFilePersistence.Delete(domain, filePath, onSucceeded, onFailed);
        }

        public static void Persist_DeleteByDomain(string domain, Action onSucceeded, Action<string> onFailed)
        {
            autoya._autoyaFilePersistence.DeleteByDomain(domain, onSucceeded, onFailed);
        }

        /*
            url caching series
         */

        public static void Persist_URLCaching_Load<T>(string domain, string key, string url, Func<byte[], T> bytesToTConverter, Action<T> onLoaded, Action<int, string> onLoadFailed, Dictionary<string, string> requestHeader = null, int timeout = (int)BackyardSettings.HTTP_TIMEOUT_SEC) where T : UnityEngine.Object
        {
            if (autoya._autoyaURLCache == null)
            {
                autoya._autoyaURLCache = new URLCache(autoya._autoyaFilePersistence);
            }

            var cor = autoya._autoyaURLCache.LoadFromURLAs<T>(domain, key, url, bytesToTConverter, onLoaded, onLoadFailed, requestHeader, timeout);
            autoya.mainthreadDispatcher.Commit(cor);
        }

        public static void Persist_URLCaching_Load<T>(string domain, string url, Func<byte[], T> bytesToTConverter, Action<T> onLoaded, Action<int, string> onLoadFailed, Dictionary<string, string> requestHeader = null, int timeout = (int)BackyardSettings.HTTP_TIMEOUT_SEC) where T : UnityEngine.Object
        {
            if (autoya._autoyaURLCache == null)
            {
                autoya._autoyaURLCache = new URLCache(autoya._autoyaFilePersistence);
            }

            var cor = autoya._autoyaURLCache.LoadFromURLAs<T>(domain, url, bytesToTConverter, onLoaded, onLoadFailed, requestHeader, timeout);
            autoya.mainthreadDispatcher.Commit(cor);
        }

        public static bool Persist_URLCaching_IsLoaded(string domain, string url)
        {
            if (autoya._autoyaURLCache == null)
            {
                autoya._autoyaURLCache = new URLCache(autoya._autoyaFilePersistence);
            }

            return autoya._autoyaURLCache.IsLoaded(domain, url, out var path);
        }

        public static void Persist_URLCaching_Unload(string domain, string url, bool destroyLoadedObject = false)
        {
            if (autoya._autoyaURLCache == null)
            {
                autoya._autoyaURLCache = new URLCache(autoya._autoyaFilePersistence);
            }

            autoya._autoyaURLCache.Unload(domain, url, destroyLoadedObject);
        }

        public static void Persist_URLCaching_UnloadByDomain(string domain, bool destroyLoadedObject = false)
        {
            if (autoya._autoyaURLCache == null)
            {
                autoya._autoyaURLCache = new URLCache(autoya._autoyaFilePersistence);
            }

            autoya._autoyaURLCache.UnloadByDomain(domain, destroyLoadedObject);
        }

        public static void Persist_URLCaching_Purge(string domain, string url)
        {
            if (autoya._autoyaURLCache == null)
            {
                autoya._autoyaURLCache = new URLCache(autoya._autoyaFilePersistence);
            }

            var urlBase = new Uri(url);
            var urlWithoutHash = urlBase.Authority + urlBase.LocalPath;

            autoya._autoyaURLCache.PurgeCache(domain, urlWithoutHash);
        }

        public static void Persist_URLCaching_PurgeByDomain(string domain, bool destroyLoadedObject = false)
        {
            if (autoya._autoyaURLCache == null)
            {
                autoya._autoyaURLCache = new URLCache(autoya._autoyaFilePersistence);
            }

            autoya._autoyaURLCache.PurgeCacheByDomain(domain, destroyLoadedObject);
        }

        public static string Persist_URLCaching_PathOf(string domain, string url)
        {
            if (autoya._autoyaURLCache == null)
            {
                autoya._autoyaURLCache = new URLCache(autoya._autoyaFilePersistence);
            }
            return autoya._autoyaURLCache.PathOf(domain, url);
        }

        public static void Persist_URLCaching_ExecuteExpiration(string domain, int expirationDayCount, int parallelHandleFileCount, Action onDone)
        {
            if (autoya._autoyaURLCache == null)
            {
                autoya._autoyaURLCache = new URLCache(autoya._autoyaFilePersistence);
            }

            var cor = autoya._autoyaURLCache.ExecuteExpiration(domain, expirationDayCount, parallelHandleFileCount);
            IEnumerator waitDone()
            {
                yield return cor;
                onDone();
            }

            autoya.mainthreadDispatcher.Commit(waitDone());
        }



        /*
            hach cache series
        */

        public static void Persist_CacheHashes(
            string domain,
            string[] items,
            Action onSucceeded,
            Action<int, string> onFailed)
        {
            if (autoya._autoyaChacheHit == null)
            {
                autoya._autoyaChacheHit = new HashHit(autoya._autoyaFilePersistence);
            }

            var cor = autoya._autoyaChacheHit.CacheHashes(domain, items, onSucceeded, onFailed);
            autoya.mainthreadDispatcher.Commit(cor);
        }

        public static bool Persist_HitHash(
            string domain,
            string item
        )
        {
            if (autoya._autoyaChacheHit == null)
            {
                autoya._autoyaChacheHit = new HashHit(autoya._autoyaFilePersistence);
            }

            return autoya._autoyaChacheHit.HitHash(domain, item);
        }

        public static void Persist_ClearOnMemoryHashCache()
        {
            if (autoya._autoyaChacheHit == null)
            {
                autoya._autoyaChacheHit = new HashHit(autoya._autoyaFilePersistence);
            }

            autoya._autoyaChacheHit.ClearOnMemoryHashCache();
        }

        public static int Debug_Persist_HashCountByDomain(string domain, Char index)
        {
            if (autoya._autoyaChacheHit == null)
            {
                autoya._autoyaChacheHit = new HashHit(autoya._autoyaFilePersistence);
            }

            return autoya._autoyaChacheHit.HashCountByDomain(domain, index);
        }
    }
}
