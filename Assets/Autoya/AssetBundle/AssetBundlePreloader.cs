using System;
using System.Collections;
using System.Collections.Generic;

namespace AutoyaFramework.AssetBundles {
    public class AssetBundlePreloader {
        private readonly string urlBase;
        private readonly Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate;

        private void BasicResponseHandlingDelegate (string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed) {
            if (200 <= httpCode && httpCode < 299) {
                succeeded(connectionId, data);
                return;
            }
            failed(connectionId, httpCode, errorReason, new AutoyaStatus());
        }
        public AssetBundlePreloader (string urlBase, Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate =null) {
            this.urlBase = urlBase;

            if (httpResponseHandlingDelegate == null) {
                this.httpResponseHandlingDelegate = BasicResponseHandlingDelegate;
            } else {
                this.httpResponseHandlingDelegate = httpResponseHandlingDelegate;
            }
        }

        public IEnumerator Preload (string preloadKey, Action<string> done) {
            // Preloadリストを取得して、取得が終わったら中に書かれているものを取得する。
            // Assetの更新とかもあると思うけど、そこはLoadFromCacheOrDownloadみたいな機構に任せたいところ。
            done(preloadKey);
            yield break;
        }
        
    }
}