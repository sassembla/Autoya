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


	 */
	public partial class Autoya {
		public enum AssetBundlesFeatureState {
			None,
			ListLoading,
			Ready,
		}
		private AssetBundlesFeatureState assetBundleFeatState;



		public enum CurrentUsingBundleCondition {
			UsingAssetsAreChanged,

			NoUsingAssetsChanged,

			AlreadyUpdated
		}
		

		/*
			Initializer
		 */
		private void InitializeAssetBundleFeature () {
			// initialize AssetBundleListDownloader.
			AssetBundleListDownloader.HttpResponseHandlingDelegate httpResponseHandlingDel = (p1, p2, p3, p4, p5, p6, p7) => {
				httpResponseHandlingDelegate(p1, p2, p3, p4, p5, p6, p7);
			};
			AssetBundleListDownloader.AssetBundleListGetRequestHeaderDelegate assetBundleGetRequestHeaderDel = (p1, p2) => {
				return assetBundleListGetRequestHeaderDelegate(p1, p2);
			};

			_assetBundleListDownloader = new AssetBundleListDownloader(assetBundleGetRequestHeaderDel, httpResponseHandlingDel);
			 
			// check if assetBundleList are stored.
			var listCandidate = LoadAssetBundleListFromStorage();

			if (!listCandidate.Exists()) {
				// no list stored.
				assetBundleFeatState = AssetBundlesFeatureState.None;
				return;
			}

			_currentAssetBundleList = listCandidate;
			assetBundleFeatState = AssetBundlesFeatureState.Ready;
			ReadyLoaderAndPreloader();
		}

		private string GetAssetBundleListVersionedBasePath (string basePath) {
			switch (assetBundleFeatState) {
				case AssetBundlesFeatureState.Ready: {
					return basePath + _currentAssetBundleList.version + "/";
				}
				default: {
					return string.Empty;
				}
			}
		}


		public static bool AssetBundle_IsAssetBundleFeatureReady () {
			if (autoya == null) {
				return false;
			}
			
			switch (autoya.assetBundleFeatState) {
				case AssetBundlesFeatureState.Ready: {
					return true;
				}
				default: {
					return false;
				}
			}
		}

		public static void AssetBundle_DiscardAssetBundleList (Action onDiscarded, Action<int, string> onDiscardFailed) {
			var cont = CheckAssetBundlesFeatureCondition(
				(code, reason) => {
					onDiscardFailed(-(int)code, reason);
				}
			);

			if (!cont) {
				return;
			}

			// delete assetBundleList manually.
			var deleted = autoya.DeleteAssetBundleListFromStorage();
			if (!deleted) {
				onDiscardFailed(-2, "failed to discard list data.");
				return;
			}

			// ここで、manifestのデータは消さない。その代わり、リストは削除されているので、リストの取得をしようとする試みが成功しないといけない。

			autoya.assetBundleFeatState = AssetBundlesFeatureState.None;

			onDiscarded();
		}

		public static bool AssetBundle_DeleteAllStorageCache () {
			var cont = CheckAssetBundlesFeatureCondition(
				(code, reason) => {
					// do nothing.
				}
			);

			if (!cont) {
				return false;
			}

			return Caching.CleanCache();
		}
		
		
		private AssetBundleList _currentAssetBundleList;

		/*
			Downloader
		*/
		private AssetBundleListDownloader _assetBundleListDownloader;
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

		private enum NewListDownloaderState {
			Ready,
			Downloading,
		}

		private NewListDownloaderState newListDownloaderState = NewListDownloaderState.Ready;

		private void DownloadNewAssetBundleList (string url) {
			newListDownloaderState = NewListDownloaderState.Downloading;
			mainthreadDispatcher.Commit(
				autoya._assetBundleListDownloader.DownloadAssetBundleList(
					url, 
					newList => {
						// got new list.
						OnUpdatingListReceived(newList);
					}, 
					(code, reason, autoyaStatus) => {
						// do nothing.
					}
				)
			);
		}

		private void OnUpdatingListReceived (AssetBundleList newList) {
			var assetUsingCondition = GetCurrentAssetBundleUsingCondition(newList);

			if (ShouldUpdateToNewAssetBundleList(assetUsingCondition)) {
				var result = StoreAssetBundleListToStorage(newList);
				if (result) {
					// update runtime manifest. set "resVersion" to downloaded version.
					{
						var runtimeManifest = Autoya.Manifest_LoadRuntimeManifest();
						runtimeManifest.resVersion = newList.version;
						Autoya.Manifest_UpdateRuntimeManifest(runtimeManifest);
					}

					_currentAssetBundleList = newList;

					// discard postponed cache.
					_postponedNewAssetBundleList = null;

					// finish downloading new assetBundleList.
					newListDownloaderState = NewListDownloaderState.Ready;
					return;
				}

				// failed to store new assetBundleList.

			}

			// store on memory as postponed.
			// list is not updated actually.
			_postponedNewAssetBundleList = newList;

			// finish downloading new assetBundleList.
			newListDownloaderState = NewListDownloaderState.Ready;
			return;
		}
		
		public enum ListDownloadResult {
			AlreadyDownloaded,
			ListDownloaded
		}

		public enum ListDownloadError {
			AutoyaNotReady,
			AlreadyDownloading,
			FailedToDownload,
			FailedToStoreDownloadedAssetBundleList
		}
		
		public static string AssetBundle_GetCurrentAssetBundleListUrl () {
			if (autoya == null) {
				return string.Empty;
			}

			return autoya.AssetBundleListDownloadUrl();
		}
		
		/**
			Download assetBundleList from OverridePoints url if need.
		 */
		public static void AssetBundle_DownloadAssetBundleListIfNeed (Action<ListDownloadResult> downloadSucceeded, Action<ListDownloadError, string, AutoyaStatus> downloadFailed, double timeoutSec=AssetBundlesSettings.TIMEOUT_SEC) {
			if (autoya == null) {
				downloadFailed(ListDownloadError.AutoyaNotReady, "Autoya not ready.", new AutoyaStatus());
				return;
			}

			// 起動時、リストを保持してたら自動的にReadyになる。
			// 起動時、リストを保持していなかった場合、このメソッドからDLしてReadyモードになる。
			// 取得中にこのメソッドを連打してもfailになる(最初のやつがDLを継続する)

			// 起動時、リストを保持していなかった場合、リクエストヘッダにはデフォルトのresVersionが入る
			// サーバがresVersionレスポンスヘッダを返す + そのバージョンがデフォルトと異なった場合、リストDLが自動的に開始される。

			switch (autoya.assetBundleFeatState) {
				case AssetBundlesFeatureState.ListLoading: {
					// already loading.
					downloadFailed(ListDownloadError.AlreadyDownloading, "already loading.", new AutoyaStatus());
					return;
				}
				case AssetBundlesFeatureState.Ready: {
					downloadSucceeded(ListDownloadResult.AlreadyDownloaded);
					return;
				}
				case AssetBundlesFeatureState.None: {
					// pass.
					break;
				}
				default: {
					downloadFailed(ListDownloadError.FailedToDownload, "unexpected state:" + autoya.assetBundleFeatState, new AutoyaStatus());
					return;
				}
			}
			
			/*
				assetBundleFeatState is None.
			 */
			
			var listUrl = autoya.AssetBundleListDownloadUrl();
			Internal_AssetBundle_DownloadAssetBundleListFromUrl(listUrl, downloadSucceeded, downloadFailed, timeoutSec);
		}

		
		private static void Internal_AssetBundle_DownloadAssetBundleListFromUrl (string listUrl, Action<ListDownloadResult> downloadSucceeded, Action<ListDownloadError, string, AutoyaStatus> downloadFailed, double timeoutSec=AssetBundlesSettings.TIMEOUT_SEC) {
			Action act = () => {	
				autoya.mainthreadDispatcher.Commit(
					autoya._assetBundleListDownloader.DownloadAssetBundleList(
						listUrl, 
						newList => {
							var result = autoya.StoreAssetBundleListToStorage(newList);
							if (result) {
								// update runtime manifest. set "resVersion" to downloaded version.
								{
									var runtimeManifest = Autoya.Manifest_LoadRuntimeManifest();
									runtimeManifest.resVersion = newList.version;
									Autoya.Manifest_UpdateRuntimeManifest(runtimeManifest);
								}

								autoya._currentAssetBundleList = newList;

								// set state to loaded.
								autoya.assetBundleFeatState = AssetBundlesFeatureState.Ready;
								autoya.ReadyLoaderAndPreloader();

								downloadSucceeded(ListDownloadResult.ListDownloaded);
								return;
							}

							// failed to store assetBundleList.
							autoya.assetBundleFeatState = AssetBundlesFeatureState.None;
							downloadFailed(ListDownloadError.FailedToStoreDownloadedAssetBundleList, "failed to store AssetBundleList to storage. let's check StoreAssetBundleListToStorage method.", new AutoyaStatus());
						}, 
						(code, reason, autoyaStatus) => {
							autoya.assetBundleFeatState = AssetBundlesFeatureState.None;
							downloadFailed(ListDownloadError.FailedToDownload, "code:" + code + " reason]" + reason, autoyaStatus);
						},
						timeoutSec
					)
				);
			};

			autoya.mainthreadDispatcher.Commit(
				autoya.ListLoaderCoroutine(act)
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
			var cont = CheckAssetBundlesFeatureCondition(
				(code, reason) => {}
			);

			if (!cont) {
				return new AssetBundleList();
			}
			
			if (autoya._postponedNewAssetBundleList != null) {
				return autoya._postponedNewAssetBundleList;
			}
			return new AssetBundleList();
		}

		/**
			check if assetBundleList contains specific named asset.
		 */
		public static bool AssetBundle_IsAssetExist (string assetName) {
			var cont = CheckAssetBundlesFeatureCondition(
				(code, reason) => {}
			);

			if (!cont) {
				return false;
			}

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
			var cont = CheckAssetBundlesFeatureCondition(
				(code, reason) => {}
			);

			if (!cont) {
				return false;
			}

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
			var cont = CheckAssetBundlesFeatureCondition(
				(code, reason) => {}
			);

			if (!cont) {
				return 0L;
			}

			return autoya._assetBundleLoader.GetAssetBundlesWeight(bundleNames);
		}



		/**
			get bundle names of "not storage cached" assetBundle from assetBundleList.
		 */
		public static void AssetBundle_NotCachedBundleNames (Action<string[]> onBundleNamesReady) {
			var cont = CheckAssetBundlesFeatureCondition(
				(code, reason) => {}
			);

			if (!cont) {
				return;
			}

			var cor = autoya.GetNotCachedAssetBundleNames(onBundleNamesReady);
			Autoya.Mainthread_Commit(cor);
		}

		private IEnumerator GetNotCachedAssetBundleNames (Action<string[]> onBundleNamesReady) {
			while (!Caching.ready) {
				yield return null;
			}

			var bundleNames = new List<string>();
			var assetBundleList = AssetBundle_AssetBundleList();
			foreach (var bundleInfo in assetBundleList.assetBundles) {
				var bundleName = bundleInfo.bundleName;
				var url = GetAssetBundleListVersionedBasePath(AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSET) + bundleName;
				var hash = Hash128.Parse(bundleInfo.hash);

				var isCachedOnStorage = Caching.IsVersionCached(url, hash);

				if (isCachedOnStorage) {
					continue;
				}

				bundleNames.Add(bundleName);
			}

			onBundleNamesReady(bundleNames.ToArray());
		}


		private IEnumerator ListLoaderCoroutine (Action execute) {
			// start loading.
			assetBundleFeatState = AssetBundlesFeatureState.ListLoading;
			execute();
			yield break;
		}



		/*
			Loader
		*/
		private AssetBundleLoader _assetBundleLoader;
		
		public static void AssetBundle_LoadAsset<T> (
			string assetName, 
			Action<string, T> loadSucceeded, 
			Action<string, AssetBundleLoadError, string, AutoyaStatus> loadFailed
		) where T : UnityEngine.Object {
			var cont = CheckAssetBundlesFeatureCondition(
				(code, reason) => {
					switch (code) {
						case AssetBundlesError.AutoyaNotReady: {
							loadFailed(assetName, AssetBundleLoadError.DownloadFailed, "code:" + code + " reason:" + reason, new AutoyaStatus());
							break;
						}
						case AssetBundlesError.ListLoading:
						case AssetBundlesError.NeedToDownloadAssetBundleList: {
							loadFailed(assetName, AssetBundleLoadError.AssetBundleListIsNotReady, "code:" + code + " reason:" + reason, new AutoyaStatus());
							break;
						}
						default: {
							loadFailed(assetName, AssetBundleLoadError.Undefined, "code:" + code + " reason:" + reason, new AutoyaStatus());
							break;
						}
					}	
				}
			);

			if (!cont) {
				return;
			}

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
			var cont = CheckAssetBundlesFeatureCondition(
				(code, reason) => {
					// do nothing.
				}
			);

			if (!cont) {
				return;
			}

			autoya._assetBundleLoader.UnloadOnMemoryAssetBundles();
		}
		
		public static void AssetBundle_UnloadOnMemoryAssetBundle (string bundleName) {
			var cont = CheckAssetBundlesFeatureCondition(
				(code, reason) => {
					// do nothing.
				}
			);

			if (!cont) {
				return;
			}

			autoya._assetBundleLoader.UnloadOnMemoryAssetBundle(bundleName);
		}
		
		public static void AssetBundle_UnloadOnMemoryAsset (string assetName) {
			var cont = CheckAssetBundlesFeatureCondition(
				(code, reason) => {
					// do nothing.
				}
			);

			if (!cont) {
				return;
			}

			autoya._assetBundleLoader.UnloadOnMemoryAsset(assetName);
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
			}
			
			execute();
		}

		private void ReadyLoaderAndPreloader () {
			// initialize AssetBundleLoader.
			{
				AssetBundleLoader.HttpResponseHandlingDelegate httpResponseHandlingDel = (p1, p2, p3, p4, p5, p6, p7) => {
					httpResponseHandlingDelegate(p1, p2, p3, p4, p5, p6, p7);
				};
				AssetBundleLoader.AssetBundleGetRequestHeaderDelegate assetBundleGetRequestHeaderDel = (p1, p2) => {
					return assetBundleGetRequestHeaderDelegate(p1, p2);
				};

				_assetBundleLoader = new AssetBundleLoader(GetAssetBundleListVersionedBasePath(AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSET), _currentAssetBundleList, assetBundleGetRequestHeaderDel, httpResponseHandlingDel);
			}

			// initialize AssetBundlePreloader.
			{
				AssetBundlePreloader.HttpResponseHandlingDelegate httpResponseHandlingDel = (p1, p2, p3, p4, p5, p6, p7) => {
					httpResponseHandlingDelegate(p1, p2, p3, p4, p5, p6, p7);
				};
				AssetBundlePreloader.AssetBundleGetRequestHeaderDelegate assetBundleGetRequestHeaderDel = (p1, p2) => {
					return assetBundleGetRequestHeaderDelegate(p1, p2);
				};

				_assetBundlePreloader = new AssetBundlePreloader(assetBundleGetRequestHeaderDel, httpResponseHandlingDel);
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
        public static void AssetBundle_Preload (string preloadListUrl, Action<string[], Action, Action> onBeforePreloading, Action<double> progress, Action done, Action<int, string, AutoyaStatus> preloadListDownloadFailed, Action<string, int, string, AutoyaStatus> bundleDownloadFailed, int maxParallelCount, double timeoutSec=AssetBundlesSettings.TIMEOUT_SEC) {
			var cont = CheckAssetBundlesFeatureCondition(
				(code, reason) => {
					preloadListDownloadFailed(-(int)code, reason, new AutoyaStatus());
				}
			);

			if (!cont) {
				return;
			}

			Action<AssetBundleLoader> act = loader => {
				var url = AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_PRELOADLIST + preloadListUrl;
				autoya.mainthreadDispatcher.Commit(
					autoya._assetBundlePreloader.Preload(
						loader,
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
			};

			autoya.mainthreadDispatcher.Commit(
				autoya.PreloaderCoroutine(act, preloadListDownloadFailed)
			);
		}
		
		/**
			download assetBundles by the preloadList, then download assetBundles.
			this feature will download "not downloaded" assetBundles only.

			onBeforePreloading:
				you can set the Action to this param for getting "will be download assetBundles names".
				then if execute proceed(), download will be started. 
				else, execute cancel(), download will be cancelled
		 */
		public static void AssetBundle_PreloadByList (PreloadList preloadList, Action<string[], Action, Action> onBeforePreloading, Action<double> progress, Action done, Action<int, string, AutoyaStatus> preloadListDownloadFailed, Action<string, int, string, AutoyaStatus> bundleDownloadFailed, int maxParallelCount, double timeoutSec=0) {
			var cont = CheckAssetBundlesFeatureCondition(
				(code, reason) => {
					preloadListDownloadFailed((int)code, reason, new AutoyaStatus());
				}
			);

			if (!cont) {
				return;
			}

			Action<AssetBundleLoader> act = loader => {
				autoya.mainthreadDispatcher.Commit(
					autoya._assetBundlePreloader.Preload(
						loader,
						preloadList,
						onBeforePreloading,
						progress,
						done,
						preloadListDownloadFailed,
						bundleDownloadFailed,
						maxParallelCount
					)
				);
			};

			autoya.mainthreadDispatcher.Commit(
				autoya.PreloaderCoroutine(act, preloadListDownloadFailed)
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
			}
			
			execute(_assetBundleLoader);
		}


		public enum AssetBundlesError {
			AutoyaNotReady,
			NeedToDownloadAssetBundleList,
			ListLoading
		}

		private static bool CheckAssetBundlesFeatureCondition (Action<AssetBundlesError, string> failed) {
			if (autoya == null) {
				failed(AssetBundlesError.AutoyaNotReady, "Autoya not ready.");
				return false;
			}

			switch (autoya.assetBundleFeatState) {
				case AssetBundlesFeatureState.Ready: {
					// pass.
					return true;
				}
				case AssetBundlesFeatureState.None: {
					autoya.mainthreadDispatcher.Commit(
						FailEnumerationEmitter(
							() => {
								failed(AssetBundlesError.NeedToDownloadAssetBundleList, "need to download AssetBundleList first. use AssetBundle_DownloadAssetBundleList().");
							}
						)
					);
					return false;
				}
				case AssetBundlesFeatureState.ListLoading: {
					autoya.mainthreadDispatcher.Commit(
						FailEnumerationEmitter(
							() => {
								failed(AssetBundlesError.ListLoading, "AssetBundleList is loading. please wait end.");
							}
						)
					);
					return false;
				}
				default: {
					throw new Exception("unhandled feature state:" + autoya.assetBundleFeatState);
				}
			}
		}

		private static IEnumerator FailEnumerationEmitter (Action failAct) {
			failAct();
			yield break;
		}


		/*
			debug
		 */

		public static void Debug_AssetBundle_DownloadAssetBundleListFromUrl (string listUrl, Action<ListDownloadResult> downloadSucceeded, Action<ListDownloadError, string, AutoyaStatus> downloadFailed, double timeoutSec=AssetBundlesSettings.TIMEOUT_SEC) {
			Internal_AssetBundle_DownloadAssetBundleListFromUrl(listUrl, downloadSucceeded, downloadFailed, timeoutSec);
		}

		public static AssetBundlesFeatureState Debug_AssetBundle_FeatureState () {
			return autoya.assetBundleFeatState;
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

		public static void Debug_SetOverridePoint_OnNewAppRequested (Action<string> onNewAppDetected) {
			autoya.OnNewAppRequested = onNewAppDetected;
		}
	}
}