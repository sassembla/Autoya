using UnityEngine;
using Connections.HTTP;
using AutoyaFramework.Persistence;


/**
	main behaviour implementation of Autoya.
*/
namespace AutoyaFramework {
    public partial class Autoya {
		/**
			all conditions which Autoya has.
		*/
		private class AutoyaParameters {
			public string _app_version;
			public string _asset_version;
			
			public string _buildNumber;
		}
		
		private AutoyaParameters _parameters;

		private Autoya (string basePath="") {
			Debug.LogWarning("autoya initialize start.");
			
			_parameters = new AutoyaParameters();

			_autoyaFilePersistence = new FilePersistence(basePath);

			_autoyaHttp = new HTTPConnection();

			/* 
				セッティングよみ出ししちゃおう。なんか、、LocalStorageからapp_versionとかだな。Unityで起動時に上書きとかしとけば良い気がする。
				asset_versionはAssetsListに組み込まれてるんで、それを読みだして云々、っていう感じにできる。
			*/
			
			// authの状態を取得する、、そのためのユーティリティは必要かなあ、、まあこのクラス内で良い気がするな。
			// ログインが終わってるかどうかっていうのでなんか判断すれば良いのではっていう。
			// ログインが成功した記録があれば、そのトークンを使って接続を試みる。
			// あれ、、試みるだけなら、token読めたらログイン完了っていう扱いでいいのでは？　って思ったけど
			// 毎回なんか初回通信を蹴られるの面倒だからやっぱ通信しておこうねっていう気持ちになった。
			
			/*
				初期化機構を起動する
			*/
			this.InitializeTokenAuth();
		}


		public static int BuildNumber () {
			return -1;
		}
		
    }


	public enum AutoyaErrorFlowCode {
		Autoya_Logout,
		Autoya_Maintenance,
		Autoya_ShouldUpdateApp,
		Autoya_PleaseUpdateApp,
		Autoya_UpdateAssets,
		StorageChecker_NoSpace,
		Connection_Offline
	}
}