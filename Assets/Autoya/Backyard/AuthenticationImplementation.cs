using UnityEngine;
using AutoyaFramework.Connections.HTTP;
using AutoyaFramework.Settings.Auth;
using System;
using System.Collections.Generic;
using System.Collections;


/*
	authentication impl.
*/
namespace AutoyaFramework {

    public partial class Autoya {
		private AuthRouter _autoyaAuthRouter;

		private Action onAuthenticated = () => {};

		private void Authenticate (bool isFirstBoot) {

			Action onLogonSucceeded = () => {
				onAuthenticated();
			};
			Action onLogonRetryFailed = () => {
				Debug.LogError("メンテナンス以外の理由で、初回起動 or refreshTokenに3連続で失敗した。ので、なんかお知らせを出す必要がある。「時間をあけてアクセスしてね」とかそのへん。");
			};

			_autoyaAuthRouter = new AuthRouter(
				this.mainthreadDispatcher,
				
				onLogonSucceeded,
				onLogonRetryFailed,
				
				onLogonSucceeded,
				onLogonRetryFailed,

				isFirstBoot
			);
		}

		/*
			AuthRouter:

				internal authentication state machine implementation.
					this feature controls the flow of authentication.
				
				Automatic-Authentination in Autoya is based on these scenerio: 
					"get and store token on initial boot, access with token, renew token, revoke token.".
					this is useful for player. player can play game without input anything.

					Autoya supports that basically.


					fig 1: basic [APP STATE] and (LOGIN CONDITION)

									[init-boot]		→ [authenticate]	(automatic-authentication)
															↓
									[refresh token] → [use token]		(logged-in)
											↑				↓
											∟		← [token expired]	(logged-out)
			
			ToDo:
				no authentication carryOut.
				id+pass based carryOut = logIn on another device.
				id+pass based logout(who need this?)
				
		*/
		private class AuthRouter {
			private enum AuthState {
				Booting,
				BootFailed,
				
				Logon,

				Refreshing,
				RefreshFailed
			}

			private readonly ICoroutineUpdater mainthreadDispatcher;

			private AuthState authState = AuthState.Booting;
			
			private Action onBootSucceeded;
			private Action onBootFailed;

			private Action onRefreshSucceeded;
			private Action onRefreshFailed;
			
			public AuthRouter (ICoroutineUpdater mainthreadDispatcher, Action onBootSucceeded, Action onBootFailed, Action onRefreshSucceeded, Action onRefreshFailed, bool isFirstBoot) {
				this.mainthreadDispatcher = mainthreadDispatcher;
				
				// set boot handler.
				this.onBootSucceeded = onBootSucceeded;
				this.onBootFailed = onBootFailed;

				// set refreshToken handler.
				this.onRefreshSucceeded = onRefreshSucceeded;
				this.onRefreshFailed = onRefreshFailed;

				if (!isFirstBoot) {
					authState = AuthState.Logon;
					onBootSucceeded();
					return;
				}
				
				// start first boot.
				FirstBoot();
			}
			
			public bool IsLogon () {
				return authState == AuthState.Logon;
			}

			private void FirstBoot () {
				var tokenHttp = new HTTPConnection();
				var tokenConnectionId = AuthSettings.AUTH_CONNECTIONID_BOOT_PREFIX + Guid.NewGuid().ToString();
				
				var tokenUrl = AuthSettings.AUTH_URL_BOOT;
				var bootData = 1048524945039600.ToString();
				
				var key = Autoya.OnBootAuthRequested();

				var tokenRequestHeaders = new Dictionary<string, string>{
					{"Authorization", key}
				};

				mainthreadDispatcher.Commit(
					tokenHttp.Post(
						tokenConnectionId,
						tokenRequestHeaders,
						tokenUrl,
						bootData,
						(conId, code, responseHeaders, data) => {
							OnBootResult(conId, responseHeaders, code, data, string.Empty);
						},
						(conId, code, failedReason, responseHeaders) => {
							OnBootResult(conId, responseHeaders, code, string.Empty, failedReason);
						},
						BackyardSettings.HTTP_TIMEOUT_SEC
					)
				);
			}

			/**
				boot処理完了時のハンドル、完全に独自の処理でいいんだと思う。
			*/
			private void OnBootResult (string tokenConnectionId, Dictionary<string, string> responseHeaders, int responseCode, string resultData, string errorReason) {
				autoya.HttpResponseHandling(
					tokenConnectionId,
					responseHeaders, 
					responseCode,  
					resultData, 
					errorReason,
					(succeededConId, succeededData) => {
						var tokenData = succeededData as string;
						
						autoya.OnBootReceived(responseHeaders, tokenData);

						// reset retry count.
						bootRetryCount = 0;
						
						authState = AuthState.Logon;
						onBootSucceeded();
					},
					(failedConId, failedCode, failedReason) => {
						/*
							maintenance or auth failed is already handled.
						*/
						if (autoya.IsMaintenance(responseCode, responseHeaders) || autoya.IsAuthFailed(responseCode, responseHeaders)) {
							return;
						}

						// other errors. 

						// reached to the max retry for boot access.
						if (bootRetryCount == AuthSettings.AUTH_FIRSTBOOT_MAX_RETRY_COUNT) {
							authState = AuthState.BootFailed;
							onBootFailed();
							return;
						}

						bootRetryCount++;
						mainthreadDispatcher.Commit(BootRetryCoroutine());
					}
				);
			}

			private int bootRetryCount = 0;

			private IEnumerator BootRetryCoroutine () {
				// wait 2, 4, 8... sec.
				var bootRetryWait = Math.Pow(2, bootRetryCount);
				var limitTick = DateTime.Now.Ticks + TimeSpan.FromSeconds(bootRetryWait).Ticks;

				while (DateTime.Now.Ticks < limitTick) {
					yield return null;
				}
				
				// start boot authentication again.
				var tokenHttp = new HTTPConnection();
				var tokenConnectionId = AuthSettings.AUTH_CONNECTIONID_BOOT_PREFIX + Guid.NewGuid().ToString();
				
				var tokenUrl = AuthSettings.AUTH_URL_BOOT;
				var bootData = 1048524945039600.ToString();
				
				var key = Autoya.OnBootAuthRequested();

				var tokenRequestHeaders = new Dictionary<string, string>{
					{"Authorization", key}
				};

				var cor = tokenHttp.Post(
					tokenConnectionId,
					tokenRequestHeaders,
					tokenUrl,
					bootData,
					(conId, code, responseHeaders, data) => {
						OnBootResult(conId, responseHeaders, code, data, string.Empty);
					},
					(conId, code, failedReason, responseHeaders) => {
						OnBootResult(conId, responseHeaders, code, string.Empty, failedReason);
					},
					BackyardSettings.HTTP_TIMEOUT_SEC
				);

				while (cor.MoveNext()) {
					yield return null;
				}
			}

			
			/**
				token has been expired. start refresh token if need.
			*/
			public void Expired () {
				switch (authState) {
					case AuthState.Refreshing: {
						// now refreshing. ignore.
						break;
					}
					case AuthState.Logon:
					case AuthState.RefreshFailed: {
						RefreshToken();
						break;
					}
					default: {
						// ignore.
						break;
					}
				}
			}

			private void RefreshToken () {
				authState = AuthState.Refreshing;

				// start refreshing token.
				var refresingTokenHttp = new HTTPConnection();
				var refreshTokenUrl = AuthSettings.AUTH_URL_REFRESH_TOKEN;
				
				var refreshTokenData = 28245896454354.ToString();

				var refreshTokenConnectionId = AuthSettings.AUTH_CONNECTIONID_REFRESH_TOKEN_PREFIX + Guid.NewGuid().ToString();
				
				var authStr = autoya.OnTokenRefreshRequested();
				
				var tokenRequestHeaders = new Dictionary<string, string>{
					{"Authorization", authStr}
				};
				
				mainthreadDispatcher.Commit(
					refresingTokenHttp.Post(
						refreshTokenConnectionId,
						tokenRequestHeaders,
						refreshTokenUrl,
						refreshTokenData,
						(conId, code, responseHeaders, data) => {
							OnRefreshResult(conId, responseHeaders, code, data, string.Empty);
						},
						(conId, code, failedReason, responseHeaders) => {
							OnRefreshResult(conId, responseHeaders, code, string.Empty, failedReason);
						},
						BackyardSettings.HTTP_TIMEOUT_SEC
					)
				);
			}

			private void OnRefreshResult (string tokenConnectionId, Dictionary<string, string> responseHeaders, int responseCode, string resultData, string errorReason) {
				autoya.HttpResponseHandling(
					tokenConnectionId,
					responseHeaders, 
					responseCode,  
					resultData, 
					errorReason,
					(succeededConId, succeededData) => {
						var tokenData = succeededData as string;
						
						autoya.OnTokenRefreshReceived(responseHeaders, tokenData);
						
						// reset retry count.
						tokenRefreshRetryCount = 0;

						authState = AuthState.Logon;
						onRefreshSucceeded();
					},
					(failedConId, failedCode, failedReason) => {
						/*
							maintenance or auth failed is already handled.
						*/
						if (autoya.IsMaintenance(responseCode, responseHeaders) || autoya.IsAuthFailed(responseCode, responseHeaders)) {
							/*
								すっげー考えるの難しいんですけど、
								ここでmaintenanceが出ると、メンテ画面が表示される。
								で、onRefreshFailedは呼ばれない。

								maintenanceが終わらないとrefreshもなにも無いので。

								一方、
								もしもメンテ時に、tokenRefreshの通信が認証サービスのサーバまで到達し、tokenRefreshの処理ができてしまうと、サーバ内でこちらから送付しているrefreshTokenが消えてしまう。
								するとこのクライアントは二度とrefreshTokenでログイン(新しいaccessTokenの取得とか)ができなくなる。
								これは、id/passを入れていないと、詰む、ということを意味する。(お問い合わせコースに乗る。)

								ということで、メンテ中の場合、サーバでtokenRefreshの通信が来ても処理しない
								&&
								サーバ側は認証よりも必ず「前」の地点にmaintenanceを置いておき、maintenance処理をオンにしたらサーバに到達できない、という
								状態を作らないといけない。

								また、メンテ突入の瞬間にtokenRefreshしたりする人がいると、これまたその人は詰みそうなので、なにかしら時差をもたせてあげる必要がある。
								・expire処理を停止する
								・その後、maintenanceモード入り
								・expire処理を再開
								・maintenanceモード解除
								とか。
							*/
							authState = AuthState.RefreshFailed;
							return;
						}

						// other errors. 

						// reached to the max retry for token refresh access.
						if (tokenRefreshRetryCount == AuthSettings.AUTH_TOKENREFRESH_MAX_RETRY_COUNT) {
							authState = AuthState.RefreshFailed;
							onRefreshFailed();
							return;
						}

						// retry.
						tokenRefreshRetryCount++;
						mainthreadDispatcher.Commit(TokenRefreshRetryCoroutine());
					}
				);
			}

			private int tokenRefreshRetryCount = 0;

			private IEnumerator TokenRefreshRetryCoroutine () {
				// wait 2, 4, 8... sec.
				var refreshTokenRetryWait = Math.Pow(2, tokenRefreshRetryCount);
				var limitTick = DateTime.Now.Ticks + TimeSpan.FromSeconds(refreshTokenRetryWait).Ticks;

				while (DateTime.Now.Ticks < limitTick) {
					yield return null;
				}
				
				// start refreshing token again.
				var refresingTokenHttp = new HTTPConnection();
				var refreshTokenUrl = AuthSettings.AUTH_URL_REFRESH_TOKEN;
				
				var refreshTokenData = 28245896454354.ToString();

				var refreshTokenConnectionId = AuthSettings.AUTH_CONNECTIONID_REFRESH_TOKEN_PREFIX + Guid.NewGuid().ToString();
				
				var authStr = autoya.OnTokenRefreshRequested();
				
				var tokenRequestHeaders = new Dictionary<string, string>{
					{"Authorization", authStr}
				};
				
				var cor = refresingTokenHttp.Post(
					refreshTokenConnectionId,
					tokenRequestHeaders,
					refreshTokenUrl,
					refreshTokenData,
					(conId, code, responseHeaders, data) => {
						OnRefreshResult(conId, responseHeaders, code, data, string.Empty);
					},
					(conId, code, failedReason, responseHeaders) => {
						OnRefreshResult(conId, responseHeaders, code, string.Empty, failedReason);
					},
					BackyardSettings.HTTP_TIMEOUT_SEC
				);

				while (cor.MoveNext()) {
					yield return null;
				}
			}

			/**
				attpemt to retry authentication flow.

				this method is useful in these cases.
					When first boot was failed and player saw that information, then player pushed to retry.
						or
					When token refreshing was failed and player saw that information, then player pushed to retry.
			*/
			public void RetryAuthentication () {
				if (IsLogon()) {
					// no fail detected. do nothing.
					return;
				}

				switch (authState) {
					case AuthState.BootFailed: {
						FirstBoot();
						break;
					}
					case AuthState.RefreshFailed: {
						RefreshToken();
						break;
					}
					default: {
						// do nothing.
						break;
					}
				}
			}
		}



		/**
			Autoyaのhttpエラーハンドリングのコアメソッド。


			共通処理、こいつがAPIレイヤにあるほうが好ましい。
			HttpResultHandler

			・Unityの返してくるhttpコードを処理し、failedを着火する。
				offlineとかそのへん。
			
			・メンテナンスなどのチェックを行い、メンテモードの通知をする。
				その後failedを着火する。

			・401のチェックを行い、tokenRefreshを行う。
				その後failedを着火する。

			・httpコードのチェックを行い、200系でなければfailedを着火する

			・200系であればsucceededを着火する。
		*/
		private void HttpResponseHandling (string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string> failed) {
			/*
				handle Autoya internal error.
			*/
			if (httpCode < 0) {
				var internalErrorMessage = errorReason;
				failed(connectionId, httpCode, internalErrorMessage);
				return;
			}

			/*
				UnityWebRequest handled internal error.
			*/
			if (httpCode == 0) {
				Debug.LogError("httpCode = 0, misc errors. errorReason:" + errorReason);
				var troubleMessage = errorReason;
				failed(connectionId, httpCode, troubleMessage);
				return;
			}
			
			/*
				fall-through handling area of Autoya's events.

				this block NEVER fire succeeded/failed handler and never return controls from this block.
				these events are possibly overlap.
			*/
			{
				/*
					detect maintenance from response code or response header value.
				*/
				CheckMaintenance(httpCode, responseHeaders);

				/*
					detect unauthorized from response code or response header value.
				*/
				CheckAuthFailed(httpCode, responseHeaders);
			}
			
			// Debug.LogError("connectionId:" + connectionId + " httpCode:" + httpCode + " data:" + data);
			
			/*
				pit falls for not 2XX.
			*/
			{
				if (httpCode < 200) {
					failed(connectionId, httpCode, errorReason);
					return;
				}

				if (299 < httpCode) {
					failed(connectionId, httpCode, errorReason);
					return; 
				}
			}

			/*
				finally, connection is done as succeeded.
			*/
			succeeded(connectionId, data);
		}

		
		/**
			received 401 response code from server.
			should authenticate again.
		*/
		private void CheckAuthFailed (int httpCode, Dictionary<string, string> responseHeaders) {
			if (IsAuthFailed(httpCode, responseHeaders)) {
				_autoyaAuthRouter.Expired();
			}
		}

		private bool IsAuthFailed (int httpCode, Dictionary<string, string> responseHeaders) {
			return httpCode == 401;
		}

		/*
			public authenticate APIs
		*/
		
		public static void Auth_SetOnAuthenticated (Action authenticated=null) {
			if (Auth_IsAuthenticated()) {
				if (authenticated != null) {
					authenticated();
				}
			}

			if (authenticated != null) {
				autoya.onAuthenticated = authenticated;
			}
		}

		public static bool Auth_IsAuthenticated () {
			return autoya._autoyaAuthRouter.IsLogon();
		}

		public static void Auth_AttemptAuthentication () {
			autoya._autoyaAuthRouter.RetryAuthentication();
		}

		public static void Auth_Logout () {
			Debug.LogError("ログアウト、うーん、、必要？ tokenを消す処理とかをすることはできるんだけど、テスト以外でログアウトしたい、というニーズが存在しない気がする。");
		}
		
		/*
			test methods.
		*/
		public static void Auth_Test_CreateAuthError () {
			/*
				generate fake response for generate fake accidential logout error.
			*/
			autoya.HttpResponseHandling(
				"Auth_Test_AccidentialLogout_ConnectionId", 
				new Dictionary<string, string>(),
				401, 
				string.Empty,
				"Auth_Test_AccidentialLogout test error", 
				(conId, data) => {}, 
				(conId, code, reason) => {}
			);
		}
		
	}
}