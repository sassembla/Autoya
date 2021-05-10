using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoyaFramework.Settings.AssetBundles;
using UnityEngine;
using UnityEngine.Networking;

namespace AutoyaFramework.AssetBundles
{

    /**
       preloadList
        └ name // human readable name of list.
        └ bundleNames // string[]
            └ bundleName // preload target bundle name.
     */
    [Serializable]
    public class PreloadList
    {
        public string name;
        public string[] bundleNames;
        public PreloadList(string name, string[] bundleNames)
        {
            this.name = name;
            this.bundleNames = bundleNames;
        }

        /**
			create preloadList which contains whole assetBundle names in the AssetBundleList.
		 */
        public PreloadList(string name, AssetBundleList list)
        {
            this.name = name;
            this.bundleNames = list.assetBundles.Select(abInfo => abInfo.bundleName).ToArray();
        }
    }


    public class AssetBundlePreloader
    {
        /*
			delegate for handle http response for modules.
		*/
        public delegate void HttpResponseHandlingDelegate(string connectionId, Dictionary<string, string> responseHeader, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed);

        /*
			delegate for supply assetBundle get request header geneate func for modules.
		*/
        public delegate Dictionary<string, string> PreloadListGetRequestHeaderDelegate(string url, Dictionary<string, string> requestHeader);
        private readonly HttpResponseHandlingDelegate httpResponseHandlingDelegate;
        private readonly PreloadListGetRequestHeaderDelegate preloadListGetRequestHeaderDelegate;

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

        public AssetBundlePreloader(PreloadListGetRequestHeaderDelegate requestHeader = null, HttpResponseHandlingDelegate httpResponseHandlingDelegate = null)
        {
            if (requestHeader != null)
            {
                this.preloadListGetRequestHeaderDelegate = requestHeader;
            }
            else
            {
                this.preloadListGetRequestHeaderDelegate = BasicRequestHeaderDelegate;
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

        /**
			preload assetBundle from list url.
		 */

        public IEnumerator Preload(AssetBundleLoader loader, string listUrl, Action<string[], Action, Action> onBeforePreloading, Action<double> progress, Action done, Action<int, string, AutoyaStatus> preloadFailed, Action<string, AssetBundleLoadError, string, AutoyaStatus> bundlePreloadFailed, int maxParallelCount = 1, double timeoutSec = 0)
        {
            if (0 < maxParallelCount)
            {
                // pass.
            }
            else
            {
                yield return null;
                preloadFailed(-1, "maxParallelCount is negative or 0. unable to start preload.", new AutoyaStatus());
                yield break;
            }

            if (loader != null)
            {
                // pass.
            }
            else
            {
                yield return null;
                preloadFailed(-1, "attached AssetBundleLoader is null. unable to start preload.", new AutoyaStatus());
                yield break;
            }

            var connectionId = AssetBundlesSettings.ASSETBUNDLES_PRELOADLIST_PREFIX + Guid.NewGuid().ToString();
            var reqHeader = preloadListGetRequestHeaderDelegate(listUrl, new Dictionary<string, string>());

            PreloadList list = null;
            Action<string, object> listDonwloadSucceeded = (conId, listData) =>
            {
                var listStr = listData as string;
                list = JsonUtility.FromJson<PreloadList>(listStr);
            };

            Action<string, int, string, AutoyaStatus> listDownloadFailedAct = (conId, code, reason, autoyaStatus) =>
            {
                preloadFailed(code, reason, autoyaStatus);
            };

            var downloadCoroutine = DownloadPreloadList(
                connectionId,
                reqHeader,
                listUrl,
                (conId, code, responseHeaders, listData) =>
                {
                    httpResponseHandlingDelegate(connectionId, responseHeaders, code, listData, string.Empty, listDonwloadSucceeded, listDownloadFailedAct);
                },
                (conId, code, reason, responseHeaders) =>
                {
                    httpResponseHandlingDelegate(connectionId, responseHeaders, code, string.Empty, reason, listDonwloadSucceeded, listDownloadFailedAct);
                },
                timeoutSec
            );

            while (downloadCoroutine.MoveNext())
            {
                yield return null;
            }

            // check if the list download is done.
            if (list != null)
            {
                // pass.
            }
            else
            {
                yield break;
            }

            var bundleDownloadCor = Preload(loader, list, onBeforePreloading, progress, done, preloadFailed, bundlePreloadFailed, maxParallelCount);
            while (bundleDownloadCor.MoveNext())
            {
                yield return null;
            }
        }


        public IEnumerator Preload(
            AssetBundleLoader loader,
            PreloadList preloadList,
            Action<string[], Action, Action> onBeforePreloading,
            Action<double> progress,
            Action done,
            Action<int, string, AutoyaStatus> preloadFailed,
            Action<string, AssetBundleLoadError, string, AutoyaStatus> bundlePreloadFailed,
            int maxParallelCount = 1
        )
        {
            if (0 < maxParallelCount)
            {
                // pass.
            }
            else
            {
                yield return null;
                preloadFailed(-1, "maxParallelCount is negative or 0. unable to start preload.", new AutoyaStatus());
                yield break;
            }

            if (loader != null)
            {
                // pass.
            }
            else
            {
                yield return null;
                preloadFailed(-1, "attached AssetBundleLoader is null. unable to start preload.", new AutoyaStatus());
                yield break;
            }

            if (preloadList != null)
            {
                // pass.
            }
            else
            {
                yield return null;
                preloadFailed(-1, "preloadList is null. unable to start preload.", new AutoyaStatus());
                yield break;
            }

            /*
                check if preloadList's assetBundleNames are contained by assetBundleList.
             */
            var targetAssetBundleNames = preloadList.bundleNames;
            var assetBundleListContainedAssetBundleNames = loader.GetWholeBundleNames();

            // start preload assetBundles.
            var loadingCoroutines = new Queue<IEnumerator>();

            var currentDownloadCount = 0;

            // define.
            var totalLoadingCoroutinesCount = 0;

            /*
                assetBundle downloaded actions.
             */
            Action<string> bundlePreloadSucceededAct = (bundleName) =>
            {
                currentDownloadCount++;
                // Debug.Log("bundleName:" + bundleName);

                var count = ((currentDownloadCount * 100.0) / totalLoadingCoroutinesCount) * 0.01;
                progress(count);
            };

            Action<string, AssetBundleLoadError, string, AutoyaStatus> bundlePreloadFailedAct = (bundleName, error, reason, autoyaStatus) =>
            {
                bundlePreloadFailed(bundleName, error, reason, autoyaStatus);
            };

            var wholeDownloadableAssetBundleNames = new List<string>();
            foreach (var targetAssetBundleName in targetAssetBundleNames)
            {
                // skip not contained bundle.
                if (!assetBundleListContainedAssetBundleNames.Contains(targetAssetBundleName))
                {
                    continue;
                }

                // reserve this assetBundle name.
                wholeDownloadableAssetBundleNames.Add(targetAssetBundleName);

                // add only 1st level dependent AsstBundle Names.
                var dependentBundleNames = loader.AssetBundleInfoFromBundleName(targetAssetBundleName).dependsBundleNames;
                wholeDownloadableAssetBundleNames.AddRange(dependentBundleNames);
            }

            var shouldDownloadAssetBundleNamesCandidate = wholeDownloadableAssetBundleNames.Distinct().ToArray();
            var shouldDownloadAssetBundleNames = new List<string>();

            foreach (var shouldDownloadAssetBundleName in shouldDownloadAssetBundleNamesCandidate)
            {
                var bundleUrl = loader.GetAssetBundleDownloadUrl(shouldDownloadAssetBundleName);
                var targetBundleInfo = loader.AssetBundleInfoFromBundleName(shouldDownloadAssetBundleName);
                var hash = Hash128.Parse(targetBundleInfo.hash);

                // ignore cached one.
                if (Caching.IsVersionCached(bundleUrl, hash))
                {
                    continue;
                }

                // ignore loaded one.
                if (loader.IsAssetBundleCachedOnMemory(shouldDownloadAssetBundleName))
                {
                    continue;
                }

                shouldDownloadAssetBundleNames.Add(shouldDownloadAssetBundleName);
            }

            // ここまでで、DLする対象のAssetBundleと、そのAssetBundleが依存しているAssetBundleまでが確保できている。
            /*
                ask should continue or not before downloading target assetBundles.
                estimation is not considered about duplication of download same AssetBundle at same time.
                this is max case.
             */
            var shouldContinueCor = shouldContinuePreloading(shouldDownloadAssetBundleNames.ToArray(), onBeforePreloading);
            while (shouldContinueCor.MoveNext())
            {
                yield return null;
            }

            var shouldContinue = shouldContinueCor.Current;
            if (!shouldContinue)
            {
                done();
                yield break;
            }

            /*
                bundles are not cached on storage. should start download.
             */
            foreach (var shouldDownloadAssetBundleName in shouldDownloadAssetBundleNames)
            {
                var targetBundleInfo = loader.AssetBundleInfoFromBundleName(shouldDownloadAssetBundleName);
                var crc = targetBundleInfo.crc;

                var hash = Hash128.Parse(targetBundleInfo.hash);

                // bundle is now cached on storage. cancel downloading.
                if (loader.IsAssetBundleCachedOnStorage(shouldDownloadAssetBundleName))
                {
                    continue;
                }

                // bundle is not cached and found on memory.
                if (loader.IsAssetBundleCachedOnMemory(shouldDownloadAssetBundleName))
                {
                    // bundle is not cached on storage, but old bundle is on memory. need to unload it.
                    loader.UnloadOnMemoryAssetBundle(shouldDownloadAssetBundleName);
                }

                var bundlePreloadTimeoutTick = 0;// preloader does not have limit now.
                /*
                    AssetBundleのダウンロードを行う。
                    依存取得はoffで、ハンドリングはAssetBundleLoaderのものを使用する。
                 */
                var cor = loader.LoadAssetBundleThenNotUse(
                    shouldDownloadAssetBundleName,
                    crc,
                    hash,
                    false,// not download more dependency.
                    downloadedBundleName =>
                    {
                        bundlePreloadSucceededAct(downloadedBundleName);
                    },
                    (downloadedBundleName, error, reason, autoyaStatus) =>
                    {
                        bundlePreloadFailedAct(downloadedBundleName, error, reason, autoyaStatus);
                    },
                    bundlePreloadTimeoutTick
                );

                loadingCoroutines.Enqueue(cor);
            }

            // update total count to actual.
            totalLoadingCoroutinesCount = loadingCoroutines.Count;

            /*
                execute loading.
             */
            var currentLoadingCoroutines = new IEnumerator[maxParallelCount];
            while (true)
            {
                for (var j = 0; j < currentLoadingCoroutines.Length; j++)
                {
                    var currentLoadingCoroutine = currentLoadingCoroutines[j];
                    if (currentLoadingCoroutine == null)
                    {
                        // set next coroutine to currentLoadingCoroutines.
                        if (0 < loadingCoroutines.Count)
                        {
                            var next = loadingCoroutines.Dequeue();
                            currentLoadingCoroutines[j] = next;
                        }
                        else
                        {
                            // no coroutine exists.
                            continue;
                        }
                    }
                }

                var loading = false;
                for (var j = 0; j < currentLoadingCoroutines.Length; j++)
                {
                    var cor = currentLoadingCoroutines[j];
                    if (cor == null)
                    {
                        continue;
                    }

                    var result = cor.MoveNext();

                    // just finished.
                    if (!result)
                    {
                        currentLoadingCoroutines[j] = null;
                    }
                    else
                    {
                        loading = true;
                    }
                }

                if (0 < loadingCoroutines.Count)
                {
                    loading = true;
                }

                if (loading)
                {
                    yield return null;
                }
                else
                {
                    // nothing loading.
                    break;
                }
            }

            // every bundle downloading is done.
            done();
        }

        private IEnumerator<bool> shouldContinuePreloading(string[] willLoadBundleNames, Action<string[], Action, Action> onBeforePreloading)
        {
            var determined = false;
            var go = false;

            Action proceed = () =>
            {
                determined = true;
                go = true;
            };

            Action cancel = () =>
            {
                determined = true;
                go = false;
            };

            // ask to user.
            onBeforePreloading(willLoadBundleNames, proceed, cancel);

            while (!determined)
            {
                yield return false;
            }

            yield return go;
        }

        private IEnumerator DownloadPreloadList(
            string connectionId,
            Dictionary<string, string> requestHeader,
            string url,
            Action<string, int, Dictionary<string, string>, string> succeeded,
            Action<string, int, string, Dictionary<string, string>> failed,
            double timeoutSec = 0
        )
        {
            var timeoutTick = (DateTime.UtcNow + TimeSpan.FromSeconds(timeoutSec)).Ticks;
            using (var request = UnityWebRequest.Get(url))
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
                    if (timeoutSec != 0 && timeoutTick < DateTime.UtcNow.Ticks)
                    {
                        request.Abort();
                        failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, "timeout to download preload list:" + url, new Dictionary<string, string>());
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

                if (200 <= responseCode && responseCode <= 299)
                {
                    // do nothing.
                }
                else
                {
                    failed(connectionId, responseCode, "failed to download preload list:" + url + " was not downloaded.", responseHeaders);
                    yield break;
                }

                var result = Encoding.UTF8.GetString(request.downloadHandler.data);
                succeeded(connectionId, responseCode, responseHeaders, result);
            }
        }

    }
}