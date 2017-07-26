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
		public static ViewCursor NextLine (ViewCursor baseCursor, float lineEndHeight, float viewWidth) {
			baseCursor.offsetX = 0;
			baseCursor.offsetY = lineEndHeight;

			baseCursor.viewWidth = viewWidth;
			return baseCursor;
		}

		/**
			次の要素の起点となるviewCursorを返す
		 */
        public static ViewCursor Update(ViewCursor childView, ViewCursor viewCursor){
			// オフセットを直前のオフセット + 幅のポイントにずらす。
            childView.offsetX = childView.offsetX + childView.viewWidth;

			// コンテンツが取り得る幅を、大元の幅 - 現在のオフセットから計算。
			childView.viewWidth = viewCursor.viewWidth - childView.offsetX;

			// offsetYは変わらず、高さに関しては特に厳密な計算をしない。
			childView.viewHeight = viewCursor.viewHeight;
			return childView;
        }

		/**
			sourceのwidthを使い、
			lastChildのあるポイントからの最大高さを返す。
		 */
		public static ViewCursor Wrap(ViewCursor source, float lastChildEndY){
			Debug.LogError("lastChildEndY:" + lastChildEndY);
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

		private enum InsertType {
			Continue,
			InsertContentToNextLine,
			RetryWithNextLine,
		};

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
			Debug.LogError("root:" + viewCursor);
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

			viewCursor = cor.Current;
			// ここでviewHeightが出せる。
			Debug.LogError("root result viewCursor:" + viewCursor);

			layouted(@this);

			yield break;
		}

		private IEnumerator<ViewCursor> DoLayout (ParsedTree @this, ViewCursor viewCursor, Action<InsertType, ParsedTree> insertion=null) {

			// カスタムタグかどうかで分岐
			if (infoResLoader.IsCustomTag(@this.parsedTag)) {
				var cor = DoCustomTagContainerLayout(@this, viewCursor);
				while (cor.MoveNext()) {
					yield return null;
				}

				Debug.LogError("カスタムタグの計算結果を返す");
				yield return viewCursor;
			} else if (@this.isContainer) {
				var cor = DoContainerLayout(@this, viewCursor);
				while (cor.MoveNext()) {
					yield return null;
				}

				yield return cor.Current;
			} else {
				var cor = DoContentLayout(@this, viewCursor, insertion);
				while (cor.MoveNext()) {
					yield return null;
				}

				yield return cor.Current;
			}
		}
		
		/**
			カスタムタグのレイアウトを行う。
			customTag_CONTAINER/box/boxContents というレイヤーになっていて、必ず規定のポジションでレイアウトされる。
			ここだけ相対的なレイアウトが崩れる。
		 */
		private IEnumerator<ViewCursor> DoCustomTagContainerLayout (ParsedTree @this, ViewCursor viewCursor) {
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
				Debug.LogWarning("カスタムタグのboxのレイアウトが終わった。で、親のサイズが変わる(高さが高くなった)可能性がある。幅は変化しない。");
			}

			yield return viewCursor;
		}
		

		private IEnumerator<ViewCursor> DoContentLayout (ParsedTree @this, ViewCursor viewCursor, Action<InsertType, ParsedTree> insertion=null) {
			// ここにくるのはこれコンテンツかコンテナだ。コンテンツのみがくると楽なのだが、コンテンツ 含有 コンテナ なので、
			// 自動的にコンテナがくることがあり得る。まあしょうがない。
			// で。その分解はparserで済んでると思う。
			// customTagの機構はタグの機構の完全上位みたいになってるような気がする。まあフローが違うからそうか。

			/*
				このタグはカスタムタグではない => デフォルトタグなので、resourcesから引いてくるか。一応。
				*/
			var path = "Views/" + InformationConstSettings.VIEWNAME_DEFAULT + "/" + @this.prefabName;
			Debug.LogError("default path:" + path + " parsedTag:" + @this.parsedTag + " prefabName:" + @this.prefabName + " isContainer:" + @this.isContainer);

			// んで、prefabの名前はあってると思う。
			
			var contentViewCursor = viewCursor;
			switch (@this.parsedTag) {
				case (int)HtmlTag._TEXT_CONTENT: {
					// こいつ自身がテキストコンテンツの場合、ここでタグの内容のサイズを推し量って返してOKになる。
					var text = @this.keyValueStore[Attribute._CONTENT] as string;
					
					var cor = LayoutTextContent(@this, text, viewCursor, insertion);
					while (cor.MoveNext()) {
						yield return null;
					}
					Debug.LogError("この時点でのviewが:" + cor.Current);
					contentViewCursor = cor.Current;
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
					Debug.LogError("まだ画像のviewCursorを反映してない");
					break;
				}
				default: {
					Debug.LogWarning("謎のコンテンツ。");
					break;
				}
			}
			
			yield return contentViewCursor;
		}

		private IEnumerator<ViewCursor> DoContainerLayout (ParsedTree @this, ViewCursor viewCursor) {
			Debug.LogError("before container layout:" + viewCursor);
			/*
				このタグはカスタムタグではない => デフォルトタグなので、resourcesから引いてくるか。一応。
			*/
			var path = "Views/" + InformationConstSettings.VIEWNAME_DEFAULT + "/" + @this.prefabName;
			Debug.LogError("default path:" + path + " parsedTag:" + @this.parsedTag + " prefabName:" + @this.prefabName);

			// んで、prefabの名前はあってると思う。
			// CONTENTではないので、CONTAINER。

			/*
				子供のタグを整列させる処理。
				横に整列、縦に並ぶ、などが実行される。

				初期カーソルは親と同じ。
			*/
			var childView = new ViewCursor(viewCursor);
			var linedObject = new List<ParsedTree>();
			
			var containerChildren = @this.GetChildren();
			var childCount = containerChildren.Count;
			
			if (childCount == 0) {
				yield break;
			}

			for (var i = 0; i < childCount; i++) {
				var child = containerChildren[i];
				
				currentLineRetry: {
					linedObject.Add(child);

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
							Debug.LogError("テキストコンテンツが0行を叩き出したので、このコンテンツ自体をもう一度レイアウトする。");
							
							// 最後の一つ=この処理の開始時にいれていたものを削除
							linedObject.RemoveAt(linedObject.Count - 1);

							// 含まれているものの整列処理をし、列の高さを受け取る
							var newLineOffsetY = DoLining(linedObject);

							// 整列と高さ取得が完了したのでリセット
							linedObject.Clear();

							// ここまでの行の高さがcurrentHeightに出ているので、currentHeightから次の行を開始する。
							childView = ViewCursor.NextLine(childView, newLineOffsetY, viewCursor.viewWidth);

							// もう一度この行を処理する。
							goto currentLineRetry;
						}
						case InsertType.InsertContentToNextLine: {
							// ここまでで前の行が終わり、次の行のコンテンツが入れ終わってるので、改行する。
							var newLineOffsetY = DoLining(linedObject);

							// 整列と高さ取得が完了したのでリセット
							linedObject.Clear();

							// ここまでの行の高さがcurrentHeightに出ているので、currentHeightから次の行を開始する。
							childView = ViewCursor.NextLine(childView, newLineOffsetY, viewCursor.viewWidth);
							continue;
						}
					}

					// レイアウトが済んだchildの位置を受け取る。
					var currentChildView = cor.Current;
					Debug.LogError("currentChildView:" + currentChildView);
					if (viewCursor.viewWidth < currentChildView.offsetX + currentChildView.viewWidth) {
						// 最後の一つ = 現在のコンテンツを列から削除
						linedObject.RemoveAt(linedObject.Count - 1);

						// 整列処理をし、結果を受け取る
						var newLineOffsetY = DoLining(linedObject);

						// 整列と高さ取得が完了したのでリセット
						linedObject.Clear();

						// ちょうどこのラインの処理が終わった時に右端の限界を超えたので、このコンテンツの位置を新しい行の行頭にずらす。
						childView = child.SetPos(0, newLineOffsetY, currentChildView.viewWidth, currentChildView.viewHeight);
					} else {
						// 無事枠内に入っているので、childViewを子供の最新のchildViewに更新。
						childView = currentChildView;
					}

					// 次のchildの開始ポイントをセットする。
					childView = ViewCursor.Update(childView, viewCursor);
				}
			}

			// 最後の列はそのまま1列扱いになるので、整列。
			if (linedObject.Any()) {
				// ここでは高さを取得、使用しない。
				DoLining(linedObject);
			}

			viewCursor = ViewCursor.Wrap(viewCursor, containerChildren[containerChildren.Count-1].offsetY + containerChildren[containerChildren.Count-1].viewHeight);
			
			// 自分自身のサイズを再規定
			@this.SetPosFromViewCursor(viewCursor);
			yield return viewCursor;
		}

		/**
			linedChildrenの中で一番高度のあるコンテンツをもとに、他のコンテンツを下揃いに整列させ、最大のyを返す。
			整列が終わったら、それぞれのコンテンツのオフセットをいじる。サイズとかは変化しない。
		*/
		private float DoLining (List<ParsedTree> linedChildren) {
			var tallestHeight = 0f;

			for (var i = 0; i < linedChildren.Count; i++) {
				var child = linedChildren[i];
				if (tallestHeight < child.viewHeight) {
					tallestHeight = child.viewHeight;
				}
			}
			
			// 高さを位置として反映させる。
			for (var i = 0; i < linedChildren.Count; i++) {
				var child = linedChildren[i];
				var diff = tallestHeight - child.viewHeight;
				child.offsetY = child.offsetY + diff;
			}

			return linedChildren[0].offsetY + tallestHeight;
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

			foreach (var child in box.GetChildren()) {
				// ここでのコンテンツはboxの中身のコンテンツなので、必ず縦に並ぶ。列切り替えが発生しない。
				// 幅と高さを与えるが、高さは変更されて帰ってくる可能性が高い。

				// コンテンツが一切ない場合でもこの高さを維持する。
				// コンテンツがこの高さを切ってもこの高さを維持する。
				var cor = LayoutBoxedContent(child, box.sizeDelta);
				while (cor.MoveNext()) {
					yield return null;
				}

				var resultCursor = cor.Current;
			}
			
			// 適当にまず返す
			Debug.LogWarning("適当に返す。本来ここで返却される可能性があるのは、子供が複数いるときに、その高さが異なる = 縦に伸びる、みたいなケースで、その時、次に用意してあるboxの位置をずらす。");
			yield return layerViewCursor;
		}

		private IEnumerator<ViewCursor> LayoutBoxedContent (ParsedTree boxedContainer, Vector2 size) {
			/*
				boxの要素。表示位置が0固定されたコンテナになっている。
				ここで、このboxedContainerの子要素を列挙する。

				幅が上位から決定されていて、このビューに何が入ろうと幅は変化しない。
				高さに関しては、内容のレイアウト結果に応じて変化する。
			 */
			
			var children = boxedContainer.GetChildren();
			var childViewCursor = new ViewCursor(0, 0, size.x, size.y);

			for (var i = 0; i < children.Count; i++) {
				var cor = DoLayout(
					boxedContainer, 
					childViewCursor, 
					(type, newChild) => {
						Debug.LogError("type:" + type);
						children.Insert(i + 1, newChild);
					}
				);
				
				while (cor.MoveNext()) {
					yield return null;
				}

				// 更新
				childViewCursor = cor.Current;
				
				if (childViewCursor.offsetX + childViewCursor.viewWidth < size.x) {
					continue;
				}

				if (childViewCursor.offsetX + childViewCursor.viewWidth <= size.x) {
					Debug.LogError("改行して次！");
					continue;
				}

				if (size.x < childViewCursor.offsetX + childViewCursor.viewWidth) {
					// 長さが超えてるので、
				}

				/*
					子はカスタムタグコンテナか、コンテナか、コンテンツ。それらのミックスが入る。なるほど。
					幅が固定されているので、常にliningが走っている状態になる。

					例えばここにくる全てのchildがcontentとかだと、ライニングは一定の規模で発生する。
				 */

			}

			Debug.LogError("高さの合計値を計算して、元のheightとどっちが大きいか比較して返す。今は適当。");
			yield return new ViewCursor(0, 0, size.x, size.y);
		}

		/**
			テキストコンテンツのレイアウトを行う。
			もしテキストが複数行に渡る場合、最終行だけを新規コンテンツとして上位に返す。
		 */
		private IEnumerator<ViewCursor> LayoutTextContent (ParsedTree textTree, string text, ViewCursor textViewCursor, Action<InsertType, ParsedTree> insertion) {
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

			Debug.LogWarning("適当な高さをどうやって与えようか考え中。10000は適当。");
			
			// set content to prefab.
			
			var generator = new TextGenerator();
			
			textComponent.text = text;
			var setting = textComponent.GetGenerationSettings(new Vector2(textViewCursor.viewWidth, 10000));
			generator.Populate(text, setting);

			using (new Lock(textComponent, generator)) {
				// この時点で、複数行に分かれるんだけど、最後の行のみ分離する必要がある。
				var lineCount = generator.lineCount;
				Debug.LogError("lineCount:" + lineCount);
				Debug.LogError("width:" + textComponent.preferredWidth);// この部分が66になるのが正しいので、最終行が66で終わるのが正しい、という感じのテストを組むか。
				
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

				if (isStartAtZeroOffset) {
					if (isMultilined) {
						// 複数行が頭から出ている状態で、改行を含んでいる。最終行が中途半端なところにあるのが確定しているので、切り離して別コンテンツとして処理する必要がある。
						var bodyContent = text.Substring(0, generator.lines[generator.lineCount-1].startCharIdx);

						// 最終行
						var lastLineContent = text.Substring(generator.lines[generator.lineCount-1].startCharIdx);

						// 最終行を分割して送り出す。追加されたコンテンツを改行後に処理する。
						var nextLineContent = new ParsedTree(lastLineContent, textTree.rawTagName, textTree.prefabName);
						insertion(InsertType.InsertContentToNextLine, nextLineContent);

						// 最終行以外はハコ型に収まった状態なので、ハコとして出力する。

						// 最終一つ前までの高さを出して、
						var totalHeight = 0;
						for (var i = 0; i < generator.lineCount-1; i++) {
							var line = generator.lines[i];
							totalHeight += line.height;
						}

						// このビューのポジションとしてセット
						var newViewCursor = textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, textComponent.preferredWidth, totalHeight);
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
						// 複数行が途中から出ている状態で、まず折り返しているところまでを分離して、後続の文章を新規にstringとしてinsertする。
						var currentLineContent = text.Substring(0, generator.lines[1].startCharIdx);
						textTree.keyValueStore[Attribute._CONTENT] = currentLineContent;

						var restContent = text.Substring(generator.lines[1].startCharIdx);
						var nextLineContent = new ParsedTree(restContent, textTree.rawTagName, textTree.prefabName);

						// 次のコンテンツを新しい行から開始する。
						insertion(InsertType.InsertContentToNextLine, nextLineContent);

						var width = textComponent.preferredWidth;
						var height = generator.lines[0].height;
						var newViewCursor = textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, textComponent.preferredWidth, height);
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