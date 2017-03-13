using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoyaFramework.Settings.AssetBundles;
using UnityEngine;
using UnityEngine.Networking;

namespace AutoyaFramework.AssetBundles {
	public class AssetBundleLoader {

		public const int CODE_CRC_MISMATCHED = 399;

		public enum AssetBundleLoadError {
			Unauthorized,
			NotContained,
			CrcMismatched,
			DownloadFailed,
			AssetLoadFailed,
			NullAssetFound,
			NoAssetBundleFoundInList,
			FailedToLoadDependentBundles
		}

		private string assetDownloadBasePath;
		private AssetBundleList list;

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

		public AssetBundleLoader (string basePath, AssetBundleList list, Autoya.AssetBundleGetRequestHeaderDelegate requestHeader=null, Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate=null) {

			this.assetDownloadBasePath = basePath;
			this.list = list;

			if (requestHeader != null) {
				this.requestHeader = requestHeader;
			} else {
				this.requestHeader = BasicRequestHeaderDelegate;
			}

			if (httpResponseHandlingDelegate == null) {
				this.httpResponseHandlingDelegate = BasicResponseHandlingDelegate;
			} else {
				this.httpResponseHandlingDelegate = httpResponseHandlingDelegate;
			}

			/*
				construct assetName - AssetBundleName dictionary for fast loading.
			*/
			assetNamesAndAssetBundleNamesDict.Clear();

			foreach (var assetBundle in list.assetBundles) {
				var bundleName = assetBundle.bundleName;
				foreach (var assetName in assetBundle.assetNames) {
					assetNamesAndAssetBundleNamesDict[assetName] = bundleName;
				}
			}
		}

		/**
			update assetbundle list and basePath.
		 */
		public void UpdateList (string basePath, AssetBundleList newList, Action<string[], string> updatedOnMemoryAssetNameAndBundleName) {
			this.assetDownloadBasePath = basePath;
			
			/*
				check updated asset -> notify with asset names.
			 */
			var loadedBundleNames = assetBundleDict.Keys.ToArray();
			if (loadedBundleNames.Any()) {
				// current.
				var bundleNameHashDict = this.list.assetBundles
					.Where(b => loadedBundleNames.Contains(b.bundleName))
					.ToDictionary(i => i.bundleName, i => i.hash);

				// new.
				var newBundleNameHashDict = newList.assetBundles
					.Where(b => loadedBundleNames.Contains(b.bundleName))
					.ToDictionary(i => i.bundleName, i => i.hash);
				
				foreach (var loadedBundleName in loadedBundleNames) {
					if (!newBundleNameHashDict.ContainsKey(loadedBundleName)) {
						continue;
					}

					var currentHash = bundleNameHashDict[loadedBundleName];
					var newHash = newBundleNameHashDict[loadedBundleName];

					// if hash is not matched between current and new, this "on memory assetBundle" is updated.
					// notify to user.
					if (currentHash != newHash) {
						var updatedAssetNames = assetBundleDict[loadedBundleName].GetAllAssetNames();
						updatedOnMemoryAssetNameAndBundleName(updatedAssetNames, loadedBundleName);
					}
				}
			}
			
			// update list.
			this.list = newList;
		}

		/*
			unload all assetBundles and delete all assetBundle caches.
		*/
		public bool CleanCachedAssetBundles () {
			/*
				clean all loaded assets.
			*/
			UnloadAllAssetBundles();
			
			return Caching.CleanCache();
		}

		private List<string> loadingAssetBundleNames = new List<string>();
		private Dictionary<string, string> assetNamesAndAssetBundleNamesDict = new Dictionary<string, string>();

		/**
			load specific type Asset from AssetBundle.
			dependency of AssetBundle will be solved automatically.

			note:
				this timeoutSec param is enabled only for downloading AssetBundle from web.

				複数のAssetBundleに依存していて、それのうちのひとつとかがtimeoutしたら
				

		*/
		public IEnumerator LoadAsset<T> (
			string assetName, 
			Action<string, T> loadSucceeded, 
			Action<string, AssetBundleLoadError, string, AutoyaStatus> loadFailed, 
			double timeoutSec=0) where T : UnityEngine.Object {
			if (!assetNamesAndAssetBundleNamesDict.ContainsKey(assetName)) {
				loadFailed(assetName, AssetBundleLoadError.NotContained, string.Empty, new AutoyaStatus());
				yield break;
			}
			
			var timeoutTick = (DateTime.UtcNow + TimeSpan.FromSeconds(timeoutSec)).Ticks;
			if (timeoutSec == 0) timeoutTick = 0;

			var bundleName = assetNamesAndAssetBundleNamesDict[assetName];
			var assetBundleInfo = list.assetBundles.Where(a => a.bundleName == bundleName).ToArray();
			
			if (assetBundleInfo.Length == 0) {
				// no assetBundleInfo found.
				loadFailed(assetName, AssetBundleLoadError.NoAssetBundleFoundInList, "no assetBundle found:" + bundleName + " in list.", new AutoyaStatus());
				yield break;
			}

			var crc = assetBundleInfo[0].crc;
			var hash = Hash128.Parse(assetBundleInfo[0].hash);
			
			var coroutine = LoadAssetBundleOnMemory(bundleName, assetName, crc, hash, loadSucceeded, loadFailed, timeoutTick);
			while (coroutine.MoveNext()) {
				yield return null;
			}
		}

		private struct DependentBundleError {
			readonly public string bundleName;
			readonly public AssetBundleLoadError err;
			readonly public string reason;
			readonly public AutoyaStatus status;

			public DependentBundleError (string bundleName, AssetBundleLoadError err, string reason, AutoyaStatus status) {
				this.bundleName = bundleName;
				this.err = err;
				this.reason = reason;
				this.status = status;
			}
		}

		/**
			load assetBundle on memory.
		*/
		private IEnumerator LoadAssetBundleOnMemory<T> (
			string bundleName, 
			string assetName, 
			uint crc,
			Hash128 hash,
			Action<string, T> loadSucceeded, 
			Action<string, AssetBundleLoadError, string, AutoyaStatus> loadFailed, 
			long timeoutTick, 
			bool isDependency=false
		) where T : UnityEngine.Object {
			while (!Caching.ready) {
				yield return null;
			}

			var dependentBundleNames = list.assetBundles.Where(bundle => bundle.bundleName == bundleName).FirstOrDefault().dependsBundleNames;
			var assetBundleInfo = list.assetBundles.Where(a => a.bundleName == bundleName).ToArray();
			
			if (assetBundleInfo.Length == 0) {
				// no assetBundleInfo found.
				loadFailed(assetName, AssetBundleLoadError.NoAssetBundleFoundInList, "no assetBundle found:" + bundleName + " in list.", new AutoyaStatus());
				yield break;
			}
			
			var dependentBundleLoadErrors = new List<DependentBundleError>();

			/*
				resolve dependencies.
			*/
			{
				if (dependentBundleNames.Any()) {
					var coroutines = new Dictionary<string, IEnumerator>();

					foreach (var dependentBundleName in dependentBundleNames) {
						// skip if assetBundle is already loaded on memory.
						if (assetBundleDict.ContainsKey(dependentBundleName) && assetBundleDict[dependentBundleName] != null) {
							continue;
						}

						var dependedBundleInfos = list.assetBundles.Where(a => a.bundleName == dependentBundleName).ToArray();
						if (dependedBundleInfos.Length != 1) {
							continue;
						}

						var dependedBundleInfo = dependedBundleInfos[0];
						var dependentBundleCrc = dependedBundleInfo.crc;
						var dependentBundleHash = Hash128.Parse(dependedBundleInfo.hash);

						// set UnityEngine.Object for request. this asset will never use directory.
						var loadCoroutine = LoadAssetBundleOnMemory<UnityEngine.Object>(
							dependentBundleName, 
							string.Empty,// bundleName not required.
							dependentBundleCrc,
							dependentBundleHash,
							(depBundleName, depObj) => {
								// do nothing. this bundle is currently on memory.
							},
							(depBundleName, depErr, depReason, autoyaStatus) => {
								// collect error for this dependent bundle loading.
								dependentBundleLoadErrors.Add(new DependentBundleError(depBundleName, depErr, depReason, autoyaStatus));
							},
							timeoutTick,
							true // this loading is for resolve dependency of root asset. no need to return any instances.
						);

						coroutines[dependentBundleName] = loadCoroutine;
					}

					if (coroutines.Count != 0) {
						while (true) {
							if (!coroutines.Where(c => c.Value != null).Any()) {
								// load done.
								break;
							}

							for (var i = 0; i < coroutines.Count; i++) {
								var loadingAssetBundleName = coroutines.Keys.ToArray()[i];
								var coroutine = coroutines[loadingAssetBundleName];
								if (coroutine == null) continue;

								if (!coroutine.MoveNext()) {
									coroutines[loadingAssetBundleName] = null;
								}
							}
							yield return null;
						}

						// all dependencies are loaded on memory.
					}
				}
			}

			// check now loading or not. if same bundle is already under loading, wait it here.
			while (loadingAssetBundleNames.Contains(bundleName)) {
				yield return null;
			}

			var url = GetAssetBundleDownloadUrl(bundleName);
			
			// assetBundle is already allocated on memory. load that.
			if (assetBundleDict.ContainsKey(bundleName)) {
				if (isDependency) {
					yield break;
				}

				var loadOnMemoryCachedAssetCoroutine = LoadOnMemoryAssetAsync(bundleName, assetName, loadSucceeded, loadFailed);
				while (loadOnMemoryCachedAssetCoroutine.MoveNext()) {
					yield return null;
				}
				yield break;
			}

			/*
				assetBundle is..
					not yet cached (or) already cached.
					not allocated on memory.

				assetBundle is not on memory yet. start downloading.
			*/
			
			// binded block.
			using (var loadingConstraint = new AssetBundleLoadingConstraint(bundleName, loadingAssetBundleNames)) {
				/*
					download bundle or load donwloaded bundle from cache.
					load to memory.
				*/
				{
					var downloadCoroutine = DownloadAssetThenCacheAndLoadToMemory(bundleName, assetName, url, crc, hash, loadFailed, timeoutTick);
					while (downloadCoroutine.MoveNext()) {
						yield return null;
					}

					if (!assetBundleDict.ContainsKey(bundleName)) {
						// error is already fired in above.
						yield break;
					}
					
					if (!isDependency) {

						/*
							break if dependent bundle has load error.
						*/
						if (dependentBundleLoadErrors.Any()) {
							var loadErrorBundleMessages = new StringBuilder();
							loadErrorBundleMessages.Append("failed to load/download dependent bundle:");
							foreach (var dependentBundleLoadError in dependentBundleLoadErrors) {
								loadErrorBundleMessages.Append("bundleName:");
								loadErrorBundleMessages.Append(dependentBundleLoadError.bundleName);
								loadErrorBundleMessages.Append(" error:");
								loadErrorBundleMessages.Append(dependentBundleLoadError.err);
								loadErrorBundleMessages.Append(" reason:");
								loadErrorBundleMessages.Append(dependentBundleLoadError.reason);
								loadErrorBundleMessages.Append(" autoyaStatus:");
								loadErrorBundleMessages.Append("  inMaintenance:" + dependentBundleLoadError.status.inMaintenance);
								loadErrorBundleMessages.Append("  isAuthFailed:" + dependentBundleLoadError.status.isAuthFailed);
							}
							loadFailed(assetName, AssetBundleLoadError.FailedToLoadDependentBundles, loadErrorBundleMessages.ToString(), new AutoyaStatus());
							
							yield break;
						}

						/*
							load asset from on memory AssetBundle.
						*/
						var loadAssetCoroutine = LoadOnMemoryAssetAsync(bundleName, assetName, loadSucceeded, loadFailed);
						while (loadAssetCoroutine.MoveNext()) {
							yield return null;
						}
					}
				}
			}
		}

		private class AssetBundleLoadingConstraint : IDisposable {
			private string target;
			private List<string> list;
			
			public AssetBundleLoadingConstraint (string target, List<string> list) {
				this.target = target;
				this.list = list;

				this.list.Add(this.target);
			}

			private bool disposedValue = false;

			protected virtual void Dispose (bool disposing) {
				if (!disposedValue) {
					if (disposing) {
						list.Remove(target);
					}
					disposedValue = true;
				}
			}

			void IDisposable.Dispose () {
				Dispose(true);
			}
		}

		private IEnumerator DownloadAssetThenCacheAndLoadToMemory (
			string bundleName, 
			string assetName, 
			string url, 
			uint crc, 
			Hash128 hash,
			Action<string, AssetBundleLoadError, string, AutoyaStatus> failed, 
			long timeoutTick
		) {
			var connectionId = AssetBundlesSettings.ASSETBUNDLES_DOWNLOAD_PREFIX + Guid.NewGuid().ToString();

			Action<string, object> succeeded = (conId, downloadedAssetBundle) => {
				// set loaded assetBundle to on-memory cache.
				assetBundleDict[bundleName] = (AssetBundle)downloadedAssetBundle;
			};

			Action<string, int, string, AutoyaStatus> downloadFailed = (conId, code, reason, autoyaStatus) => {
				// 結局codeに依存しないエラーが出ちゃうのをどうしようかな、、、仕組みで避けきるしかないのか、、try-catchできないからな、、
				switch (code) {
					case CODE_CRC_MISMATCHED: {
						failed(assetName, AssetBundleLoadError.CrcMismatched, reason, autoyaStatus);
						break;
					}
					default: {
						failed(assetName, AssetBundleLoadError.DownloadFailed, "failed to download AssetBundle. code:" + code + " reason:" + reason, autoyaStatus);
						break;
					}
				}
			};

			var reqHeader = requestHeader(url, new Dictionary<string, string>());
			
			var connectionCoroutine = DownloadAssetBundle(
				bundleName,
				connectionId,
				reqHeader,
				url, 
				crc, 
				hash,
				(conId, code, responseHeader, downloadedAssetBundle) => {
					httpResponseHandlingDelegate(connectionId, responseHeader, code, downloadedAssetBundle, string.Empty, succeeded, downloadFailed);
				}, 
				(conId, code, reason, responseHeader) => {
					httpResponseHandlingDelegate(connectionId, responseHeader, code, string.Empty, reason, succeeded, downloadFailed);
				},
				timeoutTick
			);

			while (connectionCoroutine.MoveNext()) {
				yield return null;
			}
		}
		
		private IEnumerator DownloadAssetBundle (
			string bundleName, 
			string connectionId, 
			Dictionary<string, string> requestHeader, 
			string url, 
			uint crc, 
			Hash128 hash,
			Action<string, int, Dictionary<string, string>, AssetBundle> succeeded, 
			Action<string, int, string, Dictionary<string, string>> failed, 
			long limitTick
		) {
			var alreadyStorageCached = false;
			if (Caching.IsVersionCached(url, hash)) {
				alreadyStorageCached = true;
			}

			using (var request = UnityWebRequest.GetAssetBundle(url, hash, crc)) {
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
						failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, "timeout to download bundle:" + bundleName, new Dictionary<string, string>());
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

				if (alreadyStorageCached) {
					// set response code to 200 manually if already cached and succeeded to load from cache.
					// sadly, in this case, the code is not 200 by default.
					responseCode = 200;
				} else {
					if (200 <= responseCode && responseCode <= 299) {
						// do nothing.
					} else {
						failed(connectionId, responseCode, "failed to load assetBundle. downloaded bundle:" + bundleName + " was not downloaded.", responseHeaders);
						yield break;
					}
				}

				var dataHandler = (DownloadHandlerAssetBundle)request.downloadHandler;
				
				var assetBundle = dataHandler.assetBundle;

				if (assetBundle == null) {
					responseCode = CODE_CRC_MISMATCHED;
					failed(connectionId, responseCode, "failed to load assetBundle. downloaded bundle:" + bundleName + ", requested crc was not matched.", responseHeaders);
					yield break;
				}
				
				// wait for cache.
				while (!Caching.IsVersionCached(url, hash)) {
					yield return null;
				}

				succeeded(connectionId, responseCode, responseHeaders, assetBundle);
			}
		}

		private Dictionary<string, AssetBundle> assetBundleDict = new Dictionary<string, AssetBundle>();
		private IEnumerator LoadOnMemoryAssetAsync<T> (string bundleName, string assetName, Action<string, T> loadSucceeded, Action<string, AssetBundleLoadError, string, AutoyaStatus> loadFailed) where T : UnityEngine.Object {
			var assetBundle = assetBundleDict[bundleName];
			var request = assetBundle.LoadAssetAsync<T>(assetName);			
			while (!request.isDone) {
				yield return null;
			}

			/*
				asset is loaded asynchronously.
			*/
			try {
				var asset = request.asset as T;

				if (asset == null) {
					loadFailed(assetName, AssetBundleLoadError.NullAssetFound, "loaded assetName:" + assetName + " type:" + typeof(T) + " is null. maybe type does not matched. from bundleName:" + bundleName + ". please check asset type and that bundle contains this asset.", new AutoyaStatus());
					yield break;
				}

				loadSucceeded(assetName, asset);
			} catch (Exception e) {
				loadFailed(assetName, AssetBundleLoadError.AssetLoadFailed, "failed to load assetName:" + assetName + " from bundleName:" + bundleName + " error:" + e.ToString(), new AutoyaStatus());
			}
		}

		private string GetAssetBundleDownloadUrl (string bundleName) {
			return assetDownloadBasePath + bundleName;
		}

		public string[] OnMemoryAssetNames () {
			var loadedAssetBundleNames = assetBundleDict.Where(kv => kv.Value != null).Select(kv => kv.Key).ToArray();
			return list.assetBundles.Where(ab => loadedAssetBundleNames.Contains(ab.bundleName)).SelectMany(ab => ab.assetNames).ToArray();
		}

		public string GetContainedAssetBundleName (string assetName) {
			if (!assetNamesAndAssetBundleNamesDict.ContainsKey(assetName)) {
				return string.Empty;
			}
			return assetNamesAndAssetBundleNamesDict[assetName];
		}

		public bool IsAssetBundleCachedOnMemory (string bundleName) {
			var assetBundleNames = assetBundleDict.Keys.ToArray();
			return assetBundleNames.Contains(bundleName);
		}

		public bool IsAssetBundleCachedOnStorage (string bundleName) {
			var candidateAssetBundles = list.assetBundles.Where(a => a.bundleName == bundleName).ToArray();
			if (candidateAssetBundles.Length == 0) {
				return false;
			}
			
			var url = GetAssetBundleDownloadUrl(bundleName);
			var hash = Hash128.Parse(candidateAssetBundles[0].hash);
			return Caching.IsVersionCached(url, hash);
		}

		public void UnloadAllAssetBundles () {
			var assetBundleNames = assetBundleDict.Keys.ToArray();

			foreach (var assetBundleName in assetBundleNames) {
				var asset = assetBundleDict[assetBundleName];
				if (asset != null) {
					asset.Unload(true);
				}
			}

			assetBundleDict.Clear();
		}

		public void UnloadAssetBundle (string bundleName) {
			if (assetBundleDict.ContainsKey(bundleName)) {
				var asset = assetBundleDict[bundleName];
				if (asset != null) {
					asset.Unload(true);
				}
				
				assetBundleDict.Remove(bundleName);
			}
		}
	}
}