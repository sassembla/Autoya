using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Connection.HTTP {
	/**
		HTTP header structure.
	*/
	public struct HTTPHeader {
		public string app_version;
		public string asset_version;

		public HTTPHeader (string app_version, string asset_version) {
			this.app_version = app_version;
			this.asset_version = asset_version;
		}
	}

	/**
		んー、起動時になんか必須のハンドラを渡すとかするか。エラー時にエラー内容を判別するのはAutoya側にしてしまえば、通信に専念できる。
		あとAssetBundleの通信もできる、、んだよな確か。
	*/
	public class HTTPConnection {
		private HTTPHeader baseHeader;

		public HTTPConnection (HTTPHeader baseHeader) {
			this.baseHeader = baseHeader;	
		}

		public void ResetHTTPHeader (HTTPHeader newBaseHeader) {
			this.baseHeader = newBaseHeader;
		}

		public IEnumerator Get (string connectionId, HTTPHeader additionalHeader, string url, Action<string, string> succeeded, Action<string, int, string> failed) {
			// var conId = Guid.NewGuid().ToString();
			// var myWr = UnityWebRequest.Get("http://www.myserver.com/foo.txt");
			// foreach (var ) myWr.SetRequestHeader(k, v);
			// myWr.Send();
			// return conId;
			// return string.Empty;
			yield break;
		}

		public IEnumerator Get (string connectionId, string url, Action<string, string> succeeded, Action<string, int, string> failed) {
			using (var request = UnityWebRequest.Get(url)) {
				
				// foreach (var kv in baseHeader.) myWr.SetRequestHeader(k, v);
				yield return request.Send();

				while (!request.isDone) {
					Debug.LogError("んで、このcoroutineが終わるころには、っていう。");
					yield return null;
				}

				var responseCode = (int)request.responseCode;

				/*
					大まかな通信エラーをこの辺で捌く
				*/
				if (request.isError) {
					failed(connectionId, responseCode, request.error);
					yield break;
				}


				/*
					あと残るのは、通信には成功したけどエラー、っていうケースで、うーーん、、400とかどうなっちゃうんだろう。
					unauthあたりがどう出るか。
				*/

				var responseHeaders = request.GetResponseHeaders();
				foreach (var a in responseHeaders) {
					Debug.LogError("a:" + a);
				}


				/*
					この時点で通信は終わってるんで、

					・エラーハンドルができるための情報をどう渡すか(平易な型情報) -> responseヘッダ？ 
						何かのデータ？ データもらえない場合は？みたいなのが
						見たいな。データサンプル作ってから考えるかな。

						どっちにしてもstringとか渡す前提だな、AssetBundleをCacheするのとかはなんか専用で考えたほうが良いのかな。
					・
				*/
			}
		}

		public string Post (HTTPHeader additionalHeader, string url, string data, Action<string, string> succeeded, Action<string, int, string> failed) {
			return "additionalHeader + dummyConnectionId";
		}

		public string Post (string url, string data, Action<string, string> succeeded, Action<string, int, string> failed) {
			return "dummyConnectionId";
		}
	}

}