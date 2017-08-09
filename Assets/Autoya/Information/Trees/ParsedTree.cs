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
        Container,
        Content_Text,
        Content_Img,
        Content_Empty,
        CustomLayer,
        CustomBox,
        CustomEmptyLayer,
    }
    /**
        parsed tree structure.
     */
    public class ParsedTree {
        // tree params.
        private List<ParsedTree> _children = new List<ParsedTree>();
        
        // tag params.
        public readonly int parsedTag;
        public readonly AttributeKVs keyValueStore;
        public readonly TreeType treeType;

        private bool hidden = false;

        public void ShowOrHide () {
            Debug.LogError("hidden:" + hidden);
            hidden = !hidden;
            Debug.LogError("after hidden:" + hidden);
            
            if (hidden) {
                SetHide();
            }
        }
        public void SetHide () {
            hidden = true;

            offsetX = 0;
            offsetY = 0;
            viewWidth = 0;
            viewHeight = 0;
        }

        public bool IsHidden () {
            return hidden;
        }

        // レイアウト処理
        public float offsetX;
        public float offsetY;
        public float viewWidth;
        public float viewHeight;

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

        public ParsedTree () {
            this.parsedTag = (int)HtmlTag._ROOT;
            this.keyValueStore = new AttributeKVs();
            this.treeType = TreeType.Container;
        }

        public ParsedTree (string textContent, int baseTag) {// as text_content.
            this.parsedTag = baseTag;
            
            this.keyValueStore = new AttributeKVs();
            keyValueStore[HTMLAttribute._CONTENT] = textContent;
            
            this.treeType = TreeType.Content_Text;
        }

        public ParsedTree (int parsedTag, AttributeKVs kv, TreeType treeType) {
            this.parsedTag = parsedTag;
            this.keyValueStore = kv;
            this.treeType = treeType;
        }
        
        public void SetParent (ParsedTree t) {
            if (
                t.parsedTag == (int)HtmlTag._ROOT && 
                this.treeType == TreeType.Content_Text
            ) {
                var val = this.keyValueStore[HTMLAttribute._CONTENT];
                throw new Exception("invalid text contains outside of tag. val:" + val);
            }
            
			t._children.Add(this);
		}

        public List<ParsedTree> GetChildren () {
			return _children;
		}


        public void AddChildren (ParsedTree[] children) {
            this._children.AddRange(children);
        }

        public void RemoveChild (ParsedTree child) {
            this._children.Remove(child);
        }
        public void ReplaceChildrenToBox (ParsedTree[] oldTrees, ParsedTree newTree) {
            foreach (var oldTree in oldTrees) {
                this._children.Remove(oldTree);
            }

            this._children.Add(newTree);
        }

        public string ShowContent() {
            return this.treeType.ToString();
        }


        /**
            uGUIのパラメータからRectを出す。
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

         public static Vector2 AnchoredPositionOf (ParsedTree tree) {
            return new Vector2(tree.offsetX, -tree.offsetY);
        }

        public static Vector2 SizeDeltaOf (ParsedTree tree) {
            return new Vector2(tree.viewWidth, tree.viewHeight);
        }

        public static ParsedTree RevertInsertedTree (ParsedTree rootTree) {
			RevertRecursive(rootTree);
            return rootTree;
		}

        private static void RevertRecursive (ParsedTree tree) {
            var children = tree.GetChildren();
            children.Reverse();

            var count = children.Count;
            
            var list = new List<ParsedTree>();
            for (var i = 0; i < count; i++) {
                var child = children[i];
                RevertRecursive(child);

                if (child is InsertedTree) {
                    var insertedTree = child as InsertedTree;
                    var baseTree = insertedTree.parentTree;

                    // merge contents.
                    baseTree.keyValueStore[HTMLAttribute._CONTENT] = baseTree.keyValueStore[HTMLAttribute._CONTENT] as string + insertedTree.keyValueStore[HTMLAttribute._CONTENT] as string;
                    
                    list.Add(insertedTree);
                }
            }

            foreach (var removedInsertedTree in list) {
                tree.RemoveChild(removedInsertedTree);
            }
        }
    }

    public class InsertedTree : ParsedTree {
        public readonly ParsedTree parentTree;
        public InsertedTree (ParsedTree baseTree, string textContent, int baseTag) : base(textContent, baseTag) {
            this.parentTree = baseTree;
        }
    }
}