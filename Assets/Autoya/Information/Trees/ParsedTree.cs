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
    
    /**
        parsed tree structure.
     */
    public class ParsedTree {
        // tree params.
        private List<ParsedTree> _children = new List<ParsedTree>();
        
        // tag params.
        public readonly int parsedTag;
        public readonly string rawTagName;
        public readonly string prefabName;
        public readonly AttributeKVs keyValueStore;
        public readonly bool isContainer;


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
            this.rawTagName = HtmlTag._ROOT.ToString();
            this.prefabName = HtmlTag._ROOT.ToString();
            this.isContainer = true;
        }

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

        public ParsedTree (string textContent, string rawTagName, string prefabName) {// as text_content.
            this.parsedTag = (int)HtmlTag._TEXT_CONTENT;
            
            this.keyValueStore = new AttributeKVs();
            keyValueStore[Attribute._CONTENT] = textContent;
            
            this.rawTagName = rawTagName;
            this.prefabName = prefabName;
            this.isContainer = false;
        }

        public ParsedTree (int parsedTag, ParsedTree parent, AttributeKVs kv, string rawTagName) {
            this.parsedTag = parsedTag;
            this.keyValueStore = kv;
            this.rawTagName = rawTagName;

            /*
                determine which tag should be loaded.
             */
            switch (parsedTag) {
                // this is content of container. use parnt tag for materialize.
                case (int)HtmlTag._TEXT_CONTENT: {
                    this.prefabName = parent.rawTagName.ToUpper();
                    this.isContainer = false;
                    break;
                }

                // pure value tags,
                case (int)HtmlTag.img:
                case (int)HtmlTag.br:
                case (int)HtmlTag.hr: {
                    this.prefabName =  rawTagName.ToUpper();
                    this.isContainer = false;
                    break;
                }

                // and root tag is container.
                case (int)HtmlTag._ROOT: {
                    throw new Exception("invalid root tag found.");
                }

                // other tags are container.
                default: {
                    Debug.LogWarning("ここで、カスタムコンテンツがここに落ちちゃうのはまずい。カスタムイメージとカスタムテキストのコンテンツをそのうち用意する。");
                    this.prefabName = rawTagName.ToUpper() + InformationConstSettings.NAME_PREFAB_CONTAINER;
                    this.isContainer = true;
                    break;
                }
            }
        }
        
        public void SetParent (ParsedTree t) {
            if (
                t.parsedTag == (int)HtmlTag._ROOT && 
                this.parsedTag == (int)HtmlTag._TEXT_CONTENT
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
                throw new Exception("failed to replace old tree to new tree. oldTree:" + oldTree.rawTagName + " did not found from children of:" + this.rawTagName);
            }

            this._children.RemoveAt(index);
            this._children.Insert(index, newTree);
        }

        public string ShowContent() {
            if (isContainer) return "\"container.\"";

            switch (prefabName) {
                case "IMG": {
                    return "\"img.\"";
                }
                default: {
                    return "\"" + (keyValueStore[Attribute._CONTENT] as string) + "\"";
                }
            }
        }
    }
}