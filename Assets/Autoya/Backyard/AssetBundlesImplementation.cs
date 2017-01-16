using System;
using AutoyaFramework.AssetBundles;
using UnityEngine;

namespace AutoyaFramework {
	public partial class Autoya {

        private const string basePath = "まだセットされてない。APIとかを鑑みるに、Settingsにあるといいと思う。リスト取得、preloadリスト取得、assetBundle取得の3種。";

        /*
            Downloader
        */
        private AssetBundleListDownloader _assetBundleListDownloader = new AssetBundleListDownloader(basePath);
        public static void AssetBundle_DownloadList () {
            Debug.LogError("リスト自体のロードを開始する。Connectionとかを使って云々。IEnumeratorになるので、なんかUniRxがらみで処理できる気がする。総合的なTimeoutとかをセットする？ 終わったことだけが検知できればいい感じ。");
            autoya._assetBundleListDownloader.DownloadList();
        }

        /*
            Preloader
        */
        private AssetBundlePreloader _assetBundlePreloader = new AssetBundlePreloader(basePath);
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

            autoya._assetBundleLoader = new AssetBundleLoader(path, list, autoya.httpResponseHandlingDelegate);// 仮のリストの更新API。実際に使うとしたら、内部から。
        }

        public static void AssetBundle_LoadAsset<T> (string assetName, Action<string, T> loadSucceeded, Action<string, AssetBundleLoader.AssetBundleLoadError, string> loadFailed) where T : UnityEngine.Object {
            // if (autoya == null) {
			// 	failed();
			// } 
			// if (Autoya.Auth_IsLoggedIn()) {
			// 	failed();
			// }

            if (autoya._assetBundleLoader == null) {
                autoya._assetBundleLoader = new AssetBundleLoader(basePath, new AssetBundleList()/*このへんで、リストを読み出す? もっといい仕組みがある気がする。*/, autoya.httpResponseHandlingDelegate);
            }
            autoya.mainthreadDispatcher.Commit(
                autoya._assetBundleLoader.LoadAsset(assetName, loadSucceeded, loadFailed)
            );
        }
        public static void AssetBundle_UnloadAllAssets () {
            if (autoya._assetBundleLoader == null) {
                autoya._assetBundleLoader = new AssetBundleLoader(basePath, new AssetBundleList()/*このへんで、リストを読み出す? もっといい仕組みがある気がする。*/, autoya.httpResponseHandlingDelegate);
            }
            autoya._assetBundleLoader.UnloadAllAssetBundles();
        }
    }
}