using System;
using System.Linq;
using UnityEngine;

/**
	data structure for AssetBundle and Asset information.

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
namespace AutoyaFramework.AssetBundles
{

    /// <summary>
    /// Asset bundle list type.
    /// </summary>
    [Serializable]
    public class AssetBundleList
    {
        [SerializeField] public string identity;
        [SerializeField] public string target;
        [SerializeField] public string version;
        [SerializeField] public AssetBundleInfo[] assetBundles;

        public AssetBundleList() { }

        public AssetBundleList(string identity, string target, string version, AssetBundleInfo[] assetBundles)
        {
            this.identity = identity;
            this.target = target;
            this.version = version;
            this.assetBundles = assetBundles;
        }

        public AssetBundleList(AssetBundleList baseList)
        {
            this.identity = baseList.identity;
            this.target = baseList.target;
            this.version = baseList.version;
            this.assetBundles = new AssetBundleInfo[baseList.assetBundles.Length];
            for (var i = 0; i < assetBundles.Length; i++)
            {
                assetBundles[i] = new AssetBundleInfo(baseList.assetBundles[i]);
            }
        }

        public bool Exists()
        {
            var exists = !(string.IsNullOrEmpty(this.identity) || string.IsNullOrEmpty(this.target) || string.IsNullOrEmpty(this.version) || this.assetBundles.Length == 0);
            return exists;
        }
    }

    [Serializable]
    public struct AssetBundleInfo
    {
        [SerializeField] public string bundleName;
        [SerializeField] public string[] assetNames;
        [SerializeField] public string[] dependsBundleNames;
        [SerializeField] public uint crc;
        [SerializeField] public string hash;
        [SerializeField] public long size;

        public AssetBundleInfo(string bundleName, string[] assetNames, string[] dependsBundleNames, uint crc, string hash, long size)
        {
            this.bundleName = bundleName;
            this.assetNames = assetNames;
            this.dependsBundleNames = dependsBundleNames;
            this.crc = crc;
            this.hash = hash;
            this.size = size;
        }

        public AssetBundleInfo(AssetBundleInfo baseAssetBundleInfo)
        {
            this.bundleName = baseAssetBundleInfo.bundleName;
            this.assetNames = new string[baseAssetBundleInfo.assetNames.Length];
            for (var i = 0; i < assetNames.Length; i++)
            {
                assetNames[i] = baseAssetBundleInfo.assetNames[i];
            }

            this.dependsBundleNames = new string[baseAssetBundleInfo.dependsBundleNames.Length];
            for (var i = 0; i < dependsBundleNames.Length; i++)
            {
                dependsBundleNames[i] = baseAssetBundleInfo.dependsBundleNames[i];
            }

            this.crc = baseAssetBundleInfo.crc;
            this.hash = baseAssetBundleInfo.hash;
            this.size = baseAssetBundleInfo.size;
        }

        public static bool IsEmpty(AssetBundleInfo target)
        {
            return target.size == 0 || target.assetNames.Length == 0 || string.IsNullOrEmpty(target.bundleName);
        }
    }
}