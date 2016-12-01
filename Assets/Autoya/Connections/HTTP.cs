using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

/**
	implementation of HTTP connection.
*/
namespace AutoyaFramework.Connections.HTTP {

    public class HTTPConnection {
		
		public IEnumerator Get (string connectionId, Dictionary<string, string> headers, string url, Action<string, int, Dictionary<string, string>, string> succeeded, Action<string, int, string, Dictionary<string, string>> failed) {
			using (var request = UnityWebRequest.Get(url)) {
				if (headers != null) foreach (var kv in headers) request.SetRequestHeader(kv.Key, kv.Value);
				
				var uploader = new UploadHandlerRaw(new byte[100]);
				// request.uploadHandler = uploader;

				// var buffer = new DownloadHandlerBuffer();
				// request.downloadHandler = buffer;
				
				yield return request.Send();
				
				var responseCode = (int)request.responseCode;
				var responseHeaders = request.GetResponseHeaders();


				/*
					大まかな通信接続状態のエラーをこの辺で捌く、、のが成立する前提
				*/
				if (request.isError) {
					failed(connectionId, responseCode, request.error, responseHeaders);
					yield break;
				}

				/*
					この時点で通信は終わってるんで、

					・エラーハンドルができるための情報をどう渡すか(平易な型情報) -> responseヘッダ？ 
						何かのデータ？ データもらえない場合は？みたいなのが
						見たいな。データサンプル作ってから考えるかな。

						どっちにしてもstringとか渡す前提だな、AssetBundleをCacheするのとかはなんか専用で考えたほうが良いのかな。
					
					・このへんの切り分けをどうするかな〜いっぺん考えてみよう。
						道具の粒度がわかった。
						WebRequestとWWWの違いが本当に無い気がする、、、
				*/

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

		public IEnumerator DownloadAssetBundle (string connectionId, Dictionary<string, string> headers, string url, Action<string, int, Dictionary<string, string>, string> succeeded, Action<string, int, string, Dictionary<string, string>> failed) {
			/*
				このメソッド名がいいのかどうかっていう感じだな〜〜
			*/
			throw new Exception("not yet implemented.");
		}
	}

}