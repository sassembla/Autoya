using UnityEngine;
using Connections.HTTP;
using System;

/**
	main behaviour implementation class of Autoya.
*/
namespace AutoyaFramework {
    public partial class Autoya {

		public static int BuildNumber () {
			return -1;
		}
		
		/*
			authorization
		*/
		private bool _isLoggedIn = false;
		
		private void InitializeAuth () {
			_isLoggedIn = false;

			OnAuthSucceeded = () => {
				_isLoggedIn = true;
			};

			OnAuthFailed = (conId, failedReason) => {
				_isLoggedIn = false;
				return false;// by default, autoya decline auto-relogin.
			};
		}

		public static void SetOnAuthSucceeded (Action onAuthSucceeded) {
			Debug.LogError("SetOnAuthSucceeded start, autoya._isLoggedIn:" + autoya._isLoggedIn);
			autoya.OnAuthSucceeded = () =>{
				autoya._isLoggedIn = true;
				onAuthSucceeded();
			};

			if (autoya._isLoggedIn) onAuthSucceeded();
        }

		public static void SetOnAuthFailed(Func<string, string, bool> onAuthFailed) {
            autoya.OnAuthFailed = (conId, reason) =>{
				autoya._isLoggedIn = false;
				return onAuthFailed(conId, reason);
			};
        }
		
		public Action OnAuthSucceeded;

		public Func<string, string, bool> OnAuthFailed;

		public static bool IsLoggedIn () {
			return autoya._isLoggedIn;
		}



		private Autoya () {
			Debug.Log("autoya initialize start.");
			/* 
				セッティングよみ出ししちゃおう。なんか、、LocalStorageからapp_versionとかだな。Unityで起動時に上書きとかしとけば良い気がする。
				res_versionはAssetsListに組み込まれてるんで、それを読みだして云々、っていう感じにできる。
			*/
			
			Debug.LogError("should design auth usage first.");
			// authの状態を取得する、、そのためのユーティリティは必要かなあ、、まあこのクラス内で良い気がするな。
			// ログインが終わってるかどうかっていうのでなんか判断すれば良いのではっていう。
			// ログインが成功した記録があれば、そのトークンを使って接続を試みる。
			this.RenewAuthorizedHTTPHeader(new AuthorizedHTTPHeader("a", "b"));

			// ログイン状態が影響するかな？
			this.InitializeAuth();

			_autoyaHttp = new HTTPConnection();

			// 必要であればこのへんでログイン処理を開始する。完了するのは非同期の向こう側。
			OnAuthSucceeded();
			
			Debug.Log("autoya initialize end.");
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