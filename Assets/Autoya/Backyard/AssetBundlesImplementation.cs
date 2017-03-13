using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Settings.AssetBundles;
using UnityEngine;

namespace AutoyaFramework {
	public partial class Autoya {

		/*
			Downloader
		*/
		private AssetBundleListDownloader _assetBundleListDownloader = new AssetBundleListDownloader("listDlPath");
		public static void AssetBundle_DownloadList () {
			Debug.LogError("リスト自体のロードを開始する。Connectionとかを使って云々。IEnumeratorになるので、なんかUniRxがらみで処理できる気がする。総合的なTimeoutとかをセットする？ 終わったことだけが検知できればいい感じ。");
			autoya._assetBundleListDownloader.DownloadList();
		}

		/*
			Preloader
		*/
		private AssetBundlePreloader _assetBundlePreloader = new AssetBundlePreloader("preloadListDlPath");
		public static void AssetBundle_Preload (string preloadKey) {
			autoya._assetBundlePreloader.Preload(preloadKey, preloadedKey => {});
		}

		/*
			Loader
		*/
		private AssetBundleLoader _assetBundleLoader;
		public static void AssetBundle_UpdateList (string path, AssetBundleList list) {
			// if (autoya == null) {
			// 	failed();
			// } 
			// if (Autoya.Auth_IsLoggedIn()) {
			// 	failed();
			// }

			autoya._assetBundleLoader = new AssetBundleLoader(path, list, autoya.assetBundleRequestHeaderDelegate, autoya.httpResponseHandlingDelegate);// 仮のリストの更新API。実際に使うとしたら、内部から。
		}

		public static void AssetBundle_LoadAsset<T> (string assetName, Action<string, T> loadSucceeded, Action<string, AssetBundleLoader.AssetBundleLoadError, string, AutoyaStatus> loadFailed) where T : UnityEngine.Object {
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
				autoya._assetBundleLoader = new AssetBundleLoader(AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSET, new AssetBundleList()/*このへんで、リストを読み出す? もっといい仕組みがある気がする。*/, autoya.assetBundleRequestHeaderDelegate, autoya.httpResponseHandlingDelegate);
			}

			autoya.mainthreadDispatcher.Commit(
				autoya._assetBundleLoader.LoadAsset(assetName, loadSucceeded, loadFailed)
			);
		}
		public static void AssetBundle_UnloadAllAssets () {
			Debug.LogError("キャッシュから消す、っていうのをどう見せるか考える必要がある。明示が必要。　それとは別に、キャッシュから消すのも必要。");
			if (autoya._assetBundleLoader == null) {
				autoya._assetBundleLoader = new AssetBundleLoader(AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSET, new AssetBundleList()/*このへんで、リストを読み出す? もっといい仕組みがある気がする。*/, autoya.assetBundleRequestHeaderDelegate, autoya.httpResponseHandlingDelegate);
			}
			autoya._assetBundleLoader.UnloadOnMemoryAssetBundles();
		}

		private class AssetBundleLoadErrorInstance {
			private readonly string connectionId;
			private const AssetBundleLoader.AssetBundleLoadError code = AssetBundleLoader.AssetBundleLoadError.Unauthorized;
			private readonly string reason;
			private readonly Action<string, AssetBundleLoader.AssetBundleLoadError, string, AutoyaStatus> failed;
			private static AutoyaStatus status = new AutoyaStatus();

			public AssetBundleLoadErrorInstance (string connectionId, string reason, Action<string, AssetBundleLoader.AssetBundleLoadError, string, AutoyaStatus> failed) {
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