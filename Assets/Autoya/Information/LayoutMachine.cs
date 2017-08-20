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
            var viewCursor = new ViewCursor(view);
            
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
                // Debug.LogError("hidden tree:" + GetTagStr(tree.tagValue) + " treeType:" + tree.treeType + " viewCursor:" + viewCursor);

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
            
            if (!layerTree.keyValueStore.ContainsKey(HTMLAttribute.LAYER_PARENT_TYPE)) {
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

            var childView = ViewCursor.ContainedViewCursor(viewCursor);

            var cor = DoContainerLayout(emptyLayerTree, childView, (type, tree) => {throw new Exception("なんか無視した方がよさそう。");});
            
            while (cor.MoveNext()) {
                if (cor.Current != null) {
                    break;
                }
                yield return null;
            }
            
            var layoutedContainerPos = cor.Current;
            
            /*
                レイアウト済みの高さがlayer本来の高さより低い場合、レイヤー本来の高さを使用する(隙間ができる)
             */
            if (layoutedContainerPos.viewHeight < baseViewCursorHeight) {
                layoutedContainerPos.viewHeight = baseViewCursorHeight;
            }
            
            // treeに位置をセットしてposを返す
            yield return emptyLayerTree.SetPos(layoutedContainerPos);
        }

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
            {
                var nextChildViewCursor = new ViewCursor(0, 0, containerViewCursor.viewWidth, containerViewCursor.viewHeight);
                for (var i = 0; i < childCount; i++) {

                    var child = containerChildren[i];
                    
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

                        /*
                            <このコンテナ発のinsertion発動地点>
                            この時点でinsertionは発生済み or No で、発生している場合、そのタイプによって上位へと伝搬するイベントが変わる。
                         */
                        
                        switch (currentInsertType) {
                            case InsertType.RetryWithNextLine: {
                                // Debug.LogError("テキストコンテンツが0行を叩き出したので、このコンテンツ自体をもう一度レイアウトする。");
                                
                                // 処理の開始時にラインにいれていたものを削除
                                linedElements.Remove(child);

                                Debug.LogError("受け取ったchildと同じ相対位置を持ったコンテンツを足して、その高さを加えてLiningする必要がある。");

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
                                        このコンテナからさらに親のコンテナに対して、折り返しが発生した要素を送りつける。
                                        親コンテナで、このコンテナが行途中から開始したコンテナかどうかを判定、
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
                                        Debug.LogError("viewが変更されてるので、コンテナ自体のviewが変更される。で、それに伴ってinsertしたコンテンツのx位置をズラさないといけない。 newView:" + newView);

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
                                    最終行のコンテンツ高さをどうするかな〜〜面倒くさいな〜〜、、幅さえあってれば文句ないみたいなのをまずやってみるか。
                                    ここで、子のコンテナがこのコンテンツを最後にレイアウトを終えているので、カーソルが弄れる。
                                 */

                                // とりあえずイベント発行元である子コンテナ自身はLiningから除外
                                linedElements.Remove(child);

                                var childContainer = child;
                                var containersLastChild = childContainer.GetChildren().Last();

                                // 次のコンテンツのオフセットをセット。
                                nextChildViewCursor = new ViewCursor(
                                    containersLastChild.viewWidth, 
                                    (childContainer.offsetY + childContainer.viewHeight) - containersLastChild.viewHeight, 
                                    containerViewCursor.viewWidth,
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

                        /*
                            今回の場合、pコンテンツの改行によって末尾が行中で余った状態で出る。
                            で、この情報を上位に伝えることで、後続のコンテンツと同じラインに「pの末尾」を入れ込んで変形させる、ということがしたい感じ。コンテナ内要素の参照渡し。一個上までしか需要がない。ので、コンテナで下がコンテナの時だけ何かすればいいはず。
                            
                            cont
                                [cont, cont]
                            
                                とかが例か。

                            ・複数行化コンテナが出た場合、その処理は上位のコンテナに対して伝えられる。

                                ・前方コンテンツからの頭の複数行巻き込まれ
                                ・広報コンテンツへの尻尾の複数行巻き込み
                                の2つの場合があるんだけど、

                                あたま改行イベントが発生した際、その時点で親のLiningを走らせることが可能。
                                1.あたま改行イベント発生
                                2.親のライニングを部分で発動(親のなかでのliningの末尾要素 = イベント元であるコンテナ)の参照を、あたまコンテンツのサイズで一旦確定したとして実行
                                3.あたまコンテンツの位置 = コンテナのoffsetYのみが変更される。
                                4.帰ってきたら、カーソル位置を次の行からにセット。offsetYの変更を受けられるようにしとく。
                                5.続く


                                しっぽ改行
                                1.しっぽ改行イベントが発生
                                2.胴体はそのままでいいはず。
                                3.しっぽ部分まで終わったら、しっぽがliningに巻き込まれるようにliningRefみたいなのにセットしとく必要がある。
                                4.しっぽのliningが発生したら、コンテナの末尾の位置が変わる(しっぽの位置をもとにviewHeightが変わるだけ)
                         */
                        
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

            var lastY = 0f;

            // 最後の列が存在する場合、整列。(最後の要素が改行要因とかだと最後の列が存在しない場合がある)
            if (linedElements.Any()) {
                lastY = DoLining(linedElements);
            } else {
                // 存在しない場合、子要素の最後の一つのoffset+height
                var lastChild = containerChildren.Last();
                lastY = lastChild.offsetY + lastChild.viewHeight;
            }

            // 自分自身のサイズを規定
            yield return containerTree.SetPos(containerViewCursor.offsetX, containerViewCursor.offsetY, mostRightPoint, lastY);
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
                        // Debug.LogError("行頭での折り返しのある文字ヒット");
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
                        // 行頭の単一行
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

                        // Debug.LogError("newViewCursor:" + newViewCursor);
                        yield return textTree.SetPos(textViewCursor.offsetX, textViewCursor.offsetY, currentLineWidth, currentLineHeight);
                    } else {
                        // 行の途中に追加された単一行で、いい感じに入った。
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