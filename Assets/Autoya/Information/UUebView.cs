using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AutoyaFramework.Information {

	/**
		UUebView instance.

		なんかこのインスタンスをnewできたら使えます的な感じになったほうがいい気がしてきたぞ。
	 */
	public class UUebView : MonoBehaviour {
		public ParsedTree root;

		private Dictionary<string, List<ParsedTree>> listenerDict = new Dictionary<string, List<ParsedTree>>();
		private LayoutMachine lMachine;
		private MaterializeMachine mMachine;

        public void CoroutineExecutor (IEnumerator iEnum) {
			StartCoroutine(iEnum);
		}

        public void OnImageTapped (string tag, string key, string buttonId="") {
			Debug.LogError("image. tag:" + tag + " key:" + key + " buttonId:" + buttonId);

			if (!string.IsNullOrEmpty(buttonId)) {
				if (listenerDict.ContainsKey(buttonId)) {
					listenerDict[buttonId].ForEach(t => t.ShowOrHide());
					StartCoroutine(Reload());
				}
			}
		}

        public void OnLinkTapped (string tag, string key, string linkId="") {
			Debug.LogError("link. tag:" + tag + " key:" + key + " linkId:" + linkId);

			if (!string.IsNullOrEmpty(linkId)) {
				if (listenerDict.ContainsKey(linkId)) {
					listenerDict[linkId].ForEach(t => t.ShowOrHide());
					StartCoroutine(Reload());
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

		private IEnumerator Reload () {
			// reset inserted trees.
			root = ParsedTree.RevertInsertedTree(root);

			Debug.LogError("reloadには来てる");
			var layout = lMachine.Layout(
				root, 
				layouted => {
					Debug.LogError("reloadには来て、その後layoutも終わってるんだけど。");
					root = layouted;
					try {
						var mat = mMachine.Materialize(this.gameObject, root, 0, progress => {}, () => {Debug.LogError("done");});
						StartCoroutine(mat);
					} catch (Exception e) {
						Debug.LogError("e:" + e);
					}
				}
			);
			
			return layout;
        }

		public void SetLayoutMachine (LayoutMachine lMachine) {
			Debug.LogError("SetLayoutMachine 適当");
            this.lMachine = lMachine;
        }

        public void SetMaterializeMachine (MaterializeMachine mMachine) {
			Debug.LogError("SetMaterializeMachine 適当2");
            this.mMachine = mMachine;
        }
    }
}