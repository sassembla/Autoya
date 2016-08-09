using UnityEngine;
using Connections.HTTP;
using System;
using System.Collections.Generic;
using AutoyaFramework.Persistence;
using UniRx;


/**
	main behaviour implementation class of Autoya.
*/
namespace AutoyaFramework {
	public partial class Autoya {
		/**
			all conditions which Autoya has.
		*/
		private class AutoyaConditions {
			public bool _isOnline;
			public bool _isUnderMaintenance;
			
			public string _app_version;
			public string _asset_version;
			
			public string _buildNumber;
		}

		CompositeDisposable _frameworkDisposables = new CompositeDisposable();
		
		
		private AutoyaConditions _conditions;

		private Autoya (string basePath="") {
			Debug.Log("autoya initialize start.");
			
			_conditions = new AutoyaConditions();

			_autoyaFilePersistence = new FilePersistence(basePath);

			_autoyaHttp = new HTTPConnection();

			/* 
				セッティングよみ出ししちゃおう。なんか、、LocalStorageからapp_versionとかだな。Unityで起動時に上書きとかしとけば良い気がする。
				asset_versionはAssetsListに組み込まれてるんで、それを読みだして云々、っていう感じにできる。
			*/
			
			// authの状態を取得する、、そのためのユーティリティは必要かなあ、、まあこのクラス内で良い気がするな。
			// ログインが終わってるかどうかっていうのでなんか判断すれば良いのではっていう。
			// ログインが成功した記録があれば、そのトークンを使って接続を試みる。
			
			/*
				初期化機構を起動する
			*/
			this.InitializeAuth();
			
			Debug.Log("autoya initialize end.");
		}


		public static int BuildNumber () {
			return -1;
		}
		
		/*
			authorization.
				1.get token for identification
				2.login with token

				or 

				1.load token from app
				2.login with token
		*/
		private string _token;
		private void InitializeAuth () {
			/*
				set handlers.
			*/
			OnAuthSucceeded = token => {
				UpdateToken(token);
			};

			OnAuthFailed = (conId, reason) => {
				RevokeToken();
				return false;
			};

			LoadTokenThenLogin();
		}

		private void LoadTokenThenLogin () {
			var tokenCandidate = _autoyaFilePersistence.Load(
				AutoyaConsts.AUTH_STORED_TOKEN_DOMAIN, 
				AutoyaConsts.AUTH_STORED_TOKEN_FILENAME
			);

			/*
				if token is already stored and valid, goes to login.
				else, get token then login.
			*/
			var isValid = IsTokenValid(tokenCandidate);
			if (isValid) {
				Debug.Log("(maybe) valid token found. start login with it.");
				AttemptLoginByTokenCandidate(tokenCandidate);
			} else {
				Debug.Log("no token found. get token then login.");
				GetTokenThenLogin();
			}
		}

		private bool SaveToken (string newTokenCandidate) {
			return _autoyaFilePersistence.Update(
				AutoyaConsts.AUTH_STORED_TOKEN_DOMAIN, 
				AutoyaConsts.AUTH_STORED_TOKEN_FILENAME,
				newTokenCandidate
			);
		}

		private void GetTokenThenLogin () {
			var tokenUrl = AutoyaConsts.AUTH_URL_TOKEN;
			var tokenHttp = new HTTPConnection();
			var tokenConnectionId = AutoyaConsts.AUTH_CONNECTIONID_GETTOKEN_PREFIX + Guid.NewGuid().ToString();
			
			Observable.FromCoroutine(
				() => tokenHttp.Get(
					tokenConnectionId,
					null,
					tokenUrl,
					(conId, code, responseHeaders, data) => {
						EvaluateTokenResult(conId, responseHeaders, code, data);
					},
					(conId, code, failedReason, responseHeaders) => {
						EvaluateTokenResult(conId, responseHeaders, code, failedReason);
					}
				)
			).Timeout(
				TimeSpan.FromSeconds(AutoyaConsts.HTTP_TIMEOUT_SEC)
			).Subscribe(
				_ => {},
				ex => {
					Debug.LogError("token取得リクエストのタイムアウト、まだちゃんと作ってない。");
					EvaluateTokenResult(tokenConnectionId, new Dictionary<string, string>(), 0, ex.ToString());
				}
			).AddTo(_frameworkDisposables);;
		}

		private void EvaluateTokenResult (string tokenConnectionId, Dictionary<string, string> responseHeaders, int responseCode, string resultDataOrFailedReason) {
			// 取得したtokenを検査する必要がある。ヘッダとかで検証とかいろいろ。 検証メソッドとか外に出せばいいことあるかな。
			Debug.LogWarning("EvaluateTokenResult!! " + " tokenConnectionId:" + tokenConnectionId + " responseCode:" + responseCode + " resultDataOrFailedReason:" + resultDataOrFailedReason);

			ErrorFlowHandling(
				tokenConnectionId,
				responseHeaders, 
				responseCode,  
				resultDataOrFailedReason, 
				(succeededConId, succeededData) => {
					var isValid = IsTokenValid(succeededData);
					Debug.LogWarning("成功したぞ＝＝＝これがtokenだ〜〜 succeededData:" + succeededData + " クライアントとサーバが同じパラメータからハッシュ作れば、、とか思うがどうなんだろう。");
					
					if (isValid) {
						var isSaved = SaveToken(succeededData);
						if (isSaved) LoadTokenThenLogin();
						else Debug.LogError("取得したばっかりのtokenのSaveに失敗、この辺、保存周りのイベントと連携させないとな〜〜〜");
					}
				},
				(failedConId, failedCode, failedReason) => {
					Debug.LogError("トークン自体が取得できなかった。 failedCode:" + failedCode + " failedReason:" + failedReason);
					
					if (IsInMaintenance(responseCode, responseHeaders)) {
						// in maintenance, do nothing here.
						return;
					}

					if (IsAuthFailed(responseCode, responseHeaders)) {
						// get token url should not return unauthorized response. do nothing here.
						return;
					}

					// other errors. 
					var shouldRetry = OnAuthFailed(failedConId, failedReason);
					if (shouldRetry) {
						Debug.LogError("リトライすべきなんだけどちょっとまってな1");
						// GetTokenThenLogin();
					} 
				}
			);
		}

		private bool IsTokenValid (string tokenCandidate) {
			if (string.IsNullOrEmpty(tokenCandidate)) return false; 
			return true;
		}


		/**
			login with token candidate.
		*/
		private void AttemptLoginByTokenCandidate (string tokenCandidate) {
			// attempt login with token.
			var loginUrl = AutoyaConsts.AUTH_URL_LOGIN;
			var loginHeaders = new Dictionary<string, string>{
				{"token", tokenCandidate}
			};
			var loginHttp = new HTTPConnection();
			var loginConnectionId = AutoyaConsts.AUTH_CONNECTIONID_ATTEMPTLOGIN_PREFIX + Guid.NewGuid().ToString();

			var cancellable = Observable.FromCoroutine(
				_ => loginHttp.Get(
					loginConnectionId,
					loginHeaders,
					loginUrl,
					(conId, code, responseHeaders, data) => {
						EvaluateLoginResult(conId, responseHeaders, code, data);
					},
					(conId, code, failedReason, responseHeaders) => {
						EvaluateLoginResult(conId, responseHeaders, code, failedReason);
					}
				)
			).Timeout(
				TimeSpan.FromSeconds(AutoyaConsts.HTTP_TIMEOUT_SEC)
			).Subscribe(
				_ => {},
				ex => {
					Debug.LogError("ログインリクエストのタイムアウト、まだちゃんと作ってない。loginConnectionId:" + loginConnectionId);
					EvaluateLoginResult(loginConnectionId, new Dictionary<string, string>(), 0, ex.ToString());
				}
			).AddTo(_frameworkDisposables);
		}

		private void EvaluateLoginResult (string loginConnectionId, Dictionary<string, string> responseHeaders, int responseCode, string resultDataOrFailedReason) {
			// ログイン時のresponseHeadersに入っている情報について、ここで判別前〜後に扱う必要がある。

			ErrorFlowHandling(
				loginConnectionId,
				responseHeaders, 
				responseCode,  
				resultDataOrFailedReason, 
				(succeededConId, succeededData) => {
					Debug.LogWarning("401チェックも超えて成功したので、ヘッダかbodyか、どっかからtokenを取得する。 succeededData:" + succeededData);
					
					// ログインは完了してるんで、そのまま動作できるはずっていうアレ。
					// ここをハンドラにしちゃえば良いのか。まあ確認して結果を受けて、
					OnAuthSucceeded(succeededData);
				},
				(failedConId, failedCode, failedReason) => {
					// if Unauthorized, OnAuthFailed is already called.
					if (IsAuthFailed(responseCode, responseHeaders)) return;
					
					/*
						we should handling NOT 401(Unauthorized) result of login act.
					*/

					// tokenはあったんだけど通信失敗とかで予定が狂ったケースか。
					// tokenはあるんで、エラーわけを細かくやって、なんともできなかったら再チャレンジっていうコースな気がする。
					

					var shouldRetry = OnAuthFailed(failedConId, failedReason);
					if (shouldRetry) {
						Debug.LogError("リトライすべきなんだけどちょっとまってな2");
						// LoadTokenThenLogin();
					}
				}
			);
		}

		private void UpdateToken (string newValidToken) {
			var isSaved = SaveToken(newValidToken);
			if (isSaved) _token = newValidToken;
			else Debug.LogError("tokenのSaveに失敗、この辺、保存周りのイベントと連携させないとな〜〜〜");
		}
		private void RevokeToken () {
			_token = string.Empty;
			var isRevoked = SaveToken(string.Empty);
			if (!isRevoked) Debug.LogError("revokeに失敗、この辺、保存周りのイベントと連携させないとな〜〜〜");
		}

		/*
			public auth APIs
		*/

		public static void Auth_SetOnAuthSucceeded (Action onAuthSucceeded) {
			autoya.OnAuthSucceeded = token =>{
				autoya.UpdateToken(token);
				onAuthSucceeded();
			};

			// if already logged in, fire immediately.
			if (Auth_IsLoggedIn()) onAuthSucceeded();
        }

		public static void Auth_SetOnAuthFailed(Func<string, string, bool> onAuthFailed) {
            autoya.OnAuthFailed = (conId, reason) => {
				autoya.RevokeToken();
				return onAuthFailed(conId, reason);
			};
        }
		
		private Action<string> OnAuthSucceeded;

		private Func<string, string, bool> OnAuthFailed;

		public static bool Auth_IsLoggedIn () {
			if (string.IsNullOrEmpty(autoya._token)) return false;
			return true; 
		}

		public static void Auth_CancelLogIn () {
			if (0 < autoya._frameworkDisposables.Count) autoya._frameworkDisposables.Dispose();
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
			autoya._token = "dummy_token";
			autoya.OnAuthSucceeded(autoya._token);
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