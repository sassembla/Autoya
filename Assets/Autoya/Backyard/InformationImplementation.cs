using System;
using UnityEngine;
using UUebView;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

/*
	information implementation.
*/
namespace AutoyaFramework
{

    public partial class Autoya
    {
        /*
			public apis
		*/

        /**
			show information view from url.
            scrollView should have component which implement IUUebViewEventHandler for receiving events.
		*/
        public static void Info_Show(GameObject scrollView, string url, string viewName = ConstSettings.ROOTVIEW_NAME)
        {
            var eventReceiverCandidate = scrollView.GetComponents<Component>().Where(component => component is IUUebViewEventHandler).FirstOrDefault();
            if (eventReceiverCandidate == null)
            {
                throw new Exception("information scroll view should have IUUebViewEventHandler implemented component.");
            }

            var content = scrollView.GetComponentsInChildren<RectTransform>().Where(t => t.gameObject.name == "Content").FirstOrDefault();
            if (content == null)
            {
                throw new Exception("information scroll view should have 'Content' GameObject like uGUI default ScrollView.");
            }

            var viewSize = scrollView.GetComponent<RectTransform>().sizeDelta;


            ResourceLoader.MyHttpRequestHeaderDelegate httpReqHeaderDel = (p1, p2, p3, p4) =>
            {
                return autoya.httpRequestHeaderDelegate(p1, p2, p3, p4);
            };

            ResourceLoader.MyHttpResponseHandlingDelegate httpResponseHandleDel = (p1, p2, p3, p4, p5, p6, p7) =>
            {
                Action<string, int, string, AutoyaStatus> p8 = (q1, q2, q3, q4) =>
                {
                    p7(q1, q2, q3);
                };
                autoya.httpResponseHandlingDelegate(p1, p2, p3, p4, p5, p6, p8);
            };

            var view = UUebViewComponent.GenerateSingleViewFromUrl(scrollView, url, viewSize, httpReqHeaderDel, httpResponseHandleDel, viewName);
            view.transform.SetParent(content.gameObject.transform, false);
        }
    }
}