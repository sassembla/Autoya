using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AutoyaFramework.AssetBundles {
	
	[Serializable] public struct AssetBundleList {
		[SerializeField] public string target;
		[SerializeField] public string version;
		[SerializeField] public AssetBundleInfo[] assetBundles;
		public AssetBundleList (string target, string version, AssetBundleInfo[] assetBundles) {
			this.target = target;
			this.version = version;
			this.assetBundles = assetBundles;
		}

		public AssetBundleList (AssetBundleList baseList) {
			this.target = baseList.target;
			this.version = baseList.version;
			this.assetBundles = new AssetBundleInfo[baseList.assetBundles.Length];
			for (var i = 0; i < assetBundles.Length; i++) {
				assetBundles[i] = new AssetBundleInfo(baseList.assetBundles[i]);
			}
		}
	}

	[Serializable] public struct AssetBundleInfo {
		[SerializeField] public string bundleName;
		[SerializeField] public string[] assetNames;
		[SerializeField] public string[] dependsBundleNames;
		[SerializeField] public uint crc;
		[SerializeField] public string hash;
		public AssetBundleInfo (string bundleName, string[] assetNames, string[] dependsBundleNames, uint crc, string hash) {
			this.bundleName = bundleName;
			this.assetNames = assetNames;
			this.dependsBundleNames = dependsBundleNames;
			this.crc = crc;
			this.hash = hash;
		}

		public AssetBundleInfo (AssetBundleInfo baseAssetBundleInfo) {
			this.bundleName = baseAssetBundleInfo.bundleName;
			this.assetNames = new string[baseAssetBundleInfo.assetNames.Length];
			for (var i = 0; i < assetNames.Length; i++) {
				assetNames[i] = baseAssetBundleInfo.assetNames[i];
			}

			this.dependsBundleNames = new string[baseAssetBundleInfo.dependsBundleNames.Length];
			for (var i = 0; i < dependsBundleNames.Length; i++) {
				dependsBundleNames[i] = baseAssetBundleInfo.dependsBundleNames[i];
			}

			this.crc = baseAssetBundleInfo.crc;
			this.hash = baseAssetBundleInfo.hash;
		}
	}
	
	public class AssetBundleListDownloader {
		private readonly string basePath;
		private readonly Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate;

		private void BasicResponseHandlingDelegate (string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed) {
			if (200 <= httpCode && httpCode < 299) {
				succeeded(connectionId, data);
				return;
			}
			failed(connectionId, httpCode, errorReason, new AutoyaStatus());
		}

		public AssetBundleListDownloader (string basePath, Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate =null) {
			this.basePath = basePath;

			if (httpResponseHandlingDelegate == null) {
				this.httpResponseHandlingDelegate = BasicResponseHandlingDelegate;
			} else {
				this.httpResponseHandlingDelegate = httpResponseHandlingDelegate;
			}
		}

		public IEnumerator DownloadList () {
			yield return null;
		}
	}
}