using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public readonly Dictionary<string, KeyValuePair<GameObject, string>> eventObjectCache = new Dictionary<string, KeyValuePair<GameObject, string>>();

		public IEnumerator Materialize (GameObject root, UUebViewCore core, TagTree tree, float yOffset, Action onLoaded) {
			// Debug.LogWarning("yOffsetで、viewの範囲にあるものだけを表示する、とかができそう。TableViewとかにコンテンツ足して云々とか。まあそこまで必要かっていうと微妙。");

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
			

			// materialize root's children in parallel.
			var children = tree.GetChildren();

			var cors = children.Select(child => MaterializeRecursive(child, root)).ToArray();
			var done = 0;
			while (true) {
				for (var i = 0; i < cors.Length; i++) {
					var cor = cors[i];
					if (cor == null) {
						continue;
					}

					var cont = cor.MoveNext();

<<<<<<< HEAD
					// set position. convert layout position to uGUI position system.
					rectTrans.anchoredPosition = new Vector2(currentTree.anchoredPosition.x, -currentTree.anchoredPosition.y);
					
					// use content size for display contents.
					rectTrans.sizeDelta = currentTree.sizeDelta;
					break;
=======
					if (!cont) {
						cor = null;
						done++;
					}
>>>>>>> dev_information
				}

				if (done == cors.Length) break;
				yield return null;
			}

			onLoaded();
        }

		private IEnumerator MaterializeRecursive (TagTree tree, GameObject parent) {
			// Debug.LogError("materialize:" + tree.treeType + " tag:" + resLoader.GetTagFromValue(tree.tagValue));
			if (tree.keyValueStore.ContainsKey(HTMLAttribute.LISTEN)) {
				core.AddListener(tree, tree.keyValueStore[HTMLAttribute.LISTEN] as string);
			}
			
			if (tree.hidden || tree.treeType == TreeType.Content_CRLF) {
				// cancel materialize of this tree.
				yield break;
			}

			var objCor = resLoader.LoadGameObjectFromPrefab(tree.id, tree.tagValue, tree.treeType);

			while (objCor.MoveNext()) {
				if (objCor.Current != null) {
					break; 
				}
				yield return null;
			}

			// set pos and size.
			var newGameObject = objCor.Current;

			var cached = false;
			if (newGameObject.transform.parent != null) {
				cached = true;
			}
			
			newGameObject.transform.SetParent(parent.transform);
			var rectTrans = newGameObject.GetComponent<RectTransform>();
			rectTrans.anchoredPosition = TagTree.AnchoredPositionOf(tree);
			rectTrans.sizeDelta = TagTree.SizeDeltaOf(tree);

			// set parameters and events by container type. button, link.
			switch (tree.treeType) {
				case TreeType.Content_Img: {
					var src = tree.keyValueStore[HTMLAttribute.SRC] as string;
					if (tree.viewHeight == 0) {
						break;
					}
					
					// 画像コンテンツはキャッシュ済みの場合再度画像取得を行わない。
					if (!cached) {
						var imageLoadCor = resLoader.LoadImageAsync(src);

						// combine coroutine.
						var setImageCor = SetImageCor(newGameObject, imageLoadCor);
						resLoader.LoadParallel(setImageCor);
					}
					
					if (tree.keyValueStore.ContainsKey(HTMLAttribute.BUTTON)) {
						var enable = tree.keyValueStore[HTMLAttribute.BUTTON] as string == "true";
						if (enable) {
							var buttonId = string.Empty;
							if (tree.keyValueStore.ContainsKey(HTMLAttribute.ID)) {
								buttonId = tree.keyValueStore[HTMLAttribute.ID] as string;
							}

							eventObjectCache[buttonId] = new KeyValuePair<GameObject, string>(newGameObject, src);

							// add button component.
							AddButton(newGameObject, () => core.OnImageTapped(newGameObject, src, buttonId));
						}
					
					}
					break;
				}
				
				case TreeType.Content_Text: {
					// テキストコンテンツは毎回内容が変わる可能性があるため、キャッシュに関わらず更新する。
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

						eventObjectCache[linkId] = new KeyValuePair<GameObject, string>(newGameObject, href);

						// add button component.
						AddButton(newGameObject, () => core.OnLinkTapped(newGameObject, href, linkId));
					}
					break;
				}
				
				default: {
					// do nothing.
					break;
				}
			
			}

			var children = tree.GetChildren();

			// Debug.LogWarning("レイアウトが終わってるので、このへんはまだフルに分散できそう。内部的に分散する手法がいい感じになったらやろう。まあ2017で。");
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

		private IEnumerator SetImageCor (GameObject target, IEnumerator<Sprite> imageLoadCor) {
			while (imageLoadCor.MoveNext()) {
				if (imageLoadCor.Current != null) {
					break;
				}
				yield return null;
			}

			if (imageLoadCor.Current != null) {
				var sprite = imageLoadCor.Current;
				target.GetComponent<Image>().sprite = sprite;
			}
		}

		private void AddButton (GameObject obj, UnityAction param) {
			var button = obj.GetComponent<Button>();
			if (button == null) {
				button = obj.AddComponent<Button>();
			}

			if (Application.isPlaying) {
				button.onClick.RemoveAllListeners();

				/*
					this code can set action to button. but it does not appear in editor inspector.
				*/
				button.onClick.AddListener(param);
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