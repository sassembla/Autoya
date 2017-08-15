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
    
    public enum TreeType {
        NotFound,
        Container,
        Content_Text,
        Content_Img,
        Content_CRLF,
        CustomLayer,
        CustomBox,
        CustomEmptyLayer,
    }

    public class ParsedTree : TagTree {
        public List<ParseError> errors = new List<ParseError>();
    }

    /**
        tree structure.
     */
    public class TagTree {
        // tree params.
        public readonly string id;
        private List<TagTree> _children = new List<TagTree>();
        
        // tag params.
        public readonly int tagValue;
        public readonly AttributeKVs keyValueStore;
        public readonly TreeType treeType;

        public bool hidden {
            get; private set;
        }

        // レイアウト処理
        public float offsetX;
        public float offsetY;
        public float viewWidth;
        public float viewHeight;

        public TagTree () {
            this.id = Guid.NewGuid().ToString();
            this.tagValue = (int)HTMLTag._ROOT;
            this.keyValueStore = new AttributeKVs();
            this.treeType = TreeType.Container;
        }

        public TagTree (string textContent, int baseTagValue) {// as text_content.
            this.id = Guid.NewGuid().ToString();
            this.tagValue = baseTagValue;
            
            this.keyValueStore = new AttributeKVs();
            keyValueStore[HTMLAttribute._CONTENT] = textContent;
            
            this.treeType = TreeType.Content_Text;
        }

        public TagTree (string baseId, string textContent, int baseTagValue) {// as inserted text_content.
            this.id = baseId + ".";
            this.tagValue = baseTagValue;
            
            this.keyValueStore = new AttributeKVs();
            keyValueStore[HTMLAttribute._CONTENT] = textContent;
            
            this.treeType = TreeType.Content_Text;
        }

        public TagTree (int parsedTag, AttributeKVs kv, TreeType treeType) {
            this.id = Guid.NewGuid().ToString();
            this.tagValue = parsedTag;
            this.keyValueStore = kv;
            this.treeType = treeType;

            if (kv.ContainsKey(HTMLAttribute.HIDDEN) && kv[HTMLAttribute.HIDDEN] as string == "true") {
                hidden = true;
            }
        }

        public void ShowOrHide () {
            hidden = !hidden;

            if (hidden) {
                SetHidePos();
            }
        }
        public void SetHidePos () {
            offsetX = 0;
            offsetY = 0;
            viewWidth = 0;
            viewHeight = 0;
        }

        public ViewCursor SetPos (float offsetX, float offsetY, float viewWidth, float viewHeight) {
            this.offsetX = offsetX;
            this.offsetY = offsetY;
            this.viewWidth = viewWidth;
            this.viewHeight = viewHeight;
            return new ViewCursor(offsetX, offsetY, viewWidth, viewHeight);
        }

        public void SetPosFromViewCursor (ViewCursor source) {
            this.offsetX = source.offsetX;
            this.offsetY = source.offsetY;
            this.viewWidth = source.viewWidth;
            this.viewHeight = source.viewHeight;
        }
        
        public void SetParent (TagTree t) {
            t._children.Add(this);

            Debug.LogWarning("そのうちなんか分けたほうがいいかも。");
            // inherit specific kv to child if child does not have kv.
            if (this.treeType == TreeType.Content_Text && t.keyValueStore.ContainsKey(HTMLAttribute.HREF)) {
                this.keyValueStore[HTMLAttribute.HREF] = t.keyValueStore[HTMLAttribute.HREF];
            }
		}

        public List<TagTree> GetChildren () {
			return _children;
		}


        public void AddChildren (TagTree[] children) {
            this._children.AddRange(children);
        }

        public void RemoveChild (TagTree child) {
            this._children.Remove(child);
        }

        public void ReplaceChildrenToBox (TagTree[] oldTrees, TagTree newTree) {
            foreach (var oldTree in oldTrees) {
                this._children.Remove(oldTree);
            }

            this._children.Add(newTree);
        }

        public string ShowContent() {
            return this.treeType.ToString();
        }


        /**
            画面幅、高さから、uGUIの計算を行って実際のレイアウト時のパラメータを算出する。
         */
        public static Rect GetChildViewRectFromParentRectTrans (float parentWidth, float parentHeight, BoxPos pos) {
            // アンカーからwidthとheightを出す。
            var anchorWidth = (parentWidth * pos.anchorMin.x) + (parentWidth * (1 - pos.anchorMax.x));
            var anchorHeight = (parentHeight * pos.anchorMin.y) + (parentHeight * (1 - pos.anchorMax.y));

            var viewWidth = parentWidth - anchorWidth - pos.offsetMin.x + pos.offsetMax.x;
            var viewHeight = parentHeight - anchorHeight - pos.offsetMin.y + pos.offsetMax.y;

            // 左上原点を出す。
            var offsetX = (parentWidth * pos.anchorMin.x) + pos.offsetMin.x;
            var offsetY = (parentHeight * (1-pos.anchorMax.y)) - (pos.offsetMax.y);

            return new Rect(offsetX, offsetY, viewWidth, viewHeight);
        }

         public static Vector2 AnchoredPositionOf (TagTree tree) {
            return new Vector2(tree.offsetX, -tree.offsetY);
        }

        public static Vector2 SizeDeltaOf (TagTree tree) {
            return new Vector2(tree.viewWidth, tree.viewHeight);
        }

        /**
            ・!hiddenなtreeのid列挙
            ・レイアウト変更をする予定なので、InsertedTreeの解消
         */
        public static string[] CorrectTrees (TagTree rootTree) {
            var usingIds = new List<string>();
			CorrectRecursive(rootTree, usingIds);
            return usingIds.ToArray();
		}

        private static void CorrectRecursive (TagTree tree, List<string> usingIds) {
            var isUsing = !tree.hidden;

            if (isUsing) {
                usingIds.Add(tree.id);
            }

            var children = tree.GetChildren();
            
            /*
                前方に元tree、後方に挿入treeがある場合があるので、
                childrenを逆にした配列を用意して畳み込みを行う。
             */
            var removeTargets = new List<TagTree>();
            foreach (var reverted in children.AsEnumerable().Reverse()) {
                CorrectRecursive(reverted, usingIds);

                if (reverted is InsertedTree) {
                    var insertedTree = reverted as InsertedTree;
                    var baseTree = insertedTree.parentTree;

                    // merge contents to base.
                    baseTree.keyValueStore[HTMLAttribute._CONTENT] = baseTree.keyValueStore[HTMLAttribute._CONTENT] as string + insertedTree.keyValueStore[HTMLAttribute._CONTENT] as string;
                    
                    removeTargets.Add(insertedTree);
                }
            }
            
            foreach (var removeTarget in removeTargets) {
                tree.RemoveChild(removeTarget);
            }
        }
    }

    public class InsertedTree : TagTree {
        public readonly TagTree parentTree;
        public InsertedTree (TagTree baseTree, string textContent, int baseTag) : base(baseTree.id, textContent, baseTag) {
            this.parentTree = baseTree;
        }
    }
}