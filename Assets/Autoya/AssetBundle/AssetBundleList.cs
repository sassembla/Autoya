using System;
using UnityEngine;

/**
	data structure for AssetBunlde - Asset information.

	list - target // target platform name.
		 \ version // human readable version desc.
		 \ assetBundles
		 		\ assetBundle
				 		\ bundleName // bundle name.
						\ assetNames // contained asset names. e,g, "Assets/Somewhere/texture.png"
						\ dependsBundleNames // the bundle names which this assetBundle depends on.
						\ crc // crc parameter. used for crc check.
						\ hash // hash parameter. used for exchange same asset from old one to new one.
 */
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
		[SerializeField] public long size;

		public AssetBundleInfo (string bundleName, string[] assetNames, string[] dependsBundleNames, uint crc, string hash, long size) {
			this.bundleName = bundleName;
			this.assetNames = assetNames;
			this.dependsBundleNames = dependsBundleNames;
			this.crc = crc;
			this.hash = hash;
			this.size = size;
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
			this.size = baseAssetBundleInfo.size;
		}
	}
}