using System;
using System.Linq;
using UnityEngine;

/**
	data structure for AssetBunlde - Asset information.

    list
        └ target // human readable target platform name.
    	└ version // human readable version desc.
    	└ assetBundles // assetBundleInfo[]
    		└ assetBundleInfo
    			└ bundleName // bundle name.
    			└ assetNames // contained asset names. e,g, "Assets/Somewhere/texture.png"
    			└ dependsBundleNames // the bundle names which this assetBundle depends on.
    			└ crc // crc parameter. used for crc check.
    			└ hash // hash parameter. used for exchange same asset from old one to new one.
    			└ size // size of uncompressed AssetBundle.
 */
namespace AutoyaFramework.AssetBundles {
	
    /// <summary>
    /// Asset bundle list type.
    /// </summary>
	[Serializable] public class AssetBundleList {
		[SerializeField] public string target;
		[SerializeField] public string version;
		[SerializeField] public AssetBundleInfo[] assetBundles;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AutoyaFramework.AssetBundles.AssetBundleList"/> class.
        /// </summary>
		public AssetBundleList () {}

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AutoyaFramework.AssetBundles.AssetBundleList"/> class.
        /// </summary>
        /// <param name="target">Target.</param>
        /// <param name="version">Version.</param>
        /// <param name="assetBundles">Asset bundles.</param>
		public AssetBundleList (string target, string version, AssetBundleInfo[] assetBundles) {
			this.target = target;
			this.version = version;
			this.assetBundles = assetBundles;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AutoyaFramework.AssetBundles.AssetBundleList"/> class.
        /// </summary>
        /// <param name="baseList">Base list.</param>
		public AssetBundleList (AssetBundleList baseList) {
			this.target = baseList.target;
			this.version = baseList.version;
			this.assetBundles = new AssetBundleInfo[baseList.assetBundles.Length];
			for (var i = 0; i < assetBundles.Length; i++) {
				assetBundles[i] = new AssetBundleInfo(baseList.assetBundles[i]);
			}
		}

        /// <summary>
        /// Exists this instance.
        /// </summary>
        /// <returns>The exists.</returns>
		public bool Exists () {
			return (!string.IsNullOrEmpty(this.target) && !string.IsNullOrEmpty(this.version) && this.assetBundles.Any());
		}
	}

    /// <summary>
    /// type of Asset bundle info.
    /// </summary>
	[Serializable] public struct AssetBundleInfo {
		[SerializeField] public string bundleName;
		[SerializeField] public string[] assetNames;
		[SerializeField] public string[] dependsBundleNames;
		[SerializeField] public uint crc;
		[SerializeField] public string hash;
		[SerializeField] public long size;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AutoyaFramework.AssetBundles.AssetBundleInfo"/> struct.
        /// </summary>
        /// <param name="bundleName">Bundle name.</param>
        /// <param name="assetNames">Asset names.</param>
        /// <param name="dependsBundleNames">Depends bundle names.</param>
        /// <param name="crc">Crc.</param>
        /// <param name="hash">Hash.</param>
        /// <param name="size">Size.</param>
		public AssetBundleInfo (string bundleName, string[] assetNames, string[] dependsBundleNames, uint crc, string hash, long size) {
			this.bundleName = bundleName;
			this.assetNames = assetNames;
			this.dependsBundleNames = dependsBundleNames;
			this.crc = crc;
			this.hash = hash;
			this.size = size;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AutoyaFramework.AssetBundles.AssetBundleInfo"/> struct.
        /// </summary>
        /// <param name="baseAssetBundleInfo">Base asset bundle info.</param>
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