using System;
using AutoyaFramework.Connections.HTTP;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace AutoyaFramework {
    public partial class Autoya {
		/*
			http.
				1.hold header with auth-token and identity for each http action.
				2.renew header when these are replaced or deleted. 
		*/
		
		private HTTPConnection _autoyaHttp;
		
		/*
			public HTTP APIs.
		*/

		public static string Http_Get (
			string url, 
			Action<string, string> succeeded, 
			Action<string, int, string, AutoyaStatus> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			var connectionId = Guid.NewGuid().ToString();

			if (autoya == null) {
				var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				return connectionId;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				
				return connectionId;
			}
			
			if (additionalHeader == null) {
				additionalHeader = new Dictionary<string, string>();
			}
			var headers = autoya.httpRequestHeaderDelegate(HttpMethod.Get, url, additionalHeader, string.Empty);
			
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
			Action<string, int, string, AutoyaStatus> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			var connectionId = Guid.NewGuid().ToString();

			if (autoya == null) {
				var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				return connectionId;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				
				return connectionId;
			}
			
			if (additionalHeader == null) {
				additionalHeader = new Dictionary<string, string>();
			}
			var headers = autoya.httpRequestHeaderDelegate(HttpMethod.Post, url, additionalHeader, data);
			
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
			Action<string, int, string, AutoyaStatus> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			var connectionId = Guid.NewGuid().ToString();

			if (autoya == null) {
				var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				return connectionId;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				
				return connectionId;
			}
			
			if (additionalHeader == null) {
				additionalHeader = new Dictionary<string, string>();
			}
			var headers = autoya.httpRequestHeaderDelegate(HttpMethod.Put, url, additionalHeader, data);
			
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
			Action<string, int, string, AutoyaStatus> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			var connectionId = Guid.NewGuid().ToString();

			if (autoya == null) {
				var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				return connectionId;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				
				return connectionId;
			}

			if (additionalHeader == null) {
				additionalHeader = new Dictionary<string, string>();
			}
			var headers = autoya.httpRequestHeaderDelegate(HttpMethod.Delete, url, additionalHeader, data);
			
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

		private class ConnectionErrorInstance {
			private readonly string connectionId;
			private static Dictionary<string, string> responseHeader = new Dictionary<string, string>();
			private const int code = 0;// そのうち変更する。
			private readonly string reason;
			private readonly Action<string, int, string, AutoyaStatus> failed;
			private static AutoyaStatus status = new AutoyaStatus();

			public ConnectionErrorInstance (string connectionId, string reason, Action<string, int, string, AutoyaStatus> failed) {
				Debug.LogWarning("まだauthが終わっていない状態でのhttpのエラーコードcodeが　0に固定されているのをなんとかする。判別できたほうがいい。");
				this.connectionId = connectionId;
				this.reason = reason;
				this.failed = failed;
			}

			public IEnumerator Coroutine () {
				yield return null;
				failed(connectionId, code, reason, status);
			}
		}
    }
}