using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Settings.AssetBundles;
using UnityEngine;

namespace AutoyaFramework {
	public partial class Autoya {

		private AssetBundleList currentAssetBundleList;

		/*
			Downloader
		*/
		private AssetBundleListDownloader _assetBundleListDownloader = new AssetBundleListDownloader();
		public static void AssetBundle_DownloadAssetBundleList (string url, Action<string> done, Action<string, AssetBundleLoadError, string, AutoyaStatus> downloadFailed) {
			if (autoya == null) {
				var cor = new AssetBundleLoadErrorInstance(url, "Autoya is null.", downloadFailed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				return;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				var cor = new AssetBundleLoadErrorInstance(url, "not authenticated.", downloadFailed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);				
				return;
			}

			autoya.mainthreadDispatcher.Commit(
				autoya._assetBundleListDownloader.DownloadAssetBundleList(
					url, 
					assetBundleList => {
						Debug.LogError("assetBundleListの更新処理を行う。ローカルに持ってるものを、、ああ、OverridePointに保存箇所からのロードとかそういうのを書かないといけないのか。");

						var version = assetBundleList.version;
						done(version);
					},
					(downloadFailedUrl, error, reason, autoyaStatus) => {
						Debug.LogError("assetBundleListのダウンロードに失敗したんでどうしようかな。 downloadFailedUrl:" + downloadFailedUrl + " error:" + error + " reason:" + reason);		
					},
					10// うまい指定方法がよくわかってない、ここで固定するのはまずいよな〜〜という気持ち
				)
			);
		}

		/*
			Preloader
		*/
		private AssetBundlePreloader _assetBundlePreloader = new AssetBundlePreloader();
		public static void AssetBundle_Preload (string url, Action<double> progress, Action done, Action<int, string, AutoyaStatus> listDownloadFailed, Action<string, AssetBundleLoadError, AutoyaStatus> bundleDownloadFailed) {
			if (autoya == null) {
				// var cor = new PreloadListLoadErrorInstance(url, "Autoya is null.", downloadFailed).Coroutine();
				// autoya.mainthreadDispatcher.Commit(cor);
				return;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				// var cor = new PreloadListLoadErrorInstance(url, "not authenticated.", downloadFailed).Coroutine();
				// autoya.mainthreadDispatcher.Commit(cor);				
				return;
			}

			Debug.LogError("仮でリストを入れる");
			var assetBundleList = new AssetBundleList(
				"Mac",
				"1.0.0", 
				new AssetBundleInfo[]{
					// pngが一枚入ったAssetBundle
					new AssetBundleInfo(
						"bundlename", 
						new string[]{"Assets/AutoyaTests/Runtime/AssetBundles/TestResources/textureName.png"}, 
						new string[0], 
						621985162,
						"578b73927bc11f6e80072caa17983776",
						100
					)
				}
			);

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

		private class PreloadListLoadErrorInstance {
			private readonly string connectionId;
			private const AssetBundleLoadError code = AssetBundleLoadError.Unauthorized;
			private readonly string reason;
			private readonly Action<string, AssetBundleLoadError, string, AutoyaStatus> failed;
			private static AutoyaStatus status = new AutoyaStatus();

			public PreloadListLoadErrorInstance (string connectionId, string reason, Action<string, AssetBundleLoadError, string, AutoyaStatus> failed) {
				this.connectionId = connectionId;
				this.reason = reason;
				this.failed = failed;
			}

			public IEnumerator Coroutine () {
				yield return null;
				failed(connectionId, code, reason, status);
			}
		}

		/*
			Loader
		*/
		private AssetBundleLoader _assetBundleLoader;
		public static void AssetBundle_UpdateList (string path, AssetBundleList list) {
			// if (autoya == null) {
			// 	failed();
			// } 
			// if (Autoya.Auth_IsLoggedIn()) {// 認証との兼ね合いが微妙。
			// 	failed();
			// }

			autoya._assetBundleLoader = new AssetBundleLoader(path, list, autoya.assetBundleRequestHeaderDelegate, autoya.httpResponseHandlingDelegate);// 仮のリストの更新API。実際に使うとしたら、内部から。
		}

		public static void AssetBundle_LoadAsset<T> (string assetName, Action<string, T> loadSucceeded, Action<string, AssetBundleLoadError, string, AutoyaStatus> loadFailed) where T : UnityEngine.Object {
			if (autoya == null) {
				var cor = new AssetBundleLoadErrorInstance(assetName, "Autoya is null.", loadFailed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				return;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				var cor = new AssetBundleLoadErrorInstance(assetName, "not authenticated.", loadFailed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);				
				return;
			}

			if (autoya._assetBundleLoader == null) {
				autoya._assetBundleLoader = new AssetBundleLoader(AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSET, autoya.currentAssetBundleList, autoya.assetBundleRequestHeaderDelegate, autoya.httpResponseHandlingDelegate);
			}

			autoya.mainthreadDispatcher.Commit(
				autoya._assetBundleLoader.LoadAsset(assetName, loadSucceeded, loadFailed)
			);
		}
		public static void AssetBundle_UnloadAllAssets () {
			if (autoya._assetBundleLoader == null) {
				autoya._assetBundleLoader = new AssetBundleLoader(AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSET, autoya.currentAssetBundleList, autoya.assetBundleRequestHeaderDelegate, autoya.httpResponseHandlingDelegate);
			}
			autoya._assetBundleLoader.UnloadOnMemoryAssetBundles();
		}

		public static void AssetBundle_UnloadAssetBundle (string bundleName) {
			if (autoya._assetBundleLoader == null) {
				autoya._assetBundleLoader = new AssetBundleLoader(AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSET, autoya.currentAssetBundleList, autoya.assetBundleRequestHeaderDelegate, autoya.httpResponseHandlingDelegate);
			}
			autoya._assetBundleLoader.UnloadOnMemoryAssetBundle(bundleName);
		}
		
		public static void AssetBundle_UnloadAsset (string assetName) {
			if (autoya._assetBundleLoader == null) {
				autoya._assetBundleLoader = new AssetBundleLoader(AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSET, autoya.currentAssetBundleList, autoya.assetBundleRequestHeaderDelegate, autoya.httpResponseHandlingDelegate);
			}
			autoya._assetBundleLoader.UnloadOnMemoryAsset(assetName);
		}

		private class AssetBundleLoadErrorInstance {
			private readonly string connectionId;
			private const AssetBundleLoadError code = AssetBundleLoadError.Unauthorized;
			private readonly string reason;
			private readonly Action<string, AssetBundleLoadError, string, AutoyaStatus> failed;
			private static AutoyaStatus status = new AutoyaStatus();

			public AssetBundleLoadErrorInstance (string connectionId, string reason, Action<string, AssetBundleLoadError, string, AutoyaStatus> failed) {
				this.connectionId = connectionId;
				this.reason = reason;
				this.failed = failed;
			}

			public IEnumerator Coroutine () {
				yield return null;
				failed(connectionId, code, reason, status);
			}
		}
	}
}