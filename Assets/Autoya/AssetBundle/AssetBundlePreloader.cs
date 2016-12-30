using System;
using System.Collections;
using UnityEngine;

namespace AutoyaFramework.AssetBundles {
    public class AssetBundlePreloader {
        private readonly string urlBase;
        public AssetBundlePreloader (string urlBase) {
            this.urlBase = urlBase;
        }

        public IEnumerator Preload (string preloadKey, Action<string> done) {
            // Preloadリストを取得して、取得が終わったら中に書かれているものを取得する。
            // Assetの更新とかもあると思うけど、そこはLoadFromCacheOrDownloadみたいな機構に任せたいところ。
            done(preloadKey);
            yield break;
        }
        
    }
}