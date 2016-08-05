using UnityEngine;
using Connections.HTTP;
using System;
using System.Collections.Generic;

/**
	main behaviour implementation class of Autoya.
*/
namespace AutoyaFramework {
	/**
		all conditions which Autoya has.
	*/
	public class AutoyaConditions {
		public bool _isOnline;
		public bool _isUnderMaintenance;
		
		public bool _isLoggedIn;
		
		public string app_version;
		public string asset_version;
		
		public string _buildNumber;
	}

    public partial class Autoya {
		private AutoyaConditions _conditions;

		private Autoya () {
			Debug.Log("autoya initialize start.");
			
			_conditions = new AutoyaConditions();

			/* 
				セッティングよみ出ししちゃおう。なんか、、LocalStorageからapp_versionとかだな。Unityで起動時に上書きとかしとけば良い気がする。
				asset_versionはAssetsListに組み込まれてるんで、それを読みだして云々、っていう感じにできる。
			*/
			
			Debug.LogError("should design auth usage first.");
			// authの状態を取得する、、そのためのユーティリティは必要かなあ、、まあこのクラス内で良い気がするな。
			// ログインが終わってるかどうかっていうのでなんか判断すれば良いのではっていう。
			// ログインが成功した記録があれば、そのトークンを使って接続を試みる。
			
			
			// ログイン状態が影響するかな？
			this.InitializeAuth();

			_autoyaHttp = new HTTPConnection();

			// 必要であればこのへんでログイン処理を開始する。完了するのは非同期の向こう側。
			AttemptLogin();
			
			Debug.Log("autoya initialize end.");
		}


		public static int BuildNumber () {
			return -1;
		}
		
		/*
			authorization.
				1.get token for identification
				2.login with token
		*/
		
		private void InitializeAuth () {
			_conditions._isLoggedIn = false;

			OnAuthSucceeded = () => {
				_conditions._isLoggedIn = true;
			};

			OnAuthFailed = (conId, reason) => {
				_conditions._isLoggedIn = false;
				return false;
			};
		}

		private string LoadToken () {
			Debug.LogError("保存してあるtokenを読み出す");
			return string.Empty;
		}

		private void AttemptLogin () {
			// token should be exist inside App.
			var token = LoadToken();
			
			// attempt login with token.
			var loginUrl = AutoyaConsts.AUTH_URL_LOGIN;
			var loginHeaders = new Dictionary<string, string>();
			var loginHttp = new HTTPConnection();
			var logionConnectionId = AutoyaConsts.AUTH_CONNECTIONID_ATTEMPTLOGIN_PREFIX + Guid.NewGuid().ToString();

			loginHttp.Get(
				logionConnectionId,
				loginHeaders,
				loginUrl,
				(conId, code, responseHeaders, data) => {
					EvaluateLoginResult(conId, responseHeaders, code, data);
				},
				(conId, code, failedReason, responseHeaders) => {
					EvaluateLoginResult(conId, responseHeaders, code, failedReason);
				}
			);
		}

		private void EvaluateLoginResult (string loginConnectionId, Dictionary<string, string> responseHeaders, int responseCode, string resultDataOrFailedReason) {
			// ログイン時のresponseHeadersに入っている情報について、ここで判別前〜後に扱う必要がある。

			ErrorFlowHandling(
				loginConnectionId,
				responseHeaders, 
				responseCode,  
				resultDataOrFailedReason, 
				(succeededConId, succeededData) => {
					// if code == 401, OnAuthFailed is already called.
					if (responseCode == 401) return;
					
					/*
						we handling NOT 401 result of login act.
					*/

					// 手元のトークン上書き？
					// ログインは完了してるんで、そのまま動作できるはずっていうアレ。
					// ここをハンドラにしちゃえば良いのか。まあ確認して結果を受けて、
					OnAuthSucceeded();
				},
				(failedConId, failedCode, failedReason) => {
					// if code == 401, OnAuthFailed is already called.
					if (failedCode == 401) return;
					
					/*
						we handling NOT 401 error of login act.
					*/

					// 内容によっては、手元のトークン消したりしそう。
					// 単純な通信失敗とかなんで、ErrorFlowに乗ってない。なるほど、フローに乗っける必要がある。
					OnAuthFailed(failedConId, failedReason);
				}
			);
		}

		/*
			public auth APIs
		*/
		public static void Auth_SetOnAuthSucceeded (Action onAuthSucceeded) {
			autoya.OnAuthSucceeded = () =>{
				autoya._conditions._isLoggedIn = true;
				onAuthSucceeded();
			};

			// if already logged in, fire immediately.
			if (autoya._conditions._isLoggedIn) onAuthSucceeded();
        }

		public static void Auth_SetOnAuthFailed(Func<string, string, bool> onAuthFailed) {
            autoya.OnAuthFailed = (conId, reason) => {
				autoya._conditions._isLoggedIn = false;
				return onAuthFailed(conId, reason);
			};
        }
		
		private Action OnAuthSucceeded = () => {};

		private Func<string, string, bool> OnAuthFailed;

		public static bool Auth_IsLoggedIn () {
			return autoya._conditions._isLoggedIn;
		}


		/*
			test methods.
		*/
		public static void Auth_Test_AccidentialLogout () {
			/*
				generate fake response for generate fake accidential logout error.
			*/
			autoya.ErrorFlowHandling(
				"Auth_Test_AccidentialLogout_ConnectionId", 
				new Dictionary<string, string>(),
				401, 
				"Auth_Test_AccidentialLogout test error", 
				(conId, data) => {}, 
				(conId, code, reason) => {}
			);
		}
		
		public static void Auth_Test_AuthSuccess () {
			// tokenも保存しておかないといけないかな〜
			autoya._conditions._isLoggedIn = true;
			autoya.OnAuthSucceeded();
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