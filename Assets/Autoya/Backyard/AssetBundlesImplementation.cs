using System;
using System.Collections;
using System.Collections.Generic;
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

		
		public static void AssetBundle_DeleteAllStorageCache (Action<bool> result) {
			autoya.mainthreadDispatcher.Commit(autoya.CleanCacheEnumRunner(result));
		}
		private IEnumerator CleanCacheEnumRunner (Action<bool> result) {
			while (!Caching.ready) {
				yield return null;
			}

			var isDeleted = Caching.CleanCache();

			result(isDeleted);
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
		public static void AssetBundle_DownloadAssetBundleList (string fileName, string version, Action downloadSucceeded, Action<int, string, AutoyaStatus> downloadFailed, double timeoutSec=0) {
			Action act = () => {	
				autoya.mainthreadDispatcher.Commit(
					autoya._assetBundleListDownloader.DownloadAssetBundleList(
						AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST + version + "/" + fileName, 
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

		public static AssetBundleList AssetBundle_AssetBundleList () {
			return autoya.LoadAssetBundleListFromStorage();
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
					// new or renew AssetBundleLoader.
					_assetBundleLoader = new AssetBundleLoader(Autoya.AssetBundle_GetAssetBundleListVersionedBasePath(AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSET), _currentAssetBundleList, assetBundleRequestHeaderDelegate, httpResponseHandlingDelegate);
					_assetBundlePreloader = new AssetBundlePreloader(assetBundleRequestHeaderDelegate, httpResponseHandlingDelegate);

					// set to load ready.
					assetBundleFeatState = AssetBundlesFeatureState.LoaderReady;
					break;
				}
				case AssetBundlesFeatureState.LoaderReady: {
					break;
				}
			}
			
			execute();
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
					// new or renew AssetBundleLoader.
					_assetBundleLoader = new AssetBundleLoader(Autoya.AssetBundle_GetAssetBundleListVersionedBasePath(AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSET), _currentAssetBundleList, autoya.assetBundleRequestHeaderDelegate, autoya.httpResponseHandlingDelegate);
					_assetBundlePreloader = new AssetBundlePreloader(assetBundleRequestHeaderDelegate, httpResponseHandlingDelegate);

					// set to load ready.
					assetBundleFeatState = AssetBundlesFeatureState.LoaderReady;
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
		public static void AssetBundle_Preload (string preloadListPath, Action<double> progress, Action done, Action<int, string, AutoyaStatus> listDownloadFailed, Action<string, int, string, AutoyaStatus> bundleDownloadFailed, int maxParallelCount, double timeoutSec=0) {

			Action<AssetBundleLoader> act = loader => {
				var url = AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_PRELOADLIST + preloadListPath;
				autoya.mainthreadDispatcher.Commit(
					autoya._assetBundlePreloader.Preload(
						loader,
						url, 
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

		public static void AssetBundle_Preload (PreloadList preloadList, Action<double> progress, Action done, Action<int, string, AutoyaStatus> listDownloadFailed, Action<string, int, string, AutoyaStatus> bundleDownloadFailed, int maxParallelCount, double timeoutSec=0) {

			Action<AssetBundleLoader> act = loader => {
				autoya.mainthreadDispatcher.Commit(
					autoya._assetBundlePreloader.Preload(
						loader,
						preloadList,
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
					// new or renew AssetBundleLoader.
					_assetBundleLoader = new AssetBundleLoader(Autoya.AssetBundle_GetAssetBundleListVersionedBasePath(AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSET), _currentAssetBundleList, assetBundleRequestHeaderDelegate, httpResponseHandlingDelegate);
					_assetBundlePreloader = new AssetBundlePreloader(assetBundleRequestHeaderDelegate, httpResponseHandlingDelegate);

					// set to load ready.
					assetBundleFeatState = AssetBundlesFeatureState.LoaderReady;
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