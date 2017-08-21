using System;
using UnityEngine;
using AutoyaFramework.Information;
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
        public static void Info_Show(GameObject scrollView, string url)
        {
            var eventReceiverCandidate = scrollView.GetComponents<Component>().Where(component => component is IUUebViewEventHandler).FirstOrDefault();
            if (eventReceiverCandidate == null) {
                throw new Exception("information scroll view should have IUUebViewEventHandler implemented component.");
            }

            var content = scrollView.GetComponentsInChildren<RectTransform>().Where(t => t.gameObject.name == "Content").FirstOrDefault();
            if (content == null) {
                throw new Exception("information scroll view should have 'Content' GameObject like uGUI default ScrollView.");
            }

            var viewSize = scrollView.GetComponent<RectTransform>().sizeDelta;

            var view = UUebViewCore.GenerateSingleViewFromUrl(scrollView, url, viewSize, autoya.httpRequestHeaderDelegate, autoya.httpResponseHandlingDelegate);
            view.transform.SetParent(content.gameObject.transform, false);
        }
    }
}