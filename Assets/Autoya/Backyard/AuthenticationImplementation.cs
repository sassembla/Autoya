using UnityEngine;
using AutoyaFramework.Connections.HTTP;
using AutoyaFramework.Settings.Auth;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using AutoyaFramework.Settings.AssetBundles;
using System.Collections.Specialized;


/*
	authentication impl.
*/
namespace AutoyaFramework
{
    public partial class Autoya
    {
        /*
			delegate for supply http request header generate func for modules.
		*/
        public delegate Dictionary<string, string> HttpRequestHeaderDelegate(string method, string url, Dictionary<string, string> requestHeader, object data);

        /*
			delegate for handle http response for modules.
		*/
        public delegate void HttpResponseHandlingDelegate(string connectionId, Dictionary<string, string> responseHeader, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed);

        /*
			delegate for supply assetBundleList get request header generate func for modules.
		*/
        public delegate Dictionary<string, string> AssetBundleListGetRequestHeaderDelegate(string url, Dictionary<string, string> requestHeader);
        /*
			delegate for supply assetBundlePreloadList get request header generate func for modules.
		*/
        public delegate Dictionary<string, string> AssetBundlePreloadListGetRequestHeaderDelegate(string url, Dictionary<string, string> requestHeader);
        /*
			delegate for supply assetBundle get request header generate func for modules.
		*/
        public delegate Dictionary<string, string> AssetBundleGetRequestHeaderDelegate(string url, Dictionary<string, string> requestHeader);
        /*
            delegate for supply endPoint get request header generate func for modules.
        */
        public delegate Dictionary<string, string> EndPointGetRequestHeaderDelegate(string url, Dictionary<string, string> requestHeader);


        /*
            delegates
        */

        // request header handling.
        private HttpRequestHeaderDelegate httpRequestHeaderDelegate;

        // response handling.
        private HttpResponseHandlingDelegate httpResponseHandlingDelegate;



        private AuthRouter _autoyaAuthRouter;

        private Action onAuthenticated = () => { };
        private Action<int, string> onBootAuthFailed = (code, reason) => { };
        private Action<int, string> onRefreshAuthFailed = (code, reason) => { };

        private void Authenticate(bool isFirstBoot, Action internalOnAuthenticated)
        {
            // setup request headers handling.
            this.httpRequestHeaderDelegate = OnHttpRequest;

            Action onAuthSucceeded = () =>
            {
                internalOnAuthenticated();
                onAuthenticated();
            };
            Action<int, string> onBootAuthenticationRetryFailed = (code, reason) =>
            {
                onBootAuthFailed(code, reason);
            };
            Action<int, string> onRefreshAuthenticationRetryFailed = (code, reason) =>
            {
                onRefreshAuthFailed(code, reason);
            };

            _autoyaAuthRouter = new AuthRouter(
                this.mainthreadDispatcher,

                onAuthSucceeded,
                onBootAuthenticationRetryFailed,

                onAuthSucceeded,
                onRefreshAuthenticationRetryFailed,

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
        private class AuthRouter
        {
            private enum AuthState
            {
                None,

                Booting,
                BootFailed,

                Logon,
                Logout,

                Refreshing,
                RefreshFailed
            }

            private readonly ICoroutineUpdater mainthreadDispatcher;

            private AuthState authState = AuthState.None;

            private Action onBootSucceeded;
            private Action<int, string> onBootFailed;

            private Action onRefreshSucceeded;
            private Action<int, string> onRefreshFailed;

            public AuthRouter(ICoroutineUpdater mainthreadDispatcher, Action onBootSucceeded, Action<int, string> onBootFailed, Action onRefreshSucceeded, Action<int, string> onRefreshFailed, bool isFirstBoot)
            {
                this.mainthreadDispatcher = mainthreadDispatcher;

                // set boot handler.
                this.onBootSucceeded = onBootSucceeded;
                this.onBootFailed = onBootFailed;

                // set refreshToken handler.
                this.onRefreshSucceeded = onRefreshSucceeded;
                this.onRefreshFailed = onRefreshFailed;

                if (!isFirstBoot)
                {
                    authState = AuthState.Logon;
                    onBootSucceeded();
                    return;
                }

                // start first boot.
                authState = AuthState.Booting;
                mainthreadDispatcher.Commit(FirstBoot());
            }

            public bool IsLogon()
            {
                return authState == AuthState.Logon;
            }

            public bool IsBootAuthFailed()
            {
                return authState == AuthState.BootFailed;
            }

            public bool IsTokenRefreshFailed()
            {
                return authState == AuthState.RefreshFailed;
            }

            private IEnumerator FirstBoot()
            {
                var tokenHttp = new HTTPConnection();
                var tokenConnectionId = AuthSettings.AUTH_CONNECTIONID_BOOT_PREFIX + Guid.NewGuid().ToString();

                var tokenUrl = AuthSettings.AUTH_URL_BOOT;

                Dictionary<string, string> tokenRequestHeader = null;
                var bootData = string.Empty;
                Action<Dictionary<string, string>, string> authRequestHeaderLoaded = (requestHeader, data) =>
                {
                    tokenRequestHeader = requestHeader;
                    bootData = data;
                };

                // get boot auth request parameter or skipped by developer.
                var skipped = false;
                var bootKeyLoadCor = autoya.OnBootAuthRequest(
                    authRequestHeaderLoaded,
                    () =>
                    {
                        skipped = true;
                    }
                );

                while (bootKeyLoadCor.MoveNext())
                {
                    yield return null;
                }

                if (skipped)
                {
                    // set as suceeded without request.
                    authState = AuthState.Logon;
                    onBootSucceeded();
                    yield break;
                }

                if (string.IsNullOrEmpty(bootData))
                {
                    var cor = tokenHttp.Get(
                        tokenConnectionId,
                        tokenRequestHeader,
                        tokenUrl,
                        (conId, code, responseHeader, data) =>
                        {
                            OnBootResult(conId, responseHeader, code, data, string.Empty);
                        },
                        (conId, code, failedReason, responseHeader) =>
                        {
                            OnBootResult(conId, responseHeader, code, string.Empty, failedReason);
                        },
                        BackyardSettings.HTTP_TIMEOUT_SEC
                    );

                    while (cor.MoveNext())
                    {
                        yield return null;
                    }
                }
                else
                {
                    var cor = tokenHttp.Post(
                        tokenConnectionId,
                        tokenRequestHeader,
                        tokenUrl,
                        bootData,
                        (conId, code, responseHeader, data) =>
                        {
                            OnBootResult(conId, responseHeader, code, data, string.Empty);
                        },
                        (conId, code, failedReason, responseHeader) =>
                        {
                            OnBootResult(conId, responseHeader, code, string.Empty, failedReason);
                        },
                        BackyardSettings.HTTP_TIMEOUT_SEC
                    );

                    while (cor.MoveNext())
                    {
                        yield return null;
                    }

                }
            }

            /**
				http response rooting of boot.
			*/
            private void OnBootResult(string tokenConnectionId, Dictionary<string, string> responseHeader, int responseCode, string resultData, string errorReason)
            {

                if (forceFailFirstBoot)
                {
                    responseCode = AuthSettings.FORCE_FAIL_FIRSTBOOT_CODE;
                    errorReason = "failed by forceFailFirstBoot = true.";
                }

                autoya.HttpResponseHandling(
                    tokenConnectionId,
                    responseHeader,
                    responseCode,
                    resultData,
                    errorReason,
                    (succeededConId, succeededData) =>
                    {
                        var tokenData = succeededData as string;

                        mainthreadDispatcher.Commit(OnBootRequestSucceeded(responseHeader, tokenData));
                    },
                    (failedConId, failedCode, failedReason, autoyaStatus) =>
                    {
                        /*
							maintenance or auth failed is already handled.
						*/
                        if (autoyaStatus.isAuthFailed)
                        {
                            return;
                        }

                        // other errors. 

                        // reached to the max retry for boot access.
                        if (bootRetryCount == AuthSettings.AUTH_FIRSTBOOT_MAX_RETRY_COUNT)
                        {
                            authState = AuthState.BootFailed;
                            onBootFailed(failedCode, failedReason);
                            return;
                        }

                        bootRetryCount++;
                        mainthreadDispatcher.Commit(BootRetryCoroutine());
                    }
                );
            }

            private IEnumerator OnBootRequestSucceeded(Dictionary<string, string> responseHeader, string tokenData)
            {
                var failed = false;
                var code = 0;
                var reason = string.Empty;
                var cor = autoya.OnBootAuthResponse(
                    responseHeader,
                    tokenData,
                    (_code, _reason) =>
                    {
                        failed = true;
                        code = _code;
                        reason = _reason;
                    }
                );
                while (cor.MoveNext())
                {
                    yield return null;
                }

                // reset retry count.
                bootRetryCount = 0;

                if (failed)
                {
                    authState = AuthState.BootFailed;
                    onBootFailed(code, reason);
                    yield break;
                }

                authState = AuthState.Logon;
                onBootSucceeded();
            }

            private int bootRetryCount = 0;

            private IEnumerator BootRetryCoroutine()
            {
                // wait 2, 4, 8... sec.
                var bootRetryWait = Math.Pow(2, bootRetryCount);

                if (forceFailFirstBoot)
                {
                    // set 0sec for shortcut.
                    bootRetryWait = 0;
                }

                var limitTick = DateTime.Now.Ticks + TimeSpan.FromSeconds(bootRetryWait).Ticks;

                while (DateTime.Now.Ticks < limitTick)
                {
                    yield return null;
                }

                // start boot authentication again.
                var tokenHttp = new HTTPConnection();
                var tokenConnectionId = AuthSettings.AUTH_CONNECTIONID_BOOT_PREFIX + Guid.NewGuid().ToString();

                var tokenUrl = AuthSettings.AUTH_URL_BOOT;

                Dictionary<string, string> tokenRequestHeader = null;
                var bootData = string.Empty;
                Action<Dictionary<string, string>, string> authRequestHeaderLoaded = (requestHeader, data) =>
                {
                    tokenRequestHeader = requestHeader;
                    bootData = data;
                };

                // get boot auth request parameter or skipped by developer.
                var skipped = false;
                var bootKeyLoadCor = autoya.OnBootAuthRequest(
                    authRequestHeaderLoaded,
                    () =>
                    {
                        skipped = true;
                    }
                );

                while (bootKeyLoadCor.MoveNext())
                {
                    yield return null;
                }

                if (skipped)
                {
                    // set as suceeded without request.
                    authState = AuthState.Logon;
                    onBootSucceeded();
                    yield break;
                }

                if (string.IsNullOrEmpty(bootData))
                {
                    var cor = tokenHttp.Get(
                        tokenConnectionId,
                        tokenRequestHeader,
                        tokenUrl,
                        (conId, code, responseHeader, data) =>
                        {
                            OnBootResult(conId, responseHeader, code, data, string.Empty);
                        },
                        (conId, code, failedReason, responseHeader) =>
                        {
                            OnBootResult(conId, responseHeader, code, string.Empty, failedReason);
                        },
                        BackyardSettings.HTTP_TIMEOUT_SEC
                    );

                    while (cor.MoveNext())
                    {
                        yield return null;
                    }
                }
                else
                {
                    var cor = tokenHttp.Post(
                        tokenConnectionId,
                        tokenRequestHeader,
                        tokenUrl,
                        bootData,
                        (conId, code, responseHeader, data) =>
                        {
                            OnBootResult(conId, responseHeader, code, data, string.Empty);
                        },
                        (conId, code, failedReason, responseHeader) =>
                        {
                            OnBootResult(conId, responseHeader, code, string.Empty, failedReason);
                        },
                        BackyardSettings.HTTP_TIMEOUT_SEC
                    );

                    while (cor.MoveNext())
                    {
                        yield return null;
                    }
                }
            }


            /**
				logout.
			*/
            public void Logout(Action succeeded, Action<string> failed)
            {
                Action onLogoutSucceeded = () =>
                {
                    authState = AuthState.Logout;
                    succeeded();
                };

                Autoya.Mainthread_Commit(autoya.OnLogout(onLogoutSucceeded, failed));
            }

            /**
				token has been expired. start refresh token if need.
			*/
            public void Expired()
            {
                switch (authState)
                {
                    case AuthState.Refreshing:
                        {
                            // now refreshing. ignore.
                            break;
                        }
                    case AuthState.Logon:
                    case AuthState.RefreshFailed:
                        {
                            authState = AuthState.Refreshing;

                            // start refreshing token.
                            tokenRefreshRetryCount = 0;
                            mainthreadDispatcher.Commit(RequestRefreshToken());
                            break;
                        }
                    default:
                        {
                            // ignore.
                            break;
                        }
                }
            }


            private IEnumerator RequestRefreshToken()
            {
                var refreshingTokenHttp = new HTTPConnection();
                var refreshTokenUrl = AuthSettings.AUTH_URL_REFRESH_TOKEN;

                var refreshTokenConnectionId = AuthSettings.AUTH_CONNECTIONID_REFRESH_TOKEN_PREFIX + Guid.NewGuid().ToString();


                var requestMethod = "GET";
                Dictionary<string, string> refreshRequestHeader = null;
                object refreshTokenData = null;

                Action<string, Dictionary<string, string>, object> refreshRequestHeaderLoaded = (method, requestHeader, data) =>
                {
                    Debug.Assert(method.ToUpper() == "GET" || method.ToUpper() == "POST", "refresh token request method must be GET or POST. incoming value is:" + method);
                    requestMethod = method;

                    refreshRequestHeader = requestHeader;

                    Debug.Assert(data is byte[] || data is string, "refresh token request payload should be byte[] or string. incoming type is:" + data.GetType());
                    Debug.Assert(data != null, "refresh token request payload must not be null, set byte[0] or string.Empty instead null. incoming value is null of:" + data.GetType());
                    refreshTokenData = data;
                };

                var authKeyLoadCor = autoya.OnTokenRefreshRequest(refreshRequestHeaderLoaded);
                while (authKeyLoadCor.MoveNext())
                {
                    yield return null;
                }

                // request value is string. request with string then expects response is also string.
                if (refreshTokenData is string)
                {
                    switch (requestMethod)
                    {
                        case "GET":
                            {
                                var cor = refreshingTokenHttp.Get(
                                    refreshTokenConnectionId,
                                    refreshRequestHeader,
                                    refreshTokenUrl,
                                    (conId, code, responseHeader, data) =>
                                    {
                                        OnRefreshResult(conId, responseHeader, code, data, string.Empty);
                                    },
                                    (conId, code, failedReason, responseHeader) =>
                                    {
                                        OnRefreshResult(conId, responseHeader, code, string.Empty, failedReason);
                                    },
                                    BackyardSettings.HTTP_TIMEOUT_SEC
                                );

                                while (cor.MoveNext())
                                {
                                    yield return null;
                                }

                                break;
                            }
                        case "POST":
                            {
                                var cor = refreshingTokenHttp.Post(
                                    refreshTokenConnectionId,
                                    refreshRequestHeader,
                                    refreshTokenUrl,
                                    refreshTokenData as string,
                                    (conId, code, responseHeader, data) =>
                                    {
                                        OnRefreshResult(conId, responseHeader, code, data, string.Empty);
                                    },
                                    (conId, code, failedReason, responseHeader) =>
                                    {
                                        OnRefreshResult(conId, responseHeader, code, string.Empty, failedReason);
                                    },
                                    BackyardSettings.HTTP_TIMEOUT_SEC
                                );

                                while (cor.MoveNext())
                                {
                                    yield return null;
                                }
                                break;
                            }
                        default:
                            Debug.LogError("unsupported refresh token request method:" + requestMethod);
                            break;
                    }

                    yield break;
                }

                // request value is byte[] then expects response is also byte[].
                switch (requestMethod)
                {
                    case "GET":
                        {
                            var cor = refreshingTokenHttp.GetByBytes(
                                refreshTokenConnectionId,
                                refreshRequestHeader,
                                refreshTokenUrl,
                                (conId, code, responseHeader, data) =>
                                {
                                    OnRefreshResult(conId, responseHeader, code, data, string.Empty);
                                },
                                (conId, code, failedReason, responseHeader) =>
                                {
                                    OnRefreshResult(conId, responseHeader, code, new byte[0], failedReason);
                                },
                                BackyardSettings.HTTP_TIMEOUT_SEC
                            );

                            while (cor.MoveNext())
                            {
                                yield return null;
                            }

                            break;
                        }
                    case "POST":
                        {
                            var cor = refreshingTokenHttp.Post(
                                refreshTokenConnectionId,
                                refreshRequestHeader,
                                refreshTokenUrl,
                                (byte[])refreshTokenData,
                                (conId, code, responseHeader, data) =>
                                {
                                    OnRefreshResult(conId, responseHeader, code, data, string.Empty);
                                },
                                (conId, code, failedReason, responseHeader) =>
                                {
                                    OnRefreshResult(conId, responseHeader, code, new byte[0], failedReason);
                                },
                                BackyardSettings.HTTP_TIMEOUT_SEC
                            );

                            while (cor.MoveNext())
                            {
                                yield return null;
                            }
                            break;
                        }
                    default:
                        Debug.LogError("unsupported refresh token request method:" + requestMethod);
                        break;
                }
            }

            private void OnRefreshResult(string tokenConnectionId, Dictionary<string, string> responseHeader, int responseCode, object resultData, string errorReason)
            {
                if (forceFailTokenRefresh)
                {
                    responseCode = 500;
                    errorReason = "failed by forceFailTokenRefresh = true.";
                }

                autoya.HttpResponseHandling(
                    tokenConnectionId,
                    responseHeader,
                    responseCode,
                    resultData,
                    errorReason,
                    (succeededConId, succeededData) =>
                    {
                        mainthreadDispatcher.Commit(OnTokenRefreshSucceeded(responseHeader, succeededData));
                    },
                    (failedConId, failedCode, failedReason, autoyaStatus) =>
                    {
                        /*
							maintenance or auth failed is already handled.
						*/
                        if (autoyaStatus.inMaintenance || autoyaStatus.isAuthFailed)
                        {
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
                        if (tokenRefreshRetryCount == AuthSettings.AUTH_TOKENREFRESH_MAX_RETRY_COUNT)
                        {
                            authState = AuthState.RefreshFailed;
                            onRefreshFailed(failedCode, failedReason);
                            return;
                        }

                        // retry.
                        tokenRefreshRetryCount++;
                        mainthreadDispatcher.Commit(TokenRefreshRetryCoroutine());
                    }
                );
            }

            private IEnumerator OnTokenRefreshSucceeded(Dictionary<string, string> responseHeader, object tokenData)
            {
                var failed = false;
                var code = 0;
                var reason = string.Empty;
                var cor = autoya.OnTokenRefreshResponse(
                    responseHeader,
                    tokenData,
                    (_code, _reason) =>
                    {
                        failed = true;
                        code = _code;
                        reason = _reason;
                    }
                );

                while (cor.MoveNext())
                {
                    yield return null;
                }

                // reset retry count.
                tokenRefreshRetryCount = 0;

                if (failed)
                {
                    authState = AuthState.RefreshFailed;
                    onRefreshFailed(code, reason);
                }

                authState = AuthState.Logon;
                onRefreshSucceeded();
            }

            private int tokenRefreshRetryCount = 0;

            private IEnumerator TokenRefreshRetryCoroutine()
            {
                // wait 2, 4, 8... sec.
                var refreshTokenRetryWait = Math.Pow(2, tokenRefreshRetryCount);

                if (forceFailTokenRefresh)
                {
                    // set 0sec for shortcut.
                    refreshTokenRetryWait = 0;
                }

                var limitTick = DateTime.Now.Ticks + TimeSpan.FromSeconds(refreshTokenRetryWait).Ticks;

                while (DateTime.Now.Ticks < limitTick)
                {
                    yield return null;
                }

                // start refreshing token again.
                var cor = RequestRefreshToken();
                while (cor.MoveNext())
                {
                    yield return null;
                }
            }

            /**
				attpemt to restart authentication flow.

				this method is useful in these cases.
					When first boot was failed and player saw that information, then player pushed to retry.
						or
					When token refreshing was failed and player saw that information, then player pushed to retry.
			*/
            public bool RetryAuthentication()
            {
                if (IsLogon())
                {
                    // no fail detected. do nothing.
                    return false;
                }

                switch (authState)
                {
                    case AuthState.Logout:
                    case AuthState.BootFailed:
                        {
                            authState = AuthState.Booting;
                            mainthreadDispatcher.Commit(FirstBoot());
                            return true;

                        }
                    case AuthState.RefreshFailed:
                        {
                            authState = AuthState.Refreshing;

                            // start refreshing token again.
                            mainthreadDispatcher.Commit(RequestRefreshToken());
                            return true;
                        }
                    default:
                        {
                            // do nothing.
                            return false;
                        }
                }
            }
        }

        /**
			Autoyaのhttpエラーハンドリングのコアメソッド。

			HttpResponseHandling

			・Unityの返してくるhttpコードを処理し、failedを着火する。
				offlineとかそのへん。
			
			・メンテナンスなどのチェックを行い、メンテモードの通知をする。
				その後failedを着火する。

			・401のチェックを行い、tokenRefreshを行う。
				その後failedを着火する。

			・httpコードのチェックを行い、200系でなければfailedを着火する

			・200系であればsucceededを着火する。

			・AssetBundleListのダウンロードを行なった後、resversionが含まれたレスポンスが来た場合、追加でリストの取得処理を行うかどうか判断する。
		*/
        private void HttpResponseHandling(string connectionId, Dictionary<string, string> responseHeader, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed)
        {
            if (forceFailHttp)
            {
                failed(connectionId, httpCode, "intentional http failure.", new AutoyaStatus());
                return;
            }

            if (forceSetHttpCodeAsUnauthorized)
            {
                httpCode = AuthSettings.AUTH_HTTP_CODE_UNAUTHORIZED;
            }

            /*
				handle Autoya internal error.
			*/
            if (httpCode < 0)
            {
                var internalErrorMessage = errorReason;
                failed(connectionId, httpCode, internalErrorMessage, new AutoyaStatus());
                return;
            }

            /*
				UnityWebRequest handled internal error.
			*/
            if (httpCode == 0)
            {
                var troubleMessage = errorReason;
                failed(connectionId, httpCode, troubleMessage, new AutoyaStatus());
                return;
            }


            // fall-through handling area of Autoya's events.


            /*
				detect maintenance from response code or response header value.
			*/
            var inMaintenance = CheckMaintenance(httpCode, responseHeader);

            /*
				detect unauthorized from response code or response header value.
			*/
            var isAuthFailed = CheckAuthFailed(httpCode, responseHeader);


            // Debug.LogError("connectionId:" + connectionId + " httpCode:" + httpCode + " data:" + data);


            /*
				pit falls for not 2XX.
			*/
            {
                if (httpCode < 200)
                {
                    failed(connectionId, httpCode, errorReason, new AutoyaStatus(inMaintenance, isAuthFailed));
                    return;
                }

                if (299 < httpCode)
                {
                    failed(connectionId, httpCode, errorReason, new AutoyaStatus(inMaintenance, isAuthFailed));
                    return;
                }
            }

            /*
				pit fall for maintenance or authFailed.
			*/
            if (inMaintenance || isAuthFailed)
            {
                failed(connectionId, httpCode, "good status code but under maintenance or failed to auth or both.", new AutoyaStatus(inMaintenance, isAuthFailed));
                return;
            }

            /*
				finally, connection is done as succeeded.
			*/
            succeeded(connectionId, data);


            var insencitiveKV = new NameValueCollection();
            foreach (var item in responseHeader)
            {
                insencitiveKV[item.Key] = item.Value;
            }

            /*
				fire assetBundleList-request check after succeeded.
			*/
            if (assetBundleFeatState == AssetBundlesFeatureState.Ready)
            {
                if (responseHeader.ContainsKey(AuthSettings.AUTH_RESPONSEHEADER_RESVERSION))
                {
                    var newListVersionsOnResponseHeader = responseHeader[AuthSettings.AUTH_RESPONSEHEADER_RESVERSION];

                    var listIdentitiesAndNewVersionDescriptions = GetListIdentityAndNewVersionDescriptions(newListVersionsOnResponseHeader);

                    var urls = new List<string>();
                    foreach (var listItem in listIdentitiesAndNewVersionDescriptions)
                    {
                        var identity = listItem.Key;
                        var newVersion = listItem.Value;

                        // compare with runtimeManifest data.
                        var listIdentities = LoadAppUsingAssetBundleListIdentities();

                        if (listIdentities.Contains(identity))
                        {
                            // request new version.
                            var answer = OnRequestNewAssetBundleList(identity, newVersion);
                            if (answer.doOrNot)
                            {
                                // start download new list.
                                var listUrl = answer.url;
                                urls.Add(listUrl);
                            }
                        }
                        else
                        {
                            Debug.LogError("含まれてないリストがきてるのでエラー");
                        }
                    }

                    DownloadNewAssetBundleList(urls.ToArray());
                }
            }

            /*
                fire application update request after succeeded.
            */
            var appUpdateDescription = insencitiveKV[AuthSettings.AUTH_RESPONSEHEADER_APPVERSION];
            if (!string.IsNullOrEmpty(appUpdateDescription) || forceAppUpdated)
            {
                if (forceAppUpdated)
                {
                    // must be 1X.Y.Z, grater number than current app.
                    appUpdateDescription = "1" + OnAppVersionRequired();
                }

                OnNewAppRequested(appUpdateDescription);
            }
        }

        public struct ShouldRequestOrNot
        {
            public readonly bool doOrNot;
            public readonly string url;

            private ShouldRequestOrNot(string url)
            {
                this.doOrNot = true;
                this.url = url;
            }

            private ShouldRequestOrNot(bool _unused)
            {
                this.doOrNot = false;
                this.url = string.Empty;
            }

            public static ShouldRequestOrNot Yes(string url)
            {
                return new ShouldRequestOrNot(url);
            }
            public static ShouldRequestOrNot No()
            {
                return new ShouldRequestOrNot(true);
            }
        }


        /**
			received auth-failed response from server.
			should authenticate again.
		*/
        private bool CheckAuthFailed(int httpCode, Dictionary<string, string> responseHeader)
        {
            if (IsAuthFailed(httpCode, responseHeader))
            {
                _autoyaAuthRouter.Expired();
                return true;
            }
            return false;
        }

        private bool IsAuthFailed(int httpCode, Dictionary<string, string> responseHeader)
        {
            if (forceFailAuthentication)
            {
                return true;
            }

            return IsUnauthorized(httpCode, responseHeader);
        }




        /*
			public authenticate APIs
		*/

        public static void Auth_SetOnAuthenticated(Action authenticated = null)
        {
            if (Auth_IsAuthenticated())
            {
                if (authenticated != null)
                {
                    authenticated();
                }
            }

            if (authenticated != null)
            {
                autoya.onAuthenticated = authenticated;
            }
        }

        public static void Auth_SetOnBootAuthFailed(Action<int, string> bootAuthenticationFailed = null)
        {
            if (autoya._autoyaAuthRouter == null)
            {
                return;
            }

            if (autoya._autoyaAuthRouter.IsBootAuthFailed())
            {
                if (bootAuthenticationFailed != null)
                {
                    bootAuthenticationFailed(0, "already failed. let's show interface.");
                }
            }

            if (bootAuthenticationFailed != null)
            {
                autoya.onBootAuthFailed = bootAuthenticationFailed;
            }
        }

        /**
			set new handler for auth token refreshed.
		 */
        public static void Auth_SetOnRefreshAuthFailed(Action<int, string> refreshAuthenticationFailed = null)
        {
            if (autoya._autoyaAuthRouter == null)
            {
                return;
            }

            if (autoya._autoyaAuthRouter.IsTokenRefreshFailed())
            {
                if (refreshAuthenticationFailed != null)
                {
                    refreshAuthenticationFailed(0, "already failed to refresh token. let's show interface.");
                }
            }

            if (refreshAuthenticationFailed != null)
            {
                autoya.onRefreshAuthFailed = refreshAuthenticationFailed;
            }
        }


        public static bool Auth_IsAuthenticated()
        {
            if (autoya._autoyaAuthRouter != null)
            {
                return autoya._autoyaAuthRouter.IsLogon();
            }
            return false;
        }

        public static bool Auth_AttemptAuthenticationIfNeed()
        {
            if (autoya._autoyaAuthRouter != null)
            {
                return autoya._autoyaAuthRouter.RetryAuthentication();
            }
            return false;
        }

        public static void Auth_Logout(Action succeeded, Action<string> failed)
        {
            if (autoya._autoyaAuthRouter != null)
            {
                if (autoya._autoyaAuthRouter.IsLogon() || autoya._autoyaAuthRouter.IsTokenRefreshFailed())
                {
                    autoya._autoyaAuthRouter.Logout(succeeded, failed);
                }
                else
                {
                    failed("not logged in yet.");
                }
            }
            else
            {
                failed("Autoya is not ready. maybe OverrideoPoints/OnBootApplication, IsFirstBoot, OnBootAuthRequest or OnBootAuthResponse is not finished yet.");
            }
        }
    }
}
