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
		private readonly ResourceLoader resLoader;
        private UUebViewCore core;
		private GameObject root;
        public MaterializeMachine(ResourceLoader resLoader) {
			this.resLoader = resLoader;
		}

		public IEnumerator Materialize (GameObject root, UUebViewCore core, TagTree tree, float yOffset, Action onLoaded) {
			Debug.LogWarning("yOffsetで、viewの範囲にあるものだけを表示する、とかができそう。その場合はGameObjectは渡した方がいいのか。取り合えず書いちゃう。");

			this.root = root;
			root.name = HTMLTag._ROOT.ToString();
			{
				var rootRectTrans = root.GetComponent<RectTransform>();
				this.core = core;
				
				// set anchor to left top.
				rootRectTrans.anchorMin = Vector2.up;
				rootRectTrans.anchorMax = Vector2.up;
				rootRectTrans.pivot = Vector2.up;
			}
			
			var cor = MaterializeRecursive(tree, root);
			while (cor.MoveNext()) {
				yield return null;
			}

			onLoaded();
        }

		private IEnumerator MaterializeRecursive (TagTree tree, GameObject parent) {
			GameObject newGameObject = null;
			Debug.LogWarning("ここだけ逃すと良さそう");
			if (tree.tagValue == (int)HTMLTag._ROOT) {
				newGameObject = parent;
			} else {
				if (tree.keyValueStore.ContainsKey(HTMLAttribute.LISTEN)) {
					core.AddListener(tree, tree.keyValueStore[HTMLAttribute.LISTEN] as string);
				}
				
				if (tree.IsHidden()) {
					// cancel materialize of this tree.
					yield break;
				}

				var prefabCor = resLoader.LoadGameObjectFromPrefab(tree.tagValue, tree.treeType);

				while (prefabCor.MoveNext()) {
					if (prefabCor.Current != null) {
						break; 
					}
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
						var imageLoadCor = resLoader.LoadImageAsync(
							src, 
							sprite => {
								newGameObject.GetComponent<Image>().sprite = sprite;
							},
							() => {
								// download failed. do nothing.
							}
						);

						core.LoadParallel(imageLoadCor);
						
						if (tree.keyValueStore.ContainsKey(HTMLAttribute.BUTTON)) {
							var enable = tree.keyValueStore[HTMLAttribute.BUTTON] as string == "true";
							if (enable) {
								var buttonId = string.Empty;
								if (tree.keyValueStore.ContainsKey(HTMLAttribute.ID)) {
									buttonId = tree.keyValueStore[HTMLAttribute.ID] as string;
								}

								// add button component.
								AddButton(newGameObject, () => core.OnImageTapped(resLoader.GetTagFromValue(tree.tagValue), src, buttonId));
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
							
							var linkId = string.Empty;
							if (tree.keyValueStore.ContainsKey(HTMLAttribute.ID)) {
								linkId = tree.keyValueStore[HTMLAttribute.ID] as string;
							}

							// add button component.
							AddButton(newGameObject, () => core.OnLinkTapped(resLoader.GetTagFromValue(tree.tagValue), href, linkId));
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

			Debug.LogWarning("レイアウトが終わってるので、このへんはフルに分散できそう。内部的に分散する手法がいい感じになったらやろう。まあ2017で。");
			foreach (var child in children) {
				var cor = MaterializeRecursive(child, newGameObject);
				while (cor.MoveNext()) {
					if (cor.Current != null) {
						break;
					}
					yield return null;
				}
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
		
        public void RemoveContents () {
			var list = new List<GameObject>();

			for (var i = 0; i < this.root.transform.childCount; i++) {
				list.Add(this.root.transform.GetChild(i).gameObject);
			}

			// 取り出してから消す
			foreach (var childObj in list) {
				GameObject.Destroy(childObj);
			}
        }
    }
}