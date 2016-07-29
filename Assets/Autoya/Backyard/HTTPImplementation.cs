using UnityEngine;
using System.Collections;
using Connection.HTTP;
using System;

namespace AutoyaFramework {
	public partial class Autoya {
		
		private HTTPConnection _autoyaHttp;
        public static string HttpGet (HTTPHeader additionalHeader, string url, Action<string, string> succeeded, Action<string, int, string> failed) {
			var combinedHeader = additionalHeader;
            return autoya._autoyaHttp.Get(
				combinedHeader, 
				url, 
				(conId, resultData) => {
					succeeded(conId, resultData);
				},
				(conId, code, reason) => {
					autoya.ErrorFlowHandler(conId, code, reason, failed);
				}
			);
        }
		public static string HttpGet (string url, Action<string, string> succeeded, Action<string, int, string> failed) {
            return autoya._autoyaHttp.Get(
				url,
				(conId, resultData) => {
					succeeded(conId, resultData);
				},
				(conId, code, reason) => {
					autoya.ErrorFlowHandler(conId, code, reason, failed);
				}
			);
        }
		
		public static string HttpPost (HTTPHeader additionalHeader, string url, string data, Action<string, string> succeeded, Action<string, int, string> failed) {
			var combinedHeader = additionalHeader;
            return autoya._autoyaHttp.Post(
				combinedHeader, 
				url, 
				data, 
				(conId, resultData) => {
					succeeded(conId, resultData);
				},
				(conId, code, reason) => {
					autoya.ErrorFlowHandler(conId, code, reason, failed);
				}
			);
        }
		public static string HttpPost (string url, string data, Action<string, string> succeeded, Action<string, int, string> failed) {
            return autoya._autoyaHttp.Post( 
				url, 
				data, 
				(conId, resultData) => {
					succeeded(conId, resultData);
				},
				(conId, code, reason) => {
					autoya.ErrorFlowHandler(conId, code, reason, failed);
				}
			);
        }

		private void ErrorFlowHandler (string connectionId, int httpErrorCode, string reason, Action<string, int, string> failed) {
			Debug.LogError("通信エラーが出た時に、その内容がAutoya関連かどうかを判断して、Autoya側の状態を変更する。オンラインオフライン、ほか");
			/*
				多彩なハンドリングによる着火があり得るんだけど、全部ここでまとめることで対処できそう。
			*/
			failed(connectionId, httpErrorCode, reason);
		}

    }
}