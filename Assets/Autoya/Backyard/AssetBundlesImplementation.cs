using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Settings.AssetBundles;
using UnityEngine;

namespace AutoyaFramework {
	public partial class Autoya {
		
		/*
			Initializer
		 */
		private void InitializeAssetBundleFeature () {
			// check if assetBundleList are stored.
			var listCandidate = LoadAssetBundleListFromStorage();

			if (listCandidate == null) {
				// no list stored.
				assetBundleFeatState = AssetBundlesFeatureState.None;
				return;
			}

			_currentAssetBundleList = listCandidate;
			assetBundleFeatState = AssetBundlesFeatureState.ListLoaded;
		}

		public static bool AssetBundle_IsAssetBundleListReady () {
			switch (autoya.assetBundleFeatState) {
				case AssetBundlesFeatureState.ListLoaded:
				case AssetBundlesFeatureState.LoaderReady: {
					return true;
				}
				default: {
					return false;
				}
			}
		}

		private static string AssetBundle_GetAssetBundleListVersionedBasePath (string basePath) {
			if (autoya._currentAssetBundleList != null) {
				return basePath + autoya._currentAssetBundleList.version + "/";
			}

			return "no list exists yet. run AssetBundle_DownloadAssetBundleList() first.";
		}

		public static bool AssetBundle_DiscardAssetBundleList () {
			return autoya.DiscardAssetBundleList();
		}

		
		public static void AssetBundle_DeleteAllStorageCache (Action<bool, string> result, bool runAnyway=false) {
			if (runAnyway) {
				var isCacheDeleted = Caching.CleanCache();
				if (isCacheDeleted) {
					result(true, "succeeded to delete all assetBundles in storage.");
					return;
				}

				// failed.
				result(false, "failed to delete all assetBundles in storage. maybe this is not mainthread or some assetBundles are in use. or Caching feature is not ready.");
				return;
			}
			
			autoya.mainthreadDispatcher.Commit(autoya.CleanCacheEnumRunner(result));
		}
		private IEnumerator CleanCacheEnumRunner (Action<bool, string> result) {
			while (!Caching.ready) {
				yield return null;
			}

			var isDeleted = Caching.CleanCache();

			if (isDeleted) {
				result(true, "succeeded to delete all assetBundles in storage.");
				yield break;
			}

			// failed.
			result(false, "failed to delete all assetBundles in storage. some assetBundles are in use or Caching feature is not ready.");
		}
		
		private enum AssetBundlesFeatureState {
			None,
			ListLoading,
			ListLoaded,
			LoaderReady,
		}
		private AssetBundlesFeatureState assetBundleFeatState;


		private AssetBundleList _currentAssetBundleList;

		/*
			Downloader
		*/

		private AssetBundleListDownloader _assetBundleListDownloader = new AssetBundleListDownloader();
		/**
			Download assetBundleList from AssetBundleSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST/version/fileName path.
		 */
		public static void AssetBundle_DownloadAssetBundleList (string fileName, string version, Action downloadSucceeded, Action<int, string, AutoyaStatus> downloadFailed, double timeoutSec=0) {
			var listUrl = AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST + version + "/" + fileName;
			AssetBundle_DownloadAssetBundleList(listUrl, downloadSucceeded, downloadFailed, timeoutSec);
		}

		/**
			Download assetBundle from url.
		 */
		public static void AssetBundle_DownloadAssetBundleList (string listUrl, Action downloadSucceeded, Action<int, string, AutoyaStatus> downloadFailed, double timeoutSec=0) {
			Action act = () => {	
				autoya.mainthreadDispatcher.Commit(
					autoya._assetBundleListDownloader.DownloadAssetBundleList(
						listUrl, 
						list => {
							var result = autoya.StoreAssetBundleListToStorage(list);
							if (result) {
								autoya.assetBundleFeatState = AssetBundlesFeatureState.ListLoaded;
								autoya._currentAssetBundleList = list;
								downloadSucceeded();
								return;
							}

							// failed to store assetBundleList.
							autoya.assetBundleFeatState = AssetBundlesFeatureState.None;
							downloadFailed(-1, "failed to store to storage. see StoreAssetBundleListToStorage method.", new AutoyaStatus());
						}, 
						(code, reason, autoyaStatus) => {
							autoya.assetBundleFeatState = AssetBundlesFeatureState.None;
							downloadFailed(code, reason, autoyaStatus);
						},
						timeoutSec
					)
				);
			};

			autoya.mainthreadDispatcher.Commit(
				autoya.ListLoaderCoroutine(act, downloadFailed)
			);
		}

		/**
			get copy of assetBundleList which is storead in this device.
		 */
		public static AssetBundleList AssetBundle_AssetBundleList () {
			return autoya.LoadAssetBundleListFromStorage();
		}


		/**
			get total weight of specific AssetBundles.
		 */
		public static long AssetBundle_GetAssetBundlesWeight (string[] bundleNames) {
			var list = AssetBundle_AssetBundleList();
			if (list != null) {
				return list.assetBundles.Where(bundleInfo => bundleNames.Contains(bundleInfo.bundleName)).Sum(b => b.size);
			}
			return 0;
		}



		/**
			get bundle names of "not storage cached" assetBundle from assetBundleList.
		 */
		public static void AssetBundle_NotCachedBundleNames (Action<string[]> onBundleNamesReady) {
			var cor = GetNotCachedAssetBundleNames(onBundleNamesReady);
			Autoya.Mainthread_Commit(cor);
		}

		private static IEnumerator GetNotCachedAssetBundleNames (Action<string[]> onBundleNamesReady) {
			while (!Caching.ready) {
				yield return null;
			}

			var bundleNames = new List<string>();
			var assetBundleList = AssetBundle_AssetBundleList();
			foreach (var bundleInfo in assetBundleList.assetBundles) {
				var bundleName = bundleInfo.bundleName;
				var url = Autoya.AssetBundle_GetAssetBundleListVersionedBasePath(AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSET) + bundleName;
				var hash = Hash128.Parse(bundleInfo.hash);

				var isCachedOnStorage = Caching.IsVersionCached(url, hash);

				if (isCachedOnStorage) {
					continue;
				}

				bundleNames.Add(bundleName);
			}

			onBundleNamesReady(bundleNames.ToArray());
		}


		private IEnumerator ListLoaderCoroutine (Action execute, Action<int, string, AutoyaStatus> downloadFailed) {
			// check current state.
			switch (assetBundleFeatState) {
				case AssetBundlesFeatureState.ListLoading: {
					downloadFailed(-2, "already downloading AssetBundleList.", new AutoyaStatus());
					yield break;
				}
				case AssetBundlesFeatureState.None:
				case AssetBundlesFeatureState.ListLoaded: {
					// pass. start loading/reloading list.
					break;
				}
				case AssetBundlesFeatureState.LoaderReady: {
					// すでにloaderReadyなloaderがあるんで、listLoaderに対しての処理は制限されていないといけない。
					// そのへんのチェックをすべきなのかな、、
					Debug.LogWarning("考え中の、loaderやpreloaderが元気でリストを再取得したいケース。loaderの状態によってやるべきことが変わる。");

					if (true) {
						downloadFailed(-3, "downloading AssetBundles with AssetBundleLoader.", new AutoyaStatus());
						yield break;
					}

					// pass. start loading/reloading list.
					break;
				}
				default: {
					downloadFailed(-4, "unexpected state. state:" + assetBundleFeatState, new AutoyaStatus());
					yield break;
				}
			}

			// start loading.
			assetBundleFeatState = AssetBundlesFeatureState.ListLoading;
			execute();
		}

		private bool DiscardAssetBundleList () {
			switch (assetBundleFeatState) {
				case AssetBundlesFeatureState.None: {
					// no list exists.
					break;
				}
				case AssetBundlesFeatureState.ListLoading: {
					// now loading. not exist yet.
					break;
				}
				case AssetBundlesFeatureState.ListLoaded: {
					// delete assetBundleList manually.
					DeleteAssetBundleListFromStorage();
					assetBundleFeatState = AssetBundlesFeatureState.None;
					return true;
				}
				case AssetBundlesFeatureState.LoaderReady: {
					Debug.LogWarning("状態によって変わる。loaderやpreloaderが頑張ってるならダメ。Preloaderは関係あるんだろうか。とりあえず消す。");
					DeleteAssetBundleListFromStorage();
					assetBundleFeatState = AssetBundlesFeatureState.None;
					break;
				}
			}
			return false;
		}



		/*
			Loader
		*/
		private AssetBundleLoader _assetBundleLoader;
		
		public static void AssetBundle_LoadAsset<T> (
			string assetName, 
			Action<string, T> loadSucceeded, 
			Action<string, AssetBundleLoadError, 
			string, AutoyaStatus> loadFailed
		) where T : UnityEngine.Object {
			Action act = () => {
				autoya.mainthreadDispatcher.Commit(
					autoya._assetBundleLoader.LoadAsset(assetName, loadSucceeded, loadFailed)
				);
			};
			autoya.mainthreadDispatcher.Commit(
				autoya.BundleLoaderCoroutine(
					act, 
					(err, reason, autoyaStatus) => {
						loadFailed(assetName, err, reason, autoyaStatus);
					}
				)
			);
		}

        public static void AssetBundle_UnloadOnMemoryAssetBundles () {
			Action act = () => {
				autoya._assetBundleLoader.UnloadOnMemoryAssetBundles();
			};

			autoya.BundleLoaderExecute(act);
		}

		public static void AssetBundle_UnloadOnMemoryAssetBundle (string bundleName) {
			Action act = () => {
				autoya._assetBundleLoader.UnloadOnMemoryAssetBundle(bundleName);
			};

			autoya.BundleLoaderExecute(act);
		}
		
		public static void AssetBundle_UnloadOnMemoryAsset (string assetName) {
			Action act = () => {
				autoya._assetBundleLoader.UnloadOnMemoryAsset(assetName);
			};

			autoya.BundleLoaderExecute(act);
		}

		private IEnumerator BundleLoaderCoroutine (Action execute, Action<AssetBundleLoadError, string, AutoyaStatus> failed) {
			switch (assetBundleFeatState) {
				case AssetBundlesFeatureState.None: {
					yield return null;
					failed(AssetBundleLoadError.AssetBundleListIsNotReady, "please run AssetBundle_DownloadAssetBundleList first.", new AutoyaStatus());
					yield break;
				}
				case AssetBundlesFeatureState.ListLoading: {
					yield return null;
					failed(AssetBundleLoadError.AssetBundleListIsNotReady, "assetBundleList is now downloading.", new AutoyaStatus());
					yield break;
				}
				case AssetBundlesFeatureState.ListLoaded: {
					ReadyLoaderAndPreloader();
					break;
				}
				case AssetBundlesFeatureState.LoaderReady: {
					break;
				}
			}
			
			execute();
		}

		private void ReadyLoaderAndPreloader () {
			// new or renew AssetBundleLoader.
			{
				AssetBundleLoader.HttpResponseHandlingDelegate httpResponseHandlingDel = (p1, p2, p3, p4, p5, p6, p7) => {
					httpResponseHandlingDelegate(p1, p2, p3, p4, p5, p6, p7);
				};
				AssetBundleLoader.AssetBundleGetRequestHeaderDelegate assetBundleGetRequestHeaderDel = (p1, p2) => {
					return assetBundleGetRequestHeaderDelegate(p1, p2);
				};

				_assetBundleLoader = new AssetBundleLoader(Autoya.AssetBundle_GetAssetBundleListVersionedBasePath(AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSET), _currentAssetBundleList, assetBundleGetRequestHeaderDel, httpResponseHandlingDel);
			}

			// new or renew AssetBundlePreloader.
			{
				AssetBundlePreloader.HttpResponseHandlingDelegate httpResponseHandlingDel = (p1, p2, p3, p4, p5, p6, p7) => {
					httpResponseHandlingDelegate(p1, p2, p3, p4, p5, p6, p7);
				};
				AssetBundlePreloader.AssetBundleGetRequestHeaderDelegate assetBundleGetRequestHeaderDel = (p1, p2) => {
					return assetBundleGetRequestHeaderDelegate(p1, p2);
				};

				_assetBundlePreloader = new AssetBundlePreloader(assetBundleGetRequestHeaderDel, httpResponseHandlingDel);
			}

			// set to load ready.
			assetBundleFeatState = AssetBundlesFeatureState.LoaderReady;
		}

		private void BundleLoaderExecute (Action execute) {
			switch (assetBundleFeatState) {
				case AssetBundlesFeatureState.None: {
					return;
				}
				case AssetBundlesFeatureState.ListLoading: {
					return;
				}
				case AssetBundlesFeatureState.ListLoaded: {
					ReadyLoaderAndPreloader();
					break;
				}
				case AssetBundlesFeatureState.LoaderReady: {
					break;
				}
			}

			execute();
		}


		/*
			Preloader
		*/
		private AssetBundlePreloader _assetBundlePreloader;
		public static void AssetBundle_Preload (string preloadListUrl, Func<string[], IEnumerator<bool>> shouldContinuePreloading, Action<double> progress, Action done, Action<int, string, AutoyaStatus> listDownloadFailed, Action<string, int, string, AutoyaStatus> bundleDownloadFailed, int maxParallelCount, double timeoutSec=0) {

			Action<AssetBundleLoader> act = loader => {
				var url = AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_PRELOADLIST + preloadListUrl;
				autoya.mainthreadDispatcher.Commit(
					autoya._assetBundlePreloader.Preload(
						loader,
						url, 
						shouldContinuePreloading,
						progress,
						done,
						listDownloadFailed,
						bundleDownloadFailed,
						maxParallelCount,
						timeoutSec
					)
				);
			};

			autoya.mainthreadDispatcher.Commit(
				autoya.PreloaderCoroutine(act, listDownloadFailed)
			);
		}

		public static void AssetBundle_Preload (PreloadList preloadList, Func<string[], IEnumerator<bool>> shouldContinePreloading, Action<double> progress, Action done, Action<int, string, AutoyaStatus> listDownloadFailed, Action<string, int, string, AutoyaStatus> bundleDownloadFailed, int maxParallelCount, double timeoutSec=0) {

			Action<AssetBundleLoader> act = loader => {
				autoya.mainthreadDispatcher.Commit(
					autoya._assetBundlePreloader.Preload(
						loader,
						preloadList,
						shouldContinePreloading,
						progress,
						done,
						listDownloadFailed,
						bundleDownloadFailed,
						maxParallelCount
					)
				);
			};

			autoya.mainthreadDispatcher.Commit(
				autoya.PreloaderCoroutine(act, listDownloadFailed)
			);
		}

		private IEnumerator PreloaderCoroutine (Action<AssetBundleLoader> execute, Action<int, string, AutoyaStatus> listDownloadFailed) {
			switch (assetBundleFeatState) {
				case AssetBundlesFeatureState.None: {
					yield return null;
					listDownloadFailed(-1, "please run AssetBundle_DownloadAssetBundleList first.", new AutoyaStatus());
					yield break;
				}
				case AssetBundlesFeatureState.ListLoading: {
					yield return null;
					listDownloadFailed(-2, "assetBundleList is now downloading.", new AutoyaStatus());
					yield break;
				}
				case AssetBundlesFeatureState.ListLoaded: {
					ReadyLoaderAndPreloader();
					break;
				}
				case AssetBundlesFeatureState.LoaderReady: {
					break;
				}
			}
			
			execute(_assetBundleLoader);
		}
	}
}