using System;
using System.Collections;
using System.Collections.Generic;

namespace AutoyaFramework.AssetBundles {
	
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
			// not yet supplied.
			yield return null;
		}
	}
}