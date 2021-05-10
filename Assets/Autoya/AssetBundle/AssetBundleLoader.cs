using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoyaFramework.Settings.AssetBundles;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace AutoyaFramework.AssetBundles
{

    /// <summary>
    /// Asset bundle load error.
    /// </summary>
	public enum AssetBundleLoadError
    {
        Undefined,
        AssetBundleListIsNotReady,
        NotContained,
        CrcMismatched,
        DownloadFailed,
        AssetLoadFailed,
        NullAssetFound,
        NoAssetBundleFoundInList,
        FailedToLoadDependentBundles,
        NotContainedAssetBundle,
    }

    public class AssetBundleLoader
    {
        /*
			delegate for handle http response for modules.
		*/
        public delegate void HttpResponseHandlingDelegate(string connectionId, Dictionary<string, string> responseHeader, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed);

        /*
			delegate for supply assetBundle get request header geneate func for modules.
		*/
        public delegate Dictionary<string, string> AssetBundleGetRequestHeaderDelegate(string url, Dictionary<string, string> requestHeader);

        private readonly HttpResponseHandlingDelegate httpResponseHandlingDelegate;
        private readonly AssetBundleGetRequestHeaderDelegate assetBundleGetRequestHeaderDelegate;

        public const int CODE_CRC_MISMATCHED = 399;

        private AssetBundleListStorage bundleListStorage;

        public class AssetBundleListStorage
        {
            private Dictionary<string, AssetBundleList> storage = new Dictionary<string, AssetBundleList>();
            private Dictionary<string, string> bundleNameAndDownloadUrlDict = new Dictionary<string, string>();

            private readonly Func<string, string> OnBundleDownloadUrlRequired;

            public AssetBundleListStorage(Func<string, string> onBundleDownloadUrlRequired)
            {
                this.OnBundleDownloadUrlRequired = onBundleDownloadUrlRequired;
            }

            /**
                update list then regenerate assetName-bundleNameDict.
             */
            public Dictionary<string, string> Update(AssetBundleList newList)
            {
                storage[newList.identity] = newList;

                // reset bundleName - downloadableUrl dictionary.
                {
                    bundleNameAndDownloadUrlDict.Clear();

                    foreach (var bundleList in storage.Values)
                    {
                        var bundleDownloadUrl = OnBundleDownloadUrlRequired(bundleList.identity);
                        foreach (var assetBundleInfo in bundleList.assetBundles)
                        {
                            bundleNameAndDownloadUrlDict[assetBundleInfo.bundleName] = bundleDownloadUrl;
                        }
                    }
                }

                var assetNamesAndAssetBundleNamesDict = new Dictionary<string, string>();
                var wholeAssetBundleInfos = storage.Values.SelectMany(s => s.assetBundles).ToList();
                foreach (var assetBundle in wholeAssetBundleInfos)
                {
                    var bundleName = assetBundle.bundleName;
                    foreach (var assetName in assetBundle.assetNames)
                    {
                        assetNamesAndAssetBundleNamesDict[assetName] = bundleName;
                    }
                }

                return assetNamesAndAssetBundleNamesDict;
            }

            public long AssetBundleWeightFromBundleNames(string[] bundleNames)
            {
                var total = 0L;
                foreach (var list in this.storage.Values)
                {
                    total += list.assetBundles.Where(bundleInfo => bundleNames.Contains(bundleInfo.bundleName)).Sum(b => b.size);
                }

                return total;
            }

            public AssetBundleInfo AssetBundleInfoFromBundleName(string bundleName)
            {
                foreach (var list in storage.Values)
                {
                    var candidate = list.assetBundles.Where(bundle => bundle.bundleName == bundleName).FirstOrDefault();
                    if (!AssetBundleInfo.IsEmpty(candidate))
                    {
                        return candidate;
                    }
                }
                return new AssetBundleInfo();
            }

            public string[] WholeBundleNames()
            {
                return storage.Values.SelectMany(list => list.assetBundles).Select(bundles => bundles.bundleName).ToArray();
            }

            public string CurrentAssetBundleListInfos()
            {
                var informations = storage.Values.Select(list => "list:" + list.identity + " ver:" + list.version).ToArray();
                return string.Join(", ", informations);
            }

            public string GetAssetBundleUrl(string bundleName)
            {
                var url = bundleNameAndDownloadUrlDict[bundleName] + bundleName;
                return url;
            }

            public AssetBundleList GetListByIdentity(string identity)
            {
                return storage[identity];
            }
        }

        public AssetBundleList GetAssetBundleListByIdentity(string identity)
        {
            return bundleListStorage.GetListByIdentity(identity);
        }

        private Dictionary<string, string> BasicRequestHeaderDelegate(string url, Dictionary<string, string> requestHeader)
        {
            return requestHeader;
        }

        private void BasicResponseHandlingDelegate(string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed)
        {
            if (200 <= httpCode && httpCode < 299)
            {
                succeeded(connectionId, data);
                return;
            }
            failed(connectionId, httpCode, errorReason, new AutoyaStatus());
        }

        public AssetBundleLoader(Func<string, string> onBundleDownloadUrlRequired, AssetBundleGetRequestHeaderDelegate requestHeader = null, HttpResponseHandlingDelegate httpResponseHandlingDelegate = null)
        {
            this.bundleListStorage = new AssetBundleListStorage(onBundleDownloadUrlRequired);

            this.onMemoryCache = new Dictionary<string, AssetBundle>();

            if (requestHeader != null)
            {
                this.assetBundleGetRequestHeaderDelegate = requestHeader;
            }
            else
            {
                this.assetBundleGetRequestHeaderDelegate = BasicRequestHeaderDelegate;
            }

            if (httpResponseHandlingDelegate != null)
            {
                this.httpResponseHandlingDelegate = httpResponseHandlingDelegate;
            }
            else
            {
                this.httpResponseHandlingDelegate = BasicResponseHandlingDelegate;
            }
        }

        public void UpdateAssetBundleList(AssetBundleList newList)
        {
            assetNamesAndAssetBundleNamesDict = this.bundleListStorage.Update(newList);
        }

        /*
			unload all assetBundles and delete all assetBundle caches.
		*/
        public bool CleanCachedAssetBundles()
        {
            /*
				clean all loaded assets.
			*/
            UnloadOnMemoryAssetBundles();

            return Caching.ClearCache();
        }

        /**
            Gets the asset bundles weight.
         */
        public long GetAssetBundlesWeight(string[] bundleNames)
        {
            return bundleListStorage.AssetBundleWeightFromBundleNames(bundleNames);
        }

        /**
			get AssetBundleInfo which contains requested asset name.
			this method is useful when you want to know which assets are contained with specific asset.
			return empty AssetBundleInfo if assetName is not contained by any AssetBundle in current AssetBundleList.
		 */
        public AssetBundleInfo AssetBundleInfoOfAsset(string assetName)
        {
            if (assetNamesAndAssetBundleNamesDict.ContainsKey(assetName))
            {
                var bundleName = assetNamesAndAssetBundleNamesDict[assetName];
                return AssetBundleInfoFromBundleName(bundleName);
            }

            // return empty assetBundle info if not contained.
            return new AssetBundleInfo();
        }


        public AssetBundleInfo AssetBundleInfoFromBundleName(string bundleName)
        {
            return bundleListStorage.AssetBundleInfoFromBundleName(bundleName);
        }

        public string[] GetWholeBundleNames()
        {
            return bundleListStorage.WholeBundleNames();
        }


        private List<string> loadingAssetBundleNames = new List<string>();

        private Dictionary<string, string> assetNamesAndAssetBundleNamesDict = new Dictionary<string, string>();

        /**
			load specific type Asset from AssetBundle.
			dependency of AssetBundle will be solved automatically.

			note:
				this timeoutSec param is enabled only for downloading AssetBundle from web.

				複数のAssetBundleに依存していて、それのうちのひとつとかがtimeoutしたら
		*/
        public IEnumerator LoadAsset<T>(
            string assetName,
            Action<string, T> loadSucceeded,
            Action<string, AssetBundleLoadError, string, AutoyaStatus> loadFailed,
            double timeoutSec = 0) where T : UnityEngine.Object
        {
            if (!assetNamesAndAssetBundleNamesDict.ContainsKey(assetName))
            {
                loadFailed(assetName, AssetBundleLoadError.NotContained, "searching asset name:" + assetName + " is not contained by any AssetBundles in all AssetBundleList.", new AutoyaStatus());
                yield break;
            }

            var timeoutTick = (DateTime.UtcNow + TimeSpan.FromSeconds(timeoutSec)).Ticks;
            if (timeoutSec == 0) timeoutTick = 0;

            var bundleName = assetNamesAndAssetBundleNamesDict[assetName];
            var assetBundleInfo = bundleListStorage.AssetBundleInfoFromBundleName(bundleName);

            if (AssetBundleInfo.IsEmpty(assetBundleInfo))
            {
                // no assetBundleInfo found.
                loadFailed(assetName, AssetBundleLoadError.NoAssetBundleFoundInList, "no assetBundle found:" + bundleName + " in list.", new AutoyaStatus());
                yield break;
            }

            var crc = assetBundleInfo.crc;
            var hash = Hash128.Parse(assetBundleInfo.hash);
            var coroutine = LoadAssetBundleOnMemory(
                bundleName,
                assetName,
                crc,
                hash,
                loadSucceeded,
                loadFailed,
                timeoutTick,
                false,
                new List<string>()
            );
            while (coroutine.MoveNext())
            {
                yield return null;
            }
        }

        private struct DependentBundleError
        {
            readonly public string bundleName;
            readonly public AssetBundleLoadError err;
            readonly public string reason;
            readonly public AutoyaStatus status;

            public DependentBundleError(string bundleName, AssetBundleLoadError err, string reason, AutoyaStatus status)
            {
                this.bundleName = bundleName;
                this.err = err;
                this.reason = reason;
                this.status = status;
            }
        }

        /**
			load assetBundle on memory.
		*/
        private IEnumerator LoadAssetBundleOnMemory<T>(
            string bundleName,
            string assetName,
            uint crc,
            Hash128 hash,
            Action<string, T> loadSucceeded,
            Action<string, AssetBundleLoadError, string, AutoyaStatus> loadFailed,
            long timeoutTick,
            bool isDependency = false,
            List<string> loadingDependentBundleNames = null
        ) where T : UnityEngine.Object
        {
            while (!Caching.ready)
            {
                yield return null;
            }

            var assetBundleInfo = bundleListStorage.AssetBundleInfoFromBundleName(bundleName);


            if (AssetBundleInfo.IsEmpty(assetBundleInfo))
            {
                // no assetBundleInfo found.
                loadFailed(assetName, AssetBundleLoadError.NoAssetBundleFoundInList, "no assetBundle found:" + bundleName + " in list.", new AutoyaStatus());
                yield break;
            }

            var dependentBundleNames = assetBundleInfo.dependsBundleNames;

            var dependentBundleLoadErrors = new List<DependentBundleError>();

            /*
				resolve dependencies.
			*/
            {
                if (dependentBundleNames.Any())
                {
                    var coroutines = new Dictionary<string, IEnumerator>();

                    foreach (var dependentBundleName in dependentBundleNames)
                    {
                        if (loadingDependentBundleNames != null)
                        {
                            if (loadingDependentBundleNames.Contains(dependentBundleName))
                            {
                                continue;
                            }
                        }

                        // skip if assetBundle is already loaded on memory.
                        if (onMemoryCache.ContainsKey(dependentBundleName) && onMemoryCache[dependentBundleName] != null)
                        {
                            continue;
                        }

                        var dependedBundleInfo = bundleListStorage.AssetBundleInfoFromBundleName(dependentBundleName);
                        if (AssetBundleInfo.IsEmpty(dependedBundleInfo))
                        {
                            // skip empty info.
                            continue;
                        }

                        var dependentBundleCrc = dependedBundleInfo.crc;
                        var dependentBundleHash = Hash128.Parse(dependedBundleInfo.hash);

                        // set UnityEngine.Object for request. this asset will never use directory.
                        var loadCoroutine = LoadAssetBundleOnMemory<UnityEngine.Object>(
                            dependentBundleName,
                            string.Empty,// bundleName not required.
                            dependentBundleCrc,
                            dependentBundleHash,
                            (depBundleName, depObj) =>
                            {
                                // do nothing. this bundle is currently on memory.
                            },
                            (depBundleName, depErr, depReason, autoyaStatus) =>
                            {
                                // collect error for this dependent bundle loading.
                                dependentBundleLoadErrors.Add(new DependentBundleError(depBundleName, depErr, depReason, autoyaStatus));
                            },
                            timeoutTick,
                            true, // this loading is for resolve dependency of root asset. no need to return any instances.
                            loadingDependentBundleNames
                        );

                        if (loadingDependentBundleNames != null)
                        {
                            loadingDependentBundleNames.Add(dependentBundleName);
                        }
                        coroutines[dependentBundleName] = loadCoroutine;
                    }

                    if (coroutines.Count != 0)
                    {
                        while (true)
                        {
                            if (!coroutines.Where(c => c.Value != null).Any())
                            {
                                // load done.
                                break;
                            }

                            for (var i = 0; i < coroutines.Count; i++)
                            {
                                var loadingAssetBundleName = coroutines.Keys.ToArray()[i];
                                var coroutine = coroutines[loadingAssetBundleName];
                                if (coroutine == null) continue;

                                if (!coroutine.MoveNext())
                                {
                                    coroutines[loadingAssetBundleName] = null;
                                }
                            }
                            yield return null;
                        }

                        // all dependencies are loaded on memory.
                    }
                }
            }

            var url = GetAssetBundleDownloadUrl(bundleName);

            // check now loading or not. if same bundle is already under loading, wait it here.
            if (loadingAssetBundleNames.Contains(bundleName))
            {
                while (loadingAssetBundleNames.Contains(bundleName))
                {
                    yield return null;
                }

                // check downloaded bundle is correctly cached or not.
                var isCached = Caching.IsVersionCached(url, hash);
                if (!isCached)
                {
                    loadFailed(assetName, AssetBundleLoadError.DownloadFailed, "caching failed.", new AutoyaStatus());
                    yield break;
                }
            }

            // assetBundle is already allocated on memory.
            if (onMemoryCache.ContainsKey(bundleName))
            {
                var isCached = Caching.IsVersionCached(url, hash);

                // on UnityEditor, IsVersionCached resurns not valid result when on memory asset is updated by hash.
                var isNotSameHashCached = hashCache.ContainsKey(bundleName) && hashCache[bundleName] != hash;

                if (isCached && !isNotSameHashCached)
                {
                    // if current target is dependency, dependent assetBundle is already on memory. and no need to load it.
                    if (isDependency)
                    {
                        yield break;
                    }

                    // start loading assetBundle on memory.
                    var loadOnMemoryCachedAssetCoroutine = LoadOnMemoryAssetAsync(
                        bundleName,
                        assetName,
                        loadSucceeded,
                        loadFailed
                    );
                    while (loadOnMemoryCachedAssetCoroutine.MoveNext())
                    {
                        yield return null;
                    }

                    // loading asset from on memory cache is done.
                    yield break;
                }

                // assetBundle is cached on memory but it's not target hashed bundle. need to download other one.
                // var oldOnMemoryAssetBundle = onMemoryCache[bundleName];

                // remove from on memory cache.
                UnloadOnMemoryAssetBundle(bundleName);
            }

            /*
				assetBundle is..
					not yet cached (or) already cached.
					not allocated on memory.

				assetBundle is not on memory yet. start downloading.
			*/

            // binded block.
            using (var loadingConstraint = new AssetBundleLoadingConstraint(bundleName, loadingAssetBundleNames))
            {
                /*
					download bundle or load donwloaded bundle from cache.
					load to memory.
				*/
                {
                    var downloadCoroutine = DownloadAssetThenCacheAndLoadToMemory(
                        bundleName,
                        assetName,
                        url,
                        crc,
                        hash,
                        loadFailed,
                        timeoutTick
                    );
                    while (downloadCoroutine.MoveNext())
                    {
                        yield return null;
                    }

                    if (!onMemoryCache.ContainsKey(bundleName))
                    {
                        // error is already fired in above.
                        yield break;
                    }

                    if (!isDependency)
                    {

                        /*
							break if dependent bundle has load error.
						*/
                        if (dependentBundleLoadErrors.Any())
                        {
                            var loadErrorBundleMessages = new StringBuilder();
                            loadErrorBundleMessages.Append("failed to load/download dependent bundle:");
                            foreach (var dependentBundleLoadError in dependentBundleLoadErrors)
                            {
                                loadErrorBundleMessages.Append("bundleName:");
                                loadErrorBundleMessages.Append(dependentBundleLoadError.bundleName);
                                loadErrorBundleMessages.Append(" error:");
                                loadErrorBundleMessages.Append(dependentBundleLoadError.err);
                                loadErrorBundleMessages.Append(" reason:");
                                loadErrorBundleMessages.Append(dependentBundleLoadError.reason);
                                loadErrorBundleMessages.Append(" autoyaStatus:");
                                loadErrorBundleMessages.Append("  inMaintenance:" + dependentBundleLoadError.status.inMaintenance);
                                loadErrorBundleMessages.Append("  isAuthFailed:" + dependentBundleLoadError.status.isAuthFailed);
                            }
                            loadFailed(assetName, AssetBundleLoadError.FailedToLoadDependentBundles, loadErrorBundleMessages.ToString(), new AutoyaStatus());

                            yield break;
                        }

                        /*
							load asset from on memory AssetBundle.
						*/
                        var loadAssetCoroutine = LoadOnMemoryAssetAsync(
                            bundleName,
                            assetName,
                            loadSucceeded,
                            loadFailed
                        );
                        while (loadAssetCoroutine.MoveNext())
                        {
                            yield return null;
                        }
                    }
                }
            }
        }


        /**
			load assetBundle but not use.
            this method is for only download AssetBundle.
		*/
        public IEnumerator LoadAssetBundleThenNotUse(
            string bundleName,
            uint crc,
            Hash128 hash,
            bool considerDependencies,
            Action<string> loadSucceeded,
            Action<string, AssetBundleLoadError, string, AutoyaStatus> loadFailed,
            long timeoutTick,
            bool isDependency = false,
            List<string> loadingDependentBundleNames = null
        )
        {
            while (!Caching.ready)
            {
                yield return null;
            }

            var assetBundleInfo = bundleListStorage.AssetBundleInfoFromBundleName(bundleName);


            if (AssetBundleInfo.IsEmpty(assetBundleInfo))
            {
                // no assetBundleInfo found.
                loadFailed(bundleName, AssetBundleLoadError.NoAssetBundleFoundInList, "no assetBundle found:" + bundleName + " in list.", new AutoyaStatus());
                yield break;
            }

            var dependentBundleNames = assetBundleInfo.dependsBundleNames;

            var dependentBundleLoadErrors = new List<DependentBundleError>();

            /*
				resolve dependencies.
			*/
            if (considerDependencies)
            {
                if (dependentBundleNames.Any())
                {
                    var coroutines = new Dictionary<string, IEnumerator>();

                    foreach (var dependentBundleName in dependentBundleNames)
                    {
                        if (loadingDependentBundleNames != null)
                        {
                            if (loadingDependentBundleNames.Contains(dependentBundleName))
                            {
                                continue;
                            }
                        }

                        // skip if dependent assetBundle is already loaded on memory.
                        if (onMemoryCache.ContainsKey(dependentBundleName) && onMemoryCache[dependentBundleName] != null)
                        {
                            continue;
                        }

                        var dependedBundleInfo = bundleListStorage.AssetBundleInfoFromBundleName(dependentBundleName);
                        if (AssetBundleInfo.IsEmpty(dependedBundleInfo))
                        {
                            // skip empty info.
                            continue;
                        }

                        var dependentBundleCrc = dependedBundleInfo.crc;
                        var dependentBundleHash = Hash128.Parse(dependedBundleInfo.hash);

                        // set UnityEngine.Object for request. this asset will never use directory.
                        var loadCoroutine = LoadAssetBundleThenNotUse(
                            dependentBundleName,
                            dependentBundleCrc,
                            dependentBundleHash,
                            considerDependencies,
                            depBundleName =>
                            {
                                // do nothing. this bundle is currently cached.
                            },
                            (depBundleName, depErr, depReason, autoyaStatus) =>
                            {
                                // collect error for this dependent bundle loading.
                                dependentBundleLoadErrors.Add(new DependentBundleError(depBundleName, depErr, depReason, autoyaStatus));
                            },
                            timeoutTick,
                            true, // this loading is for resolve dependency of root asset. no need to return any instances.
                            loadingDependentBundleNames
                        );

                        if (loadingDependentBundleNames != null)
                        {
                            loadingDependentBundleNames.Add(dependentBundleName);
                        }
                        coroutines[dependentBundleName] = loadCoroutine;
                    }

                    if (coroutines.Count != 0)
                    {
                        while (true)
                        {
                            if (!coroutines.Where(c => c.Value != null).Any())
                            {
                                // load done.
                                break;
                            }

                            for (var i = 0; i < coroutines.Count; i++)
                            {
                                var loadingAssetBundleName = coroutines.Keys.ToArray()[i];
                                var coroutine = coroutines[loadingAssetBundleName];
                                if (coroutine == null) continue;

                                if (!coroutine.MoveNext())
                                {
                                    coroutines[loadingAssetBundleName] = null;
                                }
                            }
                            yield return null;
                        }

                        // all dependencies are loaded on memory.
                    }
                }
            }

            var url = GetAssetBundleDownloadUrl(bundleName);

            // check now loading or not. if same bundle is already under loading, wait it here.
            if (loadingAssetBundleNames.Contains(bundleName))
            {
                while (loadingAssetBundleNames.Contains(bundleName))
                {
                    yield return null;
                }

                // check downloaded bundle is correctly cached or not.
                var isCached = Caching.IsVersionCached(url, hash);
                if (!isCached)
                {
                    loadFailed(bundleName, AssetBundleLoadError.DownloadFailed, "caching failed.", new AutoyaStatus());
                    yield break;
                }
            }

            // target version assetBundle is not cached.

            // assetBundle is already allocated on memory.
            if (onMemoryCache.ContainsKey(bundleName))
            {
                var isCached = Caching.IsVersionCached(url, hash);

                // on UnityEditor, IsVersionCached resurns not valid result when on memory asset is updated by hash.
                var isNotSameHashCached = hashCache.ContainsKey(bundleName) && hashCache[bundleName] != hash;

                if (isCached && !isNotSameHashCached)
                {
                    // downloading asset to storage cache is done.
                    loadSucceeded(bundleName);
                    yield break;
                }

                // assetBundle is cached on memory but it's not target hashed bundle. need to download other one.
                // var oldOnMemoryAssetBundle = onMemoryCache[bundleName];

                // remove from on memory cache.
                UnloadOnMemoryAssetBundle(bundleName);
            }

            /*
				assetBundle is..
					not yet cached (or) already cached.
					not allocated on memory.

				assetBundle is not cached yet. start downloading.
			*/

            // binded block.
            using (var loadingConstraint = new AssetBundleLoadingConstraint(bundleName, loadingAssetBundleNames))
            {
                /*
					download bundle
				*/
                {
                    Action<string, object> succeeded = (conId, obj) =>
                    {
                        // unload assetBundle anyway.
                        // downloaded assetBundle is cached on storage.
                        var bundle = obj as AssetBundle;
                        bundle.Unload(true);

                        loadSucceeded(bundleName);
                    };
                    Action<string, int, string, AutoyaStatus> failed = (conId, code, reason, autoyaStatus) =>
                    {
                        // downloadAssetBundleが出力したコードを独自にErrorに変換する。
                        switch (code)
                        {
                            case CODE_CRC_MISMATCHED:
                                {
                                    loadFailed(bundleName, AssetBundleLoadError.CrcMismatched, reason, autoyaStatus);
                                    break;
                                }
                            default:
                                {
                                    loadFailed(bundleName, AssetBundleLoadError.DownloadFailed, "failed to download AssetBundle. code:" + code + " reason:" + reason, autoyaStatus);
                                    break;
                                }
                        }
                    };

                    var connectionId = AssetBundlesSettings.ASSETBUNDLES_DOWNLOAD_PREFIX + Guid.NewGuid().ToString();

                    var reqHeader = assetBundleGetRequestHeaderDelegate(url, new Dictionary<string, string>());

                    var downloadCoroutine = DownloadAssetBundle(
                        bundleName,
                        connectionId,
                        reqHeader,
                        url,
                        crc,
                        hash,
                        (conId, code, responseHeader, downloadedAssetBundle) =>
                        {
                            httpResponseHandlingDelegate(connectionId, responseHeader, code, downloadedAssetBundle, string.Empty, succeeded, failed);
                        },
                        (conId, code, reason, responseHeader) =>
                        {
                            httpResponseHandlingDelegate(connectionId, responseHeader, code, string.Empty, reason, succeeded, failed);
                        },
                        timeoutTick
                    );

                    while (downloadCoroutine.MoveNext())
                    {
                        yield return null;
                    }

                    if (!isDependency)
                    {

                        /*
							break if dependent bundle has load error.
						*/
                        if (dependentBundleLoadErrors.Any())
                        {
                            var loadErrorBundleMessages = new StringBuilder();
                            loadErrorBundleMessages.Append("failed to load/download dependent bundle:");
                            foreach (var dependentBundleLoadError in dependentBundleLoadErrors)
                            {
                                loadErrorBundleMessages.Append("bundleName:");
                                loadErrorBundleMessages.Append(dependentBundleLoadError.bundleName);
                                loadErrorBundleMessages.Append(" error:");
                                loadErrorBundleMessages.Append(dependentBundleLoadError.err);
                                loadErrorBundleMessages.Append(" reason:");
                                loadErrorBundleMessages.Append(dependentBundleLoadError.reason);
                                loadErrorBundleMessages.Append(" autoyaStatus:");
                                loadErrorBundleMessages.Append("  inMaintenance:" + dependentBundleLoadError.status.inMaintenance);
                                loadErrorBundleMessages.Append("  isAuthFailed:" + dependentBundleLoadError.status.isAuthFailed);
                            }
                            loadFailed(bundleName, AssetBundleLoadError.FailedToLoadDependentBundles, loadErrorBundleMessages.ToString(), new AutoyaStatus());
                        }
                        yield break;
                    }
                }
            }
        }

        private class AssetBundleLoadingConstraint : IDisposable
        {
            private string target;
            private List<string> list;

            public AssetBundleLoadingConstraint(string target, List<string> list)
            {
                this.target = target;
                this.list = list;

                this.list.Add(this.target);
            }

            private bool disposedValue = false;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        list.Remove(target);
                    }
                    disposedValue = true;
                }
            }

            void IDisposable.Dispose()
            {
                Dispose(true);
            }
        }

        private IEnumerator DownloadAssetThenCacheAndLoadToMemory(
            string bundleName,
            string assetName,
            string url,
            uint crc,
            Hash128 hash,
            Action<string, AssetBundleLoadError, string, AutoyaStatus> failed,
            long timeoutTick
        )
        {
            var connectionId = AssetBundlesSettings.ASSETBUNDLES_DOWNLOAD_PREFIX + Guid.NewGuid().ToString();

            Action<string, object> succeeded = (conId, downloadedAssetBundle) =>
            {
                if (!onMemoryCache.ContainsKey(bundleName))
                {
                    // set loaded assetBundle to on-memory cache.
                    onMemoryCache[bundleName] = (AssetBundle)downloadedAssetBundle;
                    hashCache[bundleName] = hash;
                }
            };

            Action<string, int, string, AutoyaStatus> downloadFailed = (conId, code, reason, autoyaStatus) =>
            {
                // 結局codeに依存しないエラーが出ちゃうのをどうしようかな、、、仕組みで避けきるしかないのか、、try-catchできないからな、、
                switch (code)
                {
                    case CODE_CRC_MISMATCHED:
                        {
                            failed(assetName, AssetBundleLoadError.CrcMismatched, reason, autoyaStatus);
                            break;
                        }
                    default:
                        {
                            failed(assetName, AssetBundleLoadError.DownloadFailed, "failed to download AssetBundle. code:" + code + " reason:" + reason, autoyaStatus);
                            break;
                        }
                }
            };

            var reqHeader = assetBundleGetRequestHeaderDelegate(url, new Dictionary<string, string>());

            var connectionCoroutine = DownloadAssetBundle(
                bundleName,
                connectionId,
                reqHeader,
                url,
                crc,
                hash,
                (conId, code, responseHeader, downloadedAssetBundle) =>
                {
                    httpResponseHandlingDelegate(connectionId, responseHeader, code, downloadedAssetBundle, string.Empty, succeeded, downloadFailed);
                },
                (conId, code, reason, responseHeader) =>
                {
                    httpResponseHandlingDelegate(connectionId, responseHeader, code, string.Empty, reason, succeeded, downloadFailed);
                },
                timeoutTick
            );

            while (connectionCoroutine.MoveNext())
            {
                yield return null;
            }
        }

        /**
            Downloads the asset bundle.
         */
        public IEnumerator DownloadAssetBundle(
            string bundleName,
            string connectionId,
            Dictionary<string, string> requestHeader,
            string url,
            uint crc,
            Hash128 hash,
            Action<string, int, Dictionary<string, string>, AssetBundle> succeeded,
            Action<string, int, string, Dictionary<string, string>> failed,
            long limitTick
        )
        {
            var alreadyStorageCached = false;
            if (Caching.IsVersionCached(url, hash))
            {
                alreadyStorageCached = true;
            }
            using (var request = UnityWebRequestAssetBundle.GetAssetBundle(url, hash, crc))
            {
                if (requestHeader != null)
                {
                    foreach (var kv in requestHeader)
                    {
                        request.SetRequestHeader(kv.Key, kv.Value);
                    }
                }

                var p = request.SendWebRequest();

                while (!p.isDone)
                {
                    yield return null;

                    // check timeout.
                    if (limitTick != 0 && limitTick < DateTime.UtcNow.Ticks)
                    {
                        request.Abort();
                        failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, "timeout to download bundle:" + bundleName, new Dictionary<string, string>());
                        yield break;
                    }
                }

                while (!request.isDone)
                {
                    yield return null;
                }

                var responseCode = (int)request.responseCode;
                var responseHeaders = request.GetResponseHeaders();

                if (request.isNetworkError)
                {
                    failed(connectionId, responseCode, request.error, responseHeaders);
                    yield break;
                }

                if (alreadyStorageCached)
                {
                    // set response code to 200 manually if already cached and succeeded to load from cache.
                    // sadly, in this case, the code is not 200 by default.
                    responseCode = 200;
                    responseHeaders = new Dictionary<string, string>();
                }
                else
                {
                    if (200 <= responseCode && responseCode <= 299)
                    {
                        // do nothing.
                    }
                    else
                    {
                        failed(connectionId, responseCode, "failed to load assetBundle. downloaded bundle:" + bundleName + " was not downloaded.", responseHeaders);
                        yield break;
                    }
                }

                var dataHandler = (DownloadHandlerAssetBundle)request.downloadHandler;

                var assetBundle = dataHandler.assetBundle;

                if (assetBundle == null)
                {
                    responseCode = CODE_CRC_MISMATCHED;
                    failed(connectionId, responseCode, "failed to load assetBundle. downloaded bundle:" + bundleName + ", requested crc was not matched.", responseHeaders);
                    yield break;
                }

                // wait for cache.
                while (!Caching.IsVersionCached(url, hash))
                {
                    yield return null;
                }

                succeeded(connectionId, responseCode, responseHeaders, assetBundle);
            }
        }

        /*
            core dictionary of on memory cache.
         */
        private readonly Dictionary<string, AssetBundle> onMemoryCache;

        // hash cache dictionary for delete if IsVersionCached is failed.
        private Dictionary<string, Hash128> hashCache = new Dictionary<string, Hash128>();

        private IEnumerator LoadOnMemoryAssetAsync<T>(string bundleName, string assetName, Action<string, T> loadSucceeded, Action<string, AssetBundleLoadError, string, AutoyaStatus> loadFailed) where T : UnityEngine.Object
        {
            var assetBundle = onMemoryCache[bundleName];
            var request = assetBundle.LoadAssetAsync<T>(assetName);
            while (!request.isDone)
            {
                yield return null;
            }

            /*
                asset is loaded asynchronously.
            */
            try
            {
                var asset = request.asset as T;

                if (asset == null)
                {
                    loadFailed(assetName, AssetBundleLoadError.NullAssetFound, "loaded assetName:" + assetName + " type:" + typeof(T) + " is null. maybe type does not matched. from bundleName:" + bundleName + ". please check asset type and that bundle contains this asset.", new AutoyaStatus());
                    yield break;
                }

                loadSucceeded(assetName, asset);
            }
            catch (Exception e)
            {
                loadFailed(assetName, AssetBundleLoadError.AssetLoadFailed, "failed to load assetName:" + assetName + " from bundleName:" + bundleName + " error:" + e.ToString(), new AutoyaStatus());
            }
        }

        public bool IsAssetExists(string assetName)
        {
            return assetNamesAndAssetBundleNamesDict.ContainsKey(assetName);
        }

        public bool IsBundleExists(string bundleName)
        {
            return assetNamesAndAssetBundleNamesDict.ContainsValue(bundleName);
        }

        public string GetAssetBundleDownloadUrl(string bundleName)
        {
            return bundleListStorage.GetAssetBundleUrl(bundleName);
        }

        public string[] OnMemoryBundleNames()
        {
            var loadedAssetBundleNames = onMemoryCache.Where(kv => kv.Value != null).Select(kv => kv.Key).ToArray();
            return loadedAssetBundleNames;
        }

        public string[] OnMemoryAssetNames()
        {
            var loadedAssetBundleNames = onMemoryCache.Where(kv => kv.Value != null).Select(kv => kv.Key).ToArray();
            return assetNamesAndAssetBundleNamesDict.Where(assetNameAndbundleName => loadedAssetBundleNames.Contains(assetNameAndbundleName.Value)).Select(assetNameAndbundleName => assetNameAndbundleName.Key).ToArray();
        }

        public string GetContainedAssetBundleName(string assetName)
        {
            if (!assetNamesAndAssetBundleNamesDict.ContainsKey(assetName))
            {
                return string.Empty;
            }
            return assetNamesAndAssetBundleNamesDict[assetName];
        }

        public bool IsAssetBundleCachedOnMemory(string bundleName)
        {
            if (onMemoryCache.ContainsKey(bundleName))
            {
                if (onMemoryCache[bundleName] != null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsAssetBundleCachedOnStorage(string bundleName)
        {
            var candidateAssetBundle = bundleListStorage.AssetBundleInfoFromBundleName(bundleName);
            if (AssetBundleInfo.IsEmpty(candidateAssetBundle))
            {
                return false;
            }

            var url = GetAssetBundleDownloadUrl(bundleName);
            var hash = Hash128.Parse(candidateAssetBundle.hash);
            return Caching.IsVersionCached(url, hash);
        }


        /**
			load specific type Scene from AssetBundle.
			dependency of AssetBundle will be solved automatically.

			note:
				this timeoutSec param is enabled only for downloading AssetBundle from web.
		*/
        public IEnumerator LoadScene(
            string assetName,
            LoadSceneMode mode,
            Action<string> loadSucceeded,
            Action<string, AssetBundleLoadError, string, AutoyaStatus> loadFailed,
            bool async = true,
            double timeoutSec = 0)
        {
            if (!assetNamesAndAssetBundleNamesDict.ContainsKey(assetName))
            {
                loadFailed(assetName, AssetBundleLoadError.NotContained, "searching asset name:" + assetName + " is not contained by any AssetBundles in all AssetBundleList.", new AutoyaStatus());
                yield break;
            }

            var timeoutTick = (DateTime.UtcNow + TimeSpan.FromSeconds(timeoutSec)).Ticks;
            if (timeoutSec == 0) timeoutTick = 0;

            var bundleName = assetNamesAndAssetBundleNamesDict[assetName];
            var assetBundleInfo = bundleListStorage.AssetBundleInfoFromBundleName(bundleName);

            if (AssetBundleInfo.IsEmpty(assetBundleInfo))
            {
                // no assetBundleInfo found.
                loadFailed(assetName, AssetBundleLoadError.NoAssetBundleFoundInList, "no assetBundle found:" + bundleName + " in list.", new AutoyaStatus());
                yield break;
            }

            var crc = assetBundleInfo.crc;
            var hash = Hash128.Parse(assetBundleInfo.hash);

            var coroutine = LoadSceneAssetBundleOnMemory(
                bundleName,
                assetName,
                mode,
                crc,
                hash,
                loadSucceeded,
                loadFailed,
                async,
                timeoutTick,
                false,
                new List<string>()
            );

            while (coroutine.MoveNext())
            {
                yield return null;
            }
        }

        /**
			load scene assetBundle on memory.
		*/
        private IEnumerator LoadSceneAssetBundleOnMemory(
            string bundleName,
            string sceneAssetName,
            LoadSceneMode mode,
            uint crc,
            Hash128 hash,
            Action<string> loadSucceeded,
            Action<string, AssetBundleLoadError, string, AutoyaStatus> loadFailed,
            bool async = true,
            long timeoutTick = 0,
            bool isDependency = false,
            List<string> loadingDependentBundleNames = null
        )
        {
            while (!Caching.ready)
            {
                yield return null;
            }

            var assetBundleInfo = bundleListStorage.AssetBundleInfoFromBundleName(bundleName);


            if (AssetBundleInfo.IsEmpty(assetBundleInfo))
            {
                // no assetBundleInfo found.
                loadFailed(sceneAssetName, AssetBundleLoadError.NoAssetBundleFoundInList, "no assetBundle found:" + bundleName + " in list.", new AutoyaStatus());
                yield break;
            }

            var dependentBundleNames = assetBundleInfo.dependsBundleNames;

            var dependentBundleLoadErrors = new List<DependentBundleError>();

            /*
				resolve dependencies.
			*/
            {
                if (dependentBundleNames.Any())
                {
                    var coroutines = new Dictionary<string, IEnumerator>();

                    foreach (var dependentBundleName in dependentBundleNames)
                    {
                        if (loadingDependentBundleNames != null)
                        {
                            if (loadingDependentBundleNames.Contains(dependentBundleName))
                            {
                                continue;
                            }
                        }

                        // skip if assetBundle is already loaded on memory.
                        if (onMemoryCache.ContainsKey(dependentBundleName) && onMemoryCache[dependentBundleName] != null)
                        {
                            continue;
                        }

                        var dependedBundleInfo = bundleListStorage.AssetBundleInfoFromBundleName(dependentBundleName);
                        if (AssetBundleInfo.IsEmpty(dependedBundleInfo))
                        {
                            // skip empty info.
                            continue;
                        }

                        var dependentBundleCrc = dependedBundleInfo.crc;
                        var dependentBundleHash = Hash128.Parse(dependedBundleInfo.hash);

                        // set UnityEngine.Object for request. this asset will never use directory.
                        var loadCoroutine = LoadAssetBundleOnMemory<UnityEngine.Object>(
                            dependentBundleName,
                            string.Empty,// bundleName not required.
                            dependentBundleCrc,
                            dependentBundleHash,
                            (depBundleName, depObj) =>
                            {
                                // do nothing. this bundle is currently on memory.
                            },
                            (depBundleName, depErr, depReason, autoyaStatus) =>
                            {
                                // collect error for this dependent bundle loading.
                                dependentBundleLoadErrors.Add(new DependentBundleError(depBundleName, depErr, depReason, autoyaStatus));
                            },
                            timeoutTick,
                            true, // this loading is for resolve dependency of root asset. no need to return any instances.
                            loadingDependentBundleNames
                        );

                        if (loadingDependentBundleNames != null)
                        {
                            loadingDependentBundleNames.Add(dependentBundleName);
                        }
                        coroutines[dependentBundleName] = loadCoroutine;
                    }

                    if (coroutines.Count != 0)
                    {
                        while (true)
                        {
                            if (!coroutines.Where(c => c.Value != null).Any())
                            {
                                // load done.
                                break;
                            }

                            for (var i = 0; i < coroutines.Count; i++)
                            {
                                var loadingAssetBundleName = coroutines.Keys.ToArray()[i];
                                var coroutine = coroutines[loadingAssetBundleName];
                                if (coroutine == null) continue;

                                if (!coroutine.MoveNext())
                                {
                                    coroutines[loadingAssetBundleName] = null;
                                }
                            }
                            yield return null;
                        }

                        // all dependencies are loaded on memory.
                    }
                }
            }

            var url = GetAssetBundleDownloadUrl(bundleName);

            // check now loading or not. if same bundle is already under loading, wait it here.
            if (loadingAssetBundleNames.Contains(bundleName))
            {
                while (loadingAssetBundleNames.Contains(bundleName))
                {
                    yield return null;
                }

                // check downloaded bundle is correctly cached or not.
                var isCached = Caching.IsVersionCached(url, hash);
                if (!isCached)
                {
                    loadFailed(sceneAssetName, AssetBundleLoadError.DownloadFailed, "caching failed.", new AutoyaStatus());
                    yield break;
                }
            }

            // assetBundle is already allocated on memory.
            if (onMemoryCache.ContainsKey(bundleName))
            {
                var isCached = Caching.IsVersionCached(url, hash);

                if (isCached)
                {
                    // if current target is dependency, dependent assetBundle is already on memory. and no need to load it.
                    if (isDependency)
                    {
                        yield break;
                    }

                    // load scene.
                    if (async)
                    {
                        var asyncOp = SceneManager.LoadSceneAsync(sceneAssetName, mode);
                        while (!asyncOp.isDone)
                        {
                            yield return null;
                        }
                    }
                    else
                    {
                        SceneManager.LoadScene(sceneAssetName, mode);
                    }

                    loadSucceeded(sceneAssetName);

                    // loading asset from on memory cache is done.
                    yield break;
                }

                // assetBundle is cached on memory but it's not target hashed bundle. need to download other one.
                // var oldOnMemoryAssetBundle = onMemoryCache[bundleName];

                // remove from on memory cache.
                UnloadOnMemoryAssetBundle(bundleName);
            }

            /*
				assetBundle is..
					not yet cached (or) already cached.
					not allocated on memory.

				assetBundle is not on memory yet. start downloading.
			*/

            // binded block.
            using (var loadingConstraint = new AssetBundleLoadingConstraint(bundleName, loadingAssetBundleNames))
            {
                /*
					download bundle or load donwloaded bundle from cache.
					load to memory.
				*/
                {
                    var downloadCoroutine = DownloadAssetThenCacheAndLoadToMemory(
                        bundleName,
                        sceneAssetName,
                        url,
                        crc,
                        hash,
                        loadFailed,
                        timeoutTick
                    );
                    while (downloadCoroutine.MoveNext())
                    {
                        yield return null;
                    }

                    if (!onMemoryCache.ContainsKey(bundleName))
                    {
                        // error is already fired in above.
                        yield break;
                    }

                    if (!isDependency)
                    {

                        /*
							break if dependent bundle has load error.
						*/
                        if (dependentBundleLoadErrors.Any())
                        {
                            var loadErrorBundleMessages = new StringBuilder();
                            loadErrorBundleMessages.Append("failed to load/download dependent bundle:");
                            foreach (var dependentBundleLoadError in dependentBundleLoadErrors)
                            {
                                loadErrorBundleMessages.Append("bundleName:");
                                loadErrorBundleMessages.Append(dependentBundleLoadError.bundleName);
                                loadErrorBundleMessages.Append(" error:");
                                loadErrorBundleMessages.Append(dependentBundleLoadError.err);
                                loadErrorBundleMessages.Append(" reason:");
                                loadErrorBundleMessages.Append(dependentBundleLoadError.reason);
                                loadErrorBundleMessages.Append(" autoyaStatus:");
                                loadErrorBundleMessages.Append("  inMaintenance:" + dependentBundleLoadError.status.inMaintenance);
                                loadErrorBundleMessages.Append("  isAuthFailed:" + dependentBundleLoadError.status.isAuthFailed);
                            }
                            loadFailed(sceneAssetName, AssetBundleLoadError.FailedToLoadDependentBundles, loadErrorBundleMessages.ToString(), new AutoyaStatus());

                            yield break;
                        }

                        /*
							load scene from on memory AssetBundle.
						*/
                        if (async)
                        {
                            var asyncOp = SceneManager.LoadSceneAsync(sceneAssetName, mode);
                            while (!asyncOp.isDone)
                            {
                                yield return null;
                            }
                        }
                        else
                        {
                            SceneManager.LoadScene(sceneAssetName, mode);
                        }

                        loadSucceeded(sceneAssetName);
                    }
                }
            }
        }

        /**
            Unloads the on memory asset bundles.
         */
        public void UnloadOnMemoryAssetBundles()
        {
            var assetBundleNames = onMemoryCache.Keys.ToArray();
            foreach (var assetBundleName in assetBundleNames)
            {
                var asset = onMemoryCache[assetBundleName];
                if (asset != null)
                {
                    asset.Unload(true);
                }
            }

            onMemoryCache.Clear();
        }

        public void UnloadOnMemoryAssetBundle(string bundleName)
        {
            if (onMemoryCache.ContainsKey(bundleName))
            {
                var asset = onMemoryCache[bundleName];
                if (asset != null)
                {
                    asset.Unload(true);
                }

                onMemoryCache.Remove(bundleName);
            }
        }

        public void UnloadOnMemoryAsset(string assetNameName)
        {
            var bundleName = GetContainedAssetBundleName(assetNameName);

            UnloadOnMemoryAssetBundle(bundleName);
        }
    }
}
