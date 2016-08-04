using UnityEngine;
using Connection.HTTP;
using System;

/**
	main behaviour implementation class of Autoya.
*/
namespace AutoyaFramework {
    public partial class Autoya {

		public static int BuildNumber () {
			return -1;
		}
		
		public static void SetOnAuthFailed(Func<string, string, bool> onAuthFailed) {
            if (onAuthFailed != null) {
				OnAuthFailed = (conId, reason) => onAuthFailed(conId, reason);
			}
        }
		
		private static Func<string, string, bool> OnAuthFailed;
		
	
		private Autoya () {
			Debug.Log("autoya initialize start.");
			/* 
				セッティングよみ出ししちゃおう。なんか、、LocalStorageからapp_versionとかだな。Unityで起動時に上書きとかしとけば良い気がする。
				res_versionはAssetsListに組み込まれてるんで、それを読みだして云々、っていう感じにできる。
			*/
			
			// authの状態を取得する、、そのためのユーティリティは必要かなあ、、まあこのクラス内で良い気がするな。
			

			Debug.LogError("ダミーのヘッダが入ってる");
			var baseHeader = new HTTPHeader("a", "b");
			_autoyaHttp = new HTTPConnection(baseHeader);

			// 必要であればこのへんでログインを実行する

			

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