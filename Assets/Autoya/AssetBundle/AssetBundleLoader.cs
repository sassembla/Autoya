using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoyaFramework.Connections.HTTP;
using UnityEngine;

namespace AutoyaFramework.AssetBundles {
    public class AssetBundleLoader {
        public enum AssetBundleLoadError {
            NotContained,
            DownloadFailed,
            AssetLoadFailed
        }

        private uint ASSETBUNDLE_FIXED_VERSION = 1;
        private readonly string assetDownloadBasePath;
        private readonly HTTPConnection http;
        private readonly AssetBundleList list;

        private class HandleErrorFlowClass : IHTTPErrorFlow {
            // define nothing. use default error handling.
        }

        private readonly HandleErrorFlowClass flowInstance;
        
        private void HandleErrorFlow (string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string> failed) {
            flowInstance.HandleErrorFlow(connectionId, responseHeaders, httpCode, data, errorReason, succeeded, failed);
        }

        public AssetBundleLoader (string basePath, AssetBundleList list, IHTTPErrorFlow flowInstance=null) {
            this.assetDownloadBasePath = basePath;
            this.http = new HTTPConnection();
            this.list = list;
            
            /*
                if flowInstance is set, use these http error handling. else, use default http error handling.
            */
            if (flowInstance != null) this.flowInstance = flowInstance as HandleErrorFlowClass;
            else this.flowInstance = new HandleErrorFlowClass();

            /*
                construct assetName - AssetBundleName dictionary for fast loading.
            */
            assetNamesAndAssetBundleNamesDict.Clear();

            foreach (var assetBundle in list.assetBundles) {
                var bundleName = assetBundle.bundleName;
                foreach (var assetName in assetBundle.assetNames) {
                    assetNamesAndAssetBundleNamesDict[assetName] = bundleName;
                }
            }
        }

        /*
            unload all assetBundles and delete all assetBundle caches.
        */
        public bool CleanCachedAssetBundles () {
            /*
                clean all loaded assets.
            */
            UnloadAllAssetBundles();
            
            return Caching.CleanCache();
        }

        private List<string> loadingAssetBundleNames = new List<string>();
        private Dictionary<string, string> assetNamesAndAssetBundleNamesDict = new Dictionary<string, string>();

        /**
            load specific type Asset from AssetBundle.
            dependency of AssetBundle will be solved automatically.
        */
        public IEnumerator LoadAsset<T> (string assetName, Action<string, T> loadSucceeded, Action<string, AssetBundleLoadError, string> loadFailed) where T : UnityEngine.Object {
            if (!assetNamesAndAssetBundleNamesDict.ContainsKey(assetName)) {
                loadFailed(assetName, AssetBundleLoadError.NotContained, string.Empty);
                yield break;
            }
            
            var bundleName = assetNamesAndAssetBundleNamesDict[assetName];

            var coroutine = LoadAssetBundle(bundleName, assetName, loadSucceeded, loadFailed);
            while (coroutine.MoveNext()) {
                yield return null;
            }
        }

        private IEnumerator LoadAssetBundle<T> (string bundleName, string assetName, Action<string, T> loadSucceeded, Action<string, AssetBundleLoadError, string> loadFailed, bool isDependency=false) where T : UnityEngine.Object {
            var dependentAssetBundleNames = list.assetBundles.Where(bundle => bundle.bundleName == bundleName).FirstOrDefault().dependsBundleNames;

            /*
                resolve dependencies.
            */
            if (dependentAssetBundleNames.Any()) {
                var coroutines = new Dictionary<string, IEnumerator>();

                foreach (var dependentAssetBundleName in dependentAssetBundleNames) {
                    // skip if assetBundle is already loaded.
                    if (assetBundleDict.ContainsKey(dependentAssetBundleName) && assetBundleDict[dependentAssetBundleName] != null) {
                        continue;
                    }

                    var loadCoroutine = LoadAssetBundle(
                        dependentAssetBundleName, 
                        string.Empty, 
                        (string dependentAssetName, GameObject obj) => {},
                        loadFailed,
                        true // this loading is for resolve dependency. no need to return any instances.
                    );

                    coroutines[dependentAssetBundleName] = loadCoroutine;
                }

                if (coroutines.Count != 0) {
                   while (true) {
                        if (!coroutines.Where(c => c.Value != null).Any()) {
                            // load done.
                            break;
                        }

                        for (var i = 0; i < coroutines.Count; i++) {
                            var loadingAssetBundleName = coroutines.Keys.ToArray()[i];
                            var coroutine = coroutines[loadingAssetBundleName];
                            if (coroutine == null) continue;

                            if (!coroutine.MoveNext()) {
                                if (loadingAssetBundleNames.Contains(loadingAssetBundleName)) {
                                    loadingAssetBundleNames.Remove(loadingAssetBundleName);
                                }

                                coroutines[loadingAssetBundleName] = null;
                            }
                        }
                        yield return null;
                    }

                    // all dependencies are loaded on memory.
                }
            }

            // check now loading or not. if loading, wait it here.
            while (loadingAssetBundleNames.Contains(bundleName)) {
                yield return null;
            }

            while (!Caching.ready) {
                yield return null;
            }

            var url = assetDownloadBasePath + bundleName;
            var crc = list.assetBundles.Where(a => a.bundleName == bundleName).FirstOrDefault().crc;
            
            // check cached or not.
            if (Caching.IsVersionCached(url, (int)ASSETBUNDLE_FIXED_VERSION)) {
                
                /*
                    assetBundle is..
                        already cached.
                        allocated on memory or not.
                */

                // assetBundle is already allocated on memory. load that.
                if (assetBundleDict.ContainsKey(bundleName)) {
                    if (isDependency) {
                        yield break;
                    }

                    var loadOnMemoryCachedAssetCoroutine = LoadOnMemoryAssetAsync(bundleName, assetName, loadSucceeded, loadFailed);
                    while (loadOnMemoryCachedAssetCoroutine.MoveNext()) {
                        yield return null;
                    }
                    yield break;
                }
                
                // if assetBundle is cached but not on memory yet, continue loading.
            }

            /*
                assetBundle is..
                    not yet cached (or) already cached.
                    not allocated on memory.
            */
            
            // assetBundle is not on memory yet. start downloading.
            if (!loadingAssetBundleNames.Contains(bundleName)) {
                loadingAssetBundleNames.Add(bundleName);
            }
            
            var downloadCoroutine = DownloadAssetThenCacheAndLoadOnMemory(bundleName, assetName, url, crc, loadFailed);// ここで起こりうる通信エラーは、unauth含めてめっちゃいっぱいあるんだけど、認証をアセットが置いてあるところにまでかけるかどうか、という選択肢をどうにかして使用者に渡した方がいい感じがする。
            while (downloadCoroutine.MoveNext()) {
                yield return null;
            }

            if (!assetBundleDict.ContainsKey(bundleName)) {
                // error is already fired in above.
                yield break;
            }
            
            if (!isDependency) { 
                /*
                    load asset from on memory AssetBundle.
                */
                var loadAssetCoroutine = LoadOnMemoryAssetAsync(bundleName, assetName, loadSucceeded, loadFailed);
                while (loadAssetCoroutine.MoveNext()) {
                    yield return null;
                }
            }

            // unlock.
            loadingAssetBundleNames.Remove(bundleName);
        }

        private IEnumerator DownloadAssetThenCacheAndLoadOnMemory (string bundleName, string assetName, string url, uint crc, Action<string, AssetBundleLoadError, string> failed) {
            Debug.LogWarning("定冠詞を定義へ移動");
            var connectionId = "asset_" + Guid.NewGuid().ToString();

            Action<string, object> succeeded = (conId, downloadedAssetBundle) => {
                // set loaded assetBundle to on-memory cache.
                assetBundleDict[bundleName] = downloadedAssetBundle as AssetBundle;
            };

            Action<string, int, string> downloadFailed = (conId, code, reason) => {
                // 結局codeに依存しないエラーが出ちゃうのをどうしようかな、、、仕組みで避けきるしかないのか、、try-catchできないからな、、

                Debug.LogError("failed to download AssetBundle. code:" + code + " reason:" + reason);

                failed(assetName, AssetBundleLoadError.DownloadFailed, "failed to download AssetBundle. code:" + code + " reason:" + reason);
            };

            Debug.LogWarning("Autoyaのauth機構をめり込ませられる必要がある。後段はなんとかなってるので、あとは前段をなんとかする。ヘッダ部、http機能丸パクでもいいのかな、、");
            var connectionCoroutine = http.DownloadAssetBundle(
                connectionId,
                null,
                url, 
                ASSETBUNDLE_FIXED_VERSION,
                crc, 
                (conId, code, responseHeader, downloadedAssetBundle) => {
                    HandleErrorFlow(connectionId, responseHeader, code, downloadedAssetBundle, string.Empty, succeeded, downloadFailed);
                }, 
                (conId, code, reason, responseHeader) => {
                    HandleErrorFlow(connectionId, responseHeader, code, string.Empty, reason, succeeded, downloadFailed);
                }
            );

            while (connectionCoroutine.MoveNext()) {
                yield return null;
            }
        }

        private Dictionary<string, AssetBundle> assetBundleDict = new Dictionary<string, AssetBundle>();
        private IEnumerator LoadOnMemoryAssetAsync<T> (string bundleName, string assetName, Action<string, T> loadSucceeded, Action<string, AssetBundleLoadError, string> loadFailed) where T : UnityEngine.Object {
            var assetBundle = assetBundleDict[bundleName];

            var request = assetBundle.LoadAssetAsync(assetName);
            while (!request.isDone) {
                yield return null;
            }

            try {
                var asset = request.asset as T;
                loadSucceeded(assetName, asset);
            } catch (Exception e) {
                loadFailed(assetName, AssetBundleLoadError.AssetLoadFailed, e.ToString());
            }
        }

        public string[] OnMemoryAssetNames () {
            var loadedAssetBundleNames = assetBundleDict.Where(kv => kv.Value != null).Select(kv => kv.Key).ToArray();
            return list.assetBundles.Where(ab => loadedAssetBundleNames.Contains(ab.bundleName)).SelectMany(ab => ab.assetNames).ToArray();
        }

        public string GetContainedAssetBundleName (string assetName) {
            if (!assetNamesAndAssetBundleNamesDict.ContainsKey(assetName)) {
                return string.Empty;
            }
            return assetNamesAndAssetBundleNamesDict[assetName];
        }

        public void UnloadAllAssetBundles () {
            var assetBundleNames = assetBundleDict.Keys.ToArray();

            foreach (var assetBundleName in assetBundleNames) {
                var asset = assetBundleDict[assetBundleName];
                if (asset != null) {
                    asset.Unload(true);
                }
            }

            assetBundleDict.Clear();
        }

        public void UnloadAssetBundle (string bundleName) {
            if (assetBundleDict.ContainsKey(bundleName)) {
                var asset = assetBundleDict[bundleName];
                if (asset != null) {
                    asset.Unload(true);
                }
                
                assetBundleDict.Remove(bundleName);
            }
        }
    }
}