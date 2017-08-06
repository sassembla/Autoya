using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AutoyaFramework.Information {


	public class ViewCursor {
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

		/**
			次の行の起点となるviewCursorを返す
		 */
		public static ViewCursor NextLine (ViewCursor baseCursor, float nextLineOffsetY, float viewWidth) {
			baseCursor.offsetX = 0;
			baseCursor.offsetY = nextLineOffsetY;

			baseCursor.viewWidth = viewWidth;
			// 次の行の高さに関しては特に厳密な計算をしない。
			baseCursor.viewHeight = 0;
			return baseCursor;
		}

		/**
			次の要素の起点となるviewCursorを返す
		 */
        public static ViewCursor NextRightCursor(ViewCursor childView, float viewWidth){

			// 横並べ処理。

			// オフセットを直前のオフセット + 幅のポイントにずらす。
			childView.offsetX = childView.offsetX + childView.viewWidth;

			// コンテンツが取り得る幅を、大元の幅 - 現在のオフセットから計算。
			childView.viewWidth = viewWidth - childView.offsetX;
		
			// offsetYは変わらず、高さに関しては特に厳密な計算をしない。
			childView.viewHeight = 0;
			return childView;
        }

		/**
			sourceのwidthを使い、
			lastChildのあるポイントからの最大高さを返す。
		 */
		public static ViewCursor Wrap(ViewCursor source, float lastChildEndY){
			source.viewHeight = lastChildEndY;
			return source;
		}

		override public string ToString () {
			return "offsetX:" + offsetX + " offsetY:" + offsetY + " viewWidth:" + viewWidth + " viewHeight:" + viewHeight;
		}
    }	


    /**
        レイアウトを実行するクラス。
    */
    public class LayoutMachine {
		private readonly InformationResourceLoader infoResLoader;
		
		private readonly ViewBox view;

      	public LayoutMachine (
			  InformationResourceLoader infoResLoader,
			  ViewBox view
		) {
			this.infoResLoader = infoResLoader;
			this.view = view;
        }

		private enum InsertType {
			Continue,
			InsertContentToNextLine,
			RetryWithNextLine,
		};

		public IEnumerator Layout (ParsedTree @this, Action<ParsedTree> layouted) {
			Debug.LogError("どこかで、layoutによって分割されたオブジェクトのリセットを行わなければ。");
			var viewCursor = new ViewCursor(0, 0, view.width, view.height);
			
			var cor = DoLayout(@this, viewCursor);
			
			while (cor.MoveNext()) {
				yield return null;
			}

			// ビュー高さが出る。
			viewCursor = cor.Current;
			Debug.LogError("root viewCursor:" + viewCursor);
			
			layouted(@this);
		}

		private IEnumerator<ViewCursor> DoLayout (ParsedTree @this, ViewCursor viewCursor, Action<InsertType, ParsedTree> insertion=null) {
			IEnumerator<ViewCursor> cor = null;
			
			switch (@this.treeType) {
				case TreeType.CustomLayer: {
					Debug.LogError("カスタムレイヤ");
					cor = DoLayerLayout(@this, viewCursor);
					break;
				}
				case TreeType.CustomEmptyLayer: {
					Debug.LogError("中身のないカスタムレイヤ");
					cor = DoEmptyLayerLayout(@this, viewCursor);
					break;
				}
				case TreeType.Container: {
					Debug.LogError("コンテナ");
					cor = DoContainerLayout(@this, viewCursor);
					break;
				}
				case TreeType.Content_Img: {
					Debug.LogError("画像");
					cor = DoImgLayout(@this, viewCursor, insertion);
					break;
				}
				case TreeType.Content_Text: {
					Debug.LogError("テキスト");
					cor = DoTextLayout(@this, viewCursor, insertion);
					break;
				}
				default: {
					throw new Exception("unexpected tree type:" + @this.treeType);
				}
			}

			while (cor.MoveNext()) {
				yield return null;
			}

			yield return cor.Current;
		}
		
		/**
			カスタムタグのレイヤーのレイアウトを行う。
			customTagLayer/box/boxContents というレイヤーになっていて、必ず規定のポジションでレイアウトされる。
			ここだけ相対的なレイアウトが崩れる。
		 */
		private IEnumerator<ViewCursor> DoLayerLayout (ParsedTree @this, ViewCursor viewCursor) {
			// 親コンテンツのサイズを継承
			@this.SetPosFromViewCursor(viewCursor);

			var path = "Views/" + infoResLoader.CustomTagList().viewName + "/" + infoResLoader.GetTagFromIndex(@this.parsedTag);
			Debug.LogError("あとで使うprefabのパス。layer prefab path:" + path);

			/*
				レイヤーなので、prefabをロードして、原点位置は0,0、
					サイズは親サイズ、という形で生成する。
				
				・childlenにboxの中身が含まれている場合(IsContainedThisCustomTag)、childlenの要素を生成する。そうでない要素の場合は生成しない。
				・この際のchildのサイズは、layerであれば必ずboxのサイズになる。このへんがキモかな。
			*/

			var children = @this.GetChildren();
			var additionalHeight = 0f;

			foreach (var boxTree in children) {
				Debug.LogError("tag:" + infoResLoader.GetTagFromIndex(boxTree.parsedTag) + " boxTree:" + boxTree.treeType);

				/*
					位置情報はkvに入っているが、親のviewの値を使ってレイアウト後の位置に関する数値を出す。
					コンテナがここに飛び込んでくることがある。boxがないところに飛び込んでくるコンテナってことか。
				*/
				var layoutParam = boxTree.keyValueStore[Attribute._BOX] as BoxPos;
				
				var viewRect = ParsedTree.GetChildViewRectFromParentRectTrans(viewCursor.viewWidth, viewCursor.viewHeight, layoutParam);
				Debug.LogError("viewRect:" + viewRect);

				var childView = new ViewCursor(viewRect.x, viewRect.y + additionalHeight, viewRect.width, viewRect.height);

				var cor = LayoutBoxedContents(childView, boxTree);

				while (cor.MoveNext()) {
					yield return null;
				}

				childView = cor.Current;
				if (viewRect.height < childView.viewHeight) {
					Debug.LogError("レイアウト後のサイズが大きいので、次のrectの開始位置を差分だけズラす。");
					additionalHeight = childView.viewHeight - viewRect.height;
				}
			}

			Debug.LogError("ここでカスタムタグ自体のサイズを変更する。　additionalHeight:" + additionalHeight);
			viewCursor.viewHeight += additionalHeight;
			yield return viewCursor;
		}

		private IEnumerator<ViewCursor> DoEmptyLayerLayout (ParsedTree @this, ViewCursor viewCursor) {
			
			var baseViewCursorHeight = viewCursor.viewHeight;
			var cor = DoContainerLayout(@this, viewCursor);

			while (cor.MoveNext()) {
				yield return null;
			}
			
			var resultViewCursor = cor.Current;
			
			// 縮まない。伸びるぶんには放置。
			if (resultViewCursor.viewHeight < baseViewCursorHeight) {
				resultViewCursor.viewHeight = baseViewCursorHeight;
			}

			@this.SetPosFromViewCursor(resultViewCursor);
			yield return resultViewCursor;
		}

		private IEnumerator<ViewCursor> DoImgLayout (ParsedTree @this, ViewCursor viewCursor, Action<InsertType, ParsedTree> insertion=null) {
			var contentViewCursor = viewCursor;
			if (!@this.keyValueStore.ContainsKey(Attribute.SRC)) {
				throw new Exception("image should define src param.");
			}

			var src = @this.keyValueStore[Attribute.SRC] as string;
			
			// need to download image.
			
			var downloaded = false;

			var imageWidth = 0f;
			var imageHeight = 0f;

			infoResLoader.LoadImageAsync(
				src, 
				(sprite) => {
					imageWidth = sprite.rect.size.x;
					
					if (viewCursor.viewWidth < imageWidth) {
						imageWidth = viewCursor.viewWidth;
					}
					
					imageHeight = (imageWidth / sprite.rect.size.x) * sprite.rect.size.y;
					downloaded = true;
				},
				() => {
					imageHeight = 0;
					downloaded = true;
				}
			);

			while (!downloaded) {
				yield return null;
			}

			// 画像のアスペクト比に則ったサイズを返す。高さのみ変更がされる。
			contentViewCursor.viewWidth = imageWidth;
			contentViewCursor.viewHeight = imageHeight;

			// 自己のサイズに反映
			@this.SetPosFromViewCursor(contentViewCursor);
			
			yield return contentViewCursor;
		}

		/**
			テキストコンテンツのレイアウトを行う。
			もしテキストが複数行に渡る場合、最終行だけを新規コンテンツとして上位に返す。
		 */
		private IEnumerator<ViewCursor> DoTextLayout (ParsedTree textTree, ViewCursor textViewCursor, Action<InsertType, ParsedTree> insertion) {
			var text = textTree.keyValueStore[Attribute._CONTENT] as string;
			
			var cor = infoResLoader.LoadGameObjectFromPrefab(textTree.parsedTag, textTree.treeType, true);

			while (cor.MoveNext()) {
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

			Debug.LogWarning("文字を配置するときの適当な高さをどうやって与えようか考え中。10000は適当。");
			
			// set content to prefab.
			
			var generator = new TextGenerator();
			
			textComponent.text = text;
			var setting = textComponent.GetGenerationSettings(new Vector2(textViewCursor.viewWidth, 10000));
			generator.Populate(text, setting);

			using (new Lock(textComponent, generator)) {
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
					ここで、offsetXが0ではない場合、行の中間から行を書き出している。
					かつ2行以上ある場合、1行目は右端まで到達していて、
					2行目以降はoffsetが0から書かれる必要がある。

					コンテンツを分離し、それを叶える。
				*/
				var isStartAtZeroOffset = textViewCursor.offsetX == 0;
				var isMultilined = 1 < lineCount;

				// 複数行存在するんだけど、2行目のスタートが0文字目の場合、1行目に1文字も入っていない。コンテンツ全体を次の行で開始させる。
				if (isMultilined && generator.lines[1].startCharIdx == 0) {
					insertion(InsertType.RetryWithNextLine, null);
					yield break;
				}

				if (isStartAtZeroOffset) {
					if (isMultilined) {
						// 複数行が頭から出ている状態で、改行を含んでいる。最終行が中途半端なところにあるのが確定しているので、切り離して別コンテンツとして処理する必要がある。
						var bodyContent = text.Substring(0, generator.lines[generator.lineCount-1].startCharIdx);
						
						// 内容の反映
						textTree.keyValueStore[Attribute._CONTENT] = bodyContent;

						// 最終行
						var lastLineContent = text.Substring(generator.lines[generator.lineCount-1].startCharIdx);

						// Debug.LogError("lastLineContent:" + lastLineContent);
						// 最終行を分割して送り出す。追加されたコンテンツを改行後に処理する。
						var nextLineContent = new ParsedTree(lastLineContent, textTree.parsedTag);
						insertion(InsertType.InsertContentToNextLine, nextLineContent);

						// 最終行以外はハコ型に収まった状態なので、ハコとして出力する。

						// 最終一つ前までの高さを出して、
						var totalHeight = 0;
						for (var i = 0; i < generator.lineCount-1; i++) {
							var line = generator.lines[i];
							totalHeight += line.height;
						}

						// このビューのポジションとしてセット
						var newViewCursor = textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, textViewCursor.viewWidth, totalHeight);
						// Debug.LogError("newViewCursor:" + newViewCursor);
						yield return newViewCursor;
					} else {
						// 行頭の単一行
						var width = textComponent.preferredWidth;
						var height = generator.lines[0].height;
						var newViewCursor = textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, textComponent.preferredWidth, height);
						yield return newViewCursor;
					}
				} else {
					if (isMultilined) {
						// Debug.LogError("行中での折り返しのある文字ヒット");
						var currentLineHeight = generator.lines[0].height;

						// 複数行が途中から出ている状態で、まず折り返しているところまでを分離して、後続の文章を新規にstringとしてinsertする。
						var currentLineContent = text.Substring(0, generator.lines[1].startCharIdx);
						textTree.keyValueStore[Attribute._CONTENT] = currentLineContent;

						// get preferredWidht of text from trimmed line.
						textComponent.text = currentLineContent;

						var currentLineWidth = textComponent.preferredWidth;

						var restContent = text.Substring(generator.lines[1].startCharIdx);
						var nextLineContent = new ParsedTree(restContent, textTree.parsedTag);

						// 次のコンテンツを新しい行から開始する。
						insertion(InsertType.InsertContentToNextLine, nextLineContent);

						var newViewCursor = textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, currentLineWidth, currentLineHeight);
						// Debug.LogError("newViewCursor:" + newViewCursor);
						yield return newViewCursor;
					} else {
						// 行の途中に追加された単一行で、いい感じに入った。
						var width = textComponent.preferredWidth;
						var height = generator.lines[0].height;
						var newViewCursor = textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, textComponent.preferredWidth, height);
						yield return newViewCursor;
					}
				}
			}
		}


		private IEnumerator<ViewCursor> DoContainerLayout (ParsedTree @this, ViewCursor viewCursor) {
			/*
				子供のタグを整列させる処理。
				横に整列、縦に並ぶ、などが実行される。

				初期カーソルは親と同じ。
			*/
			var childView = new ViewCursor(viewCursor);
			var linedElements = new List<ParsedTree>();
			
			var containerChildren = @this.GetChildren();
			var childCount = containerChildren.Count;
			
			if (childCount == 0) {
				yield break;
			}

			for (var i = 0; i < childCount; i++) {
				var child = containerChildren[i];
				Debug.LogError("child:" + infoResLoader.GetTagFromIndex(child.parsedTag));
				currentLineRetry: {
					linedElements.Add(child);

					// set insertion type.
					var currentInsertType = InsertType.Continue;

					// 子供ごとにレイアウトし、結果を受け取る
					var cor = DoLayout(
						child, 
						childView, 
						(insertType, newChild) => {
							currentInsertType = insertType;

							switch (insertType) {
								case InsertType.InsertContentToNextLine: {
									// 次に処理するコンテンツを差し込む。
									containerChildren.Insert(i+1, newChild);
									childCount++;
									break;
								}
							}
						}
					);

					while (cor.MoveNext()) {
						yield return null;
					}
					
					switch (currentInsertType) {
						case InsertType.RetryWithNextLine: {
							// Debug.LogError("テキストコンテンツが0行を叩き出したので、このコンテンツ自体をもう一度レイアウトする。");
							
							// 最後の一つ=この処理の開始時にいれていたものを削除
							linedElements.RemoveAt(linedElements.Count - 1);

							// 含まれているものの整列処理をし、列の高さを受け取る
							var newLineOffsetY = DoLining(linedElements);

							// 整列と高さ取得が完了したのでリセット
							linedElements.Clear();

							// ここまでの行の高さがcurrentHeightに出ているので、currentHeightから次の行を開始する。
							childView = ViewCursor.NextLine(childView, newLineOffsetY, viewCursor.viewWidth);

							// もう一度この行を処理する。
							goto currentLineRetry;
						}
						case InsertType.InsertContentToNextLine: {
							// Debug.LogError("ここまでで前の行が終わり、次の行のコンテンツが入れ終わってるので、改行する。");

							// ここまでで前の行が終わり、次の行のコンテンツが入れ終わってるので、改行する。
							var newLineOffsetY = DoLining(linedElements);

							// 整列と高さ取得が完了したのでリセット
							linedElements.Clear();

							// ここまでの行の高さがcurrentHeightに出ているので、currentHeightから次の行を開始する。
							childView = ViewCursor.NextLine(childView, newLineOffsetY, viewCursor.viewWidth);
							Debug.LogError("child:" + child.parsedTag + " done," + child.ShowContent() + " next childView:" + childView);
							continue;
						}
					}

					/*
						コンテンツがwidth内に置けた(ギリギリを含む)
					 */

					// レイアウトが済んだchildの位置を受け取る。
					var layoutedChildView = cor.Current;
					Debug.LogError("layoutedChildView:" + layoutedChildView);
					Debug.Assert(layoutedChildView != null, "layoutedChildView is null.");
					
					var nextChildViewCursor = ViewCursor.NextRightCursor(layoutedChildView, viewCursor.viewWidth);

					// レイアウト直後に次のポイントの開始位置が幅を超えている場合、現行の行のライニングを行う。
					if (viewCursor.viewWidth <= nextChildViewCursor.offsetX) {
						// ライニング
						var nextLineOffsetY = DoLining(linedElements);

						// ライン解消
						linedElements.Clear();

						// 改行処理
						childView = ViewCursor.NextLine(childView, nextLineOffsetY, viewCursor.viewWidth);
					} else {
						// 次のchildの開始ポイントを現在のchildの右にセット
						childView = nextChildViewCursor;
					}

					Debug.LogError("child:" + child.parsedTag + " done," + child.ShowContent() + " next childView:" + childView);
				}

				// 現在の子供のレイアウトが終わっていて、なおかつライン処理、改行が済んでいる。
			}

			// 最後の列はそのまま1列扱いになるので、整列。
			if (linedElements.Any()) {
				// ここでは高さを取得、使用しない。
				DoLining(linedElements);
			}
			
			var lastChildEndY = containerChildren[containerChildren.Count-1].offsetY + containerChildren[containerChildren.Count-1].viewHeight;
			// Debug.Log("lastChildEndY:" + lastChildEndY + " これが更新されない場合、レイアウトされたパーツにサイズが入ってない。");
			viewCursor = ViewCursor.Wrap(viewCursor, lastChildEndY);
			
			// 自分自身のサイズを再規定
			@this.SetPosFromViewCursor(viewCursor);
			yield return viewCursor;
		}

		/**
			linedChildrenの中で一番高度のあるコンテンツをもとに、他のコンテンツを下揃いに整列させ、次の行の開始Yを返す。
			整列が終わったら、それぞれのコンテンツのオフセットをいじる。サイズは変化しない。
		*/
		private float DoLining (List<ParsedTree> linedChildren) {
			var nextOffsetY = 0f;
			var tallestHeight = 0f;

			for (var i = 0; i < linedChildren.Count; i++) {
				var child = linedChildren[i];
				if (tallestHeight < child.viewHeight) {
					tallestHeight = child.viewHeight;
					nextOffsetY = child.offsetY + tallestHeight;
				}
			}
			
			// 高さを位置として反映させる。
			for (var i = 0; i < linedChildren.Count; i++) {
				var child = linedChildren[i];
				var diff = tallestHeight - child.viewHeight;
				
				child.offsetY = child.offsetY + diff;
			}

			return nextOffsetY;
		}

		/**
			ボックス内部のコンテンツのレイアウトを行う
		 */
		private IEnumerator<ViewCursor> LayoutBoxedContents (ViewCursor boxView, ParsedTree box) {
			
			var containerChildren = box.GetChildren();
			var childCount = containerChildren.Count;

			if (childCount == 0) {
				yield break;
			}

			var linedElements = new List<ParsedTree>();
			var childView = new ViewCursor(boxView);

			for (var i = 0; i < childCount; i++) {
				var child = containerChildren[i];
				Debug.LogError("box child:" + infoResLoader.GetTagFromIndex(child.parsedTag));
				currentLineRetry: {
					linedElements.Add(child);

					// set insertion type.
					var currentInsertType = InsertType.Continue;

					// 子供ごとにレイアウトし、結果を受け取る
					var cor = DoLayout(
						child, 
						childView, 
						(insertType, newChild) => {
							currentInsertType = insertType;

							switch (insertType) {
								case InsertType.InsertContentToNextLine: {
									// 次に処理するコンテンツを差し込む。
									containerChildren.Insert(i+1, newChild);
									childCount++;
									break;
								}
							}
						}
					);

					while (cor.MoveNext()) {
						yield return null;
					}
					
					switch (currentInsertType) {
						case InsertType.RetryWithNextLine: {
							// Debug.LogError("テキストコンテンツが0行を叩き出したので、このコンテンツ自体をもう一度レイアウトする。");
							
							// 最後の一つ=この処理の開始時にいれていたものを削除
							linedElements.RemoveAt(linedElements.Count - 1);

							// 含まれているものの整列処理をし、列の高さを受け取る
							var newLineOffsetY = DoLining(linedElements);

							// 整列と高さ取得が完了したのでリセット
							linedElements.Clear();

							// ここまでの行の高さがcurrentHeightに出ているので、currentHeightから次の行を開始する。
							childView = ViewCursor.NextLine(childView, newLineOffsetY, boxView.viewWidth);

							// もう一度この行を処理する。
							goto currentLineRetry;
						}
						case InsertType.InsertContentToNextLine: {
							// Debug.LogError("ここまでで前の行が終わり、次の行のコンテンツが入れ終わってるので、改行する。");

							// ここまでで前の行が終わり、次の行のコンテンツが入れ終わってるので、改行する。
							var newLineOffsetY = DoLining(linedElements);

							// 整列と高さ取得が完了したのでリセット
							linedElements.Clear();

							// ここまでの行の高さがcurrentHeightに出ているので、currentHeightから次の行を開始する。
							childView = ViewCursor.NextLine(childView, newLineOffsetY, boxView.viewWidth);
							Debug.LogError("box child:" + child.parsedTag + " done," + child.ShowContent() + " next childView:" + childView);
							continue;
						}
					}

					/*
						コンテンツがwidth内に置けた(ギリギリを含む)
					 */

					// レイアウトが済んだchildの位置を受け取る。
					var layoutedChildView = cor.Current;
					Debug.LogError("box layoutedChildView:" + layoutedChildView);
					Debug.Assert(layoutedChildView != null, "layoutedChildView is null.");
					
					var nextChildViewCursor = ViewCursor.NextRightCursor(layoutedChildView, boxView.viewWidth);

					// レイアウト直後に次のポイントの開始位置が幅を超えている場合、現行の行のライニングを行う。
					if (boxView.viewWidth <= nextChildViewCursor.offsetX) {
						// ライニング
						var nextLineOffsetY = DoLining(linedElements);

						// ライン解消
						linedElements.Clear();

						// 改行処理
						childView = ViewCursor.NextLine(childView, nextLineOffsetY, boxView.viewWidth);
					} else {
						// 次のchildの開始ポイントを現在のchildの右にセット
						childView = nextChildViewCursor;
					}

					Debug.LogError("child:" + child.parsedTag + " done," + child.ShowContent() + " next childView:" + childView);
				}

				// 現在の子供のレイアウトが終わっていて、なおかつライン処理、改行が済んでいる。
			}

			// 最後の列はそのまま1列扱いになるので、整列。
			if (linedElements.Any()) {
				// ここでは高さを取得、使用しない。
				DoLining(linedElements);
			}
			
			var lastChildEndY = containerChildren[containerChildren.Count-1].offsetY + containerChildren[containerChildren.Count-1].viewHeight;
			// Debug.Log("lastChildEndY:" + lastChildEndY + " これが更新されない場合、レイアウトされたパーツにサイズが入ってない。");
			childView = ViewCursor.Wrap(childView, lastChildEndY);
			
			// 自分自身のサイズを再規定
			box.SetPosFromViewCursor(childView);
			yield return childView;
		}

		
		private class Lock : IDisposable {
            private Text textComponent;
			private TextGenerator gen;
            public Lock (Text textComponent, TextGenerator gen) {
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