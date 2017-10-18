/*
	このクラス自体が、url部分だけでもなんらかDL可能にしておくと、起動時にURLを返すURL、というのが実現できる。
	どうせurl自体はバレるので、一度通信してurlsを取得するようにしておいてもいいと思う。

	replaceableなURLグループみたいなのを作っておくといいと思う。
 */
namespace AutoyaFramework.Settings.AssetBundles {
	public class AssetBundlesSettings {
		public const string ASSETBUNDLES_LIST_STORED_DOMAIN = "assetbundles";
		public const string ASSETBUNDLES_LIST_FILENAME = "assetbundleslist.txt";


		/*
			urls and prefixs.
			めっちゃ未調整だここ。Mac -> OSXなんちゃらに揃えた方がいいかも。
		*/
		public const string PLATFORM_STR = 

		#if UNITY_IOS
			"Mac/";
		#elif UNITY_ANDROID
			"Mac/";
		#elif UNITY_WEBGL
			"Mac/";
		#elif UNITY_UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
			"Mac/";
		#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			"Mac/";
		#else
			"Mac/";
		#endif

		public const string ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST = "https://sassembla.github.io/Autoya/AssetBundles/" + PLATFORM_STR;
		public const string ASSETBUNDLES_URL_DOWNLOAD_ASSET = "https://sassembla.github.io/Autoya/AssetBundles/" + PLATFORM_STR;
		public const string ASSETBUNDLES_URL_DOWNLOAD_PRELOADLIST = "https://sassembla.github.io/Autoya/AssetBundles/" + PLATFORM_STR;
		

		public const string ASSETBUNDLES_DOWNLOAD_PREFIX = "assetbundle_";
		public const string ASSETBUNDLES_PRELOADLIST_PREFIX = "preloadlist_";
		public const string ASSETBUNDLES_PRELOADBUNDLE_PREFIX = "preloadassetbundle_";
		public const string ASSETBUNDLES_ASSETBUNDLELIST_PREFIX = "assetbundlelist_";

		public const double TIMEOUT_SEC = 10.0;
	}
}
