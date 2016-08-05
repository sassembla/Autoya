using UnityEngine;
using System;
using UniRx;
using Connections.HTTP;
using System.Collections.Generic;

namespace AutoyaFramework {
    public partial class Autoya {
		/*
			http.
				1.generate header with auth-token for each http action.
				2.renew 
		*/
		
		private HTTPConnection _autoyaHttp;


		private Dictionary<string, string> GetAuthorizedAndAdditionalHeaders (string data="", Dictionary<string, string> additionalHeader=null) {
			var headerDict = new Dictionary<string, string>();
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
				UnityWebRequest handled internal error.
			*/
			if (httpCode == 0) {
				var troubleMessage = data;
				failed(connectionId, httpCode, troubleMessage);
				Debug.LogError("いろんな理由が入り込む場所、 troubleMessage:" + troubleMessage);
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


				/*
					detect unauthorized response code or response header value.
				*/
				if (httpCode == 401) {
					var unauthReason = AutoyaConsts.HTTP_401_MESSAGE + data;
					var shouldRelogin = OnAuthFailed(connectionId, unauthReason);
					if (shouldRelogin) AttemptLogin();
				}
			}

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
			
			var headers = autoya.GetAuthorizedAndAdditionalHeaders(string.Empty, additionalHeader);
			
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
			).Timeout(TimeSpan.FromSeconds(timeoutSec)).Subscribe(
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
			
			var headers = autoya.GetAuthorizedAndAdditionalHeaders(data, additionalHeader);
			
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
			).Timeout(TimeSpan.FromSeconds(timeoutSec)).Subscribe(
				_ => {},
				ex => {
					failed(connectionId, 0, AutoyaConsts.HTTP_TIMEOUT_MESSAGE + ex);
				}
			);

            return connectionId;
        }

    }
}