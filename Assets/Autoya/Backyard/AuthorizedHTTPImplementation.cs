using UnityEngine;
using System;
using UniRx;
using Connections.HTTP;
using System.Collections.Generic;

namespace AutoyaFramework {
    public partial class Autoya {
		/*
			still under consideration.

			http.
				1.generate header with auth-token for each http action.
				2.renew header? 
		*/
		
		private HTTPConnection _autoyaHttp;


		private Dictionary<string, string> GetAuthorizedAndAdditionalHeaders (Dictionary<string, string> additionalHeader=null, Func<Dictionary<string, string>, Dictionary<string, string>> customizer=null) {
			var headerDict = new Dictionary<string, string>();

			// この時点で、tokenが空だったりすると、もれなくログインが必要になるようなレスポンスが返せると思う。

			
			/*
				set authorized header part.
			*/
			// headerDict.Add(AutoyaConsts.key_app_version, baseHeader.app_version);
			// headerDict.Add(AutoyaConsts.key_asset_version, baseHeader.asset_version);

			/*
				・今回送るデータ
				・token
				・app_version
				・asset_version

				とかがあれば、OAuthの暗号化とかできるよなあ。この部分をoverrideしてね、っていうのが良いのかな。
				まあ要件を先に出し切らないとな。個別実装のほうが良い部分だと思うんで、個別実装で潰せるように作る、っていうのが良い感じかな。だったら
				Dictionaryを返す関数をユーザーが自由に定義できる(といいつつ状態はAutoyaに任せられる)といいよな〜〜
			*/

			/*
				ここで、なんかパラメータを調整できると良いのかなあ。形式を選ばせるの面倒臭いから、なんか独立して存在できると良いよなあ。
				HTTPの中だけど、ServerCommunication、っていうかAuthenticationの部類なんだよな。
				もっと綺麗に切断できる気がするな？？

				ヘッダーを保持するのが責務なんで、その責務は放置できそう。
				なので、ヘッダーを保持する vs ヘッダーでAuthする、 っていうのがぶつかってる。

				どっちが勝つと良いんだろう。ヘッダーをAuthで、ってほうかな。
			*/


			/*
				set additional header part.
			*/
			if (additionalHeader != null) foreach (var kv in additionalHeader) headerDict.Add(kv.Key, kv.Value);


			/*
				customizer function can run here.
			*/
			if (customizer != null) {
				return customizer(headerDict);
			}

			return headerDict;
		}
		
		/**
			core feature of Autoya's status handling.

			analyze connection errors and handle Autoya's status like login/logout.

			many 
				online/offline,
				maintenaince/open,
				login/logout,
		*/
		private void ErrorFlowHandling (string connectionId, Dictionary<string, string> responseHeaders, int httpCode, string data, Action<string, string> succeeded, Action<string, int, string> failed) {
			/*
				handle Autoya internal error.
			*/
			if (httpCode < 0) {
				var internalErrorMessage = data;
				failed(connectionId, httpCode, internalErrorMessage);
				return;
			} 

			/*
				UnityWebRequest handled internal error.
			*/
			if (httpCode == 0) {
				Debug.LogError("httpCode = 0, misc errors. data:" + data);
				var troubleMessage = data;
				failed(connectionId, httpCode, troubleMessage);
				Debug.LogError("Unityの内部エラー、いろんな理由が入り込む場所、 troubleMessage:" + troubleMessage + " 対処方法としては一辺倒で、");
				return;
			}
			
			/*
				fall-through handling area of Autoya's events.

				this block NEVER fire succeeded/failed handler and return code.
				these events are cascadable.
			*/
			{
				/*
					detect maintenance response code or response header value.
				*/
				if (IsInMaintenance(httpCode, responseHeaders)) {
					// OnMaintenance();
				}

				/*
					detect unauthorized response code or response header value.
				*/
				if (IsAuthFailed(httpCode, responseHeaders)) {
					var unauthReason = AutoyaConsts.HTTP_401_MESSAGE + data;
					var shouldRelogin = OnAuthFailed(connectionId, unauthReason);
					if (shouldRelogin) {
						Debug.LogError("サーバが401をダイレクトに返してきたうえに、reloginを望まれている。 connectionId:" + connectionId + " まだ未実装。");
						// AttemptLoginByTokenCandidate(_token);
					}
				}
			}

			// Debug.LogError("connectionId:" + connectionId + " httpCode:" + httpCode + " data:" + data);

			/*
				pit falls for not 2XX.
			*/
			{
				if (httpCode < 200) {
					failed(connectionId, httpCode, data);
					return;
				}

				if (299 < httpCode) {
					failed(connectionId, httpCode, data);
					return; 
				}
			}

			/*
				finally, connection is done as succeeded.
			*/
			succeeded(connectionId, data);
		}

		private bool IsInMaintenance (int httpCode, Dictionary<string, string> responseHeaders) {
			var headerContainsMaintenanceCode = false;

			Debug.LogWarning("メンテ条件、そのうち実装する。さすがにクライアント内にエラーを残しとくのはまずい。");
			if (headerContainsMaintenanceCode && 200 <= httpCode && httpCode < 300) {
				throw new ArgumentException("maintenance code's http code should out of 2XX, current code is httpCode:" + httpCode);
			}
			return false;
		}
		private bool IsAuthFailed (int httpCode, Dictionary<string, string> responseHeaders) {
			if (httpCode == 401) return true;
			return false;
		}


		/*
			public HTTP APIs.
		*/

        public static string Http_Get (
			string url, 
			Action<string, string> succeeded, 
			Action<string, int, string> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=AutoyaConsts.HTTP_TIMEOUT_SEC
		) {
			var connectionId = Guid.NewGuid().ToString();
			
			var headers = autoya.GetAuthorizedAndAdditionalHeaders(additionalHeader);
			
			Observable.FromCoroutine(
				() => autoya._autoyaHttp.Get(
					connectionId,
					headers,
					url,
					(conId, code, responseHeaders, resultData) => {
						autoya.ErrorFlowHandling(conId, responseHeaders, code, resultData, succeeded, failed);
					},
					(conId, code, reason, responseHeaders) => {
						autoya.ErrorFlowHandling(conId, responseHeaders, code, reason, succeeded, failed);
					}
				)
			).Timeout(
				TimeSpan.FromSeconds(timeoutSec)
			).Subscribe(
				_ => {},
				ex => {
					failed(connectionId, 0, AutoyaConsts.HTTP_TIMEOUT_MESSAGE + ex);
				}
			);

            return connectionId;
        }

		public static string Http_Post (
			string url, 
			string data,
			Action<string, string> succeeded, 
			Action<string, int, string> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=AutoyaConsts.HTTP_TIMEOUT_SEC
		) {
			var connectionId = Guid.NewGuid().ToString();
			
			var headers = autoya.GetAuthorizedAndAdditionalHeaders(additionalHeader);
			
			Observable.FromCoroutine(
				() => autoya._autoyaHttp.Post(
					connectionId,
					headers,
					url,
					data,
					(conId, code, responseHeaders, resultData) => {
						autoya.ErrorFlowHandling(conId, responseHeaders, code, resultData, succeeded, failed);
					},
					(conId, code, reason, responseHeaders) => {
						autoya.ErrorFlowHandling(conId, responseHeaders, code, reason, succeeded, failed);
					}
				)
			).Timeout(
				TimeSpan.FromSeconds(timeoutSec)
			).Subscribe(
				_ => {},
				ex => {
					failed(connectionId, 0, AutoyaConsts.HTTP_TIMEOUT_MESSAGE + ex);
				}
			);

            return connectionId;
        }

    }
}