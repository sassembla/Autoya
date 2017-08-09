using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AutoyaFramework.Information {
    public class MaterializeMachine {
		private readonly ResourceLoader infoResLoader;
        private UUebView rootInputComponent;

        public MaterializeMachine(
			ResourceLoader infoResLoader	
		) {
			this.infoResLoader = infoResLoader;
		}

		public IEnumerator Materialize (GameObject root, TagTree tree, float yOffset, Action<double> progress, Action onLoaded) {
			Debug.LogWarning("yOffsetで、viewの範囲にあるものだけを表示する、とかができそう。その場合はGameObjectは渡した方がいいのか。取り合えず書いちゃう。");

			root.name = HtmlTag._ROOT.ToString();
			{
				var rootRectTrans = root.GetComponent<RectTransform>();
				this.rootInputComponent = root.GetComponent<UUebView>();
				
				// set anchor to left top.
				rootRectTrans.anchorMin = Vector2.up;
				rootRectTrans.anchorMax = Vector2.up;
				rootRectTrans.pivot = Vector2.up;
				rootRectTrans.position = Vector2.zero;
			}
			

			var total = 0.0;
			var done = 0.0;

            // Action<IEnumerator> act = iEnum => {
			// 	total++;
			// 	var loadAct = LoadingDone(
			// 		iEnum, 
			// 		() => {
			// 			done++;

			// 			if (progress != null) {
			// 				var progressRate = done / total;
							
			// 				if (done == total) {
			// 					progressRate = 1.0;
			// 				}

			// 				progress(progressRate);

			// 				if (done == total) {
			// 					loadDone();
			// 				}
			// 			}
			// 		}
			// 	);
			// 	Debug.LogError("途中。");
			// 	// infoResLoader.(loadAct);
			// };
			var cor = MaterializeRecursive(tree, root);
			while (cor.MoveNext()) {
				yield return null;
			}

			onLoaded();
        }

		private IEnumerator MaterializeRecursive (TagTree tree, GameObject parent) {
			GameObject newGameObject = null;
			if (tree.tagValue == (int)HtmlTag._ROOT) {// ここだけ逃すと良さそう。
				newGameObject = parent;
			} else {
				if (tree.keyValueStore.ContainsKey(HTMLAttribute.LISTEN)) {
					rootInputComponent.AddListener(tree, tree.keyValueStore[HTMLAttribute.LISTEN] as string);
				}
				
				if (tree.IsHidden()) {
					// cancel materialize of this tree.
					yield break;
				}

				var prefabCor = infoResLoader.LoadGameObjectFromPrefab(tree.tagValue, tree.treeType);

				while (prefabCor.MoveNext()) {
					yield return null;
				}

				// set pos and size.
				newGameObject = prefabCor.Current;
				newGameObject.transform.SetParent(parent.transform);
				var rectTrans = newGameObject.GetComponent<RectTransform>();
				rectTrans.anchoredPosition = TagTree.AnchoredPositionOf(tree);
				rectTrans.sizeDelta = TagTree.SizeDeltaOf(tree);

				// set parameters and events by container type. button, link.
				switch (tree.treeType) {
					case TreeType.Content_Img: {
						var src = tree.keyValueStore[HTMLAttribute.SRC] as string;
						infoResLoader.LoadImageAsync(
							src, 
							sprite => {
								newGameObject.GetComponent<Image>().sprite = sprite;
							},
							() => {
								// download failed. do nothing.
							}
						);
						
						if (tree.keyValueStore.ContainsKey(HTMLAttribute.BUTTON)) {
							var enable = tree.keyValueStore[HTMLAttribute.BUTTON] as string == "true";
							if (enable) {
								var buttonId = string.Empty;
								if (tree.keyValueStore.ContainsKey(HTMLAttribute.ID)) {
									buttonId = tree.keyValueStore[HTMLAttribute.ID] as string;
								}

								// add button component.
								AddButton(newGameObject, () => rootInputComponent.OnImageTapped(infoResLoader.GetTagFromIndex(tree.tagValue), src, buttonId));
							}
						}
						break;
					}
					
					case TreeType.Content_Text: {
						if (tree.keyValueStore.ContainsKey(HTMLAttribute._CONTENT)) {
							var text = tree.keyValueStore[HTMLAttribute._CONTENT] as string;
							if (!string.IsNullOrEmpty(text)) {
								var textComponent = newGameObject.GetComponent<Text>();
								textComponent.text = text;
							}
						}

						if (tree.keyValueStore.ContainsKey(HTMLAttribute.HREF)) {
							var href = tree.keyValueStore[HTMLAttribute.HREF] as string;

							// add button component.
							AddButton(newGameObject, () => rootInputComponent.OnLinkTapped(infoResLoader.GetTagFromIndex(tree.tagValue), href));
						}
						
						break;
					}
					
					default: {
						// do nothing.
						break;
					}
				}
			}

			var children = tree.GetChildren();

			Debug.LogWarning("レイアウトが終わってるので、このへんはフルに分散できそう。");
			foreach (var child in children) {
				var cor = MaterializeRecursive(child, newGameObject);
				while (cor.MoveNext()) {
					yield return null;
				}
			}
		}

		private IEnumerator LoadingDone (IEnumerator loadingCoroutine, Action loadDone) {
			while (loadingCoroutine.MoveNext()) {
				yield return null;
			}

			if (loadDone != null) {
				loadDone();
			}
		}






		private void AddButton (GameObject obj, UnityAction param) {
			var button = obj.GetComponent<Button>();
			if (button == null) {
				button = obj.AddComponent<Button>();
			}

			if (Application.isPlaying) {
				/*
					this code can set action to button. but it does not appear in editor inspector.
				*/
				button.onClick.AddListener(
					param
				);
			} else {
				try {
					button.onClick.AddListener(// 現状、エディタでは、Actionをセットする方法がわからん。関数単位で何かを用意すればいけそう = ButtonをPrefabにするとかしとけば行けそう。
						param
					);
					// UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
					// 	button.onClick,
					// 	() => rootMBInstance.OnImageTapped(tagPoint.tag, src)
					// );

					// // 次の書き方で、固定の値をセットすることはできる。エディタにも値が入ってしまう。
					// インスタンスというか、Prefabを作りまくればいいのか。このパーツのインスタンスを用意して、そこに値オブジェクトを入れて、それが着火する、みたいな。
					// UnityEngine.Events.UnityAction<String> callback = new UnityEngine.Events.UnityAction<String>(rootMBInstance.OnImageTapped);
					// UnityEditor.Events.UnityEventTools.AddStringPersistentListener(
					// 	button.onClick, 
					// 	callback,
					// 	src
					// );
				} catch (Exception e) {
					Debug.LogError("e:" + e);
				}
			}
		}
    }
}