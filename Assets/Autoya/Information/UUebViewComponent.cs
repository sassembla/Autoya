using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UUebView
{
    /**
		UUebView component.

		testing usage:
			attach this component to gameobject and set preset urls and event receiver.

		actual usage:
			let's use UUebView.GenerateSingleViewFromHTML or UUebView.GenerateSingleViewFromUrl.
            they returns view GameObject of UUebView and attach it to your window.
	 */
    public class UUebViewComponent : MonoBehaviour, IUUebView
    {
        /*
			preset parameters.
			you can use UUebView with preset paramters for testing.
		 */
        public string presetUrl = string.Empty;
        public GameObject presetEventReceiver = null;


        public UUebViewCore Core
        {
            get; private set;
        }

        void Start()
        {
            /*
                if preset parameters exists, UUebView shows preset view on this gameObject.
                this feature is for testing.
             */
            if (!string.IsNullOrEmpty(presetUrl) && presetEventReceiver != null)
            {
                Debug.Log("show preset view.");
                var viewObj = this.gameObject;

                var uuebView = viewObj.GetComponent<UUebViewComponent>();
                var uuebViewCore = new UUebViewCore(uuebView);
                uuebView.SetCore(uuebViewCore);
                uuebViewCore.DownloadHtml(presetUrl, GetComponent<RectTransform>().sizeDelta, presetEventReceiver);
            }
        }

        public static GameObject GenerateSingleViewFromHTML(
            GameObject eventReceiverGameObj,
            string source,
            Vector2 viewRect,
            ResourceLoader.MyHttpRequestHeaderDelegate requestHeader = null,
            ResourceLoader.MyHttpResponseHandlingDelegate httpResponseHandlingDelegate = null,
            string viewName = ConstSettings.ROOTVIEW_NAME
        )
        {
            var viewObj = new GameObject("UUebView");
            viewObj.AddComponent<RectTransform>();
            viewObj.name = viewName;

            var uuebView = viewObj.AddComponent<UUebViewComponent>();
            var uuebViewCore = new UUebViewCore(uuebView, null, requestHeader, httpResponseHandlingDelegate);
            uuebView.SetCore(uuebViewCore);
            uuebViewCore.LoadHtml(source, viewRect, 0, eventReceiverGameObj);

            return viewObj;
        }

        public static GameObject GenerateSingleViewFromUrl(
            GameObject eventReceiverGameObj,
            string url,
            Vector2 viewRect,
            ResourceLoader.MyHttpRequestHeaderDelegate requestHeader = null,
            ResourceLoader.MyHttpResponseHandlingDelegate httpResponseHandlingDelegate = null,
            string viewName = ConstSettings.ROOTVIEW_NAME
        )
        {
            var viewObj = new GameObject("UUebView");
            viewObj.AddComponent<RectTransform>();
            viewObj.name = viewName;

            var uuebView = viewObj.AddComponent<UUebViewComponent>();
            var uuebViewCore = new UUebViewCore(uuebView, null, requestHeader, httpResponseHandlingDelegate);
            uuebView.SetCore(uuebViewCore);
            uuebViewCore.DownloadHtml(url, viewRect, eventReceiverGameObj);

            return viewObj;
        }

        public void SetCore(UUebViewCore core)
        {
            this.Core = core;
        }

        void Update()
        {
            Core.Dequeue(this);
        }

        public void EmitButtonEventById(GameObject source, string url, string elementId)
        {
            Core.OnImageTapped(source, url, elementId);
        }

        public void EmitLinkEventById(GameObject source, string href, string elementId)
        {
            Core.OnLinkTapped(source, href, elementId);
        }

        void IUUebView.AddChild(Transform transform)
        {
            transform.SetParent(this.transform);
        }

        void IUUebView.UpdateParentSizeIfExist(Vector2 size)
        {
            if (this.transform.parent != null)
            {
                var parentRectTrans = this.transform.parent.GetComponent<RectTransform>();
                parentRectTrans.sizeDelta = size;
            }
            else
            {
                // do nothing.
            }
        }

        GameObject IUUebView.GetGameObject()
        {
            return this.gameObject;
        }

        void IUUebView.StartCoroutine(IEnumerator iEnum)
        {
            this.StartCoroutine(iEnum);
        }

        public void AppendContentToTree(string htmlContent, string query)
        {
            this.Core.AppendContentToLast(htmlContent, query);
        }

        public void AppendContentToLast(string htmlContent)
        {
            this.Core.AppendContentToLast(htmlContent);
        }

        public void DeleteByPoint(string query)
        {
            this.Core.DeleteByPoint(query);
        }

        public TagTree[] GetTreeById(string contentId)
        {
            return this.Core.GetTreeById(contentId);
        }
    }
}