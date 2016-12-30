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
		
		public IEnumerator Get (string connectionId, Dictionary<string, string> headers, string url, Action<string, int, Dictionary<string, string>, string> succeeded, Action<string, int, string, Dictionary<string, string>> failed) {
			using (var request = UnityWebRequest.Get(url)) {
				if (headers != null) foreach (var kv in headers) request.SetRequestHeader(kv.Key, kv.Value);
				
				yield return request.Send();
				
				var responseCode = (int)request.responseCode;
				var responseHeaders = request.GetResponseHeaders();

				if (request.isError) {
					failed(connectionId, responseCode, request.error, responseHeaders);
					yield break;
				}

				var data = request.downloadHandler.data;
				succeeded(connectionId, responseCode, responseHeaders, Encoding.UTF8.GetString(data));
				yield break;
			}
		}
		
		public IEnumerator Post (string connectionId, Dictionary<string, string> headers, string url, string data, Action<string, int, Dictionary<string, string>, string> succeeded, Action<string, int, string, Dictionary<string, string>> failed) {
			using (var request = UnityWebRequest.Post(url, data)) {
				if (headers != null) foreach (var kv in headers) request.SetRequestHeader(kv.Key, kv.Value);

				yield return request.Send();
				
				var responseCode = (int)request.responseCode;
				var responseHeaders = request.GetResponseHeaders();

				if (request.isError) {
					failed(connectionId, responseCode, request.error, responseHeaders);
					yield break;
				}

				var resultData = request.downloadHandler.data;
				succeeded(connectionId, responseCode, responseHeaders, Encoding.UTF8.GetString(resultData));
				yield break;
			}
		}

		public IEnumerator Put (string connectionId, Dictionary<string, string> headers, string url, string data, Action<string, int, Dictionary<string, string>, string> succeeded, Action<string, int, string, Dictionary<string, string>> failed) {
			using (var request = UnityWebRequest.Put(url, data)) {
				if (headers != null) foreach (var kv in headers) request.SetRequestHeader(kv.Key, kv.Value);

				yield return request.Send();
				
				var responseCode = (int)request.responseCode;
				var responseHeaders = request.GetResponseHeaders();

				if (request.isError) {
					failed(connectionId, responseCode, request.error, responseHeaders);
					yield break;
				}

				var resultData = request.downloadHandler.data;
				succeeded(connectionId, responseCode, responseHeaders, Encoding.UTF8.GetString(resultData));
				yield break;
			}
		}

		public IEnumerator Delete (string connectionId, Dictionary<string, string> headers, string url, Action<string, int, Dictionary<string, string>, string> succeeded, Action<string, int, string, Dictionary<string, string>> failed) {
			using (var request = UnityWebRequest.Delete(url)) {
				if (headers != null) foreach (var kv in headers) request.SetRequestHeader(kv.Key, kv.Value);
				
				yield return request.Send();
				
				var responseCode = (int)request.responseCode;
				var responseHeaders = request.GetResponseHeaders();

				if (request.isError) {
					failed(connectionId, responseCode, request.error, responseHeaders);
					yield break;
				}
				
				var data = request.downloadHandler.data;
				succeeded(connectionId, responseCode, responseHeaders, Encoding.UTF8.GetString(data));
				yield break;
			}
		}
		
		public IEnumerator DownloadAssetBundle (string connectionId, Dictionary<string, string> headers, string url, uint version, uint crc, Action<string, int, Dictionary<string, string>, AssetBundle> succeeded, Action<string, int, string, Dictionary<string, string>> failed) {
			using (var request = UnityWebRequest.GetAssetBundle(url, version, crc)) {
				if (headers != null) foreach (var kv in headers) request.SetRequestHeader(kv.Key, kv.Value);
				
				yield return request.Send();

				while (!request.isDone) {
					yield return null;
				}

				var responseCode = (int)request.responseCode;
				var responseHeaders = request.GetResponseHeaders();

				if (request.isError) {
					failed(connectionId, responseCode, request.error, responseHeaders);
					yield break;
				}

				while (!Caching.IsVersionCached(url, (int)version)) {
					yield return null;
				}

				var dataHandler = (DownloadHandlerAssetBundle)request.downloadHandler;
				
				var assetBundle = dataHandler.assetBundle;
				if (assetBundle == null) {
					failed(connectionId, responseCode, "failed to load assetBundle.", responseHeaders);
				} else {
					succeeded(connectionId, responseCode, responseHeaders, assetBundle);
				}
			}
		}
	}

}