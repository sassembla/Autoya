using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoyaFramework.Connections.HTTP;
using UnityEngine;
using UnityEngine.Purchasing;

/**
    endpoint selector is:
        update endpoint data from http request.
        requires the all classes which implements IEndPoint to initialize.
*/
namespace AutoyaFramework.Settings.EndPoint
{
    public class EndPointSelector
    {
        private readonly Dictionary<Type, IEndPoint> endPointDict;

        /*
			delegate for handle http response for modules.
		*/
        public delegate void HttpResponseHandlingDelegate(string connectionId, Dictionary<string, string> responseHeader, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed);
        private readonly HttpResponseHandlingDelegate httpResponseHandlingDelegate;

        /*
			delegate for supply assetBundle get request header geneate func for modules.
		*/
        public delegate Dictionary<string, string> EndPointsGetRequestHeaderDelegate(string url, Dictionary<string, string> requestHeader);
        private readonly EndPointsGetRequestHeaderDelegate endPointGetRequestHeaderDelegate;


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

        public EndPointSelector(IEndPoint[] endPointInstances, EndPointsGetRequestHeaderDelegate requestHeader = null, HttpResponseHandlingDelegate httpResponseHandlingDelegate = null)
        {
            this.endPointDict = new Dictionary<Type, IEndPoint>();
            foreach (var endPointInstance in endPointInstances)
            {
                endPointDict[endPointInstance.GetType()] = endPointInstance;
            }

            var targetType = typeof(IEndPoint);
            var collectedTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => targetType.IsAssignableFrom(p) && targetType != p);
            foreach (var collectedType in collectedTypes)
            {
                if (!endPointDict.ContainsKey(collectedType))
                {
                    Debug.LogError("EndPointSelector should be initialize with all types which impletents IEndPoint. required:" + collectedType);
                    Debug.Break();
                }
            }

            if (requestHeader != null)
            {
                this.endPointGetRequestHeaderDelegate = requestHeader;
            }
            else
            {
                this.endPointGetRequestHeaderDelegate = BasicRequestHeaderDelegate;
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

        public IEnumerator UpToDate(
            string url,
            Dictionary<string, string> requestHeader,
            Action<(string, Exception)[]> onUpdated,
            Action onFailed,
            double timeout,
            int retryCount
        )
        {
            var currentCount = 0;

            var rawStringData = string.Empty;

        retry:
            var succeeded = false;
            var reqHeader = endPointGetRequestHeaderDelegate(url, requestHeader);
            var con = new HTTPConnection();
            var cor = con.Get(
                "endPointRequest_" + Guid.NewGuid().ToString(),
                reqHeader,
                url,
                (connectionId, httpCode, responseHeader, data) =>
                {
                    httpResponseHandlingDelegate(
                        connectionId,
                        responseHeader,
                        httpCode,
                        data,
                        string.Empty,
                        (conId, response) =>
                        {
                            rawStringData = (string)response;
                            succeeded = true;
                        },
                        (conId, code, reason, status) =>
                        {
                            succeeded = false;
                        }
                    );
                },
                (connectionId, httpCode, reason, responseHeader) =>
                {
                    httpResponseHandlingDelegate(
                        connectionId,
                        responseHeader,
                        httpCode,
                        null,
                        reason,
                        (conId, response) =>
                        {
                            rawStringData = (string)response;
                            succeeded = true;
                        },
                        (conId, code, failReason, status) =>
                        {
                            succeeded = false;
                        }
                    );
                },
                timeout
            );

            while (cor.MoveNext())
            {
                yield return null;
            }

            if (succeeded)
            {
                var errors = ResetEndPoints(rawStringData);
                onUpdated(errors);
                yield break;
            }

            if (currentCount == retryCount)
            {
                onFailed();
                yield break;
            }

            currentCount++;

            // wait 2, 4, 8... sec.
            var retryWait = Math.Pow(2, currentCount);
            var limitTick = DateTime.Now.Ticks + TimeSpan.FromSeconds(retryWait).Ticks;

            while (DateTime.Now.Ticks < limitTick)
            {
                yield return null;
            }

            goto retry;
        }

        private (string, Exception)[] ResetEndPoints(string data)
        {
            var errors = new List<(string, Exception)>();

            var classNamesAndValues = MiniJson.JsonDecode(data) as Dictionary<string, object>;

            foreach (var endPointTypeAndInstance in endPointDict)
            {
                var endPointTypeStr = endPointTypeAndInstance.Key.ToString();
                try
                {
                    if (classNamesAndValues.ContainsKey(endPointTypeStr))
                    {
                        var keysAndValues = classNamesAndValues[endPointTypeStr] as Dictionary<string, object>;
                        var dataSource = new Dictionary<string, string>();
                        foreach (var keyValue in keysAndValues)
                        {
                            var key = keyValue.Key;
                            var val = keyValue.Value as string;
                            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(val))
                            {
                                continue;
                            }
                            dataSource[key] = val;
                        }

                        endPointTypeAndInstance.Value.UpToDate(dataSource);
                        continue;
                    }
                    // classnames not contained ep name.
                }
                catch (Exception e)
                {
                    Debug.LogError("endpoint failed to uptodate, e:" + e);
                    errors.Add((endPointTypeStr, e));
                }

                // ep name is not contained in classnames.
            }

            return errors.ToArray();
        }

        public T GetEndPoint<T>() where T : IEndPoint
        {
            if (endPointDict.ContainsKey(typeof(T)))
            {
                return (T)endPointDict[typeof(T)];
            }
            return default(T);
        }
    }



    public interface IEndPoint
    {
        void UpToDate(Dictionary<string, string> dataSource);
    }
}