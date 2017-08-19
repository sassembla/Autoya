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
        };

        public IEnumerator Layout (TagTree rootTree, Vector2 view, Action<TagTree> layouted) {
            var viewCursor = new ViewCursor(view);
            
            var cor = DoLayout(rootTree, viewCursor);
            
            while (cor.MoveNext()) {
                if (cor.Current != null) {
                    break;
                }
                yield return null;
            }

            viewCursor = cor.Current;
            
            // ビュー高さ込みのカーソルをセット
            rootTree.SetPosFromViewCursor(viewCursor);
            layouted(rootTree);
        }

        /**
            コンテンツ単位でのレイアウトの起点。ここからtreeTypeごとのレイアウトを実行する。
         */
        private IEnumerator<ViewCursor> DoLayout (TagTree tree, ViewCursor viewCursor, Action<InsertType, TagTree> insertion=null) {
            // Debug.LogError("tree:" + resLoader.GetTagFromValue(tree.tagValue) + " treeType:" + tree.treeType + " viewCursor:" + viewCursor);
            // Debug.LogWarning("まだ実装してない、brとかhrでの改行処理。 実際にはpとかも一緒で、「このコンテンツが終わったら改行する」みたいな属性が必須。区分けとしてはここではないか。＿なんちゃらシリーズと一緒に分けちゃうのもありかな〜");

            var cor = GetCoroutineByTreeType(tree, viewCursor, insertion);

            /*
                もしもtreeがhiddenだった場合でも、のちのち表示するために内容のロードは行う。
                コンテンツへのサイズの指定も0で行う。
                ただし、同期的に読む必要はないため、並列でロードする。
             */
            if (tree.hidden) {
                var loadThenSetHiddenPosCor = SetHiddenPosCoroutine(tree, cor);
                resLoader.LoadParallel(loadThenSetHiddenPosCor);

                var hiddenCursor = ViewCursor.ZeroSizeCursor(viewCursor);
                
                tree.SetPosFromViewCursor(hiddenCursor);
                // Debug.LogError("hidden tree:" + resLoader.GetTagFromValue(tree.tagValue) + " treeType:" + tree.treeType + " viewCursor:" + viewCursor);
                yield return hiddenCursor;
            } else {
                while (cor.MoveNext()) {
                    if (cor.Current != null) {
                        break;
                    }
                    yield return null;
                }

                // Debug.LogError("done layouted tree:" + resLoader.GetTagFromValue(tree.tagValue) + " treeType:" + tree.treeType + " viewCursor:" + cor.Current);
                yield return cor.Current;
            }
        }

        private IEnumerator<ViewCursor> GetCoroutineByTreeType (TagTree tree, ViewCursor viewCursor, Action<InsertType, TagTree> insertion=null) {
            
            switch (tree.treeType) {
                case TreeType.CustomLayer: {
                    return DoLayerLayout(tree, viewCursor);
                }
                case TreeType.CustomEmptyLayer: {
                    return DoEmptyLayerLayout(tree, viewCursor);
                }
                case TreeType.Container: {
                    return DoContainerLayout(tree, viewCursor);
                }
                case TreeType.Content_Img: {
                    return DoImgLayout(tree, viewCursor, insertion);
                }
                case TreeType.Content_Text: {
                    return DoTextLayout(tree, viewCursor, insertion);
                }
                case TreeType.Content_CRLF: {
                    return DoCRLFLayout(tree, viewCursor);
                }
                default: {
                    throw new Exception("unexpected tree type:" + tree.treeType);
                }
            }
        }
        
        /**
            カスタムタグのレイヤーのレイアウトを行う。
            customTagLayer/box/boxContents(layerとか) という構造になっていて、boxはlayer内に必ず規定のポジションでレイアウトされる。
            ここだけ相対的なレイアウトが確実に崩れる。
         */
        private IEnumerator<ViewCursor> DoLayerLayout (TagTree layerTree, ViewCursor parentBoxViewCursor) {
            ViewCursor layerViewCursor = null;
            
            if (!layerTree.keyValueStore.ContainsKey(HTMLAttribute.LAYER_PARENT_TYPE)) {
                // 親がboxではないレイヤーは、親のサイズを使わず、layer自体のprefabのサイズを使うという特例を当てる。
                var size = resLoader.GetUnboxedLayerSize(layerTree.tagValue);
                layerViewCursor = new ViewCursor(parentBoxViewCursor.offsetX, parentBoxViewCursor.offsetY, size.x, size.y);
            } else {
                // 親がboxなので、boxのoffsetYとサイズを継承。offsetXは常に0で来る。継承しない。
                layerViewCursor = new ViewCursor(0, parentBoxViewCursor.offsetY, parentBoxViewCursor.viewWidth, parentBoxViewCursor.viewHeight);
            }
            layerTree.SetPosFromViewCursor(layerViewCursor);

            // Debug.LogWarning("before layerTree:" + resLoader.GetTagFromValue(layerTree.tagValue) + " layerViewCursor:" + layerViewCursor);

            /*
                レイヤーなので、prefabをロードして、原点位置は0,0、
                    サイズは親サイズ、という形で生成する。
                
                ・childlenにboxの中身が含まれている場合(IsContainedThisCustomTag)、childlenの要素を生成する。そうでない要素の場合は生成しない。
                ・この際のchildのサイズは、layerであれば必ずboxのサイズになる。このへんがキモかな。
            */
            var children = layerTree.GetChildren();

            // collisionGroup単位での追加高さ、一番下まで伸びてるやつを基準にする。
            var boxYPosRecords = new Dictionary<float, float>();
            var collisionGrouId = 0;
            float additionalHeight = 0f;

            for (var i = 0; i < children.Count; i++) {
                var boxTree = children[i];

                // Debug.LogError("box tag:" + resLoader.GetTagFromIndex(boxTree.parsedTag) + " boxTree:" + boxTree.treeType);

                /*
                    位置情報はkvに入っているが、親のviewの値を使ってレイアウト後の位置に関する数値を出す。
                */
                var boxRect = boxTree.keyValueStore[HTMLAttribute._BOX] as BoxPos;
                var childBoxViewRect = TagTree.GetChildViewRectFromParentRectTrans(layerViewCursor.viewWidth, layerViewCursor.viewHeight, boxRect);
                
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
                childView = cor.Current;

                // add record.
                var yPos = childView.offsetY + childView.viewHeight;
                boxYPosRecords[yPos] = childView.viewHeight - childBoxViewRect.height;
            }
            
            // 最終グループの追加値をviewの高さに足す
            {
                var tallest = boxYPosRecords.Keys.Max();
                additionalHeight = boxYPosRecords[tallest] + additionalHeight;
            }
            
            layerViewCursor.viewHeight += additionalHeight;

            // Debug.LogWarning("after layerTree:" + resLoader.GetTagFromValue(layerTree.tagValue) + " layerViewCursor:" + layerViewCursor);

            // セット
            layerTree.SetPosFromViewCursor(layerViewCursor);
            yield return layerViewCursor;
        }

        private IEnumerator<ViewCursor> DoEmptyLayerLayout (TagTree emptyLayerTree, ViewCursor viewCursor) {
            var baseViewCursorHeight = viewCursor.viewHeight;

            var childView = ViewCursor.ContainedViewCursor(viewCursor);

            var cor = DoContainerLayout(emptyLayerTree, childView);
            
            while (cor.MoveNext()) {
                if (cor.Current != null) {
                    break;
                }
                yield return null;
            }
            
            var resultViewCursor = cor.Current;
            
            // 伸びるぶんには伸ばす。
            if (resultViewCursor.viewHeight < baseViewCursorHeight) {
                resultViewCursor.viewHeight = baseViewCursorHeight;
            }

            emptyLayerTree.SetPosFromViewCursor(resultViewCursor);

            yield return resultViewCursor;
        }

        private IEnumerator<ViewCursor> DoImgLayout (TagTree imgTree, ViewCursor viewCursor, Action<InsertType, TagTree> insertion=null) {
            var contentViewCursor = viewCursor;
            if (!imgTree.keyValueStore.ContainsKey(HTMLAttribute.SRC)) {
                throw new Exception("image element should define src param.");
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
            contentViewCursor.viewWidth = imageWidth;
            contentViewCursor.viewHeight = imageHeight;

            // 自己のサイズに反映
            imgTree.SetPosFromViewCursor(contentViewCursor);
            // Debug.LogError("contentViewCursor:" + contentViewCursor);

            yield return contentViewCursor;
        }

        private IEnumerator<ViewCursor> DoContainerLayout (TagTree containerTree, ViewCursor containerViewCursor) {
            /*
                子供のタグを整列させる処理。
                横に整列、縦に並ぶ、などが実行される。

                親カーソルから子カーソルを生成。高さに関しては適当。
            */
            var childView = new ViewCursor(0, 0, containerViewCursor.viewWidth - containerViewCursor.offsetX, containerViewCursor.viewHeight);
            var linedElements = new List<TagTree>();
            
            var containerChildren = containerTree.GetChildren();
            var childCount = containerChildren.Count;

            if (childCount == 0) {
                containerTree.SetPosFromViewCursor(containerViewCursor);
                yield return containerViewCursor;
                yield break;
            }

            var rightestPoint = 0f;

            for (var i = 0; i < childCount; i++) {
                var child = containerChildren[i];
                
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
                        if (cor.Current != null) {
                            break;
                        }		
                        yield return null;
                    }
                    
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
                            childView = ViewCursor.NextLine(childView, newLineOffsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);

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
                            // Debug.LogError("コンテンツ挿入での改行");
                            childView = ViewCursor.NextLine(childView, newLineOffsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);
                            // Debug.LogError("child:" + child.tagValue + " done," + child.ShowContent() + " next childView:" + childView);
                            continue;
                        }
                    }

                    /*
                        コンテンツがwidth内に置けた
                     */

                    // hiddenコンテンツ以下が来る場合は想定されてないのが惜しいな、なんかないかな、、ないか、、デバッグ用。crlf以外でheightが0になるコンテンツがあれば、それは異常なので蹴る
                    // if (!child.hidden && child.treeType != TreeType.Content_CRLF && cor.Current.viewHeight == 0) {
                    //     throw new Exception("content height is 0. tag:" + resLoader.GetTagFromValue(child.tagValue) + " treeType:" + child.treeType);
                    // }

                    var layoutedCursor = new ViewCursor(cor.Current);
                    if (rightestPoint < layoutedCursor.offsetX + layoutedCursor.viewWidth) {
                        rightestPoint = layoutedCursor.offsetX + layoutedCursor.viewWidth;
                    }

                    // 次のコンテンツの開始位置をセットする。
                    var nextChildViewCursor = ViewCursor.NextRightCursor(layoutedCursor, containerViewCursor.viewWidth);
                    // Debug.LogError("nextChildViewCursor:" + nextChildViewCursor);

                    // レイアウト直後に次のポイントの開始位置が規定幅を超えているか、改行要素が来た場合、現行の行のライニングを行う。
                    if (child.treeType == TreeType.Content_CRLF) {
                        // 行化
                        var nextLineOffsetY = DoLining(linedElements);

                        // ライン解消
                        linedElements.Clear();
                        // Debug.LogError("crlf over.");

                        // 改行処理
                        childView = ViewCursor.NextLine(childView, nextLineOffsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);
                    } else if (containerViewCursor.viewWidth <= nextChildViewCursor.offsetX) {
                        // 行化
                        var nextLineOffsetY = DoLining(linedElements);

                        // ライン解消
                        linedElements.Clear();
                        // Debug.LogError("over. child:" + resLoader.GetTagFromValue(child.tagValue) + " vs containerViewCursor.viewWidth:" + containerViewCursor.viewWidth + " vs nextChildViewCursor.offsetX:" + nextChildViewCursor.offsetX);

                        // 改行処理
                        childView = ViewCursor.NextLine(childView, nextLineOffsetY, containerViewCursor.viewWidth, containerViewCursor.viewHeight);
                    } else {
                        // 次のchildの開始ポイントを現在のchildの右にセット
                        childView = nextChildViewCursor;
                    }

                    // Debug.LogError("child:" + resLoader.GetTagFromValue(child.tagValue) + " is done," + " next childView:" + childView);
                }

                // 現在の子供のレイアウトが終わっていて、なおかつライン処理、改行が済んでいる。
            }

            var lastY = 0f;

            // 最後の列が存在する場合、整列。
            if (linedElements.Any()) {
                lastY = DoLining(linedElements);
            } else {
                // 存在しない場合、子要素の最後の一つのoffset+height
                var lastChild = containerChildren[containerChildren.Count - 1];
                lastY = lastChild.offsetY + lastChild.viewHeight;
            }

            // Debug.LogError("lastY:" + lastY);

            // このコンテナが入る箱を作成する。0,0,一番最後の子供の下位置(offset+height),うーん、、most rightを出さんといけないか。
            containerViewCursor.viewWidth = rightestPoint;
            containerViewCursor.viewHeight = lastY;
            // Debug.LogError("containerViewCursor:" + containerViewCursor);

            // 自分自身のサイズを規定
            containerTree.SetPosFromViewCursor(containerViewCursor);
            yield return containerViewCursor;
        }

        /**
            テキストコンテンツのレイアウトを行う。
            もしテキストが複数行に渡る場合、最終行だけを新規コンテンツとして上位に返す。
         */
        private IEnumerator<ViewCursor> DoTextLayout (TagTree textTree, ViewCursor textViewCursor, Action<InsertType, TagTree> insertion) {
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
                        textTree.keyValueStore[HTMLAttribute._CONTENT] = bodyContent;

                        // 最終行
                        var lastLineContent = text.Substring(generator.lines[generator.lineCount-1].startCharIdx);

                        // Debug.LogError("lastLineContent:" + lastLineContent);
                        // 最終行を分割して送り出す。追加されたコンテンツを改行後に処理する。
                        var nextLineContent = new InsertedTree(textTree, lastLineContent, textTree.tagValue);
                        insertion(InsertType.InsertContentToNextLine, nextLineContent);

                        // 最終行以外はハコ型に収まった状態なので、ハコとして出力する。

                        // 最終一つ前までの高さを出して、
                        var totalHeight = 0;
                        for (var i = 0; i < generator.lineCount-1; i++) {
                            var line = generator.lines[i];
                            // Debug.LogWarning("この+1がないと実質的な表示用高さが足りなくなるケースがあって、すごく怪しい。");
                            totalHeight += (int)(line.height * textComponent.lineSpacing);
                        }
                        
                        // このビューのポジションとしてセット
                        var newViewCursor = textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, textViewCursor.viewWidth, totalHeight);
                        // Debug.LogError("newViewCursor:" + newViewCursor);
                        yield return newViewCursor;
                    } else {
                        // 行頭の単一行
                        var width = textComponent.preferredWidth;
                        var height = generator.lines[0].height * textComponent.lineSpacing;
                        var newViewCursor = textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, textComponent.preferredWidth, height);
                        // Debug.LogError("行頭の単一行 newViewCursor:" + newViewCursor);
                        yield return newViewCursor;
                    }
                } else {
                    if (isMultilined) {
                        // Debug.LogError("行中での折り返しのある文字ヒット");
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

                        var newViewCursor = textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, currentLineWidth, currentLineHeight);
                        // Debug.LogError("newViewCursor:" + newViewCursor);
                        yield return newViewCursor;
                    } else {
                        // 行の途中に追加された単一行で、いい感じに入った。
                        var width = textComponent.preferredWidth;
                        var height = generator.lines[0].height * textComponent.lineSpacing;
                        
                        var newViewCursor = textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, textComponent.preferredWidth, height);
                        // Debug.LogError("newViewCursor:" + newViewCursor);
                        yield return newViewCursor;
                    }
                }
            }
        }

        private IEnumerator<ViewCursor> DoCRLFLayout (TagTree crlfTree, ViewCursor viewCursor) {
            // return empty size cursor.
            var zeroSizeCursor = ViewCursor.ZeroSizeCursor(viewCursor);

            // set content pos.
            crlfTree.SetPosFromViewCursor(zeroSizeCursor);
            yield return zeroSizeCursor;
        }

        private IEnumerator SetHiddenPosCoroutine (TagTree hiddenTree, IEnumerator<ViewCursor> cor) {
            while (cor.MoveNext()) {
                if (cor.Current != null) {
                    break;
                }
                yield return null;
            }

            hiddenTree.SetHidePos();
        }
        
        /**
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

        /**
            ボックス内部のコンテンツのレイアウトを行う
         */
        private IEnumerator<ViewCursor> LayoutBoxedContents (TagTree boxTree, ViewCursor boxView) {
            // Debug.LogError("boxTree:" + resLoader.GetTagFromValue(boxTree.tagValue) + " boxView:" + boxView);
            
            var containerChildren = boxTree.GetChildren();
            var childCount = containerChildren.Count;

            if (childCount == 0) {
                boxTree.SetPosFromViewCursor(boxView);
                yield return boxView;
                yield break;
            }

            // 内包されたviewCursorを生成する。
            var childView = ViewCursor.ContainedViewCursor(boxView);

            for (var i = 0; i < childCount; i++) {
                var child = containerChildren[i];
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
                childView = ViewCursor.NextLine(cor.Current, cor.Current.offsetY + cor.Current.viewHeight, boxView.viewWidth, boxView.viewHeight);
                
                // 現在の子供のレイアウトが終わっていて、なおかつライン処理、改行が済んでいる。
            }
            
            // Debug.Log("lastChildEndY:" + lastChildEndY + " これが更新されない場合、レイアウトされたパーツにサイズが入ってない。");

            // 最終コンテンツのoffsetを使ってboxの高さをセット
            boxView .viewHeight = childView.offsetY;
            // Debug.LogError("layoutBoxedContent boxView:" + boxView);

            // 自分自身のサイズを再規定
            boxTree.SetPosFromViewCursor(boxView);
            yield return boxView;
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