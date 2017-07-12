using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AutoyaFramework.Information {

    /**
        レイアウトを実行するクラス。
    */
    public class LayoutMachine {
		private readonly InformationResourceLoader infoResLoader;
		private readonly string viewName;

		private readonly ViewBox view;

      	public LayoutMachine (
			  string viewName, 
			  ParsedTree @this, 
			  InformationResourceLoader infoResLoader,
			  ViewBox view, Action<IEnumerator> executor, 
			  Action<LayoutedTree> layouted
		) {
			this.viewName = viewName;

			this.infoResLoader = infoResLoader;

			this.view = view;
			
			// start execute.
			executor(StartLayout(@this, view, layouted));
        }

		private IEnumerator StartLayout (ParsedTree @this, ViewBox view, Action<LayoutedTree> layouted) {
			var handle = new HandlePoint(0, 0, view.width, view.height);

			var cor = LayoutRecursive((int)HtmlTag._ROOT, @this, handle, (i) => {});
			while (cor.MoveNext()) {
				yield return null;
			}

			var layoutedTree = new LayoutedTree(@this);
			layouted(layoutedTree);
		}

        /**
			layout contents.

			set position and size of content.
		*/
		private IEnumerator LayoutRecursive (int parentTag, ParsedTree @this, HandlePoint handle, Action<ParsedTree[]> insert) {
			switch (@this.parsedTag) {
				case (int)HtmlTag._ROOT: {
					// do nothing.
					break;
				}
				case (int)HtmlTag._DEPTH_ASSET_LIST_INFO: {
					if (this.viewName == InformationConstSettings.VIEWNAME_DEFAULT) {
						throw new Exception("can not set depthAssetList path with viewName 'Default'. please set your specific view name.");
					}
					infoResLoader.GetDepthAssetList(@this.keyValueStore[Attribute.SRC]);
					// list downloading will be suceeded or failed.
					break;
				}
				default: {
					var cor = LayoutTagContent(@this, handle, insert);
					while (cor.MoveNext()) {
						yield return null;
					}
					break;
				}
			}

			// parent anchor layout is done. will be resized by child with padding, then padding parent itself.

			var childlen = @this.GetChildlen();
			if (0 < childlen.Count) {
				var cor = LayoutChildlen(@this, handle, childlen);
				while (cor.MoveNext()) {
					yield return null;
				}
				
				/*
				 set parent size to wrapping all childlen.
				 */
				var rightBottomPoint = Vector2.zero;

				// fit most large bottom-right point. largest point of width and y.
				foreach (var child in childlen) {
					var paddedRightBottomPoint = PaddedRightBottomPoint(child);

					if (rightBottomPoint.x < paddedRightBottomPoint.x) {
						rightBottomPoint.x = paddedRightBottomPoint.x;
					}
					if (rightBottomPoint.y < paddedRightBottomPoint.y) {
						rightBottomPoint.y = paddedRightBottomPoint.y;
					}
				}
				
				// fit size to wrap all child contents.
				@this.sizeDelta = rightBottomPoint;
				
				// calculate table's contents.
				if (@this.parsedTag == (int)HtmlTag.TABLE) {
					/*
						all contents size calculation inside this table is done.
						count up row,
						find longest content,
						and adjust left point of contents.
					 */
					var tableLayoutRecord = new TableLayoutRecord();
					
					// countup rows.
					foreach (var tableChild in @this.GetChildlen()) {
						CollectTableContentRowCountRecursively(@this, tableChild, tableLayoutRecord);
					}

					// find longest content.
					foreach (var tableChild in @this.GetChildlen()) {
						CollectTableContentRowMaxWidthsRecursively(@this, tableChild, tableLayoutRecord);
					}

					// resize & reset position of this table contents by calculated record.
					foreach (var tableChild in @this.GetChildlen()) {
						SetupTableContentPositionRecursively(@this, tableChild, tableLayoutRecord);
					}
				}
			}
			
			
			/*
				set next left-top point by parent tag kind.
			*/
			switch (parentTag) {
				default: {
					// 回り込みを実現する。んだけど、これはどちらかというと多数派で、デフォルトっぽい。
					// next content is planned to layout to the next of this content.
					handle.nextLeftHandle = @this.anchoredPosition.x + @this.sizeDelta.x;// after padding.
					break;
				}

				// Rootコンテンツにぶらさがっている項目は、全てCRLFがかかる。
				case (int)HtmlTag._ROOT: {
					// CRLF
					handle.nextLeftHandle = 0;
					handle.nextTopHandle += @this.sizeDelta.y + @this.padding.PadHeight();					
					break;
				}
			}

			/*
				adopt padding to this content.
			*/
			{
				// translate anchor position of content.(child follows parent.)
				@this.anchoredPosition += @this.padding.LeftTopPoint();
				
				// handlePoint.nextLeftHandle += padding.right;
				handle.nextTopHandle += @this.padding.top + @this.padding.bottom;
			}
			
			yield break;
		}
        
		
        private IEnumerator LayoutTagContent (ParsedTree @this, HandlePoint handle, Action<ParsedTree[]> insert) {
			var xOffset = handle.nextLeftHandle;
			var yOffset = handle.nextTopHandle;
			var viewWidth = handle.viewWidth;
			var viewHeight = handle.viewHeight;


			// set (x, y) start pos.
			@this.anchoredPosition = new Vector2(@this.anchoredPosition.x + xOffset, @this.anchoredPosition.y + yOffset);
			

			if (viewName != InformationConstSettings.VIEWNAME_DEFAULT) {
				GameObject loadedPrefab = null;
				var cor = infoResLoader.LoadPrefab(
					viewName, 
					@this, 
					prefab => {
						loadedPrefab = prefab;
					},
					() => {

					}
				);

				while (cor.MoveNext()) {
					yield return null;
				}

				if (loadedPrefab != null) {
					var anchoredPos = loadedPrefab.GetComponent<RectTransform>().anchoredPosition;
					Debug.LogError("prefab取得できた。ので、基準位置をいじる。 anchoredPos:" + anchoredPos);
					// もし指定の値がある場合、その値をダイレクトに入れちゃって良いんじゃなかろうか。
					// 実際には階層ごとの累積があるんだけど、そこをどうやって考慮すれば良いか考えよう。
					@this.anchoredPosition = new Vector2(anchoredPos.x, -anchoredPos.y);
				}
			}

			var contentWidth = 0f;
			var contentHeight = 0f;
			
			// set kv.
			switch (@this.parsedTag) {
				case (int)HtmlTag.OL: {
					foreach (var kv in @this.keyValueStore) {
						var key = kv.Key;
						switch (key) {
							case Attribute.START: {
								// do nothing yet.
								break;
							}
						}
					}
					break;
				}
				case (int)HtmlTag.A: {
					// do nothing.
					break;
				}
				case (int)HtmlTag.IMG: {
					if (!@this.keyValueStore.ContainsKey(Attribute.SRC)) {
						throw new Exception("image should define src param.");
					}

					var src = @this.keyValueStore[Attribute.SRC];

					// determine image size from image's width & height.
					if (@this.keyValueStore.ContainsKey(Attribute.WIDTH) && @this.keyValueStore.ContainsKey(Attribute.HEIGHT)) {
						var width = @this.keyValueStore[Attribute.WIDTH];
						var height = @this.keyValueStore[Attribute.HEIGHT];

						if (width.EndsWith("%")) {
							contentWidth = GetPercentOf(viewWidth, width);
						} else {
							contentWidth = Convert.ToInt32(width);
						}

						if (height.EndsWith("%")) {
							contentHeight = GetPercentOf(viewHeight, height);
						} else {
							contentHeight = Convert.ToInt32(height);
						}

					} else if (@this.keyValueStore.ContainsKey(Attribute.WIDTH)) {
						// width only.
						var width = @this.keyValueStore[Attribute.WIDTH];
						if (width.EndsWith("%")) {
							contentWidth = GetPercentOf(viewWidth, width);
						} else {
							contentWidth = Convert.ToInt32(width);
						}

						// need to download image.
						// no height set yet. set height to default aspect ratio.
						var downloaded = false;
						infoResLoader.LoadImageAsync(
							src, 
							(sprite) => {
								downloaded = true;
								contentHeight = (contentWidth / sprite.rect.size.x) * sprite.rect.size.y;
							},
							() => {
								downloaded = true;
								contentHeight = 0;
							}
						);

						while (!downloaded) {
							yield return null;
						}

					} else if (@this.keyValueStore.ContainsKey(Attribute.HEIGHT)) {
						// height only.
						var height = @this.keyValueStore[Attribute.HEIGHT];
						if (height.EndsWith("%")) {
							contentHeight = GetPercentOf(viewHeight, height);
						} else {
							contentHeight = Convert.ToInt32(height);
						}

						// need to download image.
						// no width set yet. set height to default aspect ratio.
						var downloaded = false;
						infoResLoader.LoadImageAsync(
							src, 
							(sprite) => {
								downloaded = true;
								contentWidth = (contentHeight / sprite.rect.size.y) * sprite.rect.size.x;
							},
							() => {
								downloaded = true;
								contentWidth = 0;
							}
						);

						while (!downloaded) {
							yield return null;
						}

					} else {
						// no width, no height. use default size of image.
						var downloaded = false;
						infoResLoader.LoadImageAsync(
							src, 
							(sprite) => {
								downloaded = true;
								contentWidth = sprite.rect.size.x;
								contentHeight = sprite.rect.size.y;
							},
							() => {
								downloaded = true;
								contentWidth = 0;
								contentHeight = 0;
							}
						);

						while (!downloaded) {
							yield return null;
						}

					}
					break;
				}
				case (int)HtmlTag.HR: {
					GameObject prefab = null;
					var cor = infoResLoader.LoadPrefab(
						viewName,
						@this, 
						newPrefab => {
							prefab = newPrefab;
						},
						() => {
							throw new Exception("failed to load hr prefab:" + @this.prefabName + " at viewName:" + viewName);
						}
					);

					while (cor.MoveNext()) {
						yield return null;
					}

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
				
				case (int)HtmlTag._TEXT_CONTENT: {
					// set text if exist.
					if (@this.keyValueStore.ContainsKey(Attribute._CONTENT)) {
						var text = @this.keyValueStore[Attribute._CONTENT];
						
						var cor = LayoutTextContent(
							@this, 
							xOffset, 
							text, 
							viewWidth, 
							viewHeight, 
							insert,
							contentAndWidthAndHeight => {
								@this.keyValueStore[Attribute._CONTENT] = contentAndWidthAndHeight.content;
								contentWidth = contentAndWidthAndHeight.width;
								contentHeight = contentAndWidthAndHeight.height;
							}
						);

						while (cor.MoveNext()) {
							yield return null;
						}
					}
					break;
				}
				case (int)HtmlTag.TH:
				case (int)HtmlTag.TD: {
					// has KV_KEY._CONTENT_WIDTH value, but ignore.
					break;
				}
				default: {
					if (0 < @this.keyValueStore.Count) {
						Debug.LogWarning("tag:" + @this.rawTagName + "'s attributes are ignored. ignored attr:" + string.Join(", ", @this.keyValueStore.Keys.Select(v => v.ToString()).ToArray()));
					}

					// do nothing.
					break;
				}
			}
			
			// set content size.
			@this.sizeDelta = new Vector2(contentWidth, contentHeight);
		}

		private float GetPercentOf (float baseParam, string percentStr) {
			var num = percentStr.Substring(0, percentStr.Length-1);
			var widthPer = Convert.ToInt32(num);
			return baseParam * widthPer * 0.01f;// adopt %
		}

		/**
			stringが入るコンテナのサイズを生成して返す
		 */
        private IEnumerator LayoutTextContent (ParsedTree @this, float offset, string text, float contentWidth, float contentHeight, Action<ParsedTree[]> insert, Action<ContentAndWidthAndHeight> onCalculated) {
			GameObject textPrefab = null;
			var cor = infoResLoader.LoadPrefab(
				viewName,
				@this, 
				newPrefab => {
					textPrefab = newPrefab;
				},
				() => {
					throw new Exception("failed to load _content prefab:" + @this.prefabName + " at viewName:" + viewName);
				}
			);

			while (cor.MoveNext()) {
				yield return null;
			}

			// use prefab's text component for using it's text setting.
			var textComponent = textPrefab.GetComponent<Text>();
			if (textComponent == null) {
				throw new Exception("failed to get Text component from prefab:" + @this.prefabName + " of text content:" + text);
			}

			if (textComponent.font == null) {
				throw new Exception("font is null. prefab:" + @this.prefabName + " of depth:" + @this.depth.Length + " まだ適当。");
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
                
                var nextLineVGameObject = new ParsedTree(
					@this,
					new AttributeKVs(){
						{Attribute._CONTENT, nextText}
					}
				);
				insert(new ParsedTree[]{nextLineVGameObject});
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
				textComponent.text = nextText;

				// populate again.
				generator.Invalidate();

				// populate splitted text again.
				generator.Populate(textComponent.text, textComponent.GetGenerationSettings(new Vector2(contentWidth, contentHeight)));
				if (generator.lineCount == 0) {
					throw new Exception("no line detected 2. nextText:" + nextText);
				}
			}
			
			textComponent.text = lines[0];
			var preferredWidth = textComponent.preferredWidth;
			
			if (contentWidth < preferredWidth) {
				preferredWidth = contentWidth;
			}

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
					t => new ParsedTree(
						@this,
						new AttributeKVs(){
							{Attribute._CONTENT, t}
						}
					)
				).ToArray();
				insert(newVGameObjects);
			}

			// Debug.LogError("preferredWidth:" + preferredWidth);
			
			onCalculated(new ContentAndWidthAndHeight(lines[0], preferredWidth, generator.lines[0].height));
		}

        	
        private IEnumerator LayoutChildlen (ParsedTree @this, HandlePoint handle, List<ParsedTree> childlen) {
			// locate child content in relative. create new (0,0) handle.
			var childHandlePoint = new HandlePoint(0, 0, handle.viewWidth, handle.viewHeight);
			
			// layout -> resize -> padding of childlen.
		
			var layoutLine = new List<ParsedTree>();
			var i = 0;

			while (true) {
				if (childlen.Count <= i) {
					break;
				}

				var child = childlen[i];

				// consume br as linefeed.
				if (child.parsedTag == (int)HtmlTag.BR) {
					childHandlePoint = SortByLayoutLine(layoutLine, childHandlePoint);

					// forget current line.
					layoutLine.Clear();

					// set next line.
					childHandlePoint.nextLeftHandle = 0;
					i++;
					continue;
				}
				
				// consume hr 1/2 as horizontal rule.
				if (child.parsedTag == (int)HtmlTag.HR) {
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
				Action<ParsedTree[]> insertAct = insertNewVGameObject => {
					childlen.InsertRange(i + 1, insertNewVGameObject);

					// this line is ended at this content. need layout.
					sortLayoutLineAfterLining = true;
				};


				// set position and calculate size.
				var cor = LayoutRecursive(@this.parsedTag, child, childHandlePoint, insertAct);
				while (cor.MoveNext()) {
					yield return null;
				}

				// specific tag action.
				{
					
					// consume hr 2/2 as horizontal rule.
					if (child.parsedTag == (int)HtmlTag.HR) {
						// set next line.
						childHandlePoint.nextLeftHandle = 0;
						i++;
						continue;
					}
					
					// root content never sort child contents.
					if (@this.parsedTag == (int)HtmlTag._ROOT) {
						i++;
						continue;
					}


					/*
						the insertAct(t) is raised or not raised.
					*/
					
					// if child is content and that width is 0, this is because, there is not enough width in this line.
					// line is ended.
					if (child.parsedTag == (int)HtmlTag._TEXT_CONTENT && child.sizeDelta.x == 0) {
						sortLayoutLineAfterLining = true;
					}


					/*
						nested bq.
					*/
					if (@this.parsedTag == (int)HtmlTag.BLOCKQUOTE) {
						// nested bq.
						if (child.parsedTag == (int)HtmlTag.BLOCKQUOTE) {
							sortLayoutLineBeforeLining = true;
						}
					}

					/*
						nested list's child list should be located to new line.
					*/
					if (@this.parsedTag == (int)HtmlTag.LI) {
						// nested list.
						if (child.parsedTag == (int)HtmlTag.OL || child.parsedTag == (int)HtmlTag.UL) {
							sortLayoutLineBeforeLining = true;
						}
					}

					// list's child should be ordered vertically.
					if (@this.parsedTag == (int)HtmlTag.OL || @this.parsedTag == (int)HtmlTag.UL) {
						sortLayoutLineAfterLining = true;
					}
					
					// check width overflow.
					// if next left handle is overed, sort as lined contents.
					if (childHandlePoint.viewWidth < childHandlePoint.nextLeftHandle) {
						if (0 < layoutLine.Count) {// 1件以上存在しているのであれば、このラインはここまでで終わる。
							sortLayoutLineBeforeLining = true;
						} else {// 現時点でオーバーしているので、後続のものが同じラインに並ばないように処理する
							sortLayoutLineAfterLining = true;
						}
						
					}

					// table
					{
						if (child.parsedTag == (int)HtmlTag.THEAD) {// table head is single line.
							sortLayoutLineAfterLining = true;
						} else if (child.parsedTag == (int)HtmlTag.TR) {// table row.
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
						child.anchoredPosition = new Vector2(
							childHandlePoint.nextLeftHandle + child.padding.left, 
							childHandlePoint.nextTopHandle + child.padding.top
						);
						// Debug.LogError("child.rectTransform.anchoredPosition:" + child.vRectTransform.vAnchoredPosition);
				
						// set next handle.
						childHandlePoint.nextLeftHandle = childHandlePoint.nextLeftHandle + child.padding.left + child.sizeDelta.x + child.padding.right;
					}
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

			yield break;
		}


        /**
			create line of contents -> sort all content by Y base line.
		*/
		private HandlePoint SortByLayoutLine (List<ParsedTree> layoutLine, HandlePoint childHandlePoint) {
			// find tallest content in layoutLine.
			var targetHeightObjArray = layoutLine.OrderByDescending(c => c.sizeDelta.y + c.padding.PadHeight()).ToArray();
			
			if (0 < targetHeightObjArray.Length) {
				var tallestContent = targetHeightObjArray[0];
				
				// get tallest padded height. this will be this layoutLine's bottom line.
				var paddedHighestHeightInLine = tallestContent.sizeDelta.y + tallestContent.padding.PadHeight();
				
				// other child content will be moved.
				var skipFirst = true;
				foreach (var childInLine in targetHeightObjArray) {
					if (skipFirst) {
						skipFirst = false;
						continue;
					}
					
					var childPaddedHeight = childInLine.sizeDelta.y + childInLine.padding.PadHeight();
					var heightDiff = paddedHighestHeightInLine - childPaddedHeight;
					childInLine.anchoredPosition += new Vector2(0, heightDiff);
					
					// Debug.LogError("childInLine:" + childInLine.tag + " childInLine.rectTransform.anchoredPosition:" + childInLine.vRectTransform.vAnchoredPosition + " under tag:" + this.tag + " heightDiff:" + heightDiff);
				}

				// set next line head.
				childHandlePoint.nextLeftHandle = 0;

				childHandlePoint.nextTopHandle = tallestContent.anchoredPosition.y + tallestContent.sizeDelta.y + tallestContent.padding.PadHeight();
				
				// Debug.LogError("SortByLayoutLine handlePoint.nextTopHandle:" + handlePoint.nextTopHandle);
				// Debug.LogError("SortByLayoutLine rectTransform.anchoredPosition:" + rectTransform.anchoredPosition + " of tag:" + tag + " handlePoint:" + handlePoint.nextTopHandle);
			}

			
			return childHandlePoint;
		}


		public Vector2 PaddedRightBottomPoint (ParsedTree @this) {
			return @this.anchoredPosition + @this.sizeDelta + new Vector2(@this.padding.PadWidth(), @this.padding.PadHeight());
		}



        /*
            table functions.
         */

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

		private void CollectTableContentRowCountRecursively (ParsedTree @this, ParsedTree child, TableLayoutRecord tableLayoutRecord) {
			// count up table header count.
			if (child.parsedTag == (int)HtmlTag.TH) {
				tableLayoutRecord.IncrementRow();
			}

			foreach (var nestedChild in child.GetChildlen()) {
				CollectTableContentRowCountRecursively(child, nestedChild, tableLayoutRecord);
			}
		}

		private void CollectTableContentRowMaxWidthsRecursively (ParsedTree @this, ParsedTree child, TableLayoutRecord tableLayoutRecord) {
			var total = 0f;
			foreach (var nestedChild in child.GetChildlen()) {
				CollectTableContentRowMaxWidthsRecursively(child, nestedChild, tableLayoutRecord);
				if (child.parsedTag == (int)HtmlTag.TH || child.parsedTag == (int)HtmlTag.TD) {
					var nestedChildContentWidth = nestedChild.sizeDelta.x;
					total += nestedChildContentWidth;
				}
			}

			if (child.parsedTag == (int)HtmlTag.TH || child.parsedTag == (int)HtmlTag.TD) {
				tableLayoutRecord.UpdateMaxWidth(total);
			}
		}

		private void SetupTableContentPositionRecursively (ParsedTree @this, ParsedTree child, TableLayoutRecord tableLayoutRecord) {
			// overwrite parent content width of TH and TD.
			if (child.parsedTag == (int)HtmlTag.THEAD || child.parsedTag == (int)HtmlTag.TBODY || child.parsedTag == (int)HtmlTag.THEAD || child.parsedTag == (int)HtmlTag.TR) {
				var width = tableLayoutRecord.TotalWidth();
				child.sizeDelta = new Vector2(width, child.sizeDelta.y);
			}

			/*
				change TH, TD content's x position and width.
				x position -> 0, 1st row's longest content len, 2nd row's longest content len,...
				width -> 1st row's longest content len, 2nd row's longest content len,...
			*/
			if (child.parsedTag == (int)HtmlTag.TH || child.parsedTag == (int)HtmlTag.TD) {
				var offsetAndWidth = tableLayoutRecord.GetOffsetAndWidth();
				
				child.anchoredPosition = new Vector2(offsetAndWidth.offset, child.anchoredPosition.y);
				child.sizeDelta = new Vector2(offsetAndWidth.width, child.sizeDelta.y);
			}
			
			foreach (var nestedChild in child.GetChildlen()) {
				SetupTableContentPositionRecursively(child, nestedChild, tableLayoutRecord);	
			}
		}
    }
}