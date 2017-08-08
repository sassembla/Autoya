using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AutoyaFramework.Information {

	/**
		UUebView instance.
	 */
	public class UUebView : MonoBehaviour {
		private Dictionary<string, List<ParsedTree>> listenerDict = new Dictionary<string, List<ParsedTree>>();

        public void Executor (IEnumerator iEnum) {
			StartCoroutine(iEnum);
		}

        public void OnImageTapped (string tag, string key, string buttonId="") {
			Debug.LogError("image. tag:" + tag + " key:" + key + " buttonId:" + buttonId);

			if (!string.IsNullOrEmpty(buttonId)) {
				if (listenerDict.ContainsKey(buttonId)) {
					listenerDict[buttonId].ForEach(t => t.ShowOrHide());
					Reload();
				}
			}
		}

        public void OnLinkTapped (string tag, string key, string linkId="") {
			Debug.LogError("link. tag:" + tag + " key:" + key + " linkId:" + linkId);

			if (!string.IsNullOrEmpty(linkId)) {
				if (listenerDict.ContainsKey(linkId)) {
					listenerDict[linkId].ForEach(t => t.ShowOrHide());
					Reload();
				}
			}
		}

        public void AddListener(ParsedTree tree, string listenTargetId) {
            if (!listenerDict.ContainsKey(listenTargetId)) {
				listenerDict[listenTargetId] = new List<ParsedTree>();
			}

			if (!listenerDict[listenTargetId].Contains(tree)) {
				listenerDict[listenTargetId].Add(tree);
				Debug.LogError("足した。listenTargetId:" + listenTargetId);
			}
        }

		public void RefreshListener () {
			
		}

		private void Reload () {
			// なんかいろいろとstartCoroutineで処理できそう。
            throw new NotImplementedException();
        }
    }
}