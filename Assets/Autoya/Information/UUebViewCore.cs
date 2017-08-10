using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AutoyaFramework.Information {
    public class UUebViewCore {
        private Dictionary<string, List<TagTree>> listenerDict = new Dictionary<string, List<TagTree>>();
		private readonly UUebView uuebView;


        public static GameObject GenerateSingleViewFromSource(
			GameObject eventReceiverGameObj, 
			string source, 
			Vector2 viewRect, 
			Autoya.HttpRequestHeaderDelegate requestHeader=null,
			Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate=null
		) {
            var view = new GameObject("UUebView");
			var uuebView = view.AddComponent<UUebView>();
			uuebView.LoadHtml(source, viewRect, eventReceiverGameObj, requestHeader, httpResponseHandlingDelegate);

			return view;
        }

		public static GameObject GenerateSingleViewFromUrl(
			GameObject eventReceiverGameObj, 
			string url, 
			Vector2 viewRect, 
			Autoya.HttpRequestHeaderDelegate requestHeader=null,
			Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate=null
		) {
            var view = new GameObject("UUebView");
			var uuebView = view.AddComponent<UUebView>();
			uuebView.DownloadHtml(url, viewRect, eventReceiverGameObj, requestHeader, httpResponseHandlingDelegate);

			return view;
        }

        public UUebViewCore (UUebView uuebView) {
            this.uuebView = uuebView;
        }

        public void Reload () {
            Debug.LogError("リロードを行う。");
		}

        public void OnImageTapped (string tag, string key, string buttonId="") {
			Debug.LogError("image. tag:" + tag + " key:" + key + " buttonId:" + buttonId);

			if (!string.IsNullOrEmpty(buttonId)) {
				if (listenerDict.ContainsKey(buttonId)) {
					listenerDict[buttonId].ForEach(t => t.ShowOrHide());
					uuebView.StartCoroutine(Update());
				}
			}
		}

        public void OnLinkTapped (string tag, string key, string linkId="") {
			Debug.LogError("link. tag:" + tag + " key:" + key + " linkId:" + linkId);

			if (!string.IsNullOrEmpty(linkId)) {
				if (listenerDict.ContainsKey(linkId)) {
					listenerDict[linkId].ForEach(t => t.ShowOrHide());
					uuebView.StartCoroutine(Update());
				}
			}
		}

        private IEnumerator Update () {
            yield return null;
        }

        public void AddListener(TagTree tree, string listenTargetId) {
            if (!listenerDict.ContainsKey(listenTargetId)) {
				listenerDict[listenTargetId] = new List<TagTree>();
			}

			if (!listenerDict[listenTargetId].Contains(tree)) {
				listenerDict[listenTargetId].Add(tree);
			}
        }
    }
}