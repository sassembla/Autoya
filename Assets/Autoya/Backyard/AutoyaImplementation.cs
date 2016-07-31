using UnityEngine;
using Connection.HTTP;

/**
	main behaviour implementation class of Autoya.
*/
namespace AutoyaFramework
{
    public partial class Autoya {

		public static int BuildNumber () {
			return -1;
		}
		
	
		private Autoya () {
			Debug.Log("autoya initialize start.");
			/* 
				セッティングよみ出ししちゃおう。なんか、、LocalStorageからapp_versionとかだな。Unityで起動時に上書きとかしとけば良い気がする。
				res_versionはAssetsListに組み込まれてるんで、それを読みだして云々、っていう感じにできる。
			*/
			var baseHeader = new HTTPHeader("a", "b");
			_autoyaHttp = new HTTPConnection(baseHeader);

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