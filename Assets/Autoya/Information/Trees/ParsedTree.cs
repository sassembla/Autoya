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
            keyValueStore[Attribute._CONTENT] = textContent;
            
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
                var val = this.keyValueStore[Attribute._CONTENT];
                throw new Exception("invalid text contains outside of tag. val:" + val);
            }
            
			t._children.Add(this);
		}

        public List<ParsedTree> GetChildren () {
			return _children;
		}

        public bool ContainsChild (int parsedTag) {
            return _children.FindIndex(c => c.parsedTag == parsedTag) != -1;
        }
        public ParsedTree GetChildOfTag (int parsedTag) {
            return _children[_children.FindIndex(c => c.parsedTag == parsedTag)];
        }


        public void AddChild (ParsedTree child) {
            this._children.Add(child);
        }

        public void RemoveChild (ParsedTree child) {
            this._children.Remove(child);
        }
        public void ReplaceChildren (ParsedTree oldTree, ParsedTree newTree) {
            var index = this._children.FindIndex(current => current == oldTree);
            
            if (index == -1) {
                throw new Exception("failed to replace old tree to new tree. oldTree:" + oldTree.parsedTag + " did not found from children of:" + this.parsedTag);
            }

            this._children.RemoveAt(index);
            this._children.Insert(index, newTree);
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
            foreach (var child in tree.GetChildren()) {
                RevertRecursive(child);
            }

            if (tree is InsertedTree) {
                var insertedTree = tree as InsertedTree;
                var parent = insertedTree.parentTree;

                // merge contents.
                parent.keyValueStore[Attribute._CONTENT] = parent.keyValueStore[Attribute._CONTENT] as string + insertedTree.keyValueStore[Attribute._CONTENT] as string;
                parent.RemoveChild(insertedTree);
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