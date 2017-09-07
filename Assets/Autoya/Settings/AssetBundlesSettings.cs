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
		*/
		public const string ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST = "localhost:8081//Mac/";
		public const string ASSETBUNDLES_URL_DOWNLOAD_ASSET = "localhost:8081//Mac/";
		public const string ASSETBUNDLES_URL_DOWNLOAD_PRELOADLIST = "localhost:8081//Mac/";
		

		public const string ASSETBUNDLES_DOWNLOAD_PREFIX = "assetbundle_";
		public const string ASSETBUNDLES_PRELOADLIST_PREFIX = "preloadlist_";
		public const string ASSETBUNDLES_PRELOADBUNDLE_PREFIX = "preloadassetbundle_";
		public const string ASSETBUNDLES_ASSETBUNDLELIST_PREFIX = "assetbundlelist_";
	}
}
