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
		
		private readonly ViewBox view;

      	public LayoutMachine (
			  ParsedTree @this, 
			  InformationResourceLoader infoResLoader,
			  ViewBox view, Action<IEnumerator> executor, 
			  Action<ParsedTree> layouted
		) {
			this.infoResLoader = infoResLoader;
			this.view = view;
			
			// start execute.
			executor(StartLayout2(@this, layouted));
        }

		private IEnumerator StartLayout2 (ParsedTree @this, Action<ParsedTree> layouted) {
			/*
				えーっと、やらなければいけないこと全部盛りだと、

				・上からサイズを設定する
				・サイズを規定できる場合は規定する
				・子供にサイズを渡す
				・transformとかのパラメータを保持する
				とかか。
				で、
				カスタムタグは原点は左上0で、ボックスの配置を行う必要はあるんでサイズは固定で、っていう感じになるか。box自体の配置は親に対する関係と同一でないといけない。これ守れてる気がしないな。
				テスト描こう。
			
			 */
			var viewCursor = new ViewCursor(0, 0, view.width, view.height);
			/*
			        public readonly int parsedTag;
					public readonly string rawTagName;
					public readonly string prefabName;
					public readonly AttributeKVs keyValueStore;
					public readonly bool isContainer;
					を使って、
			 */

			/*
					public Vector2 anchoredPosition = Vector2.zero;        
					public Vector2 sizeDelta = Vector2.zero;
					public Vector2 offsetMin = Vector2.zero;
					public Vector2 offsetMax = Vector2.zero;
					public Padding padding = new Padding();
					を埋めていく。
			 */
			var cor = DoLayout(@this, viewCursor);
			while (cor.MoveNext()) {
				yield return null;
			}

			layouted(@this);

			yield break;
		}

		private IEnumerator<ViewCursor> DoLayout (ParsedTree @this, ViewCursor viewCursor) {

			// カスタムタグかどうかで分岐
			if (infoResLoader.IsCustomTag(@this.parsedTag)) {
				// 自分自身のサイズを確定
				/*
					原点もアンカーも固定されていて、親サイズに対してそのまま出せばいい。
				*/
				@this.viewWidth = viewCursor.viewWidth;

				// ここにくるのは、カスタムタグ かつ　レイヤー。なので、子供はすべてbox。

				var path = "Views/" + infoResLoader.DepthAssetList().viewName + "/" + @this.rawTagName;

				Debug.LogError("path:" + path);
				var layerPrefab = Resources.Load<GameObject>(path);
				/*
					カスタムタグだったら、prefabをロードして、原点位置は0,0、
						サイズは親サイズ、という形で生成する。
					
					・childlenにboxの中身が含まれている場合(IsContainedThisCustomTag)、childlenの要素を生成する。そうでない要素の場合は生成しない。
					・この際のchildのサイズは、必ずboxのものになる。このへんがキモかな。
				 */

				// 仮でシンクロ読み
				var customTagPrefab = infoResLoader.LoadPrefabSync(path);
				Debug.LogError("customTagPrefab:" + customTagPrefab);

				var children = @this.GetChildren();
				
				foreach (var boxTree in children) {
					var cor = LayoutBox(viewCursor, boxTree);

					while (cor.MoveNext()) {
						yield return null;
					}
					var resultCor = cor.Current;
					Debug.LogWarning("カスタムタグのレイアウトが終わった。で、親のサイズが変わる(高さが高くなった)可能性がある。");
				}
			} else {
				// ここにくるのはこれコンテンツかコンテナだ。コンテンツのみがくると楽なのだが、コンテンツ 含有 コンテナ なので、
				// 自動的にコンテナがくることがあり得る。まあしょうがない。
				// で。その分解はparserで済んでると思う。
				// customTagの機構はタグの機構の完全上位みたいになってるような気がする。まあフローが違うからそうか。

				/*
					このタグはカスタムタグではない => デフォルトタグなので、resourcesから引いてくるか。一応。
				 */
				var path = "Views/" + InformationConstSettings.VIEWNAME_DEFAULT + "/" + @this.prefabName;
				Debug.LogError("default path:" + path + " parsedTag:" + @this.parsedTag + " prefabName:" + @this.prefabName);

				// んで、prefabの名前はあってると思う。
				
				switch (@this.parsedTag) {
					case (int)HtmlTag._TEXT_CONTENT: {
						// こいつ自身がテキストコンテンツの場合、ここでタグの内容のサイズを推し量って返してOKになる。
						var text = @this.keyValueStore[Attribute._CONTENT] as string;
						Debug.LogError("テキストコンテンツ:" + text + " のレイアウトをやって、サイズを出して返す。従来の方法がいろんなタグが入っても破綻しないので従来の方法を使おう。リファクタしよう。");
						
						var cor = LayoutTextContent(@this, text, viewCursor);
						while (cor.MoveNext()) {
							yield return null;
						}
						
						var result = cor.Current;
						// viewCursorを書き換える
						break;
					}
					case (int)HtmlTag.img: {
						if (!@this.keyValueStore.ContainsKey(Attribute.SRC)) {
							throw new Exception("image should define src param.");
						}

						var src = @this.keyValueStore[Attribute.SRC] as string;
						Debug.LogError("srcから画像をDLしてきて、widthに対して適応させる。 src:" + src);

						// determine image size from image's parent's width.
						var imageWidth = viewCursor.viewWidth;
						var imageHeight = 0f;

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

						// これでサイズ取得が終わったので、viewCursorの高さを上書き指定して返す。
						if (viewCursor.viewHeight < imageHeight) {
							viewCursor.viewHeight = imageHeight;
						}

						break;
					}
					default: {
						// CONTENTではないので、CONTAINER。

						/*
							子供のタグを整列させる処理。
							横に整列、縦に並ぶ、などが実行される。
						*/
						var linedObject = new List<ParsedTree>();
						foreach (var child in @this.GetChildren()) {
							linedObject.Add(child);

							// 子供ごとにレイアウト結果を受け取る
							var cor = DoLayout(child, viewCursor);

							while (cor.MoveNext()) {
								yield return null;
							}

							Debug.LogError("ここで、子供コンテナのビューが帰ってくる。どんなのが帰ってくるのを想定すればいいんだろう。");
							/*
								コンテナのビューは、列の規則によって並びをまとめる必要がある。
							 */
							var newViewCursor = cor.Current;

							if (viewCursor.viewWidth < newViewCursor.offsetX + child.viewWidth) {
								Debug.LogWarning("右端を超えたので、この項目を取り外してそれまでのラインを整列させる。 みたいなことをする。");
								
								// 最後の一つを削除
								linedObject.RemoveAt(linedObject.Count - 1);

								// 整列処理をし、結果を受け取る
								var linedEndCursor = DoLining(linedObject);

								// クリア
								linedObject.Clear();

								// 次の行のトップとしてこの要素を追加
								linedObject.Add(child);
							}
						}

						// 最終行を処理
						if (linedObject.Any()) {
							var lastChildCursor = DoLining(linedObject);
						}

						Debug.LogWarning("すべての子供のコンテンツの位置が定まったので、自身のサイズを調整する(ほぼ変わらないはず。)");
						break;
					}
				}
			}

			Debug.LogWarning("最終的にviewCursorの値を調整したものを返す。");
			yield return viewCursor;
		}

		private ViewCursor DoLining (List<ParsedTree> linedChildren) {
			// linedChildrenの中で一番高度のあるコンテンツをもとに、他のコンテンツを下揃いに整列させ、最大のyを返す。
			

			Debug.LogError("仮で適当な値を返す");
			return new ViewCursor(0,0, 0,0);
		}

		/**
			この関数に含まれるのはboxで、ここに含まれるのはすべてカスタムタグの内容。
		 */
		private IEnumerator<ViewCursor> LayoutBox (ViewCursor layerViewCursor, ParsedTree box) {
			/*
				box自身のレイアウトに関して、prefabの値を引き継ぐ以外は特になにもするべきことがないのでは的な。
				位置情報はprefabのそのままなので、うーん、まずは値を保持してセットするようにしてみよう。
			 */
			
			// こいつ自身のkvにいろいろ入っているのでは
			// このタグを入れる時点ですでに反映されていてもいいのかもしれないが、一応後方 = 遅延させて値を持っておく手段に倒しておく。
			var layoutParam = box.keyValueStore[Attribute._BOX] as BoxPos;
			
			// 自分自身へのサイズやピボットのセット
			box.offsetMin = layoutParam.offsetMin;
			box.offsetMax = layoutParam.offsetMax;
			
			box.anchoredPosition = layoutParam.anchoredPosition;
			box.sizeDelta = layoutParam.sizeDelta;

			box.anchorMin = layoutParam.anchorMin;
			box.anchorMax = layoutParam.anchorMax;
			box.pivot = layoutParam.pivot;

			// ここで問題になりそうなのが、サイズか。この時点では仮想的に持ってるから、果たしてboxから実数的なサイズが算出できるのかしら的な。
			// だったらprefabに入れといてそれをパラメータ的にいじった方がいいか。でも階層ごとに、その階層だけ変なことになるっていうのが待っている。
			// 統一的に頑張るならここでprefab作ってなんかしよう。サイズを得るための設定とか
			foreach (var child in box.GetChildren()) {
				// ここでのコンテンツはboxの中身のコンテンツなので、必ず縦に並ぶ。列切り替えが発生しない。
				// 幅と高さを与えるが、高さは変更されて帰ってくる可能性が高い。
				// コンテンツが一切ない場合でもこの高さを維持する。
				// コンテンツがこの高さを切ってもこの高さを維持する。
				var cor = LayoutBoxedContent(layerViewCursor, child, box.sizeDelta);
				while (cor.MoveNext()) {
					yield return null;
				}
				var resultCursor = cor.Current;
			}
			
			// 適当にまず返す
			Debug.LogWarning("適当に返す。本来ここで返却される可能性があるのは、子供が複数いるときに、その高さが異なる = 縦に伸びる、みたいなケースで、その時、次に用意してあるboxの位置をずらす。");
			yield return layerViewCursor;
		}

		private IEnumerator<ViewCursor> LayoutBoxedContent (ViewCursor boxViewCursor, ParsedTree child, Vector2 size) {
			/*
				boxの内容たちで、特定のタグに限られている。で、幅や高さは与えられた要素になる。
				高さに関しては、画像か文字かで適応のし方が異なる。

				・文字
					幅はもらったものを受け取り、高さは結果を使用する。
				
				・画像
					幅はもらったものを使い、高さは結果を使用する。アスペクト比を見たりする。

			 */
			var childView = new ViewCursor(0, 0, size.x, size.y);
			var cor = DoLayout(child, childView);
			while (cor.MoveNext()) {
				yield return null;
			}

			var childLastCursor = cor.Current;
			
			/*
				ここで返されるべきなのは、子供のサイズぶん縦に広がったbox自身のビュー。
			 */
			yield return childLastCursor;
		}

		private IEnumerator<ViewCursor> LayoutTextContent (ParsedTree textTree, string text, ViewCursor cursor) {
			var prefabName = textTree.prefabName;


			var cor = infoResLoader.LoadTextPrefab(prefabName);

			while (cor.MoveNext()) {
				yield return null;
			}

			var prefab = cor.Current;

			// use prefab's text component for using it's text setting.
			var textComponent = prefab.GetComponent<Text>();
			if (textComponent == null) {
				throw new Exception("failed to get Text component from prefab:" + prefabName + " of text content:" + text);
			}

			if (textComponent.font == null) {
				throw new Exception("font is null. prefab:" + prefabName);
			}

			// set content to prefab.
			textComponent.text = text;
			{
				var generator = new TextGenerator();
				Debug.LogError("viewWidth:" + cursor.viewWidth);
				var setting = textComponent.GetGenerationSettings(new Vector2(cursor.viewWidth, 1000));
				
				generator.Populate(text, setting);


				// この時点で、複数行に分かれるんだけど、最後の行のみ分離する必要がある。
				var lineCount = generator.lineCount;
				Debug.LogError("lineCount:" + lineCount);
				Debug.LogError("width:" + textComponent.preferredWidth);// この部分が66になるのが正しいので、最終行が66で終わるのが正しい、という感じのテストを組むか。
				// 末尾のポイントを返す
				

				// 0行だったら、入らなかったということなので自分が入る位置を指定して頑張る的な。
				if (lineCount == 0) {
					// // 入らなかったので、前の行のラストの高さを+して、再計算。
					// var newViewCursor = new ViewCursor();
					// var nextLineCor = LayoutTextContent();
					yield break;
				}

				// 1行しかなかったら、yは変わらない。
				
				var totalHeight = 0;
				foreach (var line in generator.lines) {
					totalHeight += line.height;
				}
				
				textTree.viewWidth = textComponent.preferredWidth;
				textTree.viewHeight = totalHeight;

				// var newCursor = new ViewCursor();// 次のためのoffsetとWidthを返す？
				yield return cursor;

				generator.Invalidate();

			}
			textComponent.text = string.Empty;


			// while (true) {
			// 	// if rest text is included by one line, line collection is done.
			// 	if (generator.lineCount == 1) {
			// 		lines.Add(nextText);
			// 		break;
			// 	}

			// 	// Debug.LogError("nextText:" + nextText + " generator.lines.Count:" + generator.lines.Count);

			// 	// 複数行が出たので、continue, 

			// 	// 折り返しが発生したところから先のtextを取得し、その前までをコンテンツとしてセットする必要がある。
			// 	var nextTextLineInfo = generator.lines[1];
			// 	var startIndex = nextTextLineInfo.startCharIdx;
				
			// 	lines.Add(nextText.Substring(0, startIndex));

			// 	nextText = nextText.Substring(startIndex);
			// 	textComponent.text = nextText;

			// 	// populate again.
			// 	generator.Invalidate();

			// 	// populate splitted text again.
			// 	generator.Populate(textComponent.text, textComponent.GetGenerationSettings(new Vector2(contentWidth, contentHeight)));
			// 	if (generator.lineCount == 0) {
			// 		throw new Exception("no line detected 2. nextText:" + nextText);
			// 	}
			// }
			
			// textComponent.text = lines[0];
			// var preferredWidth = textComponent.preferredWidth;
			
			// if (contentWidth < preferredWidth) {
			// 	preferredWidth = contentWidth;
			// }

			// // reset.
			// textComponent.text = string.Empty;

			// /*
			// 	insert new line contents after this content.
			//  */
			// if (1 < lines.Count) {
			// 	var newContentTexts = lines.GetRange(1, lines.Count-1);
				
			// 	// foreach (var s in newContentTexts) {
			// 	// 	Debug.LogError("s:" + s);
			// 	// }

			// 	var newVGameObjects = newContentTexts.Select(
			// 		t => new ParsedTree(
			// 			@this,
			// 			new AttributeKVs(){
			// 				{Attribute._CONTENT, t}
			// 			}
			// 		)
			// 	).ToArray();
			// 	insert(newVGameObjects);
			// }

			// // Debug.LogError("preferredWidth:" + preferredWidth);
			
			// onCalculated(new ContentAndWidthAndHeight(lines[0], preferredWidth, generator.lines[0].height));

			// yield break;
		}








		private class ViewCursor {// structに変えたほうがいい気がするがyieldで返したい。空を返すのも勿体無い？
			public float offsetX;
			public float offsetY;
			public float viewWidth;
			public float viewHeight;
			public ViewCursor (float offsetX, float offsetY, float viewWidth, float viewHeight) {
				this.offsetX = offsetX;
				this.offsetY = offsetY;
				this.viewWidth = viewWidth;
				this.viewHeight = viewHeight;
			}

			public ViewCursor (ViewCursor viewCursor) {
				this.offsetX = viewCursor.offsetX;
				this.offsetY = viewCursor.offsetX;
				this.viewWidth = viewCursor.viewWidth;
				this.viewHeight = viewCursor.viewHeight;
			}
		}





		private IEnumerator StartLayout (ParsedTree @this, ViewBox view, Action<ParsedTree> layouted) {
			var handle = new OldHandlePoint(0, 0, view.width, view.height);

			var cor = LayoutRecursive((int)HtmlTag._ROOT, @this, handle, (i) => {});
			while (cor.MoveNext()) {
				yield return null;
			}
			
			layouted(@this);
		}

        /**
			layout contents.

			set position and size of content.
		*/
		private IEnumerator LayoutRecursive (int parentTag, ParsedTree @this, OldHandlePoint handle, Action<ParsedTree[]> insert) {
			switch (@this.parsedTag) {
				case (int)HtmlTag._ROOT: {
					// do nothing.
					break;
				}
				case (int)HtmlTag._DEPTH_ASSET_LIST_INFO: {
					// var cor = infoResLoader.GetDepthAssetList(@this.keyValueStore[Attribute.SRC]);

					// while (cor.MoveNext()) {
					// 	yield return null;
					// }
					
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

			var childlen = @this.GetChildren();
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
				if (@this.parsedTag == (int)HtmlTag.table) {
					/*
						all contents size calculation inside this table is done.
						count up row,
						find longest content,
						and adjust left point of contents.
					 */
					var tableLayoutRecord = new TableLayoutRecord();
					
					// countup rows.
					foreach (var tableChild in @this.GetChildren()) {
						CollectTableContentRowCountRecursively(@this, tableChild, tableLayoutRecord);
					}

					// find longest content.
					foreach (var tableChild in @this.GetChildren()) {
						CollectTableContentRowMaxWidthsRecursively(@this, tableChild, tableLayoutRecord);
					}

					// resize & reset position of this table contents by calculated record.
					foreach (var tableChild in @this.GetChildren()) {
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
        
		/**
			layoutの時点で必要なのは、そのコンテンツをどこに置くか、ってところなので、これはもう別途書くか。
		 */
        private IEnumerator LayoutTagContent (ParsedTree @this, OldHandlePoint handle, Action<ParsedTree[]> insert) {
			var xOffset = handle.nextLeftHandle;
			var yOffset = handle.nextTopHandle;
			var viewWidth = handle.viewWidth;
			var viewHeight = handle.viewHeight;

			// create default rect transform.
			var prefabRectTrans = new RectTransform();
			// Debug.LogError("prefabRectTrans:" + prefabRectTrans + " viewName:" + viewName);
			
			var prefabLoadCor = infoResLoader.LoadPrefab(
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

			// by kv.
			switch (@this.parsedTag) {
				// case (int)HtmlTag.ol: {
				// 	foreach (var kv in @this.keyValueStore) {
				// 		var key = kv.Key;
				// 		switch (key) {
				// 			case Attribute.START: {
				// 				// do nothing yet.
				// 				break;
				// 			}
				// 		}
				// 	}
				// 	break;
				// }
				// case (int)HtmlTag.a: {
				// 	// do nothing.
				// 	break;
				// }
				case (int)HtmlTag.img: {
					if (!@this.keyValueStore.ContainsKey(Attribute.SRC)) {
						throw new Exception("image should define src param.");
					}

					var src = @this.keyValueStore[Attribute.SRC] as string;
					float imageWidth = 0;
					float imageHeight = 0;
					
					Debug.LogWarning("このへんの、widthとかからサイズ指定する流れは全て消える。");
					// determine image size from image's width & height.
					
					// 消した。
					
					// set content size.
					contentWidth = imageWidth;
					contentHeight = imageHeight;
					break;
				}
				case (int)HtmlTag.hr: {
					GameObject prefab = null;
					var cor = infoResLoader.LoadPrefab(
						@this, 
						newPrefab => {
							prefab = newPrefab;
						},
						() => {
							throw new Exception("failed to load hr prefab:" + @this.prefabName);
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
						var text = @this.keyValueStore[Attribute._CONTENT] as string;
						
						var cor = LayoutText_Content(
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
				case (int)HtmlTag.th:
				case (int)HtmlTag.td: {
					// has KV_KEY._CONTENT_WIDTH value, but ignore.
					break;
				}
				default: {
					// あとで消す。
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
        private IEnumerator LayoutText_Content (ParsedTree @this, float offset, string text, float contentWidth, float contentHeight, Action<ParsedTree[]> insert, Action<ContentAndWidthAndHeight> onCalculated) {
			GameObject textPrefab = null;

			var cor = infoResLoader.LoadPrefab(
				@this, 
				newPrefab => {
					textPrefab = newPrefab;
				},
				() => {
					throw new Exception("failed to load _content prefab:" + @this.prefabName);
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
				throw new Exception("font is null. prefab:" + @this.prefabName);
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

        	
        private IEnumerator LayoutChildlen (ParsedTree @this, OldHandlePoint handle, List<ParsedTree> childlen) {
			// locate child content in relative. create new (0,0) handle.
			var childHandlePoint = new OldHandlePoint(0, 0, handle.viewWidth, handle.viewHeight);
			
			// layout -> resize -> padding of childlen.
		
			var layoutLine = new List<ParsedTree>();
			var i = 0;

			while (true) {
				if (childlen.Count <= i) {
					break;
				}

				var child = childlen[i];

				// consume br as linefeed.
				if (child.parsedTag == (int)HtmlTag.br) {
					childHandlePoint = SortByLayoutLine(layoutLine, childHandlePoint);

					// forget current line.
					layoutLine.Clear();

					// set next line.
					childHandlePoint.nextLeftHandle = 0;
					i++;
					continue;
				}
				
				// consume hr 1/2 as horizontal rule.
				if (child.parsedTag == (int)HtmlTag.hr) {
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
					if (child.parsedTag == (int)HtmlTag.hr) {
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
					if (@this.parsedTag == (int)HtmlTag.blockquote) {
						// nested bq.
						if (child.parsedTag == (int)HtmlTag.blockquote) {
							sortLayoutLineBeforeLining = true;
						}
					}

					/*
						nested list's child list should be located to new line.
					*/
					if (@this.parsedTag == (int)HtmlTag.li) {
						// nested list.
						if (child.parsedTag == (int)HtmlTag.ol || child.parsedTag == (int)HtmlTag.ul) {
							sortLayoutLineBeforeLining = true;
						}
					}

					// list's child should be ordered vertically.
					if (@this.parsedTag == (int)HtmlTag.ol || @this.parsedTag == (int)HtmlTag.ul) {
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
						if (child.parsedTag == (int)HtmlTag.thead) {// table head is single line.
							sortLayoutLineAfterLining = true;
						} else if (child.parsedTag == (int)HtmlTag.tr) {// table row.
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


        /**
			create line of contents -> sort all content by Y base line.
		*/
		private OldHandlePoint SortByLayoutLine (List<ParsedTree> layoutLine, OldHandlePoint childHandlePoint) {
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
			if (child.parsedTag == (int)HtmlTag.th) {
				tableLayoutRecord.IncrementRow();
			}

			foreach (var nestedChild in child.GetChildren()) {
				CollectTableContentRowCountRecursively(child, nestedChild, tableLayoutRecord);
			}
		}

		private void CollectTableContentRowMaxWidthsRecursively (ParsedTree @this, ParsedTree child, TableLayoutRecord tableLayoutRecord) {
			var total = 0f;
			foreach (var nestedChild in child.GetChildren()) {
				CollectTableContentRowMaxWidthsRecursively(child, nestedChild, tableLayoutRecord);
				if (child.parsedTag == (int)HtmlTag.th || child.parsedTag == (int)HtmlTag.td) {
					var nestedChildContentWidth = nestedChild.sizeDelta.x;
					total += nestedChildContentWidth;
				}
			}

			if (child.parsedTag == (int)HtmlTag.th || child.parsedTag == (int)HtmlTag.td) {
				tableLayoutRecord.UpdateMaxWidth(total);
			}
		}

		private void SetupTableContentPositionRecursively (ParsedTree @this, ParsedTree child, TableLayoutRecord tableLayoutRecord) {
			// overwrite parent content width of TH and TD.
			if (child.parsedTag == (int)HtmlTag.thead || child.parsedTag == (int)HtmlTag.tbody || child.parsedTag == (int)HtmlTag.thead || child.parsedTag == (int)HtmlTag.tr) {
				var width = tableLayoutRecord.TotalWidth();
				child.sizeDelta = new Vector2(width, child.sizeDelta.y);
			}

			/*
				change TH, TD content's x position and width.
				x position -> 0, 1st row's longest content len, 2nd row's longest content len,...
				width -> 1st row's longest content len, 2nd row's longest content len,...
			*/
			if (child.parsedTag == (int)HtmlTag.th || child.parsedTag == (int)HtmlTag.td) {
				var offsetAndWidth = tableLayoutRecord.GetOffsetAndWidth();
				
				child.anchoredPosition = new Vector2(offsetAndWidth.offset, child.anchoredPosition.y);
				child.sizeDelta = new Vector2(offsetAndWidth.width, child.sizeDelta.y);
			}
			
			foreach (var nestedChild in child.GetChildren()) {
				SetupTableContentPositionRecursively(child, nestedChild, tableLayoutRecord);	
			}
		}
    }
}