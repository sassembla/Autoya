using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoyaFramework.Settings.AssetBundles;
using UnityEngine;
using UnityEngine.Networking;

namespace AutoyaFramework.AssetBundles {

	[Serializable] public class PreloadList {
		public string name;
		public string[] bundleNames;
	}

	public class AssetBundlePreloader {
		private readonly string basePath;

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
		
		public AssetBundlePreloader (Autoya.AssetBundleGetRequestHeaderDelegate requestHeader=null, Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate=null) {
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

		public IEnumerator Preload (AssetBundleLoader loader, string url, Action<double> progress, Action done, Action<int, string, AutoyaStatus> listDownloadFailed, Action<string, AssetBundleLoadError, AutoyaStatus> bundlePreloadFailed, double timeoutSec, int maxParallelCount=1) {
			if (0 < maxParallelCount) {
				// pass.
			} else {
				yield return null;
				listDownloadFailed(-1, "maxParallelCount is negative or 0. unable to start preload.", new AutoyaStatus());
			}

			if (loader != null) {
				// pass.
			} else {
				yield return null;
				listDownloadFailed(-1, "attached AssetBundleLoader is null. unable to start preload.", new AutoyaStatus());
			}

			var connectionId = AssetBundlesSettings.ASSETBUNDLES_PRELOADLIST_PREFIX + Guid.NewGuid().ToString();
			var reqHeader = requestHeader(url, new Dictionary<string, string>());
			var timeoutTick = (DateTime.UtcNow + TimeSpan.FromSeconds(timeoutSec)).Ticks;

			PreloadList list = null;
			Action<string, object> listDonwloadSucceeded = (conId, listData) => {
				var listStr = listData as string;
				list = JsonUtility.FromJson<PreloadList>(listStr);
			};

			Action<string, int, string, AutoyaStatus> listDownloadFailedAct = (conId, code, reason, autoyaStatus) => {
				listDownloadFailed(code, reason, autoyaStatus);
			};

			Action<string, object> bundlePreloadSucceededAct = (conId, obj) => {
				Debug.LogError("bundlePreloadSucceededAct着火、これでAssetBundleが手に入っている。");
			};

			Action<string, int, string, AutoyaStatus> bundlePreloadFailedAct = (bundleName, code, reason, autoyaStatus) => {
				Debug.LogError("bundlePreloadFailedActに到達");
				var error = AssetBundleLoadError.Undefined;
				bundlePreloadFailed(bundleName, error, autoyaStatus);
			};

			var downloadCoroutine = DownloadPreloadList(
				connectionId,
				reqHeader,
				url,
				(conId, code, responseHeaders, listData) => {
					httpResponseHandlingDelegate(connectionId, responseHeaders, code, listData, string.Empty, listDonwloadSucceeded, listDownloadFailedAct);
				}, 
				(conId, code, reason, responseHeaders) => {
					httpResponseHandlingDelegate(connectionId, responseHeaders, code, string.Empty, reason, listDonwloadSucceeded, listDownloadFailedAct);
				},
				timeoutTick
			);

			while (downloadCoroutine.MoveNext()) {
				yield return null;
			}

			// check if the list download is done.
			if (list != null) {
				// pass.
			} else {
				yield break;
			}
			
			// got preload list.
			var progressCount = 0.0;
			progress(progressCount);

			/*
				check if preloadList's assetBundleNames are contained by assetBundleList.
			 */
			var assetBundleList = loader.list;

			var targetAssetBundleNames = list.bundleNames;
			var assetBundleListContainedAssetBundleNames = assetBundleList.assetBundles.Select(a => a.bundleName).ToList();

			foreach (var targetAssetBundleName in targetAssetBundleNames) {
				if (assetBundleListContainedAssetBundleNames.Contains(targetAssetBundleName)) {
					// pass.
				} else {
					bundlePreloadFailed(targetAssetBundleName, AssetBundleLoadError.NotContainedAssetBundle, new AutoyaStatus());
					yield break;
				}
			}

			// start preload assetBundles.
			var loadingCoroutines = new List<IEnumerator>();

			foreach (var targetAssetBundleName in targetAssetBundleNames) {
				if (loader.IsAssetBundleCachedOnStorage(targetAssetBundleName)) {
					Debug.LogError("targetAssetBundleName is cached. targetAssetBundleName:" + targetAssetBundleName);
					continue;
				}

				var bundleLoadConId = AssetBundlesSettings.ASSETBUNDLES_PRELOADBUNDLE_PREFIX + Guid.NewGuid().ToString();
				var bundleUrl = loader.GetAssetBundleDownloadUrl(targetAssetBundleName);
				var bundleReqHeader = requestHeader(url, new Dictionary<string, string>());

				var targetBundleInfo = loader.AssetBundleInfo(targetAssetBundleName);
				var crc = targetBundleInfo.crc;
				var hash = Hash128.Parse(targetBundleInfo.hash);
				
				var bundlePreloadTimeoutTick = 0;// preloader does not have limit now.

				var cor = loader.DownloadAssetBundle(
					targetAssetBundleName, 
					bundleLoadConId,
					bundleReqHeader,
					bundleUrl,
					crc,
					hash,
					(conId, code, responseHeaders, bundle) => {
						httpResponseHandlingDelegate(connectionId, responseHeaders, code, bundle, string.Empty, bundlePreloadSucceededAct, bundlePreloadFailedAct);
						progress(progressCount);
					},
					(conId, code, reason, responseHeaders) => {
						httpResponseHandlingDelegate(connectionId, responseHeaders, code, string.Empty, reason, bundlePreloadSucceededAct, bundlePreloadFailedAct);
					},
					bundlePreloadTimeoutTick
				);

				loadingCoroutines.Add(cor);
			}

			var currentDownloadCount = 0;
			var totalLoadingCoroutinesCount = loadingCoroutines.Count;

			var currentLoadingCoroutines = new List<IEnumerator>();
			while (true) {
				if (0 < currentLoadingCoroutines.Count) {
					for (var j = 0; j < currentLoadingCoroutines.Count; j++) {
						var currentLoadingCoroutine = currentLoadingCoroutines[j];
						var result = currentLoadingCoroutine.MoveNext();
						if (!result) {
							currentLoadingCoroutines.Remove(currentLoadingCoroutine);
							
							currentDownloadCount++;

							var count = ((currentDownloadCount * 100.0) / totalLoadingCoroutinesCount);
							progress(count);
						}
					}
					yield return null;
				}

				for (var i = 0; i < maxParallelCount; i++) {
					if (loadingCoroutines.Count == 0) {
						// every bundle downloading is done.
						done();
						yield break;
					}

					var next = loadingCoroutines[0];
					loadingCoroutines.RemoveAt(0);
					currentLoadingCoroutines.Add(next);
				}
			}
		}

		private IEnumerator DownloadPreloadList (
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
						failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, "timeout to download preload list:" + url, new Dictionary<string, string>());
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
					failed(connectionId, responseCode, "failed to download preload list:" + url + " was not downloaded.", responseHeaders);
					yield break;
				}

				var result = Encoding.UTF8.GetString(request.downloadHandler.data);
				succeeded(connectionId, responseCode, responseHeaders, result);
			}
		}
		
	}
}