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
			if (autoya._currentAssetBundleList != null) {
				return true;
			}
			return false;
		}

		public static string AssetBundle_GetAssetBundleListVersion () {
			if (autoya._currentAssetBundleList != null) {
				return autoya._currentAssetBundleList.version;
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
			ListLoaded,// loaderが使える
			LoaderReady,// preloaderが使える
		}
		private AssetBundlesFeatureState assetBundleFeatState;


		private AssetBundleList _currentAssetBundleList;

		/*
			Downloader
		*/

		private AssetBundleListDownloader _assetBundleListDownloader = new AssetBundleListDownloader();
		public static void AssetBundle_DownloadAssetBundleList (string fileName, Action downloadSucceeded, Action<int, string, AutoyaStatus> downloadFailed) {
			Action act = () => {	
				var urlBase = AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST;
				autoya.mainthreadDispatcher.Commit(
					autoya._assetBundleListDownloader.DownloadAssetBundleList(
						urlBase + fileName, 
						list => {
							var result = autoya.StoreAssetBundleListToStorage(list);
							if (result) {
								autoya.assetBundleFeatState = AssetBundlesFeatureState.ListLoaded;
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
						10.0// タイムアウト直書きが辛い。
					)
				);
			};

			autoya.mainthreadDispatcher.Commit(
				autoya.ListLoaderCoroutine(act, downloadFailed)
			);
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
					Debug.LogError("考え中の、loaderやpreloaderが元気でリストを再取得したいケース。loaderの状態によってやるべきことが変わる。");

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
					Debug.LogError("状態によって変わる。loaderやpreloaderが頑張ってるならダメ。Preloaderは関係あるんだろうか。");
					break;
				}
			}
			return false;
		}



		/*
			Loader
		*/
		private AssetBundleLoader _assetBundleLoader;
		
		public static void AssetBundle_LoadAsset<T> (string assetName, Action<string, T> loadSucceeded, Action<string, AssetBundleLoadError, string, AutoyaStatus> loadFailed) where T : UnityEngine.Object {
			Action act = () => {
				autoya.mainthreadDispatcher.Commit(
					autoya._assetBundleLoader.LoadAsset(assetName, loadSucceeded, loadFailed)
				);
			};
			autoya.mainthreadDispatcher.Commit(
				autoya.BundleLoaderCoroutine(act, () => {})
			);
		}
		
		public static void AssetBundle_UnloadAllAssets () {
			Action act = () => {
				autoya._assetBundleLoader.UnloadOnMemoryAssetBundles();
			};

			autoya.mainthreadDispatcher.Commit(
				autoya.BundleLoaderCoroutine(act, () => {})
			);
		}

		public static void AssetBundle_UnloadAssetBundle (string bundleName) {
			Action act = () => {
				autoya._assetBundleLoader.UnloadOnMemoryAssetBundle(bundleName);
			};

			autoya.mainthreadDispatcher.Commit(
				autoya.BundleLoaderCoroutine(act, () => {})
			);
		}
		
		public static void AssetBundle_UnloadAsset (string assetName) {
			Action act = () => {
				autoya._assetBundleLoader.UnloadOnMemoryAsset(assetName);
			};

			autoya.mainthreadDispatcher.Commit(
				autoya.BundleLoaderCoroutine(act, () => {})
			);
		}

		private IEnumerator BundleLoaderCoroutine (Action execute, Action failed) {
			while (true) {
				switch (assetBundleFeatState) {
					case AssetBundlesFeatureState.None: {
						failed();
						yield break;
					}
					case AssetBundlesFeatureState.ListLoading: {
						// waiting download finish.
						break;
					}
					case AssetBundlesFeatureState.ListLoaded: {
						// new or renew AssetBundleLoader.
						_assetBundleLoader = new AssetBundleLoader(AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSET, _currentAssetBundleList, autoya.assetBundleRequestHeaderDelegate, autoya.httpResponseHandlingDelegate);

						// set to load ready.
						assetBundleFeatState = AssetBundlesFeatureState.LoaderReady;
						goto LoadReady;
					}
					case AssetBundlesFeatureState.LoaderReady: {
						goto LoadReady;
					}
				}

				// loop.
				yield return null;

				LoadReady:
				break;
			}
			
			execute();
		}


		/*
			Preloader
		*/
		private AssetBundlePreloader _assetBundlePreloader;
		public static void AssetBundle_Preload (string url, Action<double> progress, Action done, Action<int, string, AutoyaStatus> listDownloadFailed, Action<string, AssetBundleLoadError, AutoyaStatus> bundleDownloadFailed) {
			// Debug.LogError("仮でリストを入れる");
			// var assetBundleList = new AssetBundleList(
			// 	"Mac",
			// 	"1.0.0", 
			// 	new AssetBundleInfo[]{
			// 		// pngが一枚入ったAssetBundle
			// 		new AssetBundleInfo(
			// 			"bundlename", 
			// 			new string[]{"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName.png"}, 
			// 			new string[0], 
			// 			621985162,
			// 			"578b73927bc11f6e80072caa17983776",
			// 			100
			// 		)
			// 	}
			// );

			// loaderはそのへんに存在している気がする。
			// var loader = new AssetBundleLoader();

			// autoya.mainthreadDispatcher.Commit(
			// 	autoya._assetBundlePreloader.Preload(
			// 		loader,
			// 		url, 
			// 		progress,
			// 		done,
			// 		listDownloadFailed,
			// 		bundleDownloadFailed,
			// 		10// うまい指定方法がよくわかってない、ここで固定するのはまずいかな〜〜という気持ち
			// 	)
			// );
		}
	}
}