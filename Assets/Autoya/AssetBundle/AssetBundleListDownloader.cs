using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AutoyaFramework.Settings.AssetBundles;
using UnityEngine;
using UnityEngine.Networking;

namespace AutoyaFramework.AssetBundles
{

    /// <summary>
    /// Asset bundle list downloader.
    /// </summary>
    public class AssetBundleListDownloader
    {
        /*
			delegate for handle http response for modules.
		*/
        public delegate void HttpResponseHandlingDelegate(string connectionId, Dictionary<string, string> responseHeader, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed);
        private readonly HttpResponseHandlingDelegate httpResponseHandlingDelegate;

        /*
			delegate for supply assetBundle get request header geneate func for modules.
		*/
        public delegate Dictionary<string, string> AssetBundleListGetRequestHeaderDelegate(string url, Dictionary<string, string> requestHeader);
        private readonly AssetBundleListGetRequestHeaderDelegate assetBundleGetRequestHeaderDelegate;


        private Dictionary<string, string> BasicRequestHeaderDelegate(string url, Dictionary<string, string> requestHeader)
        {
            return requestHeader;
        }

        private void BasicResponseHandlingDelegate(string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed)
        {
            if (200 <= httpCode && httpCode < 299)
            {
                succeeded(connectionId, data);
                return;
            }
            failed(connectionId, httpCode, errorReason, new AutoyaStatus());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AutoyaFramework.AssetBundles.AssetBundleListDownloader"/> class.
        /// </summary>
        /// <param name="requestHeader">Request header.</param>
        /// <param name="httpResponseHandlingDelegate">Http response handling delegate.</param>
		public AssetBundleListDownloader(AssetBundleListGetRequestHeaderDelegate requestHeader = null, HttpResponseHandlingDelegate httpResponseHandlingDelegate = null)
        {
            if (requestHeader != null)
            {
                this.assetBundleGetRequestHeaderDelegate = requestHeader;
            }
            else
            {
                this.assetBundleGetRequestHeaderDelegate = BasicRequestHeaderDelegate;
            }

            if (httpResponseHandlingDelegate != null)
            {
                this.httpResponseHandlingDelegate = httpResponseHandlingDelegate;
            }
            else
            {
                this.httpResponseHandlingDelegate = BasicResponseHandlingDelegate;
            }
        }

        public IEnumerator DownloadAssetBundleList(string url, Action<string, AssetBundleList> done, Action<int, string, AutoyaStatus> failed, double timeoutSec = 0)
        {
            var connectionId = AssetBundlesSettings.ASSETBUNDLES_ASSETBUNDLELIST_PREFIX + Guid.NewGuid().ToString();
            var reqHeader = assetBundleGetRequestHeaderDelegate(url, new Dictionary<string, string>());

            AssetBundleList assetBundleList = null;
            Action<string, object> listDonwloadSucceeded = (conId, listData) =>
            {
                var listString = listData as string;
                assetBundleList = JsonUtility.FromJson<AssetBundleList>(listString);
                done(url, assetBundleList);
            };

            Action<string, int, string, AutoyaStatus> listDownloadFailed = (conId, code, reason, autoyaStatus) =>
            {
                failed(code, reason, autoyaStatus);
            };

            var downloadCoroutine = DownloadAssetBundleListCoroutine(
                connectionId,
                reqHeader,
                url,
                (conId, code, responseHeader, listData) =>
                {
                    httpResponseHandlingDelegate(connectionId, responseHeader, code, listData, string.Empty, listDonwloadSucceeded, listDownloadFailed);
                },
                (conId, code, reason, responseHeader) =>
                {
                    httpResponseHandlingDelegate(connectionId, responseHeader, code, string.Empty, reason, listDonwloadSucceeded, listDownloadFailed);
                },
                timeoutSec
            );

            while (downloadCoroutine.MoveNext())
            {
                yield return null;
            }
        }

        private IEnumerator DownloadAssetBundleListCoroutine(
            string connectionId,
            Dictionary<string, string> requestHeader,
            string url,
            Action<string, int, Dictionary<string, string>, string> succeeded,
            Action<string, int, string, Dictionary<string, string>> failed,
            double timeoutSec = 0
        )
        {
            var timeoutTick = (DateTime.UtcNow + TimeSpan.FromSeconds(timeoutSec)).Ticks;
            using (var request = UnityWebRequest.Get(url))
            {
                if (requestHeader != null)
                {
                    foreach (var kv in requestHeader)
                    {
                        request.SetRequestHeader(kv.Key, kv.Value);
                    }
                }

                var p = request.SendWebRequest();

                while (!p.isDone)
                {
                    yield return null;

                    // check timeout.
                    if (timeoutSec != 0 && timeoutTick < DateTime.UtcNow.Ticks)
                    {
                        request.Abort();
                        failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, "timeout to download assetBundleList:" + url, new Dictionary<string, string>());
                        yield break;
                    }
                }

                while (!request.isDone)
                {
                    yield return null;
                }

                var responseCode = (int)request.responseCode;
                var responseHeaders = request.GetResponseHeaders();

                if (request.isNetworkError)
                {
                    failed(connectionId, responseCode, request.error, responseHeaders);
                    yield break;
                }

                if (200 <= responseCode && responseCode <= 299)
                {
                    // do nothing.
                }
                else
                {
                    failed(connectionId, responseCode, "failed to load assetBundle. assetBundleList:" + url + " was not downloaded.", responseHeaders);
                    yield break;
                }

                var result = Encoding.UTF8.GetString(request.downloadHandler.data);
                succeeded(connectionId, responseCode, responseHeaders, result);
            }
        }
    }
}