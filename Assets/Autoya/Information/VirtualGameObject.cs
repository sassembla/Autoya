using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AutoyaFramework.Information {
	public class VirtualGameObject {

		// global static sprite cache.
		private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
		private static List<string> spriteDownloadingUris = new List<string>();


		public GameObject _gameObject;
		public readonly string prefabName;

		public InformationRootMonoBehaviour rootInstance;

		public readonly Tag tag;
		public readonly Tag[] depth;
		public readonly Dictionary<KV_KEY, string> keyValueStore;

		public Padding padding;
		
		private VirtualTransform vTransform;
		
		public VirtualRectTransform vRectTransform = new VirtualRectTransform();

		public VirtualGameObject parent;

		public Action<IEnumerator> executor;
		
		public VirtualTransform transform {
			get {
				return vTransform;
			}
		}

		public Vector2 PaddedRightBottomPoint () {
			return vRectTransform.vAnchoredPosition + vRectTransform.vSizeDelta + new Vector2(padding.PadWidth(), padding.PadHeight());
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
			
			if (generator.lineCount == 0) {
				// no line layouted. set this text to next line.
				var nextLineVGameObject = new VirtualGameObject(
					this.tag,
					this.depth, 
					new Dictionary<KV_KEY, string>(){
						{KV_KEY._CONTENT, nextText}
					},
					this.prefabName
				);
				insert(new List<VirtualGameObject>{nextLineVGameObject});

				// return empty line.
				new ContentAndWidthAndHeight(string.Empty, 0, 0);
			}

			while (true) {
				// if rest text is included by one line, line collection is done.
				if (generator.lineCount == 1) {
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
				if (generator.lineCount == 0) {
					throw new Exception("no line detected 2. nextText:" + nextText);
				}
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
				
				// foreach (var s in newContentTexts) {
				// 	Debug.LogError("s:" + s);
				// }

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

			// Debug.LogError("preferredWidth:" + preferredWidth);
			
			return new ContentAndWidthAndHeight(lines[0], preferredWidth, generator.lines[0].height);
		}
		
		private void LayoutTagContent (float xOffset, float yOffset, float viewWidth, float viewHeight, Action<List<VirtualGameObject>> insert) {
			// set (x, y) start pos.
			vRectTransform.vAnchoredPosition = new Vector2(vRectTransform.vAnchoredPosition.x + xOffset, vRectTransform.vAnchoredPosition.y + yOffset);
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
								var width = Convert.ToInt32(kv.Value);
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

						if (viewWidth < contentWidth) {
							contentWidth = viewWidth;
						}
					}
					break;
				}
				
				case Tag._CONTENT: {
					// set text if exist.
					if (this.keyValueStore.ContainsKey(KV_KEY._CONTENT)) {
						var widthUpdated = false;

						// already layout done.
						if (keyValueStore.ContainsKey(KV_KEY._CONTENT_WIDTH)) {
							// set view width if exist.
							viewWidth = float.Parse(keyValueStore[KV_KEY._CONTENT_WIDTH], CultureInfo.InvariantCulture.NumberFormat);
							widthUpdated = true;
						}

						var text = keyValueStore[KV_KEY._CONTENT];
						
						var contentAndWidthAndHeight = LayoutTextContent(xOffset, text, viewWidth, viewHeight, insert);
						
						// overwrite actually layouted content.
						keyValueStore[KV_KEY._CONTENT] = contentAndWidthAndHeight.content;
						
						// write width and height.
						keyValueStore[KV_KEY._CONTENT_WIDTH] = contentAndWidthAndHeight.width.ToString();
						keyValueStore[KV_KEY.HEIGHT] = contentAndWidthAndHeight.height.ToString();

						contentWidth = contentAndWidthAndHeight.width;
						contentHeight = contentAndWidthAndHeight.height;

						// overwrite content width.
						if (widthUpdated) {
							keyValueStore[KV_KEY._CONTENT_WIDTH] = viewWidth.ToString();
							contentWidth = viewWidth;
						}
					}
					break;
				}
				case Tag.TH:
				case Tag.TD: {
					// has KV_KEY._CONTENT_WIDTH value, but ignore.
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
			vRectTransform.vSizeDelta = new Vector2(contentWidth, contentHeight);
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
					LayoutTagContent(handlePoint.nextLeftHandle, handlePoint.nextTopHandle, handlePoint.viewWidth, handlePoint.viewHeight, insert);
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
				vRectTransform.vSizeDelta = rightBottomPoint;
				

				// calculate table's contents.
				if (this.tag == Tag.TABLE) {
					/*
						all contents size calculation inside this table is done.
						count up row,
						find longest content,
						and adjust left point of contents.
					 */
					var tableLayoutRecord = new TableLayoutRecord();
					
					// countup rows.
					foreach (var tableChild in this.transform.GetChildlen()) {
						CollectTableContentRowCountRecursively(tableChild, tableLayoutRecord);
					}

					// find longest content.
					foreach (var tableChild in this.transform.GetChildlen()) {
						CollectTableContentRowMaxWidthsRecursively(tableChild, tableLayoutRecord);
					}

					// resize & reset position of this table contents by calculated record.
					foreach (var tableChild in this.transform.GetChildlen()) {
						SetupTableContentPositionRecursively(tableChild, tableLayoutRecord);
					}
				}
			}
			
			/*
				set padding if need.
				default padding is 0.
			*/
			onLayoutDel(this.tag, this.depth, this.padding, this.keyValueStore);
			
			/*
				adopt padding to this content.
			*/
			{
				// translate anchor position of content.(child follows parent.)
				vRectTransform.vAnchoredPosition += padding.LeftTopPoint();
				
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
					handlePoint.nextLeftHandle = this.vRectTransform.vAnchoredPosition.x + this.vRectTransform.vSizeDelta.x + this.padding.PadWidth();// right edge with padding
					// Debug.LogError("handlePoint.nextLeftHandle:" + handlePoint.nextLeftHandle);
					break;
				}

				// Rootコンテンツにぶらさがっている項目は、全てCRLFがかかる。
				case Tag.ROOT: {
					// CRLF
					handlePoint.nextLeftHandle = 0;
					handlePoint.nextTopHandle += this.vRectTransform.vSizeDelta.y + this.padding.PadHeight();

					// Debug.LogError("親がRootなので、改行する。handlePoint.nextTopHandle:" + handlePoint.nextTopHandle + " of tag:" + tag + " rectTransform.anchoredPosition:" + this.rectTransform.anchoredPosition);
					break;
				}
			}
			
			return handlePoint;
		}

		private class TableLayoutRecord {
			private int rowIndex;
			private List<float> xWidth = new List<float>();

			public void IncrementRow () {
				xWidth.Add(0);
			}
			
			public void UpdateMaxWidth (float width) {
				if (xWidth[rowIndex] < width) {
					xWidth[rowIndex] = width;
				}
				rowIndex = (rowIndex + 1) % xWidth.Count;
			}
			public float TotalWidth () {
				var ret = 0f;
				foreach (var width in xWidth) {
					ret += width;
				}
				return ret;
			}
			
			public OffsetAndWidth GetOffsetAndWidth () {
				var currentIndex = rowIndex % xWidth.Count;
				var offset = 0f;
				for (var i = 0; i < currentIndex; i++) {
					offset += xWidth[i];
				}
				var width = xWidth[rowIndex % xWidth.Count];

				rowIndex++;

				return new OffsetAndWidth(offset, width);
			}

			public struct OffsetAndWidth {
				public float offset;
				public float width;
				public OffsetAndWidth (float offset, float width) {
					this.offset = offset;
					this.width = width;
				}
			}
		}

		private void CollectTableContentRowCountRecursively (VirtualGameObject child, TableLayoutRecord tableLayoutRecord) {
			// count up table header count.
			if (child.tag == Tag.TH) {
				tableLayoutRecord.IncrementRow();
			}

			foreach (var nestedChild in child.transform.GetChildlen()) {
				child.CollectTableContentRowCountRecursively(nestedChild, tableLayoutRecord);
			}
		}

		private void CollectTableContentRowMaxWidthsRecursively (VirtualGameObject child, TableLayoutRecord tableLayoutRecord) {
			var total = 0f;
			foreach (var nestedChild in child.transform.GetChildlen()) {
				child.CollectTableContentRowMaxWidthsRecursively(nestedChild, tableLayoutRecord);
				if (child.tag == Tag.TH || child.tag == Tag.TD) {
					var nestedChildContentWidth = nestedChild.vRectTransform.vSizeDelta.x;
					total += nestedChildContentWidth;
				}
			}

			if (child.tag == Tag.TH || child.tag == Tag.TD) {
				tableLayoutRecord.UpdateMaxWidth(total);
			}
		}

		private void SetupTableContentPositionRecursively (VirtualGameObject child, TableLayoutRecord tableLayoutRecord) {
			// overwrite parent content width of TH and TD.
			if (child.tag == Tag.THEAD || child.tag == Tag.TBODY || child.tag == Tag.THEAD || child.tag == Tag.TR) {
				var width = tableLayoutRecord.TotalWidth();
				child.vRectTransform.vSizeDelta = new Vector2(width, child.vRectTransform.vSizeDelta.y);
			}

			/*
				change TH, TD content's x position and width.
				x position -> 0, 1st row's longest content len, 2nd row's longest content len,...
				width -> 1st row's longest content len, 2nd row's longest content len,...
			*/
			if (child.tag == Tag.TH || child.tag == Tag.TD) {
				var offsetAndWidth = tableLayoutRecord.GetOffsetAndWidth();
				
				child.vRectTransform.vAnchoredPosition = new Vector2(offsetAndWidth.offset, child.vRectTransform.vAnchoredPosition.y);
				child.vRectTransform.vSizeDelta = new Vector2(offsetAndWidth.width, child.vRectTransform.vSizeDelta.y);
			}
			
			foreach (var nestedChild in child.transform.GetChildlen()) {
				child.SetupTableContentPositionRecursively(nestedChild, tableLayoutRecord);	
			}
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
				if (child.tag == Tag._CONTENT && child.vRectTransform.vSizeDelta.x == 0) {
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

				// table
				{
					if (child.tag == Tag.THEAD) {// table head is single line.
						sortLayoutLineAfterLining = true;
					} else if (child.tag == Tag.TR) {// table row.
						sortLayoutLineAfterLining = true;
					}
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
					child.vRectTransform.vAnchoredPosition = new Vector2(childHandlePoint.nextLeftHandle + child.padding.left, childHandlePoint.nextTopHandle + child.padding.top);
					// Debug.LogError("child.rectTransform.anchoredPosition:" + child.rectTransform.anchoredPosition);
			
					// set next handle.
					childHandlePoint.nextLeftHandle = childHandlePoint.nextLeftHandle + child.padding.left + child.vRectTransform.vSizeDelta.x + child.padding.right;
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
			var targetHeightObjArray = layoutLine.OrderByDescending(c => c.vRectTransform.vSizeDelta.y + c.padding.PadHeight()).ToArray();
			
			if (0 < targetHeightObjArray.Length) {
				var tallestContent = targetHeightObjArray[0];
				
				// get tallest padded height. this will be this layoutLine's bottom line.
				var paddedHighestHeightInLine = tallestContent.vRectTransform.vSizeDelta.y + tallestContent.padding.PadHeight();
				
				// other child content will be moved.
				var skipFirst = true;
				foreach (var childInLine in targetHeightObjArray) {
					if (skipFirst) {
						skipFirst = false;
						continue;
					}
					
					var childPaddedHeight = childInLine.vRectTransform.vSizeDelta.y + childInLine.padding.PadHeight();
					var heightDiff = paddedHighestHeightInLine - childPaddedHeight;
					childInLine.vRectTransform.vAnchoredPosition += new Vector2(0, heightDiff);
					
					// Debug.LogError("childInLine:" + childInLine.tag + " childInLine.rectTransform.anchoredPosition:" + childInLine.rectTransform.anchoredPosition + " under tag:" + this.tag + " heightDiff:" + heightDiff);
				}

				// set next line head.
				handlePoint.nextLeftHandle = 0;
				handlePoint.nextTopHandle = tallestContent.vRectTransform.vAnchoredPosition.y + tallestContent.vRectTransform.vSizeDelta.y + tallestContent.padding.PadHeight();
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
					rectTrans.anchoredPosition = new Vector2(vRectTransform.vAnchoredPosition.x, -vRectTransform.vAnchoredPosition.y);
					// Debug.LogError("materialize rectTrans.anchoredPosition:" + rectTrans.anchoredPosition);
					rectTrans.sizeDelta = vRectTransform.vSizeDelta;
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

								var rootObject = GetRootGameObject();
								
								LoadImageAsync(src, obj, rootObject.executor);

								// add button component.
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

		private void LoadImageAsync (string uriSource, GameObject target, Action<IEnumerator> executor) {
			IEnumerator coroutine;

			/*
				supported schemes are,
					
					^http://		http scheme => load asset from web.
					^https://		https scheme => load asset from web.
					^assetbundle://	assetbundle scheme => load asset from assetBundle.
					^*				absolute path => (Resources/)somewhere/resource path.
					^./				relative path => (Resources/)somewhere/resource path.
			 */
			var schemeAndPath = uriSource.Split(new char[]{'/'}, 2);
			var scheme = schemeAndPath[0];
			switch (scheme) {
				case "assetbundle:": {
					var bundleName = uriSource;// ./始まりのケースもあるのはまあなんかまとめない方が良さそう。
					coroutine = LoadImageFromAssetBundle(uriSource, target);
					break;
				}
				case "https:":
				case "http:": {
					coroutine = LoadImageFromWeb(uriSource, target);
					break;
				}
				case ".": {
					var resourcePath = uriSource.Substring(2);
					coroutine = LoadImageFromResources(resourcePath, target);
					break;
				}
				default: {// other.
					if (string.IsNullOrEmpty(scheme)) {
						Debug.LogError("empty uri found:" + uriSource);
						return;
					}

					// not empty. treat as resource file path.
					coroutine = LoadImageFromResources(uriSource, target);
					break;
				}
			}

			executor(coroutine);
		}

		private IEnumerator LoadImageFromAssetBundle (string assetName, GameObject target) {
			yield return null;
			Debug.LogError("LoadImageFromAssetBundle bundleName:" + assetName);
		}

		private IEnumerator LoadImageFromResources (string uriSource, GameObject target) {
			var extLen = Path.GetExtension(uriSource).Length;
			var uri = uriSource.Substring(0, uriSource.Length - extLen);

			var resourceLoading = Resources.LoadAsync(uri);
			while (!resourceLoading.isDone) {
				yield return null;
			}
			
			// create tex.
			var tex = resourceLoading.asset as Texture2D;
			
			// set tex to sprite.
			var targetImageComponent = target.GetComponent<Image>();
			targetImageComponent.sprite = Sprite.Create(tex, new Rect(0,0, tex.width, tex.height), Vector2.zero);
		}

		private IEnumerator LoadImageFromWeb (string url, GameObject target) {
			// this request does not have any request-header parameters.
			var targetImageComponent = target.GetComponent<Image>();

			if (targetImageComponent != null) {
				if (spriteDownloadingUris.Contains(url)) {
					while (!spriteCache.ContainsKey(url)) {
						yield return null;
					}

					// download is done. cached sprite exists.
					targetImageComponent.sprite = spriteCache[url];
				} else {
					spriteDownloadingUris.Add(url);
				}

				// go through.
			} else {
				Debug.LogError("no Image component found.");
				yield break;
			}

			// start download tex from url.
			using (var request = UnityWebRequest.GetTexture(url)) {
				var p = request.Send();
				var timeoutSec = 5;
				var limitTick = DateTime.UtcNow.AddSeconds(timeoutSec).Ticks;

				while (!p.isDone) {
					yield return null;

					// check timeout.
					if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks) {
						Debug.LogError("timeout. load aborted, dataPath:" + url);
						request.Abort();
						yield break;
					}
				}

				var responseCode = (int)request.responseCode;
				var responseHeaders = request.GetResponseHeaders();
				
				if (request.isError) {
					Debug.LogError("failed to download data:" + url + " reason:" + request.error);
					// failed(connectionId, responseCode, request.error, responseHeaders);
					yield break;
				}

				if (responseCode != 200) {
					Debug.LogError("failed to download data:" + url);
					yield break;
				}

				// create tex.
				var tex = DownloadHandlerTexture.GetContent(request);


				// cache this sprite for other requests.
				var spr = Sprite.Create(tex, new Rect(0,0, tex.width, tex.height), Vector2.zero);
				spriteCache[url] = spr;
				spriteDownloadingUris.Remove(url);

				// set tex to sprite.
				targetImageComponent.sprite = spr;
			}
		}

		
		private GameObject LoadPrefab (string prefabName) {
			// Debug.LogWarning("辞書にできる");
			return Resources.Load(prefabName) as GameObject;
		}

		private GameObject LoadGameObject (GameObject prefab) {
			// Debug.LogWarning("ここを後々、プールからの取得に変える。タグ単位でGameObjectのプールを作るか。スクロールとかで可視範囲に入ったら内容を当てる、みたいなのがやりたい。");
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
		public Vector2 vAnchoredPosition = Vector2.zero;
		public Vector2 vSizeDelta = Vector2.zero;
	}
}