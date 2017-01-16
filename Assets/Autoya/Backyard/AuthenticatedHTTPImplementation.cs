using UnityEngine;
using System;
using AutoyaFramework.Connections.HTTP;
using System.Collections.Generic;

namespace AutoyaFramework {
    public partial class Autoya {
		/*
			http.
				1.hold header with auth-token and identity for each http action.
				2.renew header when these are replaced or deleted. 
		*/
		
		private HTTPConnection _autoyaHttp;

		private void SetHTTPAuthorizedPart (string identity, string token) {
			Debug.LogWarning("do nothing yet.");
		}
		
		private Dictionary<string, string> GetAuthorizedAndAdditionalHeaders (Dictionary<string, string> additionalHeader=null, Func<Dictionary<string, string>, Dictionary<string, string>> customizer=null) {
			var headerDict = new Dictionary<string, string>();

			/*
				git ignoreを利用して独自暗号化とかの実装を推薦する。
				basicなやつであれば、何も載せずにJWTで基礎的なものを送るようにしたい。

				カスタマイズする場合は、特定の関数をオーバーライドしたクラスを用意し、それをignoreし、それが使われるようにしたい。
				初回起動時通信と、それ以外とで扱いが異なる。

				tokenがあったら、なかったら、expireしてるのが内部で確認できたら、という感じか。

				☆サンプルでは適当なbasic認証にしておけばいいと思う。
			*/

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
		
		/*
			public HTTP APIs.
		*/

		public static string Http_Get (
			string url, 
			Action<string, string> succeeded, 
			Action<string, int, string> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			// if (autoya == null) {
			// 	failed();
			// } 
			// if (Autoya.Auth_IsLoggedIn()) {
			// 	failed();
			// }

			var connectionId = Guid.NewGuid().ToString();
			
			var headers = autoya.GetAuthorizedAndAdditionalHeaders(additionalHeader);
			
			Action<string, object> onSucceededAsStringData = (conId, resultData) => {
				succeeded(conId, resultData as string);
			};

			autoya.mainthreadDispatcher.Commit(
				autoya._autoyaHttp.Get(
					connectionId,
					headers,
					url,
					(conId, code, responseHeaders, resultData) => {
						autoya.HttpResponseHandling(conId, responseHeaders, code, resultData, string.Empty, onSucceededAsStringData, failed);
					},
					(conId, code, reason, responseHeaders) => {
						autoya.HttpResponseHandling(conId, responseHeaders, code, string.Empty, reason, onSucceededAsStringData, failed);
					},
					timeoutSec
				)
			);
			
            return connectionId;
        }

		public static string Http_Post (
			string url, 
			string data,
			Action<string, string> succeeded, 
			Action<string, int, string> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			// if (autoya == null) {
			// 	failed();
			// } 
			// if (Autoya.Auth_IsLoggedIn()) {
			// 	failed();
			// }

			var connectionId = Guid.NewGuid().ToString();
			
			var headers = autoya.GetAuthorizedAndAdditionalHeaders(additionalHeader);
			
			Action<string, object> onSucceededAsStringData = (conId, resultData) => {
				succeeded(conId, resultData as string);
			};

			autoya.mainthreadDispatcher.Commit(
				autoya._autoyaHttp.Post(
					connectionId,
					headers,
					url,
					data,
					(conId, code, responseHeaders, resultData) => {
						autoya.HttpResponseHandling(conId, responseHeaders, code, resultData, string.Empty, onSucceededAsStringData, failed);
					},
					(conId, code, reason, responseHeaders) => {
						autoya.HttpResponseHandling(conId, responseHeaders, code, string.Empty, reason, onSucceededAsStringData, failed);
					},
					timeoutSec
				)
			);

            return connectionId;
        }

		public static string Http_Put (
			string url, 
			string data,
			Action<string, string> succeeded, 
			Action<string, int, string> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			// if (autoya == null) {
			// 	failed();
			// } 
			// if (Autoya.Auth_IsLoggedIn()) {
			// 	failed();
			// }

			var connectionId = Guid.NewGuid().ToString();
			
			var headers = autoya.GetAuthorizedAndAdditionalHeaders(additionalHeader);
			
			Action<string, object> onSucceededAsStringData = (conId, resultData) => {
				succeeded(conId, resultData as string);
			};

			autoya.mainthreadDispatcher.Commit(
				autoya._autoyaHttp.Put(
					connectionId,
					headers,
					url,
					data,
					(conId, code, responseHeaders, resultData) => {
						autoya.HttpResponseHandling(conId, responseHeaders, code, resultData, string.Empty, onSucceededAsStringData, failed);
					},
					(conId, code, reason, responseHeaders) => {
						autoya.HttpResponseHandling(conId, responseHeaders, code, string.Empty, reason, onSucceededAsStringData, failed);
					},
					timeoutSec
				)
			);

            return connectionId;
        }

		public static string Http_Delete (
			string url, 
			string data,
			Action<string, string> succeeded, 
			Action<string, int, string> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			// if (autoya == null) {
			// 	failed();
			// } 
			// if (Autoya.Auth_IsLoggedIn()) {
			// 	failed();
			// }

			var connectionId = Guid.NewGuid().ToString();
			
			var headers = autoya.GetAuthorizedAndAdditionalHeaders(additionalHeader);
			
			Action<string, object> onSucceededAsStringData = (conId, resultData) => {
				succeeded(conId, resultData as string);
			};

			autoya.mainthreadDispatcher.Commit(
				autoya._autoyaHttp.Delete(
					connectionId,
					headers,
					url,
					(conId, code, responseHeaders, resultData) => {
						autoya.HttpResponseHandling(conId, responseHeaders, code, resultData, string.Empty, onSucceededAsStringData, failed);
					},
					(conId, code, reason, responseHeaders) => {
						autoya.HttpResponseHandling(conId, responseHeaders, code, string.Empty, reason, onSucceededAsStringData, failed);
					},
					timeoutSec
				)
			);

            return connectionId;
        }

    }
}