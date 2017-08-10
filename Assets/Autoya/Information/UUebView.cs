using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AutoyaFramework.Information {

	/**
		UUebView instance.

		なんかこのインスタンスをnewできたら使えます的な感じになったほうがいい気がしてきたぞ。

		・MonoBehaviourなので、イベント送付先をuGUIのイベントで送付できる
			レシーバを登録する形になる

		・StartCoroutineが使える(executorを渡すことでOK)
			resLoaderのあり方次第か。
	 */
	public class UUebView : MonoBehaviour {
	

		/*
			preset parameters.
			you can use this UUebView with preset paramters.
		 */
		public string presetUrl;
		public GameObject presetEventReceiver;


		public UUebViewCore Core {
			get; private set;
		}

		void Start () {
			if (!string.IsNullOrEmpty(presetUrl) && presetEventReceiver != null) {
				DownloadHtml(presetUrl, GetComponent<RectTransform>().sizeDelta, presetEventReceiver);
			}
		}


		public void LoadHtml (string source, Vector2 viewRect, GameObject eventReceiverGameObj=null, Autoya.HttpRequestHeaderDelegate requestHeader=null, Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate=null) {
            Debug.LogError("htmlから展開する");
        }

        public void DownloadHtml (string url, Vector2 viewRect, GameObject eventReceiverGameObj=null, Autoya.HttpRequestHeaderDelegate requestHeader=null, Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate=null) {
            Debug.LogError("urlから展開する");
        }

		public void CoroutineExecutor (IEnumerator iEnum) {
			StartCoroutine(iEnum);
		}
    }

	public enum ContentType {
		HTML,
		IMAGE,
		LINK,
		CUSTOMTAGLIST
	}

	public interface IUUebViewEventHandler : IEventSystemHandler {
		void OnLoadProgress (double progress);
		void OnContentLoaded ();
		void OnContentLoadFailed (ContentType type, int code, string reason);
		void OnElementTapped (ContentType type, string param, string id);
		void OnElementLongTapped (ContentType type, string param, string id);
	}
}