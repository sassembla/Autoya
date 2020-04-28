using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.EndPointSelect;
using AutoyaFramework.Settings.EndPoint;
using UnityEngine;

namespace AutoyaFramework
{
    /**
		endPointSelector implementation.
	 */
    public partial class Autoya
    {
        private EndPointSelector _endPointSelector;

        /*
            delegates
        */

        // request header handling.
        private EndPointGetRequestHeaderDelegate endPointGetRequestHeaderDelegate;

        private void InitializeEndPointImplementation()
        {
            this.endPointGetRequestHeaderDelegate = OnEndPointGetRequest;

            // initialize EndPointSelector.
            EndPointSelector.HttpResponseHandlingDelegate httpResponseHandlingDel = (p1, p2, p3, p4, p5, p6, p7) =>
            {
                httpResponseHandlingDelegate(p1, p2, p3, p4, p5, p6, p7);
            };
            EndPointSelector.EndPointsGetRequestHeaderDelegate endPointGetRequestHeaderDel = (p1, p2) =>
            {
                return endPointGetRequestHeaderDelegate(p1, p2);
            };

            _endPointSelector = new EndPointSelector(OnEndPointInstanceRequired(), endPointGetRequestHeaderDel, httpResponseHandlingDel);
        }

        private IEnumerator UpdateEndPoints(Action onFailed)
        {
            if (!_endPointSelector.HasAnyEndPointInfo())
            {
                yield break;
            }

            var url = EndPointSelectorSettings.ENDPOINT_INFO_URL;
            var reqHeader = endPointGetRequestHeaderDelegate(url, new Dictionary<string, string>());

            OnEndPointGetRequestStarted();
            var cor = _endPointSelector.UpToDate(
                url,
                reqHeader,
                responseStr =>
                {
                    return OnEndPointsParseFromUpdateResponse(responseStr);
                },
                errors =>
                {
                    if (0 < errors.Length)
                    {
                        OnEndPointUpdateFailed(errors);
                        onFailed();
                        return;
                    }
                    OnEndPointUpdateSucceeded();
                },
                failReason =>
                {
                    OnEndPointUpdateFailed(
                        new (string, Exception)[]{
                            (failReason, new Exception(failReason))
                        }
                    );
                    onFailed();
                },
                EndPointSelectorSettings.TIMEOUT_SEC,
                EndPointSelectorSettings.MAX_RETRY_COUNT
            );

            while (cor.MoveNext())
            {
                yield return null;
            }
        }

        public static T EndPoint_GetEndPoint<T>() where T : IEndPoint
        {
            return autoya._endPointSelector.GetEndPoint<T>();
        }

        public static bool EndPoint_HasEndPoints()
        {
            return autoya._endPointSelector.HasAnyEndPointInfo();
        }

        public static IEnumerator Debug_EndPointUpdate(IEndPoint[] endPointInstances)
        {
            autoya._endPointSelector = new EndPointSelector(endPointInstances);
            var cor = autoya.UpdateEndPoints(() => { });
            while (cor.MoveNext())
            {
                yield return null;
            }
        }

        public static void Debug_OnEndPointInstanceRequired(Func<IEndPoint[]> debugFunc)
        {
            autoya.OnEndPointInstanceRequired = () =>
            {
                return debugFunc();
            };
        }

        public static void Debug_SetShouldRetryEndPointGetRequest(Func<bool> debugFunc)
        {
            autoya.ShouldRetryEndPointGetRequestOrNot = () =>
            {
                return debugFunc();
            };
        }
    }
}