using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/**
	implementation of HTTP connection.
*/
namespace AutoyaFramework.Connections.HTTP {

    public class HTTPConnection {
		
		public IEnumerator Get (string connectionId, Dictionary<string, string> requestHeaders, string url, Action<string, int, Dictionary<string, string>, string> succeeded, Action<string, int, string, Dictionary<string, string>> failed, double timeoutSec=0) {
			var currentDate = DateTime.UtcNow;
			var limitTick = (TimeSpan.FromTicks(currentDate.Ticks) + TimeSpan.FromSeconds(timeoutSec)).Ticks;
			
			using (var request = UnityWebRequest.Get(url)) {
				if (requestHeaders != null) foreach (var kv in requestHeaders) request.SetRequestHeader(kv.Key, kv.Value);
				
				var p = request.Send();
				
				while (!p.isDone) {
					yield return null;

					// check timeout.
					if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks) {
						request.Abort();
						failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, BackyardSettings.HTTP_TIMEOUT_MESSAGE + timeoutSec, null);
						yield break;
					}
				}
				
				var responseCode = (int)request.responseCode;
				var responseHeaders = request.GetResponseHeaders();

				if (request.isError) {
					failed(connectionId, responseCode, request.error, responseHeaders);
					yield break;
				}

				var data = request.downloadHandler.data;
				succeeded(connectionId, responseCode, responseHeaders, Encoding.UTF8.GetString(data));
			}
		}

		public IEnumerator Post (string connectionId, Dictionary<string, string> requestHeaders, string url, string data, Action<string, int, Dictionary<string, string>, string> succeeded, Action<string, int, string, Dictionary<string, string>> failed, double timeoutSec=0) {
			var currentDate = DateTime.UtcNow;
			var limitTick = (TimeSpan.FromTicks(currentDate.Ticks) + TimeSpan.FromSeconds(timeoutSec)).Ticks;
			
			using (var request = UnityWebRequest.Post(url, data)) {
				if (requestHeaders != null) foreach (var kv in requestHeaders) request.SetRequestHeader(kv.Key, kv.Value);
				var p = request.Send();
				
				while (!p.isDone) {
					yield return null;
					// check timeout.
					if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks) {
						request.Abort();
						failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, BackyardSettings.HTTP_TIMEOUT_MESSAGE + timeoutSec, null);
						yield break;
					}
				}
				
				var responseCode = (int)request.responseCode;
				var responseHeaders = request.GetResponseHeaders();
				
				if (request.isError) {
					failed(connectionId, responseCode, request.error, responseHeaders);
					yield break;
				}

				var resultData = request.downloadHandler.data;
				succeeded(connectionId, responseCode, responseHeaders, Encoding.UTF8.GetString(resultData));
			}
		}

		public IEnumerator Put (string connectionId, Dictionary<string, string> requestHeaders, string url, string data, Action<string, int, Dictionary<string, string>, string> succeeded, Action<string, int, string, Dictionary<string, string>> failed, double timeoutSec=0) {
			var currentDate = DateTime.UtcNow;
			var limitTick = (TimeSpan.FromTicks(currentDate.Ticks) + TimeSpan.FromSeconds(timeoutSec)).Ticks;
			
			using (var request = UnityWebRequest.Put(url, data)) {
				if (requestHeaders != null) foreach (var kv in requestHeaders) request.SetRequestHeader(kv.Key, kv.Value);

				var p = request.Send();
				
				while (!p.isDone) {
					yield return null;

					// check timeout.
					if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks) {
						request.Abort();
						failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, BackyardSettings.HTTP_TIMEOUT_MESSAGE + timeoutSec, null);
						yield break;
					}
				}
				
				var responseCode = (int)request.responseCode;
				var responseHeaders = request.GetResponseHeaders();

				if (request.isError) {
					failed(connectionId, responseCode, request.error, responseHeaders);
					yield break;
				}

				var resultData = request.downloadHandler.data;
				succeeded(connectionId, responseCode, responseHeaders, Encoding.UTF8.GetString(resultData));
			}
		}

		public IEnumerator Delete (string connectionId, Dictionary<string, string> requestHeaders, string url, Action<string, int, Dictionary<string, string>, string> succeeded, Action<string, int, string, Dictionary<string, string>> failed, double timeoutSec=0) {
			var currentDate = DateTime.UtcNow;
			var limitTick = (TimeSpan.FromTicks(currentDate.Ticks) + TimeSpan.FromSeconds(timeoutSec)).Ticks;
			
			using (var request = UnityWebRequest.Delete(url)) {
				if (requestHeaders != null) foreach (var kv in requestHeaders) request.SetRequestHeader(kv.Key, kv.Value);
				
				var p = request.Send();
				
				while (!p.isDone) {
					yield return null;

					// check timeout.
					if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks) {
						request.Abort();
						failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, BackyardSettings.HTTP_TIMEOUT_MESSAGE + timeoutSec, null);
						yield break;
					}
				}
				
				var responseCode = (int)request.responseCode;
				var responseHeaders = request.GetResponseHeaders();

				if (request.isError) {
					failed(connectionId, responseCode, request.error, responseHeaders);
					yield break;
				}
				
				var data = request.downloadHandler.data;
				succeeded(connectionId, responseCode, responseHeaders, Encoding.UTF8.GetString(data));
			}
		}
	}

}