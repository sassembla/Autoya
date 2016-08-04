using UnityEngine;
using System;
using UniRx;
using Connection.HTTP;

namespace AutoyaFramework {
    public partial class Autoya {
		
		private HTTPConnection _autoyaHttp;

        public static string HttpGet (HTTPHeader additionalHeader, string url, Action<string, string> succeeded, Action<string, int, string> failed) {
			// var combinedHeader = additionalHeader;
            // return autoya._autoyaHttp.Get(
			// 	combinedHeader, 
			// 	url, 
			// 	(conId, resultData) => {
			// 		succeeded(conId, resultData);
			// 	},
			// 	(conId, code, reason) => {
			// 		autoya.ErrorFlowHandler(conId, code, reason, failed);
			// 	}
			// );
			return null;
        }
		public static string HttpGet (string url, Action<string, string> succeeded, Action<string, int, string> failed) {
			var connectionId = Guid.NewGuid().ToString();
			
			var cancellerationToken = Observable.FromCoroutine(
				() => autoya._autoyaHttp.Get(
					connectionId,
					url,
					(conId, code, responseHeaders, resultData) => {
						autoya.ErrorFlowHandler(conId, code, resultData, succeeded, failed);
					},
					(conId, code, reason, responseHeaders) => {
						autoya.ErrorFlowHandler(conId, code, reason, succeeded, failed);
					}
				)
			).Subscribe();

			// cancellerationToken.Dispose();// これでキャンセルできるのはいいんだけど、理想のキャンセルってなんだろ。関数抱えて云々よりはid抱えて云々のほうが軽い？関数返しちゃたほうがいい？
			// 自動リトライとかの設定も欲しいところ。
			
            return connectionId;
        }
		
		// public static string HttpPost (HTTPHeader additionalHeader, string url, string data, Action<string, string> succeeded, Action<string, int, string> failed) {
		// 	var combinedHeader = additionalHeader;
        //     return autoya._autoyaHttp.Post(
		// 		combinedHeader, 
		// 		url, 
		// 		data, 
		// 		(conId, resultData) => {
		// 			succeeded(conId, resultData);
		// 		},
		// 		(conId, code, reason) => {
		// 			autoya.ErrorFlowHandler(conId, code, reason, succeeded, failed);
		// 		}
		// 	);
        // }
		// public static string HttpPost (string url, string data, Action<string, string> succeeded, Action<string, int, string> failed) {
        //     return autoya._autoyaHttp.Post( 
		// 		url, 
		// 		data, 
		// 		(conId, resultData) => {
		// 			succeeded(conId, resultData);
		// 		},
		// 		(conId, code, reason) => {
		// 			autoya.ErrorFlowHandler(conId, code, reason, succeeded, failed);
		// 		}
		// 	);
        // }

		private void ErrorFlowHandler (string connectionId, int httpErrorCode, string reason, Action<string, string> succeeded, Action<string, int, string> failed) {
			Debug.LogError("通信エラーが出た時に、その内容がAutoya関連かどうかを判断して、Autoya側の状態を変更する。オンラインオフライン、ほか ");
			Debug.LogError("しょっぱなからエラーで出るケース(接続してないとかそのへん)もあれば、接続は出来るんだけど200以外が帰ってきたねっていうケースも作れる。");

			Debug.LogError("connectionId:" + connectionId + " httpErrorCode:" + httpErrorCode + " reason:" + reason);

			/*
				detect unauthorized code.
			*/
			if (httpErrorCode == 401) {
				Debug.LogError("なんかヘッダにいろいろ理由を書いて、それを取得することができる気がする。っていうかbody使えるんじゃないか。");
				var unauthReason = "unauthorized:" + reason;
				var shouldRelogin = OnAuthFailed(connectionId, unauthReason);
			}

			
			
			/*
				多彩なハンドリングによる着火があり得るんだけど、全部ここでまとめることで対処できそう。
			*/
			failed(connectionId, httpErrorCode, reason);
		}

    }
}