using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AutoyaFramework.Information {
	public class VirtualGameObject {
		public GameObject _gameObject;
		public readonly string prefabName;

		public InformationRootMonoBehaviour rootInstance;

		public readonly Tag tag;
		public readonly Tag[] depth;
		public readonly Dictionary<KV_KEY, string> keyValueStore;

		public Padding padding;
		
		private VirtualTransform vTransform;
		
		public VirtualRectTransform rectTransform = new VirtualRectTransform();

		public VirtualGameObject parent;
		
		public VirtualTransform transform {
			get {
				return vTransform;
			}
		}

		public Vector2 PaddedRightBottomPoint () {
			return rectTransform.anchoredPosition + rectTransform.sizeDelta + new Vector2(padding.PadWidth(), padding.PadHeight());
		}

		public VirtualGameObject (Tag tag, Tag[] depth, Dictionary<KV_KEY, string> kv, string prefabName) {
			this.tag = tag;
			this.depth = depth;
			this.padding = new Padding();
			this.vTransform = new VirtualTransform(this);
			this.keyValueStore = kv;
			this.prefabName = prefabName;
		}

		public VirtualGameObject GetRootGameObject () {
			if (vTransform.Parent() != null) {
				return vTransform.Parent().GetRootGameObject();
			}

			// no parent. return this vGameObject.
			return this;
		}

		private ContentAndWidthAndHeight LayoutTextContent (float offset, string text, float contentWidth, float contentHeight, Action<List<VirtualGameObject>> insert) {
			// このあたりをhttpリクエストに乗っけるようなことができるとなおいいのだろうか。AssetBundleともちょっと違う何か、的な。
			/*
				・Resourcesに置ける
				・AssetBundle化できる
			*/
			var prefab = LoadPrefab(prefabName);
			if (prefab == null) {
				return new ContentAndWidthAndHeight();
			}

			// Debug.LogError("prefabName:" + prefabName);

			// use prefab's text component for using it's text setting.
			var textComponent = prefab.GetComponent<Text>();
			if (textComponent == null) {
				throw new Exception("failed to get Text component from prefab:" + prefabName);
			}

			// set content first.
			textComponent.text = text;
			
			// Debug.LogError("offset:" + offset + " text:" + text);

			
			var generator = new TextGenerator();

			var setting = textComponent.GetGenerationSettings(new Vector2(contentWidth - offset, contentHeight));
			generator.Populate(text, setting);

			var lines = new List<string>();
			var nextText = text;// get copy.
			
			while (true) {
				if (generator.lines.Count == 1) {
					lines.Add(nextText);
					break;
				}

				// Debug.LogError("nextText:" + nextText + " generator.lines.Count:" + generator.lines.Count);

				// 複数行が出たので、continue, 

				// 折り返しが発生したところから先のtextを取得し、その前までをコンテンツとしてセットする必要がある。
				var nextTextLineInfo = generator.lines[1];
				var startIndex = nextTextLineInfo.startCharIdx;
				
				lines.Add(nextText.Substring(0, startIndex));

				nextText = nextText.Substring(startIndex);
				// ここから先の行は、今は見る必要がなくて、ターゲットを切り替えて再度実行する。
				
				textComponent.text = nextText;

				// populate again.
				generator.Invalidate();

				// ここで、幅は与えられている最大のものを使えるはず(親から伝わってくる時点で本来はすり減ったのが来てるはず)
				generator.Populate(textComponent.text, textComponent.GetGenerationSettings(new Vector2(contentWidth, contentHeight)));
			}
			
			textComponent.text = lines[0];
			var preferredWidth = textComponent.preferredWidth;

			// reset.
			textComponent.text = string.Empty;
			
			/*
				insert new line contents after this content.
			 */
			if (1 < lines.Count) {
				var newContentTexts = lines.GetRange(1, lines.Count-1);
				
				foreach (var s in newContentTexts) {
					// Debug.LogError("s:" + s);
				}

				var newVGameObjects = newContentTexts.Select(
					t => new VirtualGameObject(
						this.tag,
						this.depth, 
						new Dictionary<KV_KEY, string>(){
							{KV_KEY._CONTENT, t}
						},
						this.prefabName
					)
				).ToList();
				insert(newVGameObjects);
			}

			var totalHeight = 0;
			foreach (var l in generator.lines){
				// Debug.LogError("ina:" + ina + " ch topY:" + l.topY);
				// Debug.LogError("ina:" + ina + " ch index:" + l.startCharIdx);
				// Debug.LogError("ina:" + ina + " ch height:" + l.height);
				totalHeight += l.height;
			}
			
			// Debug.LogError("preferredWidth:" + preferredWidth);
			
			return new ContentAndWidthAndHeight(lines[0], preferredWidth, totalHeight);
		}
		
		private void LayoutTagContent(HandlePoint contentHandlePoint, Action<List<VirtualGameObject>> insert) {
			// set (x, y) start pos.
			rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x + contentHandlePoint.nextLeftHandle, rectTransform.anchoredPosition.y + contentHandlePoint.nextTopHandle);
			// Debug.LogError("LayoutTagContent rectTransform.anchoredPosition:" + rectTransform.anchoredPosition + " of tag:" + tag);

			var contentWidth = 0f;
			var contentHeight = 0f;

			// set kv.
			switch (tag) {
				case Tag.OL: {
					foreach (var kv in this.keyValueStore) {
						var key = kv.Key;
						switch (key) {
							case KV_KEY.START: {
								// do nothing yet.
								break;
							}
						}
					}
					break;
				}
				case Tag.A: {
					// do nothing.
					break;
				}
				case Tag.IMG: {
					var prefab = LoadPrefab(prefabName);
					if (prefab != null) {
						// use prefab size by default.
						var rectTransform = prefab.GetComponent<RectTransform>();
						contentWidth = rectTransform.sizeDelta.x;
						contentHeight = rectTransform.sizeDelta.y;
					}

					foreach (var kv in this.keyValueStore) {
						var key = kv.Key;
						switch (key) {
							case KV_KEY.WIDTH: {
								var width = Convert.ToInt32(kv.Value);;
								contentWidth = width;
								break;
							}
							case KV_KEY.HEIGHT: {
								var height = Convert.ToInt32(kv.Value);
								contentHeight = height;
								break;
							}
							case KV_KEY.SRC: {
								// ignore on layout.
								break;
							}
							case KV_KEY.ALT: {
								// do nothing yet.
								break;
							}
							default: {
								break;
							}
						}
					}

					var viewWidth = contentHandlePoint.viewWidth;
					if (viewWidth < contentWidth) {
						var ratio = viewWidth / contentWidth;
						// Debug.LogError("ratio:" + ratio);

						contentWidth = viewWidth;
						contentHeight = contentHeight * ratio;
					}
					// Debug.LogWarning("img, 画面幅に対するサイズ限界指定を行う必要がある。");
					break;
				}
				case Tag.HR: {
					// Debug.LogWarning("hr, 画面幅に対するサイズ限界指定を行う必要がある。のと、もしサイズが画面幅より小さい場合、なんか中央寄せとか行うか？ パーセントで扱うか?みたいな。まあ面倒臭いので限界 + paddingでなんとかできるようにしとく");
					var prefab = LoadPrefab(prefabName);
					if (prefab != null) {
						// use prefab size by default.
						var rectTransform = prefab.GetComponent<RectTransform>();
						contentWidth = rectTransform.sizeDelta.x;
						contentHeight = rectTransform.sizeDelta.y;

						if (contentHandlePoint.viewWidth < contentWidth) {
							contentWidth = contentHandlePoint.viewWidth;
						}
					}
					break;
				}
				
				case Tag._CONTENT: {
					// set text if exist.
					if (this.keyValueStore.ContainsKey(KV_KEY._CONTENT)) {
						var text = keyValueStore[KV_KEY._CONTENT];

						var contentAndWidthAndHeight = LayoutTextContent(contentHandlePoint.nextLeftHandle, text, contentHandlePoint.viewWidth, contentHandlePoint.viewHeight, insert);
						
						// overwrite actually layouted content.
						keyValueStore[KV_KEY._CONTENT] = contentAndWidthAndHeight.content;
						contentWidth = contentAndWidthAndHeight.width;
						contentHeight = contentAndWidthAndHeight.totalHeight;
					}
					break;
				}
				
				default: {
					if (0 < keyValueStore.Count()) {
						Debug.LogError("tag:" + tag + "'s attributes are ignored.");
					}

					// do nothing.
					break;
				}
			}

			// set content size.
			rectTransform.sizeDelta = new Vector2(contentWidth, contentHeight);
		}
		
		/**
			return generated game object.
		*/
		public GameObject MaterializeRoot (string viewName, Vector2 viewPort, Tokenizer.OnLayoutDelegate onLayoutDel, Tokenizer.OnMaterializeDelegate onMaterializeDel) {
			var rootHandlePoint = new HandlePoint(0, 0, viewPort.x, viewPort.y);

			// 事前計算、ここでコンテンツの一覧を返すようにすればいいかな。要素単位で。
			Layout(this, rootHandlePoint, onLayoutDel, t => {});


			this._gameObject = new GameObject(viewName + Tag.ROOT.ToString());
			
			this.rootInstance = this._gameObject.AddComponent<InformationRootMonoBehaviour>();
			var rectTrans = this._gameObject.AddComponent<RectTransform>();
			rectTrans.anchorMin = Vector2.up;
			rectTrans.anchorMax = Vector2.up;
			rectTrans.pivot = Vector2.up;
			rectTrans.position = Vector2.zero;
			rectTrans.sizeDelta = viewPort;
			
			// 範囲指定してGOを充てる、ということがしたい。
			Materialize(this, onMaterializeDel);

			return this._gameObject;
		}
		
		/**
			layout contents.

			set position and size of content.
		*/
		private HandlePoint Layout (VirtualGameObject parent, HandlePoint handlePoint, Tokenizer.OnLayoutDelegate onLayoutDel, Action<List<VirtualGameObject>> insert) {
			switch (this.tag) {
				case Tag.ROOT: {
					// do nothing.
					break;
				}
				default: {
					// Debug.LogError("before layout rectTransform.anchoredPosition:" + rectTransform.anchoredPosition + " of tag:" + tag + " handlePoint:" + handlePoint.nextTopHandle);
					LayoutTagContent(handlePoint, insert);
					// Debug.LogError("after layout rectTransform.anchoredPosition:" + rectTransform.anchoredPosition + " of tag:" + tag + " handlePoint:" + handlePoint.nextTopHandle);
					break;
				}
			}

			// parent layout is done. will be resized by child, then padding.

			var childlen = this.transform.GetChildlen();
			if (0 < childlen.Count) {
				LayoutChildlen(childlen, handlePoint, onLayoutDel);

				/*
				 set parent = this content's size to wrapping all childlen.
				 */
				var rightBottomPoint = Vector2.zero;

				// fit most large bottom-right point. largest point of width and y.
				foreach (var child in childlen) {
					var paddedRightBottomPoint = child.PaddedRightBottomPoint();

					if (rightBottomPoint.x < paddedRightBottomPoint.x) {
						rightBottomPoint.x = paddedRightBottomPoint.x;
					}
					if (rightBottomPoint.y < paddedRightBottomPoint.y) {
						rightBottomPoint.y = paddedRightBottomPoint.y;
					}
				}
				
				// fit size to wrap all child contents.
				rectTransform.sizeDelta = rightBottomPoint;
				// Debug.LogError("set wrap rectTransform.sizeDelta:" + rectTransform.sizeDelta + " of tag:" + tag);
				// Debug.LogError("after wrap rectTransform.anchoredPosition:" + rectTransform.anchoredPosition + " of tag:" + tag + " handlePoint:" + handlePoint.nextTopHandle);
				// layout and padding and orientation of child tags are done.
			}
			
			/*
				set padding if need.
				default padding is 0.
			*/
			onLayoutDel(this.tag, this.depth, this.padding, new Dictionary<KV_KEY, string>(this.keyValueStore));
			
			/*
				adopt padding to this content.
			*/
			{
				// translate anchor position of content.(child follows parent.)
				rectTransform.anchoredPosition += padding.LeftTopPoint();
				
				handlePoint.nextLeftHandle += padding.PadWidth();
				handlePoint.nextTopHandle += padding.PadHeight();
				// Debug.LogWarning("実験した方が良さそう");
			}
			// Debug.LogError("rectTransform.anchoredPosition:" + rectTransform.anchoredPosition);

			/*
				set next left-top point by parent tag kind.
			*/
			switch (parent.tag) {
				default: {
					// 回り込みを実現する。んだけど、これはどちらかというと多数派で、デフォルトっぽい。
					// next content is planned to layout to the next of this content.
					handlePoint.nextLeftHandle = this.rectTransform.anchoredPosition.x + this.rectTransform.sizeDelta.x + this.padding.PadWidth();// right edge with padding
					// Debug.LogError("handlePoint.nextLeftHandle:" + handlePoint.nextLeftHandle);
					break;
				}

				// Rootコンテンツにぶらさがっている項目は、全てCRLFがかかる。
				case Tag.ROOT: {
					// CRLF
					handlePoint.nextLeftHandle = 0;
					handlePoint.nextTopHandle += this.rectTransform.sizeDelta.y + this.padding.PadHeight();

					// Debug.LogError("親がRootなので、改行する。handlePoint.nextTopHandle:" + handlePoint.nextTopHandle + " of tag:" + tag + " rectTransform.anchoredPosition:" + this.rectTransform.anchoredPosition);
					break;
				}
			}
			
			return handlePoint;
		}

		private void LayoutChildlen (List<VirtualGameObject> childlen, HandlePoint handlePoint, Tokenizer.OnLayoutDelegate onLayoutDel) {
			// Debug.LogWarning("LayoutChildlen、子供にいくに従って、親要素の起点から幅と高さの制限をつける必要がある。");
			// Debug.LogError("handlePoint.nextLeftHandle:" + handlePoint.nextLeftHandle);
			var childHandlePoint = new HandlePoint(0, 0, handlePoint.viewWidth, handlePoint.viewHeight);

			// layout -> resize -> padding of childlen.
		
			var layoutLine = new List<VirtualGameObject>();
			var i = 0;

			while (true) {
				if (childlen.Count <= i) {
					break;
				}

				var child = childlen[i];

				// consume br as linefeed.
				if (child.tag == Tag.BR) {
					// Debug.LogError("brが発生するので、handlePointのyは変わってるはず:" + handlePoint.nextTopHandle);
					childHandlePoint = SortByLayoutLine(layoutLine, childHandlePoint);
					// Debug.LogError("brが発生したので、handlePointのyは変わってるはず:" + handlePoint.nextTopHandle);

					// forget current line.
					layoutLine.Clear();

					// set next line.
					childHandlePoint.nextLeftHandle = 0;
					i++;
					continue;
				}
				
				// Debug.LogWarning("hr、これ下の方でまとめて処理できるかも。");
				// consume hr 1/2 as horizontal rule.
				if (child.tag == Tag.HR) {
					childHandlePoint = SortByLayoutLine(layoutLine, childHandlePoint);

					// forget current line.
					layoutLine.Clear();
				}


				var sortLayoutLineBeforeLining = false;
				var sortLayoutLineAfterLining = false;
				

				/*
					insert content to childlen list.
					create new content from one long content by length overflow.
				 */
				Action<List<VirtualGameObject>> insertAct = insertNewVGameObject => {
					childlen.InsertRange(i + 1, insertNewVGameObject);

					// this line is ended at this content. need layout.
					sortLayoutLineAfterLining = true;
				};

				// set position and calculate size.
				childHandlePoint = child.Layout(this, childHandlePoint, onLayoutDel, insertAct);
				
				// consume hr 2/2 as horizontal rule.
				if (child.tag == Tag.HR) {
					// set next line.
					childHandlePoint.nextLeftHandle = 0;
					i++;
					continue;
				}
				
				// root content does not request sorting child contents.
				if (this.tag == Tag.ROOT) {
					i++;
					continue;
				}


				/*
					the insertAct(t) is raised or not raised.
				 */
				
				// if child is content and that width is 0, this is because, there is not enough width in this line.
				// line is ended.
				if (child.tag == Tag._CONTENT && child.rectTransform.sizeDelta.x == 0) {
					sortLayoutLineAfterLining = true;
				}


				/*
					nested bq.
				 */
				if (this.tag == Tag.BLOCKQUOTE) {
					// nested bq.
					if (child.tag == Tag.BLOCKQUOTE) {
						sortLayoutLineBeforeLining = true;
					}
				}

				/*
					nested list's child list should be located to new line.
				 */
				if (this.tag == Tag.LI) {
					// nested list.
					if (child.tag == Tag.OL || child.tag == Tag.UL) {
						sortLayoutLineBeforeLining = true;
					}
				}

				// list's child should be ordered vertically.
				if (this.tag == Tag.OL || this.tag == Tag.UL) {
					sortLayoutLineAfterLining = true;
				}
				
				// check width overflow.
				// if next left handle is overed, sort as lined contents.
				if (childHandlePoint.viewWidth < childHandlePoint.nextLeftHandle) {
					sortLayoutLineBeforeLining = true;
				}
				

				/*
					sort current lined contents as 1 line of contents.
					before adding current content.
				 */
				if (sortLayoutLineBeforeLining) {
					childHandlePoint = SortByLayoutLine(layoutLine, childHandlePoint);

					// forget current line.
					layoutLine.Clear();

					// move current child content to next line head.
					child.rectTransform.anchoredPosition = new Vector2(childHandlePoint.nextLeftHandle + child.padding.left, childHandlePoint.nextTopHandle + child.padding.top);
					// Debug.LogError("child.rectTransform.anchoredPosition:" + child.rectTransform.anchoredPosition);
			
					// set next handle.
					childHandlePoint.nextLeftHandle = childHandlePoint.nextLeftHandle + child.padding.left + child.rectTransform.sizeDelta.x + child.padding.right;
				}

				// content width is smaller than viewpoint width.
				layoutLine.Add(child);

				/*
					sort current lined contents as 1 line of contents.
					after adding current content.

					this line is ended by this content.
				 */
				if (sortLayoutLineAfterLining) {
					childHandlePoint = SortByLayoutLine(layoutLine, childHandlePoint);

					// forget current line.
					layoutLine.Clear();

					// set next content's head position.
					childHandlePoint.nextLeftHandle = 0;
				}

				i++;
			}

			// if layoutLine content is exist, re-layout all in 1 line.
			if (0 < layoutLine.Count) {
				childHandlePoint = SortByLayoutLine(layoutLine, childHandlePoint);
			}
		}

		/**
			create line of contents -> sort all content by base line.
		*/
		private HandlePoint SortByLayoutLine (List<VirtualGameObject> layoutLine, HandlePoint handlePoint) {
			// find tallest content in layoutLine.
			var targetHeightObjArray = layoutLine.OrderByDescending(c => c.rectTransform.sizeDelta.y + c.padding.PadHeight()).ToArray();
			
			if (0 < targetHeightObjArray.Length) {
				var tallestContent = targetHeightObjArray[0];
				
				// get tallest padded height. this will be this layoutLine's bottom line.
				var paddedHighestHeightInLine = tallestContent.rectTransform.sizeDelta.y + tallestContent.padding.PadHeight();
				
				// other child content will be moved.
				var skipFirst = true;
				foreach (var childInLine in targetHeightObjArray) {
					if (skipFirst) {
						skipFirst = false;
						continue;
					}
					
					var childPaddedHeight = childInLine.rectTransform.sizeDelta.y + childInLine.padding.PadHeight();
					var heightDiff = paddedHighestHeightInLine - childPaddedHeight;
					childInLine.rectTransform.anchoredPosition += new Vector2(0, heightDiff);
					
					// Debug.LogError("childInLine:" + childInLine.tag + " childInLine.rectTransform.anchoredPosition:" + childInLine.rectTransform.anchoredPosition + " under tag:" + this.tag + " heightDiff:" + heightDiff);
				}

				// set next line head.
				handlePoint.nextLeftHandle = 0;
				handlePoint.nextTopHandle = tallestContent.rectTransform.anchoredPosition.y + tallestContent.rectTransform.sizeDelta.y + tallestContent.padding.PadHeight();
				// Debug.LogError("SortByLayoutLine handlePoint.nextTopHandle:" + handlePoint.nextTopHandle);
				// Debug.LogError("SortByLayoutLine rectTransform.anchoredPosition:" + rectTransform.anchoredPosition + " of tag:" + tag + " handlePoint:" + handlePoint.nextTopHandle);
			}

			return handlePoint;
		}


		private void Materialize (VirtualGameObject parent, Tokenizer.OnMaterializeDelegate onMaterializeDel) {
			switch (this.tag) {
				case Tag.ROOT: {
					// do nothing.
					break;
				}
				case Tag.BR: {
					// has no child. no visual. do nothing.
					break;
				}
				default: {
					this._gameObject = MaterializeTagContent();
					this._gameObject.transform.SetParent(parent._gameObject.transform, false);
					var rectTrans = this._gameObject.GetComponent<RectTransform>();
					if (rectTrans == null) {
						return;
					}

					// set position. convert layout position to uGUI position system.
					rectTrans.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, -rectTransform.anchoredPosition.y);
					// Debug.LogError("materialize rectTrans.anchoredPosition:" + rectTrans.anchoredPosition);
					rectTrans.sizeDelta = rectTransform.sizeDelta;

					break;
				}
			}
			
			foreach (var child in this.transform.GetChildlen()) {
				child.Materialize(this, onMaterializeDel);
			}
			
			onMaterializeDel(this._gameObject, this.tag, this.depth, new Dictionary<KV_KEY, string>(this.keyValueStore));
		}

		private GameObject MaterializeTagContent () {
			var prefab = LoadPrefab(prefabName);
			if (prefab == null) {
				return new GameObject("missing prefab:" + prefabName + " of tag:" + this.tag);
			}

			var obj = LoadGameObject(prefab);

			// set parameters.
			switch (tag) {
				case Tag.A: {
					foreach (var kvs in keyValueStore) {
						var key = kvs.Key;
						switch (key) {
							case KV_KEY.HREF: {
								var href = kvs.Value;

								// add button component.
								var rootObject = GetRootGameObject();
								var rootMBInstance = rootObject.rootInstance;
								
								AddButton(obj, () => rootMBInstance.OnLinkTapped(tag, href));
								break;
							}
							default: {
								// do nothing.
								break;
							}
						}
					}
					break;
				}
				case Tag.IMG: {
					foreach (var kv in keyValueStore) {
						var key = kv.Key;
						switch (key) {
							case KV_KEY.SRC: {
								var src = kv.Value;
								
								// add button component.
								var rootObject = GetRootGameObject();
								var rootMBInstance = rootObject.rootInstance;
								
								AddButton(obj, () => rootMBInstance.OnImageTapped(tag, src));
								break;
							}
							default: {
								// do nothing.
								break;
							}
						}
					}
					break;
				}
				
				case Tag._CONTENT: {
					foreach (var kvs in keyValueStore) {
						switch (kvs.Key) {
							case KV_KEY._CONTENT:{
								var text = kvs.Value;
								if (!string.IsNullOrEmpty(text)) {
									var textComponent = obj.GetComponent<Text>();
									textComponent.text = text;
								}
								break;
							}
							case KV_KEY._PARENTTAG: {
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
					// do nothing.
					break;
				}
			}

			return obj;
		}

		private GameObject LoadPrefab (string prefabName) {
			// Debug.LogWarning("辞書にできる");
			return Resources.Load(prefabName) as GameObject;
		}

		private GameObject LoadGameObject (GameObject prefab) {
			// Debug.LogWarning("ここを後々、プールからの取得に変える。タグ単位でGameObjectのプールを作るか。");
			return GameObject.Instantiate(prefab);
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

	public class VirtualTransform {
		private readonly VirtualGameObject vGameObject;
		private List<VirtualGameObject> _childlen = new List<VirtualGameObject>();
		public VirtualTransform (VirtualGameObject gameObject) {
			this.vGameObject = gameObject;
		}

		public void SetParent (VirtualTransform t) {
			t._childlen.Add(this.vGameObject);
			this.vGameObject.parent = t.vGameObject;
		}
		
		public VirtualGameObject Parent () {
			return this.vGameObject.parent;
		}

		public List<VirtualGameObject> GetChildlen () {
			return _childlen;
		}
	}

	public class VirtualRectTransform {
		public Vector2 anchoredPosition = Vector2.zero;
		public Vector2 sizeDelta = Vector2.zero;
	}
}