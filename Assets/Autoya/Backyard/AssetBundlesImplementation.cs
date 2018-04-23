using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Settings.AssetBundles;
using UnityEngine;

namespace AutoyaFramework
{
    /**
		assetBundles implementation.


	 */
    public partial class Autoya
    {
        public enum AssetBundlesFeatureState
        {
            None,
            ListDownoading,
            Ready,
        }
        private AssetBundlesFeatureState assetBundleFeatState;



        public enum CurrentUsingBundleCondition
        {
            UsingAssetsAreChanged,

            NoUsingAssetsChanged,

            AlreadyUpdated
        }


        /*
			Initializer
		 */
        private void InitializeAssetBundleFeature()
        {
            // initialize AssetBundleListDownloader.
            AssetBundleListDownloader.HttpResponseHandlingDelegate httpResponseHandlingDel = (p1, p2, p3, p4, p5, p6, p7) =>
            {
                httpResponseHandlingDelegate(p1, p2, p3, p4, p5, p6, p7);
            };
            AssetBundleListDownloader.AssetBundleListGetRequestHeaderDelegate assetBundleGetRequestHeaderDel = (p1, p2) =>
            {
                return assetBundleListGetRequestHeaderDelegate(p1, p2);
            };

            /*
                by default, only list downloader is ready on boot in AssetBundles feature.
             */
            _assetBundleListDownloader = new AssetBundleListDownloader(assetBundleGetRequestHeaderDel, httpResponseHandlingDel);

            // check if assetBundleList are stored.
            var storedLists = LoadAssetBundleListsFromStorage();
            var storedListIdentities = storedLists.Select(list => list.identity).ToArray();

            // get assetBundleList identitiy from manifest info.
            var runtimeManifestContaindAssetBundleListIdentities = LoadAppUsingAssetBundleListIdentities();


            // 取得失敗などでmanifestの内容の方が多いことがあり得る。差を取り出し、DLが必要な状態かどうか判断する。
            var excepts = runtimeManifestContaindAssetBundleListIdentities.Except(storedListIdentities);

            // not all assetBundleList are stored yet.
            // need to run AssetBundle_DownloadAssetBundleListsIfNeed().
            if (excepts.Any())
            {
                assetBundleFeatState = AssetBundlesFeatureState.None;
                return;
            }

            // all assetBundleList is stored and ready.
            foreach (var listCandidate in storedLists)
            {
                ReadyLoaderAndPreloader(listCandidate);
            }
            assetBundleFeatState = AssetBundlesFeatureState.Ready;
        }

        public static bool AssetBundle_IsAssetBundleFeatureReady()
        {
            if (autoya == null)
            {
                return false;
            }

            switch (autoya.assetBundleFeatState)
            {
                case AssetBundlesFeatureState.Ready:
                    {
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        public static void AssetBundle_DiscardAssetBundleList(Action onDiscarded, Action<AssetBundlesError, string> onDiscardFailed)
        {
            var cont = CheckAssetBundlesFeatureCondition(
                (code, reason) =>
                {
                    onDiscardFailed(code, reason);
                }
            );

            if (!cont)
            {
                return;
            }

            // delete assetBundleList manually.
            var deleted = autoya.DeleteAssetBundleListsFromStorage();
            if (!deleted)
            {
                onDiscardFailed(AssetBundlesError.FailedToDiscardList, "failed to discard list data.");
                return;
            }

            // reset runtime manifest resource infos.
            autoya.OnRestoreRuntimeManifest();

            autoya.assetBundleFeatState = AssetBundlesFeatureState.None;

            onDiscarded();
        }

        public static bool AssetBundle_DeleteAllStorageCache()
        {
            var cont = CheckAssetBundlesFeatureCondition(
                (code, reason) =>
                {
                    // do nothing.
                }
            );

            if (!cont)
            {
                return false;
            }

            return Caching.ClearCache();
        }

        /*
			Downloader
		*/
        private AssetBundleListDownloader _assetBundleListDownloader;


        private CurrentUsingBundleCondition GetCurrentAssetBundleUsingCondition(AssetBundleList newList)
        {

            // check version of new list and current stored list.

            var currentList = _assetBundleLoader.GetAssetBundleListByIdentity(newList.identity);

            if (currentList.version != newList.version)
            {
                // check using assets are changed or not.

                var newBundleCrcs = newList.assetBundles.ToDictionary(bundle => bundle.bundleName, bundle => bundle.crc);
                var oldBundleCrcs = currentList.assetBundles.ToDictionary(bundle => bundle.bundleName, bundle => bundle.crc);

                var changedUsingBundleNames = new List<string>();
                foreach (var oldBundleCrcItem in oldBundleCrcs)
                {
                    var bundleName = oldBundleCrcItem.Key;
                    var bundleCrc = oldBundleCrcItem.Value;

                    if (newBundleCrcs.ContainsKey(bundleName))
                    {
                        if (newBundleCrcs[bundleName] != bundleCrc)
                        {
                            // crc changed = assetBundle is updated.

                            // is using now?
                            if (_assetBundleLoader.IsAssetBundleCachedOnMemory(bundleName))
                            {
                                /*
									changed assetBundle is using now.
								 */
                                changedUsingBundleNames.Add(bundleName);
                            }
                        }
                    }
                    else
                    {
                        // in new list, current using assetBundle is not exists.
                        // nothing to do. but detected.
                    }
                }


                if (changedUsingBundleNames.Any())
                {
                    // using assetBundle is updated in new list.
                    return CurrentUsingBundleCondition.UsingAssetsAreChanged;
                }
                else
                {
                    // no using && change of assetBundles are detected.
                    return CurrentUsingBundleCondition.NoUsingAssetsChanged;
                }
            }

            // list version is not changed. 

            return CurrentUsingBundleCondition.AlreadyUpdated;
        }

        private enum NewListDownloaderState
        {
            Ready,
            Downloading,
        }

#pragma warning disable 414
        private NewListDownloaderState newListDownloaderState = NewListDownloaderState.Ready;
#pragma warning restore 414

        private void DownloadNewAssetBundleList(string url)
        {
            newListDownloaderState = NewListDownloaderState.Downloading;
            mainthreadDispatcher.Commit(
                autoya._assetBundleListDownloader.DownloadAssetBundleList(
                    url,
                    (downloadedUrl, newList) =>
                    {
                        // got new list.
                        mainthreadDispatcher.Commit(OnUpdatingListReceived(newList));
                    },
                    (code, reason, autoyaStatus) =>
                    {
                        // failed to get new list.
                        // Debug.Log("failed to download new AssetBundleList from url:" + url + " code:" + code + " reason:" + reason);
                        newListDownloaderState = NewListDownloaderState.Ready;
                    }
                )
            );
        }

        private IEnumerator OnUpdatingListReceived(AssetBundleList newList)
        {
            var assetUsingCondition = GetCurrentAssetBundleUsingCondition(newList);

            var proceed = false;
            var done = false;
            ShouldUpdateToNewAssetBundleList(
                assetUsingCondition,
                () =>
                {
                    done = true;
                    proceed = true;
                },
                () =>
                {
                    done = true;
                    proceed = false;
                }
            );

            while (!done)
            {
                yield return null;
            }

            if (!proceed)
            {
                yield break;
            }

            // store AssetBundleList and renew Loader and Preloader.

            var result = StoreAssetBundleListToStorage(newList);
            if (result)
            {
                // update AssetBundleList version.
                OnUpdateToNewAssetBundleList(newList.identity, newList.version);

                ReadyLoaderAndPreloader(newList);

                // finish downloading new assetBundleList.
                newListDownloaderState = NewListDownloaderState.Ready;

                /*
                    start postprocess.
                 */
                var condition = GetCurrentAssetBundleUsingCondition(newList);
                var postProcessReady = false;

                OnAssetBundleListUpdated(
                    condition,
                    () =>
                    {
                        postProcessReady = true;
                    }
                );

                while (!postProcessReady)
                {
                    yield return null;
                }

                yield break;
            }

            // failed to store new assetBundleList.
            var cor = OnNewAssetBundleListStoreFailed("failed to store new AssetBundleList to storage. maybe shortage of storage size.");
            while (cor.MoveNext())
            {
                yield return null;
            }

            // failed to store AssetBundleList. finish downloading new assetBundleList anyway.
            newListDownloaderState = NewListDownloaderState.Ready;
        }

        public enum ListDownloadResult
        {
            AlreadyDownloaded,
            ListDownloaded
        }

        public enum ListDownloadError
        {
            AutoyaNotReady,
            AlreadyDownloading,
            FailedToDownload,
            FailedToStoreDownloadedAssetBundleList,
            NotCointainedInRuntimeManifestOrOther
        }


        /**
			Download assetBundleList if need.
            using the url which supplied from OverridePoints.
		 */
        public static void AssetBundle_DownloadAssetBundleListsIfNeed(Action<ListDownloadResult> downloadSucceeded, Action<ListDownloadError, string, AutoyaStatus> downloadFailed, double timeoutSec = AssetBundlesSettings.TIMEOUT_SEC)
        {
            if (autoya == null)
            {
                downloadFailed(ListDownloadError.AutoyaNotReady, "Autoya not ready.", new AutoyaStatus());
                return;
            }

            // 起動時、リストを保持してたら自動的にReadyになる。
            // 起動時、リストを保持していなかった場合、このメソッドからDLしてReadyモードになる。
            // 取得中にこのメソッドを連打してもfailになる(最初のやつがDLを継続する)

            // 起動時、リストを保持していなかった場合、リクエストヘッダにはデフォルトのresVersionが入る
            // サーバがresVersionレスポンスヘッダを返す + そのバージョンがデフォルトと異なった場合、リストDLが自動的に開始される。

            switch (autoya.assetBundleFeatState)
            {
                case AssetBundlesFeatureState.ListDownoading:
                    {
                        // already loading.
                        downloadFailed(ListDownloadError.AlreadyDownloading, "already downloading.", new AutoyaStatus());
                        return;
                    }
                case AssetBundlesFeatureState.Ready:
                    {
                        downloadSucceeded(ListDownloadResult.AlreadyDownloaded);
                        return;
                    }
                case AssetBundlesFeatureState.None:
                    {
                        // pass.
                        break;
                    }
                default:
                    {
                        downloadFailed(ListDownloadError.FailedToDownload, "unexpected state:" + autoya.assetBundleFeatState, new AutoyaStatus());
                        return;
                    }
            }

            /*
				assetBundleFeatState is None.
                load assetBundleList info from runtimeManifest.
			 */
            var listUrls = autoya.OnAssetBundleListUrlsRequired();
            autoya.Internal_AssetBundle_DownloadAssetBundleListFromUrl(listUrls, downloadSucceeded, downloadFailed, timeoutSec);
        }


        private void Internal_AssetBundle_DownloadAssetBundleListFromUrl(string[] listUrls, Action<ListDownloadResult> downloadSucceeded, Action<ListDownloadError, string, AutoyaStatus> downloadFailed, double timeoutSec = AssetBundlesSettings.TIMEOUT_SEC)
        {
            assetBundleFeatState = AssetBundlesFeatureState.ListDownoading;

            var wholeAssetBundleListCount = listUrls.Length;

            var isDownloadFailed = false;

            var downloadedListIdentities = new List<string>();
            Action<string, AssetBundleList> succeeded = (url, newList) =>
            {
                /*
                    リストの保存に失敗した場合、全ての処理が失敗した扱いになる。
                 */
                var result = StoreAssetBundleListToStorage(newList);
                if (result)
                {
                    // update runtime manifest. set "resVersion" to downloaded version.
                    // update AssetBundleList version.
                    OnUpdateToNewAssetBundleList(newList.identity, newList.version);

                    try
                    {
                        // update list in loader.
                        ReadyLoaderAndPreloader(newList);
                    }
                    catch (Exception e)
                    {
                        assetBundleFeatState = AssetBundlesFeatureState.Ready;
                        downloadFailed(ListDownloadError.NotCointainedInRuntimeManifestOrOther, e.ToString(), new AutoyaStatus());
                        return;
                    }

                    downloadedListIdentities.Add(newList.identity);
                    if (downloadedListIdentities.Count == wholeAssetBundleListCount)
                    {
                        // set state to loaded.
                        assetBundleFeatState = AssetBundlesFeatureState.Ready;

                        // fire downloaded.
                        downloadSucceeded(ListDownloadResult.ListDownloaded);
                    }
                }
                else
                {
                    if (isDownloadFailed)
                    {
                        return;
                    }
                    isDownloadFailed = true;
                    downloadFailed(ListDownloadError.FailedToStoreDownloadedAssetBundleList, "failed to store assetBundleList info to device. downloaded list identity:" + newList.identity, new AutoyaStatus());
                }

            };


            foreach (var listUrl in listUrls)
            {

                /*
                    どれか一件でも失敗したら、リスト機構の初期化に失敗する。
                */
                Action<int, string, AutoyaStatus> failed = (code, reason, autoyaStatus) =>
                {
                    if (isDownloadFailed)
                    {
                        return;
                    }
                    isDownloadFailed = true;

                    assetBundleFeatState = AssetBundlesFeatureState.None;
                    downloadFailed(ListDownloadError.FailedToDownload, "code:" + code + " reason:" + reason + " url:" + listUrl, autoyaStatus);
                };

                // parallel.
                mainthreadDispatcher.Commit(
                    _assetBundleListDownloader.DownloadAssetBundleList(
                        listUrl,
                        succeeded,
                        failed,
                        timeoutSec
                    )
                );
            }
        }

        /**
            get copy of assetBundleList which is storead in this device.
         */
        public static AssetBundleList[] AssetBundle_AssetBundleLists()
        {
            return autoya.LoadAssetBundleListsFromStorage();
        }

        /**
            check if assetBundleList contains specific named asset.
         */
        public static bool AssetBundle_IsAssetExist(string assetName)
        {
            var cont = CheckAssetBundlesFeatureCondition(
                (code, reason) => { }
            );

            if (!cont)
            {
                return false;
            }

            if (autoya._assetBundleLoader.IsAssetExists(assetName))
            {
                return true;
            }
            return false;
        }

        /**
            check if assetBundleList contains specific named assetBundle.
         */
        public static bool AssetBundle_IsAssetBundleExist(string bundleName)
        {
            var cont = CheckAssetBundlesFeatureCondition(
                (code, reason) => { }
            );

            if (!cont)
            {
                return false;
            }

            if (autoya._assetBundleLoader.IsBundleExists(bundleName))
            {
                return true;
            }
            return false;
        }

        /**
            get total weight of specific AssetBundles.
         */
        public static long AssetBundle_GetAssetBundlesWeight(string[] bundleNames)
        {
            var cont = CheckAssetBundlesFeatureCondition(
                (code, reason) => { }
            );

            if (!cont)
            {
                return 0L;
            }

            return autoya._assetBundleLoader.GetAssetBundlesWeight(bundleNames);
        }

        /**
            get bundle names of "storage cached" assetBundle from assetBundleList. 
         */
        public static void AssetBundle_CachedBundleNames(Action<string[]> onBundleNamesReady, Action<AssetBundlesError, string> onError)
        {
            var cont = CheckAssetBundlesFeatureCondition(
                (code, reason) => onError(code, reason)
            );

            if (!cont)
            {
                return;
            }

            var cor = autoya.GetCachedAssetBundleNames(onBundleNamesReady);
            Autoya.Mainthread_Commit(cor);
        }

        private IEnumerator GetCachedAssetBundleNames(Action<string[]> onBundleNamesReady)
        {
            while (!Caching.ready)
            {
                yield return null;
            }

            var chachedBundleNames = new List<string>();

            var bundleNames = _assetBundleLoader.GetWholeBundleNames();
            foreach (var bundleName in bundleNames)
            {
                var url = _assetBundleLoader.GetAssetBundleDownloadUrl(bundleName);
                var bundleInfo = _assetBundleLoader.AssetBundleInfoFromBundleName(bundleName);
                var hash = Hash128.Parse(bundleInfo.hash);

                var isCachedOnStorage = Caching.IsVersionCached(url, hash);

                if (!isCachedOnStorage)
                {
                    continue;
                }

                chachedBundleNames.Add(bundleName);
            }

            onBundleNamesReady(chachedBundleNames.ToArray());
        }

        /**
            get bundle names of "not storage cached" assetBundle from assetBundleList.
         */
        public static void AssetBundle_NotCachedBundleNames(Action<string[]> onBundleNamesReady, Action<AssetBundlesError, string> onError)
        {
            var cont = CheckAssetBundlesFeatureCondition(
                (code, reason) => onError(code, reason)
            );

            if (!cont)
            {
                return;
            }

            var cor = autoya.GetNotCachedAssetBundleNames(onBundleNamesReady);
            Autoya.Mainthread_Commit(cor);
        }

        private IEnumerator GetNotCachedAssetBundleNames(Action<string[]> onBundleNamesReady)
        {
            while (!Caching.ready)
            {
                yield return null;
            }

            var notChachedBundleNames = new List<string>();

            var bundleNames = _assetBundleLoader.GetWholeBundleNames();
            foreach (var bundleName in bundleNames)
            {
                var url = _assetBundleLoader.GetAssetBundleDownloadUrl(bundleName);
                var bundleInfo = _assetBundleLoader.AssetBundleInfoFromBundleName(bundleName);
                var hash = Hash128.Parse(bundleInfo.hash);

                var isCachedOnStorage = Caching.IsVersionCached(url, hash);

                if (isCachedOnStorage)
                {
                    continue;
                }

                notChachedBundleNames.Add(bundleName);
            }

            onBundleNamesReady(notChachedBundleNames.ToArray());
        }


        /*
            Loader
        */
        private AssetBundleLoader _assetBundleLoader;

        public static void AssetBundle_LoadAsset<T>(
            string assetName,
            Action<string, T> loadSucceeded,
            Action<string, AssetBundleLoadError, string, AutoyaStatus> loadFailed
        ) where T : UnityEngine.Object
        {
            var cont = CheckAssetBundlesFeatureCondition(
                (code, reason) =>
                {
                    switch (code)
                    {
                        case AssetBundlesError.AutoyaNotReady:
                            {
                                loadFailed(assetName, AssetBundleLoadError.DownloadFailed, "code:" + code + " reason:" + reason, new AutoyaStatus());
                                break;
                            }
                        case AssetBundlesError.ListLoading:
                        case AssetBundlesError.NeedToDownloadAssetBundleList:
                            {
                                loadFailed(assetName, AssetBundleLoadError.AssetBundleListIsNotReady, "code:" + code + " reason:" + reason, new AutoyaStatus());
                                break;
                            }
                        default:
                            {
                                loadFailed(assetName, AssetBundleLoadError.Undefined, "code:" + code + " reason:" + reason, new AutoyaStatus());
                                break;
                            }
                    }
                }
            );

            if (!cont)
            {
                return;
            }

            autoya.mainthreadDispatcher.Commit(
                autoya._assetBundleLoader.LoadAsset(assetName, loadSucceeded, loadFailed)
            );
        }

        public static bool AssetBundle_UnloadOnMemoryAssetBundles()
        {
            var cont = CheckAssetBundlesFeatureCondition(
                (code, reason) =>
                {
                    // do nothing.
                }
            );

            if (!cont)
            {
                return false;
            }

            autoya._assetBundleLoader.UnloadOnMemoryAssetBundles();
            return true;
        }

        public static void AssetBundle_UnloadOnMemoryAssetBundle(string bundleName)
        {
            var cont = CheckAssetBundlesFeatureCondition(
                (code, reason) =>
                {
                    // do nothing.
                }
            );

            if (!cont)
            {
                return;
            }

            autoya._assetBundleLoader.UnloadOnMemoryAssetBundle(bundleName);
        }

        public static void AssetBundle_UnloadOnMemoryAsset(string assetName)
        {
            var cont = CheckAssetBundlesFeatureCondition(
                (code, reason) =>
                {
                    // do nothing.
                }
            );

            if (!cont)
            {
                return;
            }

            autoya._assetBundleLoader.UnloadOnMemoryAsset(assetName);
        }

        private void ReadyLoaderAndPreloader(AssetBundleList list)
        {
            // initialize/reload AssetBundleLoader.
            {
                if (_assetBundleLoader == null)
                {
                    AssetBundleLoader.HttpResponseHandlingDelegate httpResponseHandlingDel = (p1, p2, p3, p4, p5, p6, p7) =>
                    {
                        httpResponseHandlingDelegate(p1, p2, p3, p4, p5, p6, p7);
                    };

                    AssetBundleLoader.AssetBundleGetRequestHeaderDelegate assetBundleGetRequestHeaderDel = (p1, p2) =>
                    {
                        return assetBundleGetRequestHeaderDelegate(p1, p2);
                    };

                    _assetBundleLoader = new AssetBundleLoader(OnAssetBundleDownloadUrlRequired, assetBundleGetRequestHeaderDel, httpResponseHandlingDel);
                }

                _assetBundleLoader.UpdateAssetBundleList(list);
            }

            // initialize AssetBundlePreloader.
            {
                if (_assetBundlePreloader != null)
                {
                    // do nothing.
                }
                else
                {
                    AssetBundlePreloader.HttpResponseHandlingDelegate httpResponseHandlingDel = (p1, p2, p3, p4, p5, p6, p7) =>
                    {
                        httpResponseHandlingDelegate(p1, p2, p3, p4, p5, p6, p7);
                    };

                    AssetBundlePreloader.PreloadListGetRequestHeaderDelegate assetBundleGetRequestHeaderDel = (p1, p2) =>
                    {
                        return assetBundlePreloadListGetRequestHeaderDelegate(p1, p2);
                    };

                    _assetBundlePreloader = new AssetBundlePreloader(assetBundleGetRequestHeaderDel, httpResponseHandlingDel);
                }
            }
        }

        /*
            Preloader
        */
        private AssetBundlePreloader _assetBundlePreloader;

        /**
            download the list of prelaodable assetBundle names from preloadListUrl, then download assetBundles.
            this feature will download "not downloaded" assetBundles only.

            onBeforePreloading:
                you can set the Action to this param for getting "will be download assetBundles names".
                then if execute proceed(), download will be started. 
                else, execute cancel(), download will be cancelled.
         */
        public static void AssetBundle_Preload(string preloadListUrl, Action<string[], Action, Action> onBeforePreloading, Action<double> progress, Action done, Action<int, string, AutoyaStatus> preloadListDownloadFailed, Action<string, AssetBundleLoadError, string, AutoyaStatus> bundleDownloadFailed, int maxParallelCount, double timeoutSec = AssetBundlesSettings.TIMEOUT_SEC)
        {
            var cont = CheckAssetBundlesFeatureCondition(
                (code, reason) =>
                {
                    preloadListDownloadFailed(-(int)code, reason, new AutoyaStatus());
                }
            );

            if (!cont)
            {
                return;
            }

            var url = AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_PRELOADLIST + preloadListUrl;
            autoya.mainthreadDispatcher.Commit(
                autoya._assetBundlePreloader.Preload(
                    autoya._assetBundleLoader,
                    url,
                    onBeforePreloading,
                    progress,
                    done,
                    preloadListDownloadFailed,
                    bundleDownloadFailed,
                    maxParallelCount,
                    timeoutSec
                )
            );
        }

        /**
            download assetBundles by the preloadList, then download assetBundles.
            this feature will download "not downloaded" assetBundles only.

            onBeforePreloading:
                you can set the Action to this param for getting "will be download assetBundles names".

                then if execute proceed(), download will be started. 
                    if the preload target assetBundle is loaded on memory and changed in new version,
                    these assetBundles are unloaded automatically.

                else, execute cancel(), download will be cancelled.
         */
        public static void AssetBundle_PreloadByList(PreloadList preloadList, Action<string[], Action, Action> onBeforePreloading, Action<double> progress, Action done, Action<int, string, AutoyaStatus> preloadListDownloadFailed, Action<string, AssetBundleLoadError, string, AutoyaStatus> bundleDownloadFailed, int maxParallelCount, double timeoutSec = 0)
        {
            var cont = CheckAssetBundlesFeatureCondition(
                (code, reason) =>
                {
                    preloadListDownloadFailed((int)code, reason, new AutoyaStatus());
                }
            );

            if (!cont)
            {
                return;
            }

            autoya.mainthreadDispatcher.Commit(
                autoya._assetBundlePreloader.Preload(
                    autoya._assetBundleLoader,
                    preloadList,
                    onBeforePreloading,
                    progress,
                    done,
                    preloadListDownloadFailed,
                    bundleDownloadFailed,
                    maxParallelCount
                )
            );
        }

        public enum AssetBundlesError
        {
            AutoyaNotReady,
            NeedToDownloadAssetBundleList,
            ListLoading,
            FailedToDiscardList
        }

        private static bool CheckAssetBundlesFeatureCondition(Action<AssetBundlesError, string> failed)
        {
            if (autoya == null)
            {
                failed(AssetBundlesError.AutoyaNotReady, "Autoya not ready.");
                return false;
            }

            switch (autoya.assetBundleFeatState)
            {
                case AssetBundlesFeatureState.Ready:
                    {
                        // pass.
                        return true;
                    }
                case AssetBundlesFeatureState.None:
                    {
                        autoya.mainthreadDispatcher.Commit(
                            FailEnumerationEmitter(
                                () =>
                                {
                                    failed(AssetBundlesError.NeedToDownloadAssetBundleList, "need to download AssetBundleList first. use AssetBundle_DownloadAssetBundleListIfNeed().");
                                }
                            )
                        );
                        return false;
                    }
                case AssetBundlesFeatureState.ListDownoading:
                    {
                        autoya.mainthreadDispatcher.Commit(
                            FailEnumerationEmitter(
                                () =>
                                {
                                    failed(AssetBundlesError.ListLoading, "AssetBundleList is loading. please wait end.");
                                }
                            )
                        );
                        return false;
                    }
                default:
                    {
                        throw new Exception("unhandled feature state:" + autoya.assetBundleFeatState);
                    }
            }
        }

        private static IEnumerator FailEnumerationEmitter(Action failAct)
        {
            failAct();
            yield break;
        }

        /**
            download AssetBundleList from url manually.
         */
        public static void AssetBundle_DownloadAssetBundleListFromUrlManually(string listUrl, Action<ListDownloadResult> downloadSucceeded, Action<ListDownloadError, string, AutoyaStatus> downloadFailed, double timeoutSec = AssetBundlesSettings.TIMEOUT_SEC)
        {
            switch (autoya.assetBundleFeatState)
            {
                case AssetBundlesFeatureState.ListDownoading:
                    {
                        // already loading another AssetBundleList.
                        downloadFailed(ListDownloadError.AlreadyDownloading, "already downloading another AssetBundleList. please wait finish downloading.", new AutoyaStatus());
                        return;
                    }
                case AssetBundlesFeatureState.Ready:
                case AssetBundlesFeatureState.None:
                    {
                        // pass.
                        break;
                    }
                default:
                    {
                        downloadFailed(ListDownloadError.FailedToDownload, "unexpected state:" + autoya.assetBundleFeatState, new AutoyaStatus());
                        return;
                    }
            }

            autoya.Internal_AssetBundle_DownloadAssetBundleListFromUrl(new string[] { listUrl }, downloadSucceeded, downloadFailed, timeoutSec);
        }


        /**
            factory reset.
         */
        public static void AssetBundle_FactoryReset(Action succeeded, Action<FactoryResetError, string> failed)
        {
            var unloaded = Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
            if (!unloaded)
            {
                failed(FactoryResetError.UnloadAssetBundles_Failed, "failed to unload AssetBundles on memory.");
                return;
            }

            var discarded = Autoya.AssetBundle_DeleteAllStorageCache();
            if (!discarded)
            {
                failed(FactoryResetError.DeleteAllStorageCache_Failed, "failed to delete cached AssetBundles.");
                return;
            }

            Autoya.AssetBundle_DiscardAssetBundleList(
                () =>
                {
                    succeeded();
                },
                (err, reason) =>
                {
                    failed(FactoryResetError.DiscardAssetBundleList_Failed, "failed to delete AssetBundleList.");
                }
            );
        }

        public enum FactoryResetError
        {
            UnloadAssetBundles_Failed,
            DeleteAllStorageCache_Failed,
            DiscardAssetBundleList_Failed,

        }

        /*
            debug
         */

        public static AssetBundlesFeatureState Debug_AssetBundle_FeatureState()
        {
            return autoya.assetBundleFeatState;
        }

        public static void Debug_SetOverridePoint_ShouldRequestNewAssetBundleList(Func<string, string, ShouldRequestOrNot> debugAct)
        {
            autoya.OnRequestNewAssetBundleList = (identity, currentVersion) =>
            {
                return debugAct(identity, currentVersion);
            };
        }

        public static void Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList(Action<CurrentUsingBundleCondition, Action, Action> debugAct)
        {
            autoya.ShouldUpdateToNewAssetBundleList = (condition, proceed, cancel) =>
            {
                debugAct(condition, proceed, cancel);
            };
        }

        public static void Debug_SetOnOverridePoint_OnAssetBundleListUpdated(Action<CurrentUsingBundleCondition, Action> debugAct)
        {
            autoya.OnAssetBundleListUpdated = (condition, ready) =>
            {
                debugAct(condition, ready);
            };
        }

        public static void Debug_SetOverridePoint_OnNewAppRequested(Action<string> onNewAppDetected)
        {
            autoya.OnNewAppRequested = onNewAppDetected;
        }
    }
}