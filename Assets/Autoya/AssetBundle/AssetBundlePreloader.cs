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
		public PreloadList (string name, string[] bundleNames) {
			this.name = name;
			this.bundleNames = bundleNames;
		}
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

		public IEnumerator Preload (AssetBundleLoader loader, string url, Action<double> progress, Action done, Action<int, string, AutoyaStatus> preloadFailed, Action<string, AssetBundleLoadError, AutoyaStatus> bundleDownloadFailed, double timeoutSec, int maxParallelCount=1) {
			if (0 < maxParallelCount) {
				// pass.
			} else {
				yield return null;
				preloadFailed(-1, "maxParallelCount is negative or 0. unable to start preload.", new AutoyaStatus());
				yield break;
			}

			if (loader != null) {
				// pass.
			} else {
				yield return null;
				preloadFailed(-1, "attached AssetBundleLoader is null. unable to start preload.", new AutoyaStatus());
				yield break;
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
				preloadFailed(code, reason, autoyaStatus);
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
			
			var bundleDownloadCor = Preload(loader, list, progress, done, preloadFailed, bundleDownloadFailed, maxParallelCount);
			while (bundleDownloadCor.MoveNext()) {
				yield return null;
			}
		}

		public IEnumerator Preload (AssetBundleLoader loader, PreloadList list, Action<double> progress, Action done, Action<int, string, AutoyaStatus> preloadFailed, Action<string, AssetBundleLoadError, AutoyaStatus> bundlePreloadFailed, int maxParallelCount=1) {
			if (0 < maxParallelCount) {
				// pass.
			} else {
				yield return null;
				preloadFailed(-1, "maxParallelCount is negative or 0. unable to start preload.", new AutoyaStatus());
				yield break;
			}

			if (loader != null) {
				// pass.
			} else {
				yield return null;
				preloadFailed(-1, "attached AssetBundleLoader is null. unable to start preload.", new AutoyaStatus());
				yield break;
			}

			if (list != null) {
				// pass.
			} else {
				yield return null;
				preloadFailed(-1, "preloadList is null. unable to start preload.", new AutoyaStatus());
				yield break;
			}

			/*
				check if preloadList's assetBundleNames are contained by assetBundleList.
			 */
			var assetBundleList = loader.list;

			var targetAssetBundleNames = list.bundleNames;
			var assetBundleListContainedAssetBundleNames = assetBundleList.assetBundles.Select(a => a.bundleName).ToList();

			// start preload assetBundles.
			var loadingCoroutines = new Queue<IEnumerator>();

			var currentDownloadCount = 0;

			// define.
			var totalLoadingCoroutinesCount = 0;

			/*
				assetBundle downloaded actions.
			 */
			Action<string, object> bundlePreloadSucceededAct = (conId, obj) => {
				// unload assetBundle anyway.
				// downloaded assetBundle is 
				var bundle = obj as AssetBundle;
				bundle.Unload(true);
				
				currentDownloadCount++;

				var count = ((currentDownloadCount * 100.0) / totalLoadingCoroutinesCount)*0.01;
				progress(count);
			};

			Action<string, int, string, AutoyaStatus> bundlePreloadFailedAct = (bundleName, code, reason, autoyaStatus) => {
				Debug.LogError("bundlePreloadFailedActに到達");
				var error = AssetBundleLoadError.Undefined;
				bundlePreloadFailed(bundleName, error, autoyaStatus);
			};

			var wholeDownloadableAssetBundleNames = new List<string>();
			foreach (var targetAssetBundleName in targetAssetBundleNames) {
				if (!assetBundleListContainedAssetBundleNames.Contains(targetAssetBundleName)) {
					bundlePreloadFailed(targetAssetBundleName, AssetBundleLoadError.NotContainedAssetBundle, new AutoyaStatus());
					yield break;
				}
				
				// reserve this assetBundle and dependencies as "should be download".
				wholeDownloadableAssetBundleNames.Add(targetAssetBundleName);

				var dependentBundleNames = assetBundleList.assetBundles.Where(bundle => bundle.bundleName == targetAssetBundleName).FirstOrDefault().dependsBundleNames;
				wholeDownloadableAssetBundleNames.AddRange(dependentBundleNames);
			}

			var shouldDownloadAssetBundleNames = wholeDownloadableAssetBundleNames.Distinct().ToArray();
			
			foreach (var shouldDownloadAssetBundleName in shouldDownloadAssetBundleNames) {
				var bundleLoadConId = AssetBundlesSettings.ASSETBUNDLES_PRELOADBUNDLE_PREFIX + Guid.NewGuid().ToString();
				var bundleUrl = loader.GetAssetBundleDownloadUrl(shouldDownloadAssetBundleName);
				var bundleReqHeader = requestHeader(bundleUrl, new Dictionary<string, string>());

				var targetBundleInfo = loader.AssetBundleInfo(shouldDownloadAssetBundleName);
				var crc = targetBundleInfo.crc;
				var hash = Hash128.Parse(targetBundleInfo.hash);
				
				// check if bundle is cached.
				if (Caching.IsVersionCached(bundleUrl, hash)) {
					continue;
				}

				var bundlePreloadTimeoutTick = 0;// preloader does not have limit now.

				var cor = loader.DownloadAssetBundle(
					shouldDownloadAssetBundleName, 
					bundleLoadConId,
					bundleReqHeader,
					bundleUrl,
					crc,
					hash,
					(conId, code, responseHeaders, bundle) => {
						httpResponseHandlingDelegate(conId, responseHeaders, code, bundle, string.Empty, bundlePreloadSucceededAct, bundlePreloadFailedAct);
					},
					(conId, code, reason, responseHeaders) => {
						Debug.LogError("failed code:" + code);
						httpResponseHandlingDelegate(conId, responseHeaders, code, string.Empty, reason, bundlePreloadSucceededAct, bundlePreloadFailedAct);
					},
					bundlePreloadTimeoutTick
				);

				loadingCoroutines.Enqueue(cor);
			}

			// update total count to actual.
			totalLoadingCoroutinesCount = loadingCoroutines.Count;

			/*
				execute loading.
			 */
			var currentLoadingCoroutines = new IEnumerator[maxParallelCount];
			while (true) {
				for (var j = 0; j < currentLoadingCoroutines.Length; j++) {
					var currentLoadingCoroutine = currentLoadingCoroutines[j];
					if (currentLoadingCoroutine == null) {
						// set next coroutine to currentLoadingCoroutines.
						if (0 < loadingCoroutines.Count) {
							var next = loadingCoroutines.Dequeue();
							currentLoadingCoroutines[j] = next;
						} else {
							// no coroutine exists.
							continue;
						}
					}
				}
				
				var loading = false;
				for (var j = 0; j < currentLoadingCoroutines.Length; j++) {
					var cor = currentLoadingCoroutines[j];
					if (cor == null) {
						continue;
					}

					var result = cor.MoveNext();

					// just finished.
					if (!result) {
						currentLoadingCoroutines[j] = null;
					} else {
						loading = true;
					}
				}

				if (0 < loadingCoroutines.Count) {
					loading = true;
				}

				if (loading) {
					yield return null;
				} else {
					// nothing loading.
					break;
				}
			}
			
			// every bundle downloading is done.
			done();
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