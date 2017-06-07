using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AutoyaFramework.Settings.AssetBundles;
using UnityEngine;
using UnityEngine.Networking;

namespace AutoyaFramework.AssetBundles {
	
	public class AssetBundleListDownloader {
		
		private readonly Autoya.AssetBundleGetRequestHeaderDelegate requestHeader;
		private Dictionary<string, string> BasicRequestHeaderDelegate (string url, Dictionary<string, string> requestHeader) {
			return requestHeader;
		}

		private readonly Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate;

		private void BasicResponseHandlingDelegate (string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed) {
			if (200 <= httpCode && httpCode < 299) {
				succeeded(connectionId, data);
				return;
			}
			failed(connectionId, httpCode, errorReason, new AutoyaStatus());
		}

		public AssetBundleListDownloader (Autoya.AssetBundleGetRequestHeaderDelegate requestHeader=null, Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate =null) {
			if (requestHeader != null) {
				this.requestHeader = requestHeader;
			} else {
				this.requestHeader = BasicRequestHeaderDelegate;
			}

			if (httpResponseHandlingDelegate != null) {
				this.httpResponseHandlingDelegate = httpResponseHandlingDelegate;
			} else {
				this.httpResponseHandlingDelegate = BasicResponseHandlingDelegate;
			}
		}
		
		public IEnumerator DownloadAssetBundleList (string url, Action<AssetBundleList> done, Action<string, AssetBundleLoadError, string, AutoyaStatus> failed, double timeoutSec) {
			var connectionId = AssetBundlesSettings.ASSETBUNDLES_ASSETBUNDLELIST_PREFIX + Guid.NewGuid().ToString();
			var reqHeader = requestHeader(url, new Dictionary<string, string>());
			var timeoutTick = (DateTime.UtcNow + TimeSpan.FromSeconds(timeoutSec)).Ticks;

			AssetBundleList assetBundleList = null;
			Action<string, object> listDonwloadSucceeded = (conId, listData) => {
				var listString = listData as string;
				assetBundleList = JsonUtility.FromJson<AssetBundleList>(listString);
				
				done(assetBundleList);
			};

			Action<string, int, string, AutoyaStatus> listDownloadFailed = (conId, code, reason, autoyaStatus) => {
				failed(url, AssetBundleLoadError.FailedToGetAssetBundleList, reason, autoyaStatus);
			};
			
			var downloadCoroutine = DownloadAssetBundleList(
				connectionId,
				reqHeader,
				url,
				(conId, code, responseHeader, listData) => {
					httpResponseHandlingDelegate(connectionId, responseHeader, code, listData, string.Empty, listDonwloadSucceeded, listDownloadFailed);
				}, 
				(conId, code, reason, responseHeader) => {
					httpResponseHandlingDelegate(connectionId, responseHeader, code, string.Empty, reason, listDonwloadSucceeded, listDownloadFailed);
				},
				timeoutTick
			);

			while (downloadCoroutine.MoveNext()) {
				yield return null;
			}
		}

		private IEnumerator DownloadAssetBundleList (
			string connectionId, 
			Dictionary<string, string> requestHeader, 
			string url, 
			Action<string, int, Dictionary<string, string>, string> succeeded, 
			Action<string, int, string, Dictionary<string, string>> failed, 
			long limitTick
		) {
			using (var request = UnityWebRequest.Get(url)) {
				if (requestHeader != null) {
					foreach (var kv in requestHeader) {
						request.SetRequestHeader(kv.Key, kv.Value);
					}
				}
				
				var p = request.Send();
				
				while (!p.isDone) {
					yield return null;

					// check timeout.
					if (limitTick != 0 && limitTick < DateTime.UtcNow.Ticks) {
						request.Abort();
						failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, "timeout to download assetBundleList:" + url, new Dictionary<string, string>());
						yield break;
					}
				}

				while (!request.isDone) {
					yield return null;
				}

				var responseCode = (int)request.responseCode;
				var responseHeaders = request.GetResponseHeaders();

				if (request.isError) {
					failed(connectionId, responseCode, request.error, responseHeaders);
					yield break;
				}

				if (200 <= responseCode && responseCode <= 299) {
					// do nothing.
				} else {
					failed(connectionId, responseCode, "failed to load assetBundle. assetBundleList:" + url + " was not downloaded.", responseHeaders);
					yield break;
				}

				var result = Encoding.UTF8.GetString(request.downloadHandler.data);
				succeeded(connectionId, responseCode, responseHeaders, result);
			}
		}
	}
}