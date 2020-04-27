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

        private void InitializeEndPointImplementation()
        {
            // initialize EndPointSelector.
            EndPointSelector.HttpResponseHandlingDelegate httpResponseHandlingDel = (p1, p2, p3, p4, p5, p6, p7) =>
            {
                httpResponseHandlingDelegate(p1, p2, p3, p4, p5, p6, p7);
            };
            EndPointSelector.EndPointsGetRequestHeaderDelegate endPointGetRequestHeaderDel = (p1, p2) =>
            {
                return endPointGetRequestHeaderDelegate(p1, p2);
            };

            _endPointSelector = new EndPointSelector(OnEndPointInstansRequired(), endPointGetRequestHeaderDel, httpResponseHandlingDel);
        }

        private IEnumerator UpdateEndPoints()
        {
            if (!_endPointSelector.HasAnyEndPointInfo())
            {
                yield break;
            }

            var url = EndPointSelectorSettings.ENDPOINT_INFO_URL;
            var reqHeader = endPointGetRequestHeaderDelegate(url, new Dictionary<string, string>());

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
            var cor = autoya.UpdateEndPoints();
            while (cor.MoveNext())
            {
                yield return null;
            }
        }
    }
}