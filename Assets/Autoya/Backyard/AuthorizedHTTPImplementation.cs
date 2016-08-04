using UnityEngine;
using System;
using UniRx;
using Connections.HTTP;
using System.Collections.Generic;

namespace AutoyaFramework {
	/**
		HTTP header structure for Autoya authorization mechanism.
	*/
    public struct AuthorizedHTTPHeader {
		public string app_version;
		public string asset_version;

		public AuthorizedHTTPHeader (string app_version, string asset_version) {
			this.app_version = app_version;
			this.asset_version = asset_version;
		}
	}
	
    public partial class Autoya {
		
		private HTTPConnection _autoyaHttp;

		private AuthorizedHTTPHeader baseHeader;

		private void RenewAuthorizedHTTPHeader (AuthorizedHTTPHeader newBaseHeader) {
			this.baseHeader = newBaseHeader;
		}

		private Dictionary<string, string> AuthorizedHeaders (Dictionary<string, string> additionalHeader=null) {
			var headerDict = new Dictionary<string, string>();
			headerDict.Add(AutoyaSettings.key_app_version, baseHeader.app_version);
			headerDict.Add(AutoyaSettings.key_asset_version, baseHeader.asset_version);

			if (additionalHeader != null) foreach (var kv in additionalHeader) headerDict.Add(kv.Key, kv.Value);

			return headerDict;
		}
		

        public static string AuthedHttpGet (string url, Action<string, string> succeeded, Action<string, int, string> failed, Dictionary<string, string> additionalHeader=null) {
			var connectionId = Guid.NewGuid().ToString();
			
			var headers = autoya.AuthorizedHeaders(additionalHeader);
			
			var cancellerationToken = Observable.FromCoroutine(
				() => autoya._autoyaHttp.Get(
					connectionId,
					headers,
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

		private void ErrorFlowHandler (string connectionId, int httpCode, string data, Action<string, string> succeeded, Action<string, int, string> failed) {
			Debug.LogError("通信エラーが出た時に、その内容がAutoya関連かどうかを判断して、Autoya側の状態を変更する。オンラインオフライン、ほか ");
			Debug.LogError("ちょっとまとめすぎたかもしれない。しょっぱなからエラーで出るケース(接続してないとかそのへん)もあれば、接続は出来るんだけど200以外が帰ってきたねっていうケースも作れる。");

			Debug.LogError("connectionId:" + connectionId + " httpCode:" + httpCode + " data:" + data);

			/*
				detect unauthorized code.
			*/
			if (httpCode == 401) {
				var unauthReason = "unauthorized:" + data;
				var shouldRelogin = OnAuthFailed(connectionId, unauthReason);

				Debug.LogError("なんかヘッダにいろいろ理由を書いて、それを取得することができる気がする。っていうかbody使えるんじゃないか。 shouldRelogin:" + shouldRelogin);

				failed(connectionId, httpCode, data);
				return;
			}

			if (httpCode < 200) {
				failed(connectionId, httpCode, data);
				return;
			}

			if (299 < httpCode) {
				failed(connectionId, httpCode, data);
				return; 
			}
			
			/*
				多彩なハンドリングによる着火があり得るんだけど、全部ここでまとめることで対処できそう。
			*/
			succeeded(connectionId, data);
		}

    }
}