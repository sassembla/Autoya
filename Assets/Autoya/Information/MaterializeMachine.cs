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
		private readonly InformationResourceLoader infoResLoader;
        private readonly InformationRootMonoBehaviour rootInputComponent;
        

        public MaterializeMachine(
			InformationResourceLoader infoResLoader,
			ParsedTree layoutedTree, 
			GameObject rootGameObject, 
			ViewBox view, 
			Action<IEnumerator> executor
		) {
			this.infoResLoader = infoResLoader;
			
            this.rootInputComponent = rootGameObject.GetComponent<InformationRootMonoBehaviour>();
            
			executor(Execute(layoutedTree, rootGameObject, view));
        }

		private IEnumerator Execute (ParsedTree layoutedTree, GameObject rootGameObject, ViewBox view) {
            var cor = MaterializeRecursively(rootGameObject, layoutedTree);
			while (cor.MoveNext()) {
				yield return null;
			}
			
			Debug.LogError("封印中");
			// var totalHeight = layoutedTree.totalHeight;

            // var rootRectTrans = rootGameObject.GetComponent<RectTransform>();
            // rootRectTrans.sizeDelta = new Vector2(view.width, totalHeight);

			yield break;
		}

        private IEnumerator MaterializeRecursively (GameObject parent, ParsedTree currentTree) {
			GameObject gameObj = null;

			switch (currentTree.parsedTag) {
				case (int)HtmlTag._ROOT: {
                    gameObj = parent;
					break;
				}
				case (int)HtmlTag.br: {
					// has no child. no visual. do nothing.
					break;
				}
				default: {
					var cor = MaterializeTagContent(
						currentTree, 
						newObj => {
							gameObj = newObj;
						}
					);
					while (cor.MoveNext()) {
						yield return null;
					}
                    // Debug.LogError("gameObj:" + gameObj + " parent:" + parent);

					gameObj.transform.SetParent(parent.transform, false);
					var rectTrans = gameObj.GetComponent<RectTransform>();
					if (rectTrans == null) {
						// error, tag content should have RectTransform.
						yield break;
					}

					Debug.LogError("まだマテリアライズ封印中。");
					// // set position. convert layout position to uGUI position system.
					// rectTrans.anchoredPosition = new Vector2(currentTree.anchoredPosition.x, -currentTree.anchoredPosition.y);
					
					// // use content size for display contents.
					// rectTrans.sizeDelta = currentTree.sizeDelta;
					break;
				}
			}
			
            /*
                materialize childlen.
             */
            var childCount = currentTree.GetChildren().Count;
			foreach (var child in currentTree.GetChildren()) {
                var cor = MaterializeRecursively(gameObj, child);
				while (cor.MoveNext()) {
					yield return null;
				}
			}
		}

		private IEnumerator MaterializeTagContent (ParsedTree currentTree, Action<GameObject> onLoaded) {
			GameObject prefab = null;
			var cor = infoResLoader.LoadPrefab(
				currentTree,
				newPrefab => {
					prefab = newPrefab;
				},
				() => {
					throw new Exception("failed to load prefab:" + currentTree.parsedTag);
				}
			);

			while (cor.MoveNext()) {
				yield return null;
			}
			
			var obj = InformationResourceLoader.LoadGameObject(prefab);
			if (currentTree.treeType == TreeType.Container) {
				obj.name = infoResLoader.GetTagFromIndex(currentTree.parsedTag);
			}

			// set parameters.
			switch (currentTree.treeType) {
				case TreeType.Content_Img: {
					var src = currentTree.keyValueStore[Attribute.SRC] as string;
					
					infoResLoader.LoadImageAsync(
						src, 
						sprite => {
							obj.GetComponent<Image>().sprite = sprite;
						},
						() => {
							// download failed. do nothing.
						}
					);
					
					// add button component.
					AddButton(obj, () => rootInputComponent.OnLinkTapped(infoResLoader.GetTagFromIndex(currentTree.parsedTag), src));
					break;
				}
				
				case TreeType.Content_Text: {
					foreach (var kvs in currentTree.keyValueStore) {
						switch (kvs.Key) {
							case Attribute._CONTENT:{
								var text = kvs.Value as string;
								if (!string.IsNullOrEmpty(text)) {
									var textComponent = obj.GetComponent<Text>();
									textComponent.text = text;
								}
								break;
							}
							case Attribute.HREF: {
								Debug.LogError("このへんだいぶ怪しい気がする。");
								var href = kvs.Value as string;

								// add button component.
								AddButton(obj, () => rootInputComponent.OnLinkTapped(infoResLoader.GetTagFromIndex(currentTree.parsedTag), href));
								break;
							}
							
							default: {
								// ignore.
								break;
							}
						}
					}
					
					break;
				}
				
				default: {
					if (currentTree.keyValueStore.ContainsKey(Attribute._BOX)) {
						var transformParams = currentTree.keyValueStore[Attribute._BOX] as BoxPos;
						Debug.LogError("transformParams:" + transformParams);
					}
					break;
				}
			}

			onLoaded(obj);
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