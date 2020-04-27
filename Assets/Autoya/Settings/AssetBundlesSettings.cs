/*
	このクラス自体が、url部分だけでもなんらかDL可能にしておくと、起動時にURLを返すURL、というのが実現できる。
	どうせurl自体はバレるので、一度通信してurlsを取得するようにしておいてもいいと思う。

	replaceableなURLグループみたいなのを作っておくといいと思う。
 */
namespace AutoyaFramework.Settings.AssetBundles
{
    public class AssetBundlesSettings
    {
        public const string ASSETBUNDLES_LIST_STORED_DOMAIN = "assetbundles";


        /*
			urls and prefixs.
		*/
        public const string PLATFORM_STR =

#if UNITY_IOS
            "iOS";
#elif UNITY_ANDROID
			"Android";
#elif UNITY_WEBGL
			"WebGL";
#elif UNITY_UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            "OSX";
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			"Windows";
#else
			"OSX";
#endif

        public static string ASSETBUNDLES_URL_DOWNLOAD_PRELOADLIST = "https://raw.githubusercontent.com/sassembla/Autoya/assetbundle_multi_list_support/AssetBundles/preload/";


        public const string ASSETBUNDLES_DOWNLOAD_PREFIX = "assetbundle_";
        public const string ASSETBUNDLES_PRELOADLIST_PREFIX = "preloadlist_";
        public const string ASSETBUNDLES_PRELOADBUNDLE_PREFIX = "preloadassetbundle_";
        public const string ASSETBUNDLES_ASSETBUNDLELIST_PREFIX = "assetbundlelist_";

        public const double TIMEOUT_SEC = 10.0;
    }
}
