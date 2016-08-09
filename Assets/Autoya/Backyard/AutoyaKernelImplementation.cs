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
			// あれ、、試みるだけなら、token読めたらログイン完了っていう扱いでいいのでは？　って思ったけど毎回蹴られるの面倒だからやっぱ通信しておこうねっていう
			// 気持ちになった。
			
			/*
				初期化機構を起動する
			*/
			this.InitializeAuth();
			
			Debug.Log("autoya initialize end. auth started.");
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
			_loginState = LoginState.LOGGED_OUT;

			/*
				set default handlers.
			*/
			OnLoginSucceeded = () => {
				Debug.Assert(!(string.IsNullOrEmpty(_token)));
				_loginState = LoginState.LOGGED_IN;
			};

			OnLoginFailed = (conId, reason) => {
				_loginState = LoginState.LOGGED_OUT;
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
						UpdateTokenThenAttemptLogin(succeededData);
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
					var shouldRetry = OnLoginFailed(failedConId, failedReason);
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

			Observable.FromCoroutine(
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
					Debug.LogWarning("EvaluateLoginResult tokenを使ったログイン通信に成功。401チェックも突破。これでログイン動作が終わることになる。");
					OnLoginSucceeded();
				},
				(failedConId, failedCode, failedReason) => {
					// if Unauthorized, OnAuthFailed is already called.
					if (IsAuthFailed(responseCode, responseHeaders)) return;
					
					/*
						we should handling NOT 401(Unauthorized) result.
					*/

					// tokenはあったんだけど通信失敗とかで予定が狂ったケースか。
					// tokenはあるんで、エラーわけを細かくやって、なんともできなかったら再チャレンジっていうコースな気がする。
					
					var shouldRetry = OnLoginFailed(failedConId, failedReason);
					if (shouldRetry) {
						Debug.LogError("ログイン失敗、リトライすべきなんだけどちょっとまってな2");
						// LoadTokenThenLogin();
					} else {
						Debug.LogError("ログイン失敗、リトライすべきではない。失敗原因は failedReason:" + failedReason);
					}
				}
			);
		}

		private void UpdateTokenThenAttemptLogin (string newValidToken) {
			var isSaved = SaveToken(newValidToken);
			if (isSaved) {
				_token = newValidToken;
				LoadTokenThenLogin();
			} else {
				Debug.LogError("tokenのSaveに失敗、この辺、保存周りのイベントと連携させないとな〜〜〜");
			}
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
			autoya.OnLoginSucceeded = () => {
				autoya._loginState = LoginState.LOGGED_IN;
				onAuthSucceeded();
			};
			
			// if already logged in, fire immediately.
			if (Auth_IsLoggedIn()) onAuthSucceeded();
        }

		public static void Auth_SetOnAuthFailed(Func<string, string, bool> onAuthFailed) {
            autoya.OnLoginFailed = (conId, reason) => {
				autoya.RevokeToken();
				return onAuthFailed(conId, reason);
			};
        }
		
		private Action OnLoginSucceeded;

		private Func<string, string, bool> OnLoginFailed;


		
		public enum LoginState : int {
			LOGGED_OUT,
			LOGGED_IN,
		}

		private LoginState _loginState;

		public static bool Auth_IsLoggedIn () {
			if (autoya._loginState == LoginState.LOGGED_IN) return true; 
			return false;
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