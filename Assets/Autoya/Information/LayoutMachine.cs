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
        private readonly ResourceLoader resLoader;

          public LayoutMachine (ResourceLoader resLoader) {
            this.resLoader = resLoader;
        }

        private enum InsertType {
            Continue,
            InsertContentToNextLine,
            RetryWithNextLine,
            HeadInsertedToTheEndOfLine,
            TailInsertedToLine,
            LastLineEndedInTheMiddleOfLine,
        };

        public IEnumerator Layout (TagTree rootTree, Vector2 view, Action<TagTree> layouted) {
            var viewCursor = ViewCursor.ZeroOffsetViewCursor(new ViewCursor(0,0,view.x,view.y));
            
            var cor = DoLayout(rootTree, viewCursor);
            
            while (cor.MoveNext()) {
                if (cor.Current != null) {
                    break;
                }
                yield return null;
            }

            var childViewCursor = cor.Current;
            
            // ビュー高さをセット
            rootTree.viewHeight = childViewCursor.viewHeight;
            
            layouted(rootTree);
        }

        /**
            コンテンツ単位でのレイアウトの起点。ここからtreeTypeごとのレイアウトを実行する。
         */
        private IEnumerator<ChildPos> DoLayout (TagTree tree, ViewCursor viewCursor, Func<InsertType, TagTree, ViewCursor> insertion=null) {
            // Debug.LogError("tree:" + Debug_GetTagStrAndType(tree) + " viewCursor:" + viewCursor);
            // Debug.LogWarning("まだ実装してない、brとかhrでの改行処理。 実際にはpとかも一緒で、「このコンテンツが終わったら改行する」みたいな属性が必須。区分けとしてはここではないか。＿なんちゃらシリーズと一緒に分けちゃうのもありかな〜");

            IEnumerator<ChildPos> cor = null;
            switch (tree.treeType) {
                case TreeType.CustomLayer: {
                    cor = DoLayerLayout(tree, viewCursor);
                    break;
                }
                case TreeType.CustomEmptyLayer: {
                    cor = DoEmptyLayerLayout(tree, viewCursor, insertion);
                    break;
                }
                case TreeType.Container: {
                    cor = DoContainerLayout(tree, viewCursor, insertion);
                    break;
                }
                case TreeType.Content_Img: {
                    cor = DoImgLayout(tree, viewCursor);
                    break;
                }
                case TreeType.Content_Text: {
                    cor = DoTextLayout(tree, viewCursor, insertion);
                    break;
                }
                case TreeType.Content_CRLF: {
                    cor = DoCRLFLayout(tree, viewCursor);
                    break;
                }
                default: {
                    throw new Exception("unexpected tree type:" + tree.treeType);
                }
            }

            /*
                もしもtreeがhiddenだった場合でも、のちのち表示するために内容のロードは行う。
                コンテンツへのサイズの指定も0で行う。
                ただし、同期的に読む必要はないため、並列でロードする。
             */
            if (tree.hidden) {
                var loadThenSetHiddenPosCor = SetHiddenPosCoroutine(tree, cor);
                resLoader.LoadParallel(loadThenSetHiddenPosCor);

                var hiddenCursor = ViewCursor.ZeroSizeCursor(viewCursor);
                // Debug.LogError("hidden tree:" + Debug_GetTagStrAndType(tree) + " viewCursor:" + viewCursor);

                yield return tree.SetPosFromViewCursor(hiddenCursor);
                throw new Exception("never come here.");
            } else {
                while (cor.MoveNext()) {
                    if (cor.Current != null) {
                        break;
                    }
                    yield return null;
                }
                
                // Debug.LogError("done layouted tree:" + Debug_GetTagStrAndType(tree) + " next cursor:" + cor.Current);
                yield return cor.Current;
            }
        }
        
        /**
            カスタムタグのレイヤーのレイアウトを行う。
            customTagLayer/box/boxContents(layerとか) という構造になっていて、boxはlayer内に必ず規定のポジションでレイアウトされる。
            ここだけ相対的なレイアウトが確実に崩れる。
         */
        private IEnumerator<ChildPos> DoLayerLayout (TagTree layerTree, ViewCursor parentBoxViewCursor) {
            ViewCursor basePos;
            
            if (!layerTree.keyValueStore.ContainsKey(HTMLAttribute._LAYER_PARENT_TYPE)) {
                // 親がboxではないレイヤーは、親のサイズを使わず、layer自体のprefabのサイズを使うという特例を当てる。
                var size = resLoader.GetUnboxedLayerSize(layerTree.tagValue);
                basePos = new ViewCursor(parentBoxViewCursor.offsetX, parentBoxViewCursor.offsetY, size.x, size.y);
            } else {
                // 親がboxなので、boxのoffsetYとサイズを継承。offsetXは常に0で来る。継承しない。
                basePos = new ViewCursor(0, parentBoxViewCursor.offsetY, parentBoxViewCursor.viewWidth, parentBoxViewCursor.viewHeight);
            }


            // collisionGroup単位での追加高さ、一番下まで伸びてるやつを基準にする。
            float additionalHeight = 0f;

            {
                var boxYPosRecords = new Dictionary<float, float>();
                var collisionGrouId = 0;
            
                var childViewCursor = basePos;
                /*
                    レイヤーなので、prefabをロードして、原点位置は0,0、
                        サイズは親サイズ、という形で生成する。
                    
                    ・childlenにboxの中身が含まれている場合(IsContainedThisCustomTag)、childlenの要素を生成する。そうでない要素の場合は生成しない。
                    ・この際のchildのサイズは、layerであれば必ずboxのサイズになる。このへんがキモかな。
                */
                var children = layerTree.GetChildren();

                for (var i = 0; i < children.Count; i++) {
                    var boxTree = children[i];

                    // Debug.LogError("box tag:" + resLoader.GetTagFromIndex(boxTree.parsedTag) + " boxTree:" + boxTree.treeType);

                    /*
                        位置情報はkvに入っているが、親のviewの値を使ってレイアウト後の位置に関する数値を出す。
                    */
                    var boxRect = boxTree.keyValueStore[HTMLAttribute._BOX] as BoxPos;
                    var childBoxViewRect = TagTree.GetChildViewRectFromParentRectTrans(basePos.viewWidth, basePos.viewHeight, boxRect);
                    
                    /*
                        collisionGroupによる区切りで、コンテンツ帯ごとの高さを出し、
                        最も下にあるコンテンツの伸び幅を次の縦並びグループの開始オフセット位置追加値としてセットする。
                    */
                    var boxCollisionGroupId = (int)boxTree.keyValueStore[HTMLAttribute._COLLISION];
                    
                    if (collisionGrouId != boxCollisionGroupId) {
                        var tallest = boxYPosRecords.Select(kv => kv.Key).Max();
                        additionalHeight = boxYPosRecords[tallest] + additionalHeight;
                        
                        // update. entried to new collision group.
                        collisionGrouId = boxCollisionGroupId;

                        boxYPosRecords.Clear();
                    }
                    
                    var childView = new ViewCursor(childBoxViewRect.x, childBoxViewRect.y + additionalHeight, childBoxViewRect.width, childBoxViewRect.height);
                    
                    var cor = LayoutBoxedContents(boxTree, childView);

                    while (cor.MoveNext()) {
                        if (cor.Current != null) {
                            break;
                        }
                        yield return null;
                    }
                    
                    // fix position.
                    var childCursor = cor.Current;

                    // add record.
                    var yPos = childCursor.offsetY + childCursor.viewHeight;
                    boxYPosRecords[yPos] = childCursor.viewHeight - childBoxViewRect.height;
                }

                // 最終グループの追加値をviewの高さに足す
                var tallestInGroup = boxYPosRecords.Keys.Max();
                additionalHeight = boxYPosRecords[tallestInGroup] + additionalHeight;    
            }

            // 基礎高さ + 増加分高さ
            var newHeight = basePos.viewHeight + additionalHeight;

            // Debug.LogWarning("after layerTree:" + GetTagStr(layerTree.tagValue) + " layerViewCursor:" + layerViewCursor);

            // treeに位置をセットしてposを返す
            yield return layerTree.SetPos(basePos.offsetX, basePos.offsetY, basePos.viewWidth, newHeight);
        }

        private IEnumerator<ChildPos> DoEmptyLayerLayout (TagTree emptyLayerTree, ViewCursor viewCursor, Func<InsertType, TagTree, ViewCursor> insertion=null) {
            var baseViewCursorHeight = viewCursor.viewHeight;

            var childView = ViewCursor.ZeroOffsetViewCursor(viewCursor);

            var cor = DoContainerLayout(emptyLayerTree, childView, (type, tree) => {throw new Exception("never called.");});
            
            while (cor.MoveNext()) {
                if (cor.Current != null) {
                    break;
                }
                yield return null;
            }
            
            var layoutedChildPos = cor.Current;
            
            /*
                レイアウト済みの高さがlayer本来の高さより低い場合、レイヤー本来の高さを使用する(隙間ができる)
             */
            if (layoutedChildPos.viewHeight < baseViewCursorHeight) {
                layoutedChildPos.viewHeight = baseViewCursorHeight;
            }
            // Debug.LogError("layoutedChildPos:" + layoutedChildPos + " vs baseViewCursorHeight:" + baseViewCursorHeight);

            // treeに位置をセットしてposを返す
            yield return emptyLayerTree.SetPos(layoutedChildPos);
        }

<<<<<<< HEAD
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
				// Debug.LogError("parent sizeDelta:" + @this.sizeDelta);

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
					handle.nextLeftHandle = @this.anchoredPosition.x + @this.sizeDelta.x + @this.padding.width;
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
				handle.nextTopHandle += @this.padding.top;// + @this.padding.bottom;
			}
			
			yield break;
		}
        
		
        private IEnumerator LayoutTagContent (ParsedTree @this, HandlePoint handle, Action<ParsedTree[]> insert) {
			var xOffset = handle.nextLeftHandle;
			var yOffset = handle.nextTopHandle;
			var viewWidth = handle.viewWidth;
			var viewHeight = handle.viewHeight;

			// create default rect transform.
			var prefabRectTrans = new RectTransform();
			// Debug.LogError("prefabRectTrans:" + prefabRectTrans + " viewName:" + viewName);
			
			var prefabLoadCor = infoResLoader.LoadPrefab(
				viewName, 
				@this, 
				prefab => {
					prefabRectTrans = prefab.GetComponent<RectTransform>();
				},
				() => {
					// do nothing on failed. add zero position.
				}
			);

			while (prefabLoadCor.MoveNext()) {
				yield return null;
			}
			
			// get prefab default position.
			var prefabRectTransAnchorX = prefabRectTrans.anchoredPosition.x;
			var prefabRectTransAnchorY = prefabRectTrans.anchoredPosition.y;
			// Debug.LogError("prefabRectTransAnchorX:" + prefabRectTransAnchorX);

			var anchorMin = prefabRectTrans.anchorMin;
			var anchorMax = prefabRectTrans.anchorMax;
			// Debug.LogError("anchorMin:" + anchorMin.x); 
			// Debug.LogError("anchorMax:" + anchorMax.x);

			
			var offsetMin = prefabRectTrans.offsetMin;
			var offsetMax = prefabRectTrans.offsetMax;
			// この値は、親のどこのポイントにくっつくか、という指定ポイントからの距離。マイナスとかつく。
			// Debug.LogError("offsetMinX:" + offsetMin.x);// 左下のアンカーの座標
			// Debug.LogError("offsetMaxX:" + offsetMax.x);// 右上のアンカーの座標
			// Debug.LogError("offsetMinY:" + offsetMin.y);// 左下のアンカーの座標
			// Debug.LogError("offsetMaxY:" + offsetMax.y);// 右上のアンカーの座標
			
			var pivot = prefabRectTrans.pivot;

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
					float imageWidth = 0;
					float imageHeight = 0;

					// determine image size from image's width & height.
					if (@this.keyValueStore.ContainsKey(Attribute.WIDTH) && @this.keyValueStore.ContainsKey(Attribute.HEIGHT)) {
						var width = @this.keyValueStore[Attribute.WIDTH];
						var height = @this.keyValueStore[Attribute.HEIGHT];

						if (width.EndsWith("%")) {
							imageWidth = GetPercentOf(viewWidth, width);
						} else {
							imageWidth = Convert.ToInt32(width);
						}

						if (height.EndsWith("%")) {
							imageHeight = GetPercentOf(viewHeight, height);
						} else {
							imageHeight = Convert.ToInt32(height);
						}

					} else if (@this.keyValueStore.ContainsKey(Attribute.WIDTH)) {
						// width only.
						var width = @this.keyValueStore[Attribute.WIDTH];
						if (width.EndsWith("%")) {
							imageWidth = GetPercentOf(viewWidth, width);
						} else {
							imageWidth = Convert.ToInt32(width);
						}

						// need to download image.
						// no height set yet. set height to default aspect ratio.
						var downloaded = false;
						infoResLoader.LoadImageAsync(
							src, 
							(sprite) => {
								downloaded = true;
								imageHeight = (imageWidth / sprite.rect.size.x) * sprite.rect.size.y;
							},
							() => {
								downloaded = true;
								imageHeight = 0;
							}
						);

						while (!downloaded) {
							yield return null;
						}

					} else if (@this.keyValueStore.ContainsKey(Attribute.HEIGHT)) {
						// height only.
						var height = @this.keyValueStore[Attribute.HEIGHT];
						if (height.EndsWith("%")) {
							imageHeight = GetPercentOf(viewHeight, height);
						} else {
							imageHeight = Convert.ToInt32(height);
						}

						// need to download image.
						// no width set yet. set height to default aspect ratio.
						var downloaded = false;
						infoResLoader.LoadImageAsync(
							src, 
							(sprite) => {
								downloaded = true;
								contentWidth = (imageHeight / sprite.rect.size.y) * sprite.rect.size.x;
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
								imageWidth = sprite.rect.size.x;
								imageHeight = sprite.rect.size.y;
							},
							() => {
								downloaded = true;
								imageWidth = 0;
								imageHeight = 0;
							}
						);

						while (!downloaded) {
							yield return null;
						}
					}

					// set content size.
					contentWidth = imageWidth;
					contentHeight = imageHeight;
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

			// 左上が中心点ってどういうこと -> HTML上は左上原点で、uGUI上は左下原点なので、ここでその差の吸収というか、歪みを受ける。
			// uGUIの中心点は左下なので、このへんが食い違う。y軸の向きが違う。
			
			// pivot値の上での位置原点を出す。この値が、このオブジェクトの原点がどこにあるか、を出すパラメータになる。
			// prefabのtransformはこの原点値をもとに値を保存しているため、原点値を踏まえた座標位置にするためには、原点位置をあらかじめ足した値にする必要がある。
			var pivottedX = pivot.x * contentWidth;// 0 ~ 1の範囲で、width上の点
			var pivottedY = (1-pivot.y) * contentHeight;// 0 ~ 1の範囲で、height上の点。HTMLは左上原点なので変換が必要。
			
			// Debug.LogError("pivottedX:" + pivottedX);
			// Debug.LogError("pivottedY:" + pivottedY);

			// prefab原点からはあらかじめpivot値が引かれているので、ここで値を足す。
			// このprefab原点からすでに値が引かれているのが混乱の原因なのか。この値はそのうち排斥すると良さそう。
			prefabRectTransAnchorX += pivottedX;
			prefabRectTransAnchorY += pivottedY;

			var pivottedPosX = xOffset + (prefabRectTransAnchorX - pivottedX);
			var pivottedPosY = yOffset - (prefabRectTransAnchorY - pivottedY);
			
			// set position.
			@this.anchoredPosition = new Vector2(
				pivottedPosX,
				pivottedPosY
			);
			// Debug.LogError("pivottedPosY:" + pivottedPosY + " yOffset:" + yOffset);

			// 仮で、anchorに0-1が入っているケースで、親の横幅を引き継ぐみたいなのをセットしてみる。
			// この際、該当するanchor軸のサイズは親+2とか親-4とかの相対サイズになり、代わりにpadding.widthなどにパラメータを格納する。
			// 実際には割合だと思うんだけど、まあ変な端数は特に使わないのでは?みたいな舐めきった気持ちで0-1限定にする。もしかしたらif外せるかも。
			var resultWidth = contentWidth;
			var resultHeight = contentHeight;

			// これを一般化するのに、LeftTopPivotView_ROOTが役に立ちそう。
			if (anchorMin.x == 0 && anchorMax.x == 1) {
				@this.padding.width = contentWidth + offsetMin.x - (offsetMax.x*2);//マジックナンバー x2が入ると辻褄が合う。なぜ。
				resultWidth = 0 - offsetMin.x + offsetMax.x;
				// x0-1の値として、親サイズ-2とかそういう数値が入ってくるの平気かって思う。単一の値なんだけど、表すものが状況によって変わる。
			}
			if (anchorMin.y == 0 && anchorMax.y == 1) {
				@this.padding.height = contentHeight + (offsetMin.y*2) - offsetMax.y;// yの場合はminにマジックナンバー x2が必要
				resultHeight = 0 - offsetMin.y + offsetMax.y;
			}

			// set content size.
			@this.sizeDelta = new Vector2(resultWidth, resultHeight);
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
						// Debug.LogWarning("ここでwidth0のコンテンツ出してるの、そのうち無くせそうな気がする");
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
						// Debug.LogError("child.anchoredPosition:" + child.anchoredPosition);
				
						// set next handle.
						childHandlePoint.nextLeftHandle = childHandlePoint.nextLeftHandle + child.sizeDelta.x + child.padding.PadWidth();
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
		}
=======
        private IEnumerator<ChildPos> DoImgLayout (TagTree imgTree, ViewCursor viewCursor) {
            var contentViewCursor = viewCursor;
            if (!imgTree.keyValueStore.ContainsKey(HTMLAttribute.SRC)) {
                throw new Exception("srcがないんだけどどうするか。カスタムコンテンツならデフォ画像をセットできるんでなくてもいいはず。あとエラーをつけるならパースエラー。image element should define src param.");
            }

            var src = imgTree.keyValueStore[HTMLAttribute.SRC] as string;

            var imageWidth = 0f;
            var imageHeight = 0f;

            /*
                デフォルトタグであれば画像サイズは未定(画像依存)なのでDLして判断する必要がある。
                そうでなくカスタムタグであれば、固定サイズで画像が扱えるため、prefabをロードしてサイズを固定して計算できる。
             */
            if (resLoader.IsDefaultTag(imgTree.tagValue)) {
                // default img tag. need to download image for determine size.

                var cor = resLoader.LoadImageAsync(src);

                while (cor.MoveNext()) {
                    if (cor.Current != null) {
                        break;
                    }
                    yield return null;
                }

                if (cor.Current != null) {
                    var sprite = cor.Current;
                    imageWidth = sprite.rect.size.x;
                    
                    if (viewCursor.viewWidth < imageWidth) {
                        imageWidth = viewCursor.viewWidth;
                    }
                    
                    imageHeight = (imageWidth / sprite.rect.size.x) * sprite.rect.size.y;
                } else {
                    imageHeight = 0;
                }
            } else {
                // customtag, requires prefab.
                var cor = resLoader.LoadPrefab(imgTree.tagValue, TreeType.Content_Img);

                while (cor.MoveNext()) {
                    if (cor.Current != null) {
                        break;
                    }
                    yield return null;
                }

                var prefab = cor.Current;
                var rect = prefab.GetComponent<RectTransform>();
                imageWidth = rect.sizeDelta.x;
                imageHeight = rect.sizeDelta.y;
            }

            // 画像のアスペクト比に則ったサイズを返す。
            // treeに位置をセットしてposを返す
            yield return imgTree.SetPos(contentViewCursor.offsetX, contentViewCursor.offsetY, imageWidth, imageHeight);
        }

        private IEnumerator<ChildPos> DoContainerLayout (TagTree containerTree, ViewCursor containerViewCursor, Func<InsertType, TagTree, ViewCursor> insertion=null) {
            /*
                子供のタグを整列させる処理。
                横に整列、縦に並ぶ、などが実行される。

                親カーソルから子カーソルを生成。高さに関しては適当。
            */
            var containerChildren = containerTree.GetChildren();
            var childCount = containerChildren.Count;

            if (childCount == 0) {
                // treeに位置をセットしてposを返す
                yield return containerTree.SetPosFromViewCursor(containerViewCursor);
                throw new Exception("never come here.");
            }

            var linedElements = new List<TagTree>();
            var mostRightPoint = 0f;
            var mostBottomPoint = 0f;
            {
                var nextChildViewCursor = new ViewCursor(0, 0, containerViewCursor.viewWidth, containerViewCursor.viewHeight);
                for (var i = 0; i < childCount; i++) {

                    var child = containerChildren[i];
                    if (child.treeType == TreeType.Content_Text) {
                        child.keyValueStore[HTMLAttribute._ONLAYOUT_PRESET_X] = containerViewCursor.offsetX;
                    }

                    currentLineRetry: {
                        linedElements.Add(child);

                        // set insertion type.
                        var currentInsertType = InsertType.Continue;
                        // 子供ごとにレイアウトし、結果を受け取る
                        var cor = DoLayout(
                            child, 
                            nextChildViewCursor, 
                            /*
                                このブロックは<このコンテナ発のinsertion発動地点>か、
                                このコンテナの内部のコンテナから呼ばれる。
                             */
                            (insertType, insertingChild) => {
                                currentInsertType = insertType;

                                switch (insertType) {
                                    case InsertType.InsertContentToNextLine: {
                                        /*
                                            現在のコンテンツを分割し、後続の列へと分割後の後部コンテンツを差し込む。
                                            あたまが生成され、後続部分が存在し、それらが改行後のコンテンツとして分割、ここに挿入される。
                                        */
                                        containerChildren.Insert(i+1, insertingChild);
                                        childCount++;
                                        break;
                                    }
                                    case InsertType.HeadInsertedToTheEndOfLine: {
                                        // Debug.LogError("received:" + Debug_GetTagStrAndType(containerTree) + " inserting:" + Debug_GetTagStrAndType(insertingChild) + " text:" + insertingChild.keyValueStore[HTMLAttribute._CONTENT]);
                                        if (0 < nextChildViewCursor.offsetX) {
                                            // 行中開始の子コンテナ内での改行イベントを受け取った
                                            
                                            // 子コンテナ自体は除外
                                            linedElements.Remove(child);

                                            var childContainer = child;
                                            
                                            // 現在整列してるコンテンツの整列を行う。
                                            linedElements.Add(insertingChild);

                                            // ライン化処理
                                            DoLining(linedElements);
                                            
                                            // 消化
                                            linedElements.Clear();

                                            /*
                                                子供コンテナの基礎viewを、行頭からのものに更新する。
                                             */
                                            return new ViewCursor(0, nextChildViewCursor.offsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);
                                        }
                                        break;
                                    }
                                }

                                // 特に何もないのでemptyを返す
                                return ViewCursor.Empty;
                            }
                        );
                        
                        while (cor.MoveNext()) {
                            if (cor.Current != null) {
                                break;
                            }		
                            yield return null;
                        }

                        // update most right point.
                        if (mostRightPoint < child.offsetX + child.viewWidth) {
                            mostRightPoint = child.offsetX + child.viewWidth;
                        }

                        // update most bottom point.
                        if (mostBottomPoint < child.offsetY + child.viewHeight) {
                            mostBottomPoint = child.offsetY + child.viewHeight;
                        }

                        /*
                            <このコンテナ発のinsertion発動地点>
                            この時点でinsertionは発生済み or No で、発生している場合、そのタイプによって上位へと伝搬するイベントが変わる。
                         */
                        
                        switch (currentInsertType) {
                            case InsertType.RetryWithNextLine: {
                                // Debug.LogError("テキストコンテンツが0行を叩き出したので、このコンテンツ自体をもう一度レイアウトする。");
                                
                                // 処理の開始時にラインにいれていたものを削除
                                linedElements.Remove(child);

                                // 含まれているものの整列処理をし、列の高さを受け取る
                                var newLineOffsetY = DoLining(linedElements);

                                // 整列と高さ取得が完了したのでリセット
                                linedElements.Clear();

                                // ここまでの行の高さがcurrentHeightに出ているので、currentHeightから次の行を開始する。
                                // Debug.LogError("リトライでの改行");
                                nextChildViewCursor = ViewCursor.NextLine(newLineOffsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);

                                // もう一度この行を処理する。
                                goto currentLineRetry;
                            }
                            case InsertType.InsertContentToNextLine: {
                                /*
                                    子側、InsertContentToNextLineを発行してそれを親まで伝達するか、そのままにするか判定する。
                                 */
                                if (0 < containerViewCursor.offsetX && insertion != null) {
                                    /*
                                        親がコンテナで、かつ、現在レイアウト中のこのコンテナで、行の途中から始まっていたコンテンツが幅を使い果たして
                                        自身のコンテナに対して改行(コンテンツの分割と挿入)を行なった。

                                        このコンテナからさらに親のコンテナに対して、折り返しが発生した要素を送りつける。

                                        親コンテナ側でさらにこのコンテナが行途中から開始したコンテナかどうかを判定、
                                        もし行途中から発生したコンテナであれば、その要素の中で送りつけられたtreeの要素をLiningに掛け、
                                        そのy位置を調整する。

                                        このイベント発生後の次の行以降のコンテンツは、そのy位置調整を経て調整される。
                                     */

                                    // Debug.LogError("insertion発生、現在の子コンテナ:" + Debug_GetTagStrAndType(containerTree));
                                    var newView = insertion(InsertType.HeadInsertedToTheEndOfLine, child);

                                    /*
                                        親コンテナからみて条件を満たしていれば、このコンテナに新たなviewが与えられる。
                                        条件は、このコンテナが、親から見て行途中に開始されたコンテナだったかどうか。
                                     */
                                    
                                    if (!newView.Equals(ViewCursor.Empty)) {
                                        // Debug.LogError("viewが変更されてるので、コンテナ自体のviewが変更される。で、それに伴ってinsertしたコンテンツのx位置をズラさないといけない。 newView:" + newView);

                                        // 子のコンテンツのxOffsetを、コンテナのoffsetXが0になった際に相対的に移動しない、という前提でズラす。
                                        child.offsetX = containerViewCursor.offsetX;
                                        
                                        // update most right point again.
                                        if (mostRightPoint < child.offsetX + child.viewWidth) {
                                            mostRightPoint = child.offsetX + child.viewWidth;
                                        }

                                        // 改行が予定されているのでライン化を解除
                                        linedElements.Clear();

                                        // ビュー自体を更新
                                        containerViewCursor = newView;

                                        // 次の行のカーソルをセット
                                        nextChildViewCursor = ViewCursor.NextLine(
                                            child.offsetY + child.viewHeight, 
                                            containerViewCursor.viewWidth, 
                                            containerViewCursor.viewHeight
                                        );
                                        continue;
                                    }
                                }
                                
                                /*
                                    これ以降のコンテンツは次行になるため、現在の行についてLining処理を行う。
                                 */ 
                                var newLineOffsetY = DoLining(linedElements);

                                // 整列と高さ取得が完了したのでリセット
                                linedElements.Clear();

                                // ここまでの行の高さがcurrentHeightに出ているので、currentHeightから次の行を開始する。
                                nextChildViewCursor = ViewCursor.NextLine(newLineOffsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);
                                // Debug.LogError("child:" + child.tagValue + " done," + child.ShowContent() + " next childView:" + childView);
                                continue;
                            }
                            case InsertType.TailInsertedToLine: {
                                if (1 < containerChildren.Count && i == containerChildren.Count - 1 && insertion != null) {
                                    insertion(InsertType.LastLineEndedInTheMiddleOfLine, child);
                                }
                                break;
                            }
                            case InsertType.LastLineEndedInTheMiddleOfLine: {
                                /*
                                    ここで送られて来た子を、ラインへと加える必要がある。うわーー未来にレイアウトが完成する感じだ、そのまま足してると死ぬな〜〜。ハンドル足す形にするか。
                                    最終行のコンテンツ高さをどうするかな〜〜liningRefみたいなのを作って持っとかないといけないの面倒くさいな〜〜、、幅さえあってれば文句ないみたいなのをまずやってみるか。
                                    ここで、子のコンテナがこのコンテンツを最後にレイアウトを終えているので、カーソルが弄れる。
                                 */

                                // イベント発行元である子コンテナ自身はLiningから除外
                                linedElements.Remove(child);

                                // このへんで、child = childContainerに含まれる末尾要素のコピーを作り出してlineにいれておいて、
                                // lining処理が終わった後でchildそれ自体に反映、みたいなのをやれるといいな〜と思うが利益が少なすぎて泣ける。
                                // だいたい見た目的に変になるの確定してるし。避けるっしょみたいな。

                                var childContainer = child;
                                var containersLastChild = childContainer.GetChildren().Last();

                                // コンテナ内の最後のコンテンツの右から次のコンテンツが出るように、オフセットをセット。
                                nextChildViewCursor = new ViewCursor(
                                    containersLastChild.viewWidth, 
                                    (childContainer.offsetY + childContainer.viewHeight) - containersLastChild.viewHeight, 
                                    containerViewCursor.viewWidth - containersLastChild.viewWidth,
                                    containerViewCursor.viewHeight
                                );
                                continue;
                            }
                        }

                        /*
                            コンテンツがwidth内に置けた(少なくとも起点はwidth内にある)
                        */

                        // hiddenコンテンツ以下が来る場合は想定されてないのが惜しいな、なんかないかな、、ないか、、デバッグ用。crlf以外でheightが0になるコンテンツがあれば、それは異常なので蹴る
                        // if (!child.hidden && child.treeType != TreeType.Content_CRLF && cor.Current.viewHeight == 0) {
                        //     throw new Exception("content height is 0. tag:" + GetTagStr(child.tagValue) + " treeType:" + child.treeType);
                        // }

                        // 子供の設置位置を取得
                        var layoutedPos = cor.Current;

                        // 次のコンテンツの開始位置をセットする。
                        var nextPos = ChildPos.NextRightCursor(layoutedPos, containerViewCursor.viewWidth);
                        
                        // レイアウト直後に次のポイントの開始位置が規定幅を超えているか、改行要素が来た場合、現行の行のライニングを行う。
                        if (child.treeType == TreeType.Content_CRLF) {
                            // 行化
                            var nextLineOffsetY = DoLining(linedElements);

                            // ライン解消
                            linedElements.Clear();
                            // Debug.LogError("crlf over.");

                            // 改行処理を加えた次の開始位置
                            nextChildViewCursor = ViewCursor.NextLine(nextLineOffsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);
                        } else if (containerViewCursor.viewWidth <= nextPos.offsetX) {
                            // 行化
                            var nextLineOffsetY = DoLining(linedElements);

                            // ライン解消
                            linedElements.Clear();
                            // Debug.LogError("over. child:" + GetTagStr(child.tagValue) + " vs containerViewCursor.viewWidth:" + containerViewCursor.viewWidth + " vs nextChildViewCursor.offsetX:" + nextChildViewCursor.offsetX);

                            // 改行処理を加えた次の開始位置
                            nextChildViewCursor = ViewCursor.NextLine(nextLineOffsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);
                        } else {
                            // 次のchildの開始ポイントを現在のchildの右にセット
                            nextChildViewCursor = new ViewCursor(nextPos);
                        }

                        // Debug.LogError("child:" + GetTagStr(child.tagValue) + " is done," + " next childView:" + childView);
                    }

                    // 現在の子供のレイアウトが終わっていて、なおかつライン処理、改行が済んでいる。
                }
            }

            
            // 最後の列が存在する場合、整列。(最後の要素が改行要因とかだと最後の列が存在しない場合がある)
            if (linedElements.Any()) {
                DoLining(linedElements);
            }
            
            // Debug.LogError("mostBottomPoint:" + mostBottomPoint + " tag:" + Debug_GetTagStrAndType(containerTree));

            // 自分自身のサイズを規定
            yield return containerTree.SetPos(containerViewCursor.offsetX, containerViewCursor.offsetY, mostRightPoint, mostBottomPoint);
        }

        /**
            テキストコンテンツのレイアウトを行う。
            もしテキストが複数行に渡る場合、最終行だけを新規コンテンツとして上位に返す。
         */
        private IEnumerator<ChildPos> DoTextLayout (TagTree textTree, ViewCursor textViewCursor, Func<InsertType, TagTree, ViewCursor> insertion=null) {
            if (textViewCursor.viewWidth < 0) {
                throw new Exception("DoTextLayout cannot use negative width. textViewCursor:" + textViewCursor);
            }

            var text = textTree.keyValueStore[HTMLAttribute._CONTENT] as string;
            
            var cor = resLoader.LoadPrefab(textTree.tagValue, textTree.treeType);

            while (cor.MoveNext()) {
                if (cor.Current != null) {
                    break;
                }
                yield return null;
            }
            
            var prefab = cor.Current;
            
            // use prefab's text component for using it's text setting.
            var textComponent = prefab.GetComponent<Text>();
            if (textComponent == null) {
                throw new Exception("failed to get Text component from prefab:" + prefab.name + " of text content:" + text);
            }

            if (textComponent.font == null) {
                throw new Exception("font is null. prefab:" + prefab.name);
            }
            
            // set content to prefab.
            
            var generator = new TextGenerator();
            
            textComponent.text = text;
            var setting = textComponent.GetGenerationSettings(new Vector2(textViewCursor.viewWidth, float.PositiveInfinity));
            generator.Populate(text, setting);

            using (new TextComponentUsing(textComponent, generator)) {
                // この時点で、複数行に分かれるんだけど、最後の行のみ分離する必要がある。
                var lineCount = generator.lineCount;
                // Debug.LogError("lineCount:" + lineCount);
                // Debug.LogError("default preferred width:" + textComponent.preferredWidth);
                
                // 0行だったら、入らなかったということなので、改行をしてもらってリトライを行う。
                if (lineCount == 0 && !string.IsNullOrEmpty(textComponent.text)) {
                    insertion(InsertType.RetryWithNextLine, null);
                    yield break;
                }

                // 1行以上のラインがある。

                /*
                    ここで、このtreeに対するカーソルのoffsetXが0ではない場合、行の中間から行を書き出していることになる。

                    また上記に加え、親コンテナ自体のoffsetXが0ではない場合も、やはり、行の中間から行を書き出していることになる。
                    判定のために、親コンテナからtextTreeへ、親コンテナのoffsetX = 書き始め位置の書き込みをする。

                    行が2行以上ある場合、1行目は右端まで到達しているのが確定する。
                    2行目以降はoffsetX=0の状態で書かれる必要がある。

                    コンテンツを分離し、それを叶える。
                */
                var onLayoutPresetX = (float)textTree.keyValueStore[HTMLAttribute._ONLAYOUT_PRESET_X];
                var isStartAtZeroOffset = onLayoutPresetX == 0 && textViewCursor.offsetX == 0;
                var isMultilined = 1 < lineCount;

                // 複数行存在するんだけど、2行目のスタートが0文字目の場合、1行目に1文字も入っていない。コンテンツ全体を次の行で開始させる。
                if (isMultilined && generator.lines[1].startCharIdx == 0) {
                    insertion(InsertType.RetryWithNextLine, null);
                    yield break;
                }

                if (isStartAtZeroOffset) {
                    if (isMultilined) {
                        // Debug.LogError("行頭での折り返しのある複数行 text:" + text);

                        // 複数行が頭から出ている状態で、改行を含んでいる。最終行が中途半端なところにあるのが確定しているので、切り離して別コンテンツとして処理する必要がある。
                        var bodyContent = text.Substring(0, generator.lines[generator.lineCount-1].startCharIdx);
                        
                        // 内容の反映
                        textTree.keyValueStore[HTMLAttribute._CONTENT] = bodyContent;
                        
                        // 最終行
                        var lastLineContent = text.Substring(generator.lines[generator.lineCount-1].startCharIdx);

                        // 最終行を分割して送り出す。追加されたコンテンツを改行後に処理する。
                        var nextLineContent = new InsertedTree(textTree, lastLineContent, textTree.tagValue);
                        insertion(InsertType.InsertContentToNextLine, nextLineContent);

                        // 最終行以外はハコ型に収まった状態なので、ハコとして出力する。
                        // 最終一つ前までの高さを出して、
                        var totalHeight = 0;
                        for (var i = 0; i < generator.lineCount-1; i++) {
                            var line = generator.lines[i];
                            // Debug.LogWarning("ここに+1がないと実質的な表示用高さが足りなくなるケースがあって、すごく怪しい。");
                            totalHeight += (int)(line.height * textComponent.lineSpacing);
                        }
                        
                        // このビューのポジションをセット
                        yield return textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, textViewCursor.viewWidth, totalHeight);
                    } else {
                        // Debug.LogError("行頭の単一行 text:" + text);
                        var width = textComponent.preferredWidth;
                        var height = generator.lines[0].height * textComponent.lineSpacing;
                        
                        // 最終行かどうかの判断はここでできないので、単一行の入力が終わったことを親コンテナへと通知する。
                        insertion(InsertType.TailInsertedToLine, textTree);
                        
                        // Debug.LogError("行頭の単一行 text:" + text + " textViewCursor:" + textViewCursor);

                        // Debug.LogError("行頭の単一行 newViewCursor:" + newViewCursor);
                        yield return textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, textComponent.preferredWidth, height);
                    }
                } else {
                    if (isMultilined) {
                        // Debug.LogError("行中追加での折り返しのある複数行 text:" + text);
                        var currentLineHeight = generator.lines[0].height * textComponent.lineSpacing;

                        // 複数行が途中から出ている状態で、まず折り返しているところまでを分離して、後続の文章を新規にstringとしてinsertする。
                        var currentLineContent = text.Substring(0, generator.lines[1].startCharIdx);
                        textTree.keyValueStore[HTMLAttribute._CONTENT] = currentLineContent;

                        // get preferredWidht of text from trimmed line.
                        textComponent.text = currentLineContent;

                        var currentLineWidth = textComponent.preferredWidth;

                        var restContent = text.Substring(generator.lines[1].startCharIdx);
                        var nextLineContent = new InsertedTree(textTree, restContent, textTree.tagValue);

                        // 次のコンテンツを新しい行から開始する。
                        insertion(InsertType.InsertContentToNextLine, nextLineContent);

                        // Debug.LogError("newViewCursor:" + newViewCursor);
                        yield return textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, currentLineWidth, currentLineHeight);
                    } else {
                        // Debug.LogError("行中追加の単一行 text:" + text);
                        var width = textComponent.preferredWidth;
                        var height = generator.lines[0].height * textComponent.lineSpacing;
                        
                        // Debug.LogError("行中の単一行 text:" + text + " textViewCursor:" + textViewCursor);
                        // 最終行かどうかの判断はここでできないので、単一行の入力が終わったことを親コンテナへと通知する。
                        insertion(InsertType.TailInsertedToLine, textTree);
                        
                        // Debug.LogError("newViewCursor:" + newViewCursor);
                        yield return textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, textComponent.preferredWidth, height);
                    }
                }
            }
        }

        private IEnumerator<ChildPos> DoCRLFLayout (TagTree crlfTree, ViewCursor viewCursor) {
            // return empty size cursor.
            var zeroSizeCursor = ViewCursor.ZeroSizeCursor(viewCursor);
>>>>>>> dev_information

            // treeに位置をセットしてposを返す
            yield return crlfTree.SetPosFromViewCursor(zeroSizeCursor);
        }

        private string Debug_GetTagStrAndType (TagTree tree) {
            return resLoader.GetTagFromValue(tree.tagValue) + "_" + tree.treeType;
        }

        private IEnumerator SetHiddenPosCoroutine (TagTree hiddenTree, IEnumerator<ChildPos> cor) {
            while (cor.MoveNext()) {
                if (cor.Current != null) {
                    break;
                }
                yield return null;
            }

            hiddenTree.SetHidePos();
        }
        
        /**
<<<<<<< HEAD
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
			// Debug.LogError("PaddedRightBottomPoint @this.sizeDelta:" + @this.sizeDelta);

			var rightBottom = @this.anchoredPosition + @this.sizeDelta + new Vector2(@this.padding.PadWidth(), @this.padding.PadHeight());
			// Debug.LogError("rightBottom:" + rightBottom);
			return rightBottom;
		}
=======
            linedChildrenの中で一番高度のあるコンテンツをもとに、他のコンテンツを下揃いに整列させ、次の行の開始Yを返す。
            整列が終わったら、それぞれのコンテンツのオフセットをいじる。サイズは変化しない。
        */
        private float DoLining (List<TagTree> linedChildren) {
            var nextOffsetY = 0f;
            var tallestOffsetY = 0f;
            var tallestHeightPoint = 0f;

            for (var i = 0; i < linedChildren.Count; i++) {
                var child = linedChildren[i];

                /*
                    下端が一番下にあるコンテンツの値を取り出す
                 */
                if (tallestHeightPoint < child.offsetY + child.viewHeight) {
                    tallestOffsetY = child.offsetY;
                    tallestHeightPoint = child.offsetY + child.viewHeight;
                    nextOffsetY = tallestHeightPoint;
                }
            }

            // Debug.LogError("tallestHeightPoint:" + tallestHeightPoint);
            // tallestHeightを最大高さとして、各コンテンツのoffsetYを、この高さのコンテンツに下揃えになるように調整する。
            for (var i = 0; i < linedChildren.Count; i++) {
                var child = linedChildren[i];
                var diff = (tallestHeightPoint - tallestOffsetY) - child.viewHeight;
                
                child.offsetY = child.offsetY + diff;
            }
            
            // Debug.LogError("lining nextOffsetY:" + nextOffsetY);
            return nextOffsetY;
        }
>>>>>>> dev_information

        /**
            ボックス内部のコンテンツのレイアウトを行う
         */
        private IEnumerator<ChildPos> LayoutBoxedContents (TagTree boxTree, ViewCursor boxView) {
            // Debug.LogError("boxTree:" + GetTagStr(boxTree.tagValue) + " boxView:" + boxView);
            
            var containerChildren = boxTree.GetChildren();
            var childCount = containerChildren.Count;

            if (childCount == 0) {
                // treeに位置をセットしてposを返す
                yield return boxTree.SetPosFromViewCursor(boxView);
                throw new Exception("never come here.");
            }

            // 内包されたviewCursorを生成する。
            var childView = ViewCursor.ZeroOffsetViewCursor(boxView);

            for (var i = 0; i < childCount; i++) {
                var child = containerChildren[i];
                if (child.treeType == TreeType.Content_Text) {
                    child.keyValueStore[HTMLAttribute._ONLAYOUT_PRESET_X] = boxTree.offsetX;
                }
                
                // 子供ごとにレイアウトし、結果を受け取る
                var cor = DoLayout(
                    child, 
                    childView, 
                    (insertType, newChild) => {
                        throw new Exception("never come here.");
                    }
                );

                while (cor.MoveNext()) {
                    if (cor.Current != null) {
                        break;
                    }
                    yield return null;
                }

                /*
                    コンテンツがwidth内に置けた(ギリギリを含む)
                */

                // レイアウトが済んだchildの位置を受け取り、改行
                // Debug.LogError("layoutbox 改行");
                childView = ViewCursor.NextLine(cor.Current.offsetY + cor.Current.viewHeight, boxView.viewWidth, boxView.viewHeight);
                
                // 現在の子供のレイアウトが終わっていて、なおかつライン処理、改行が済んでいる。
            }
            
            // Debug.Log("lastChildEndY:" + lastChildEndY + " これが更新されない場合、レイアウトされたパーツにサイズが入ってない。");

            // 最終コンテンツのoffsetを使ってboxの高さをセット
            // treeに位置をセットしてposを返す
            yield return boxTree.SetPos(boxView.offsetX, boxView.offsetY, boxView.viewWidth, childView.offsetY);
        }

        
        private class TextComponentUsing : IDisposable {
            private Text textComponent;
            private TextGenerator gen;
            public TextComponentUsing (Text textComponent, TextGenerator gen) {
                this.textComponent = textComponent;
                this.gen = gen;
            }

            private bool disposedValue = false;

            protected virtual void Dispose (bool disposing) {
                if (!disposedValue) {
                    if (disposing) {
                        // dispose.
                        textComponent.text = string.Empty;
                        gen.Invalidate();
                    }
                    disposedValue = true;
                }
            }

            void IDisposable.Dispose () {
                Dispose(true);
            }
        }

        /*
            table functions.
         */

        // private class TableLayoutRecord {
        // 	private int rowIndex;
        // 	private List<float> xWidth = new List<float>();

        // 	public void IncrementRow () {
        // 		xWidth.Add(0);
        // 	}
            
        // 	public void UpdateMaxWidth (float width) {
        // 		if (xWidth[rowIndex] < width) {
        // 			xWidth[rowIndex] = width;
        // 		}
        // 		rowIndex = (rowIndex + 1) % xWidth.Count;
        // 	}
        // 	public float TotalWidth () {
        // 		var ret = 0f;
        // 		foreach (var width in xWidth) {
        // 			ret += width;
        // 		}
        // 		return ret;
        // 	}
            
        // 	public OffsetAndWidth GetOffsetAndWidth () {
        // 		var currentIndex = rowIndex % xWidth.Count;
        // 		var offset = 0f;
        // 		for (var i = 0; i < currentIndex; i++) {
        // 			offset += xWidth[i];
        // 		}
        // 		var width = xWidth[rowIndex % xWidth.Count];

        // 		rowIndex++;

        // 		return new OffsetAndWidth(offset, width);
        // 	}

        // 	public struct OffsetAndWidth {
        // 		public float offset;
        // 		public float width;
        // 		public OffsetAndWidth (float offset, float width) {
        // 			this.offset = offset;
        // 			this.width = width;
        // 		}
        // 	}
        // }

        // private void CollectTableContentRowCountRecursively (ParsedTree @this, ParsedTree child, TableLayoutRecord tableLayoutRecord) {
        // 	// count up table header count.
        // 	if (child.parsedTag == (int)HtmlTag.th) {
        // 		tableLayoutRecord.IncrementRow();
        // 	}

        // 	foreach (var nestedChild in child.GetChildren()) {
        // 		CollectTableContentRowCountRecursively(child, nestedChild, tableLayoutRecord);
        // 	}
        // }

        // private void CollectTableContentRowMaxWidthsRecursively (ParsedTree @this, ParsedTree child, TableLayoutRecord tableLayoutRecord) {
        // 	var total = 0f;
        // 	foreach (var nestedChild in child.GetChildren()) {
        // 		CollectTableContentRowMaxWidthsRecursively(child, nestedChild, tableLayoutRecord);
        // 		if (child.parsedTag == (int)HtmlTag.th || child.parsedTag == (int)HtmlTag.td) {
        // 			var nestedChildContentWidth = nestedChild.sizeDelta.x;
        // 			total += nestedChildContentWidth;
        // 		}
        // 	}

        // 	if (child.parsedTag == (int)HtmlTag.th || child.parsedTag == (int)HtmlTag.td) {
        // 		tableLayoutRecord.UpdateMaxWidth(total);
        // 	}
        // }

        // private void SetupTableContentPositionRecursively (ParsedTree @this, ParsedTree child, TableLayoutRecord tableLayoutRecord) {
        // 	// overwrite parent content width of TH and TD.
        // 	if (child.parsedTag == (int)HtmlTag.thead || child.parsedTag == (int)HtmlTag.tbody || child.parsedTag == (int)HtmlTag.thead || child.parsedTag == (int)HtmlTag.tr) {
        // 		var width = tableLayoutRecord.TotalWidth();
        // 		child.sizeDelta = new Vector2(width, child.sizeDelta.y);
        // 	}

        // 	/*
        // 		change TH, TD content's x position and width.
        // 		x position -> 0, 1st row's longest content len, 2nd row's longest content len,...
        // 		width -> 1st row's longest content len, 2nd row's longest content len,...
        // 	*/
        // 	if (child.parsedTag == (int)HtmlTag.th || child.parsedTag == (int)HtmlTag.td) {
        // 		var offsetAndWidth = tableLayoutRecord.GetOffsetAndWidth();
                
        // 		child.anchoredPosition = new Vector2(offsetAndWidth.offset, child.anchoredPosition.y);
        // 		child.sizeDelta = new Vector2(offsetAndWidth.width, child.sizeDelta.y);
        // 	}
            
        // 	foreach (var nestedChild in child.GetChildren()) {
        // 		SetupTableContentPositionRecursively(child, nestedChild, tableLayoutRecord);	
        // 	}
        // }
    }
}