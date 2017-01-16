using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AutoyaFramework.AssetBundles {
    
    [Serializable] public struct AssetBundleList {
        [SerializeField] public string version;
        [SerializeField] public AssetBundleInfo[] assetBundles;
        public AssetBundleList (string version, AssetBundleInfo[] assetBundles) {
            this.version = version;
            this.assetBundles = assetBundles;
        }
    }

    [Serializable] public struct AssetBundleInfo {
        [SerializeField] public string bundleName;
        [SerializeField] public string[] assetNames;
        [SerializeField] public string[] dependsBundleNames;
        [SerializeField] public uint crc;
        public AssetBundleInfo (string bundleName, string[] assetNames, string[] dependsBundleNames, uint crc) {
            this.bundleName = bundleName;
            this.assetNames = assetNames;
            this.dependsBundleNames = dependsBundleNames;
            this.crc = crc;
        }
    }
    
    public class AssetBundleListDownloader {
        private readonly string basePath;
        private readonly Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate;

        private void BasicResponseHandlingDelegate (string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string> failed) {
            if (200 <= httpCode && httpCode < 299) {
                succeeded(connectionId, data);
                return;
            }
            failed(connectionId, httpCode, errorReason);
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