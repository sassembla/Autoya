using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AutoyaFramework.Information {

	/**
		UUebView instance.

		なんかこのインスタンスをnewできたら使えます的な感じになったほうがいい気がしてきたぞ。

		・MonoBehaviourなので、イベント送付先をuGUIのイベントで送付できる
			レシーバを登録する形になる

		・StartCoroutineが使える(executorを渡すことでOK)

	 */
	public class UUebView : MonoBehaviour {
		public TagTree root;

		private Dictionary<string, List<TagTree>> listenerDict = new Dictionary<string, List<TagTree>>();
		
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

        public void AddListener(TagTree tree, string listenTargetId) {
            if (!listenerDict.ContainsKey(listenTargetId)) {
				listenerDict[listenTargetId] = new List<TagTree>();
			}

			if (!listenerDict[listenTargetId].Contains(tree)) {
				listenerDict[listenTargetId].Add(tree);
			}
        }

		public void RefreshListener () {
			
		}

		private IEnumerator Reload () {
			Debug.LogError("調整中。さてどうしようかな、infoResLoaderのあり方をこちらに持って来てもいいのかも。");
			// reset inserted trees.
			root = TagTree.RevertInsertedTree(root);

			// var layout = lMachine.Layout(
			// 	root, 
			// 	layouted => {
			// 		root = layouted;

			// 		var mat = mMachine.Materialize(this.gameObject, root, 0, progress => {}, () => {Debug.LogError("done");});
			// 		StartCoroutine(mat);
			// 	}
			// );
			
			// return layout;
			yield return null;
        }
    }
}