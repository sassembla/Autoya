using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Settings.AssetBundles;
using UnityEngine;

namespace AutoyaFramework {
	/**
		assetBundles implementation.

		リスト取得 -> とかそんな感じに動く必要がある。
		リスト取得が終わらないとloadAssetとかを行なってはいけない。

		・リストを取得しないとNone状態
			none状態だとリスト取得を求められる(勝手にリストを取得するのをやめる)
	 */
	public partial class Autoya {
		public enum AssetBundlesFeatureState {
			None,
			ListLoading,
			ListLoaded,
			LoaderReady,
		}
		private AssetBundlesFeatureState assetBundleFeatState;



		public enum CurrentUsingBundleCondition {
			ReceivedFirstAssetBundleList,// このenumがあるのまずい。初回 = リストがない時にしか使えないAPIとしてAPIを分割して処理すべきだ。

			UsingAssetsAreChanged,

			NoUsingAssetsChanged,
			AlreadyUpdated
		}
		
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
		
		
		private AssetBundleList _currentAssetBundleList;

		/*
			Downloader
		*/

		private AssetBundleListDownloader _assetBundleListDownloader = new AssetBundleListDownloader();
		private AssetBundleList _postponedNewAssetBundleList;

		private CurrentUsingBundleCondition GetCurrentAssetBundleUsingCondition (AssetBundleList newList) {
			
			// check version of new list and current stored list.

			var currentListVersion = _currentAssetBundleList.version;
			var newListVersion = newList.version;
			
			if (currentListVersion != newListVersion) {
				// check using assets are changed or not.

				var newBundleCrcs = newList.assetBundles.ToDictionary(bundle => bundle.bundleName, bundle => bundle.crc);
				var oldBundleCrcs = _currentAssetBundleList.assetBundles.ToDictionary(bundle => bundle.bundleName, bundle => bundle.crc);

				var changedUsingBundleNames = new List<string>();
				foreach (var oldBundleCrcItem in oldBundleCrcs) {
					var bundleName = oldBundleCrcItem.Key;
					var bundleCrc = oldBundleCrcItem.Value;

					if (newBundleCrcs.ContainsKey(bundleName)) {
						if (newBundleCrcs[bundleName] != bundleCrc) {
							// crc changed = assetBundle is updated.

							Debug.Log("_assetBundleLoader:" + _assetBundleLoader);
							// is using now?
							if (_assetBundleLoader.IsAssetBundleCachedOnMemory(bundleName)) {
								/*
									changed assetBundle is using now.
								 */
								changedUsingBundleNames.Add(bundleName);
							}
						}
					} else {
						// in new list, current using assetBundle is not exists.
						// nothing to do. but detected.
					}
				}

				if (changedUsingBundleNames.Any()) {
					// using assetBundle is updated in new list.
					return CurrentUsingBundleCondition.UsingAssetsAreChanged;
				} else {
					// no using && change of assetBundles are detected.
					return CurrentUsingBundleCondition.NoUsingAssetsChanged;
				}
			}

			// list version is not changed. 

			return CurrentUsingBundleCondition.AlreadyUpdated;
		}

		private void OnListReceived (AssetBundleList newList, Action downloadSucceeded, Action<int, string, AutoyaStatus> downloadFailed) {
			/*
				このブロックは、開発者が明示的にlistのロードを行なった場合に通過する。
				こことは別に、responseHeaderの値を元に自動的にリストの再取得に突入するルートがあるといいかもしれない。
				ユーザーが使うブロックとAPIが使うブロックを分けた方がよい。

				このへんのブロックはすべて新規のほうに移動しちゃって、このAPI自体は初回DL限定とかの用途に区切った方がいいのかな、、
				そのほうがメンテしやすそう。
				*/
			CurrentUsingBundleCondition assetUsingCondition;

			switch (autoya.assetBundleFeatState) {
				case AssetBundlesFeatureState.ListLoading: {
					/*
						この判断マズイのでやめよう。
						このAPIではリストが無いときしか処理を進めない方がいい。
						つまり初回onlyしか動作しないというか、その経路でしか動作しないようにしたほうがあとあと良い。
						*/
					assetUsingCondition = CurrentUsingBundleCondition.ReceivedFirstAssetBundleList;
					break;
				}
				default: {
					// not downloading. means developer's manual update of list.

					assetUsingCondition = autoya.GetCurrentAssetBundleUsingCondition(newList);
					break;
				}
			}

			Debug.Log("ここまで来てる");
			if (autoya.ShouldUpdateToNewAssetBundleList(assetUsingCondition)) {
				var result = autoya.StoreAssetBundleListToStorage(newList);
				if (result) {
					// update runtime manifest. set "resVersion" to downloaded version.
					{
						var runtimeManifest = Autoya.Manifest_LoadRuntimeManifest();
						runtimeManifest.resVersion = newList.version;
						Autoya.Manifest_UpdateRuntimeManifest(runtimeManifest);
					}

					// set state to loaded.
					autoya.assetBundleFeatState = AssetBundlesFeatureState.ListLoaded;
					autoya._currentAssetBundleList = newList;

					// discard postponed cache.
					autoya._postponedNewAssetBundleList = null;

					downloadSucceeded();
					return;
				}

				// failed to store assetBundleList.
				autoya.assetBundleFeatState = AssetBundlesFeatureState.None;
				downloadFailed(-1, "failed to store to storage. see StoreAssetBundleListToStorage method.", new AutoyaStatus());
				return;
			}

			// store on memory as postponed.
			autoya._postponedNewAssetBundleList = newList;

			Debug.LogWarning("初回postponeとかできないはずなのでやはり分解が必要。");
			autoya.assetBundleFeatState = AssetBundlesFeatureState.ListLoaded;

			// list is not updated actually. nothing to do.
		}
		

		/*
			public apis
		 */
		
		public static AssetBundlesFeatureState AssetBundle_FeatureState () {
			return autoya.assetBundleFeatState;
		}


		/**
			Download specific versioned assetBundleList from AssetBundleSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST/version/fileName path.
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
						newList => {
							Debug.LogWarning("ここ、first専用に用意すべき。このAPIはリストが一切無い場合以外に動くべきでは無い。");
							autoya.OnListReceived(newList, downloadSucceeded, downloadFailed);
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
			returns postponed assetBundleList instance if exist.
			otherwise return empty assetBundleList.
		 */
		public static AssetBundleList AssetBundle_PostponedNewAssetBundleList () {
			if (autoya._postponedNewAssetBundleList != null) {
				return autoya._postponedNewAssetBundleList;
			}
			return new AssetBundleList();
		}

		/**
			check if assetBundleList contains specific named asset.
		 */
		public static bool AssetBundle_IsAssetExist (string assetName) {
			var list = autoya.LoadAssetBundleListFromStorage();
			if (list.assetBundles.Select(b => b.assetNames).Where(bundledAssetNames => bundledAssetNames.Contains(assetName)).Any()) {
				return true;
			}
			return false;
		}

		/**
			check if assetBundleList contains specific named assetBundle.
		 */
		public static bool AssetBundle_IsAssetBundleExist (string bundleName) {
			var list = autoya.LoadAssetBundleListFromStorage();
			if (list.assetBundles.Where(b => b.bundleName == bundleName).Any()) {
				return true;
			}
			return false;
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
			throw new Exception("うーむ、全体的に書き換えないとダメだ。");

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

        /**
			download the list of prelaodable assetBundle names from preloadListUrl, then download assetBundles.
			this feature will download "not downloaded" assetBundles only.

			shouldContinuePreloading:
				you can set the func to this param for getting "will be download assetBundles names".
				then if "yield return true", download will progress. 
				else, download will be stopped.
		 */
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
		
		/**
			download assetBundles by the preloadList, then download assetBundles.
			this feature will download "not downloaded" assetBundles only.

			shouldContinuePreloading:
				you can set the func to this param for getting "will be download assetBundles names".
				then if "yield return true", download will progress. 
				else, download will be stopped.
		 */
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

		public static void Debug_SetOverridePoint_ShouldRequestNewAssetBundleList (Func<string, ShouldRequestOrNot> debugAct) {
			autoya.OnRequestNewAssetBundleList = (currentVersion) => {
				return debugAct(currentVersion);
			};
		}
		public static void Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList (Func<CurrentUsingBundleCondition, bool>debugAct) {
			autoya.ShouldUpdateToNewAssetBundleList = condition => {
				return debugAct(condition);
			};
		}
	}
}