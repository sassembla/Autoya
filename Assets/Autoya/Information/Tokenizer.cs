using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoyaFramework.Information {

    public enum Tag {
        NO_TAG_FOUND,
        _CONTENT,

        ROOT,

		BLOCKQUOTE,
		STRONG,
		CODE,
		PRE,
		IMG, 
		EM,
        UL, 
        OL,
        LI, 
		BR,

		HR,

        H, 
        P, 
        
        A,
        
	}

    public struct ContentWidthAndHeight {
        public float width;
        public int totalHeight;
        public ContentWidthAndHeight (float width, int totalHeight) {
            this.width = width;
            this.totalHeight = totalHeight;
        }
    }

    public enum KV_KEY {
        CONTENT,
        PARENTTAG,

        WIDTH,
        HEIGHT,
        SRC,
        ALT,
        HREF
    }

    public class Padding {
        public float top;
        public float right;
        public float bottom; 
        public float left;

        public Vector2 LeftTopPoint () {
            return new Vector2(left, top);
        }

        /**
            width of padding.
        */
        public float PadWidth () {
            return left + right;
        }

        /**
            hight of padding.
        */
        public float PadHeight () {
            return top + bottom;
        }
    }

    public class Tokenizer {
        public delegate void OnLayoutDelegate (Tag tag, Tag[] depth, Padding padding, Dictionary<KV_KEY, string> keyValue);
        public delegate void OnMaterializeDelegate (GameObject obj, Tag tag, Tag[] depth, Dictionary<KV_KEY, string> keyValue);
		
        private readonly VirtualGameObject rootObject;

        public Tokenizer (string source) {
            var root = new TagPoint(Tag.ROOT, string.Empty, new Tag[0], new Dictionary<KV_KEY, string>(), string.Empty, 0, "dummyEmpty");
            rootObject = Tokenize(root, source);
        }

        public GameObject Materialize (string viewName, Rect viewport, OnLayoutDelegate onLayoutDel, OnMaterializeDelegate onMaterializeDel) {
            var rootObj = rootObject.MaterializeRoot(viewName, viewport.size, onLayoutDel, onMaterializeDel);
            rootObj.transform.position = viewport.position;
            return rootObj;
        }

        private VirtualGameObject Tokenize (TagPoint parentTagPoint, string data) {
			var charIndex = 0;

			var tag = Tag.NO_TAG_FOUND;
			var readPoint = 0;
			
            while (true) {
				
				// consumed.
				if (data.Length <= charIndex) {
					break;
				}

				var chr = data[charIndex];
				
				if (tag == Tag.NO_TAG_FOUND) {
					if (chr == '<') {
						// 
						var current = charIndex;
						var foundTag = IsTag(data, ref charIndex);

						if (foundTag == Tag.NO_TAG_FOUND) {
							charIndex = charIndex + 1;
							continue;
						}

						if (readPoint < current) {
							var str = data.Substring(readPoint, current - readPoint);
							
							var lineReplacedStr = str.Replace("\n", string.Empty);

							if (!string.IsNullOrEmpty(lineReplacedStr)) {
								var contentTagPoint = new TagPoint(
									Tag._CONTENT, 
									parentTagPoint.originalTagName, 
									parentTagPoint.depth.Concat(new Tag[]{Tag._CONTENT}).ToArray(), 
									new Dictionary<KV_KEY, string>(), 
									string.Empty,
									charIndex,
									data
								);
								contentTagPoint.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
								contentTagPoint.vGameObject.keyValueStore[KV_KEY.CONTENT] = lineReplacedStr;
							}
						}

						var rawTagName = foundTag.ToString();
						if (foundTag == Tag.H) {
							// Hx
							rawTagName = rawTagName + data[charIndex].ToString();
							
							// progress 1 charactor.
							charIndex = charIndex + 1;
						}
						
						tag = foundTag;
						// collect attr and find start-tag end.
						
						var tagClosed = false;
						var kv = new Dictionary<KV_KEY, string>();
						{
							switch (data[charIndex]) {
								case ' ': {// <tag [attr]/>
									var tagEndIndex = data.IndexOf(">", charIndex);
									
									var attrStr = data.Substring(charIndex + 1, tagEndIndex - charIndex - 1);
									
									kv = GetAttr(tag, attrStr);
									
									// Debug.LogError("data[tagEndIndex - 1]:" + data[tagEndIndex - 1]);
									if (data[tagEndIndex - 1] == '/') {// <tag [attr]/>
										tagClosed = true;
									}

									charIndex = tagEndIndex + 1;
									break;
								}
								case '>': {// <tag> start tag is closed.
									// set to next char.
									charIndex = charIndex + 1;
									break;
								}
								default: {
									throw new Exception("parse error. unknown keyword found:" + data[charIndex] + " at tag:" + tag);
								}
							}
						}
			
						if (tagClosed) {
							// 閉じタグが見つかっていて、すでにcharIndexがセットできてる
							var tagPoint = new TagPoint(tag, parentTagPoint.originalTagName, parentTagPoint.depth.Concat(new Tag[]{tag}).ToArray(), kv, rawTagName, charIndex, data);
							tagPoint.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
						} else {
							// Debug.LogError("end tag is not closed yet:" + tag);
							
							/*
								finding end-tag of tag.
							 */
							var endTag = "</" + rawTagName.ToLower() + ">";
							var cascadedTagHead = "<" + rawTagName.ToLower();

							var endTagIndex = -2;
							while (true) {
								var cascadedTagHeadIndex = data.IndexOf(cascadedTagHead, charIndex);
								endTagIndex = data.IndexOf(endTag, charIndex);
								
								if (cascadedTagHeadIndex != -1 && endTagIndex != -1 && cascadedTagHeadIndex < endTagIndex) {
									// cascaded start-tag appears before end tag,
									// this end-tag is not pair of finding target.
									// skip to endTagIndex.
									charIndex = endTagIndex;
									continue;
								}

								break;
							}
							
							// Debug.LogError("endTagIndex:" + endTagIndex + " endTag:" + endTag + " data:" + data.Substring(charIndex));

							if (endTagIndex == -1) {
								throw new Exception("parse error. failed to find end-tag of:" + tag + " rawTagName:" + rawTagName);
							}
							
							var contents = data.Substring(charIndex, endTagIndex - charIndex);
							
							var tagPoint = new TagPoint(tag, parentTagPoint.originalTagName, parentTagPoint.depth.Concat(new Tag[]{tag}).ToArray(), kv, rawTagName, charIndex, contents);
							tagPoint.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
							
							// Debug.LogError("contents:" + contents);
							Tokenize(tagPoint, contents);
							
							charIndex = endTagIndex + endTag.Length;
						}

						// Debug.LogError("charIndex:" + charIndex + " vs len:" + data.Length);
						
						// reset.
						tag = Tag.NO_TAG_FOUND;


						// update readpoint.
						readPoint = charIndex + 1;
					}
				}
				charIndex++;
            }

			if (readPoint < data.Length) { 
				var restStr = data.Substring(readPoint);
				var lineReplacedStr = restStr.Replace("\n", string.Empty);
				
				if (!string.IsNullOrEmpty(lineReplacedStr)) {
					var contentTagPoint = new TagPoint(Tag._CONTENT, parentTagPoint.originalTagName, parentTagPoint.depth.Concat(new Tag[]{Tag._CONTENT}).ToArray(), new Dictionary<KV_KEY, string>(), string.Empty, charIndex, lineReplacedStr);
					contentTagPoint.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
					contentTagPoint.vGameObject.keyValueStore[KV_KEY.CONTENT] = restStr;
				}
			}

            return parentTagPoint.vGameObject;
        }
        
		private Dictionary<KV_KEY, string> GetAttr (Tag tag, string source) {
			// Debug.LogError("source:" + source);

			var kvDict = new Dictionary<KV_KEY, string>();
			
			// [src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' height='200' /]
			var attrs = source.Split(' ');
			foreach (var s in attrs) {
				
				if (s.Contains("=")) {
					var keyValueArray = s.Split(new char[]{'='}, 2);
					if (keyValueArray.Length == 2) {
						
						var keyStr = keyValueArray[0];
						try {
							var keyEnum = (KV_KEY)Enum.Parse(typeof(KV_KEY), keyStr, true);
							var val = keyValueArray[1].Substring(1, keyValueArray[1].Length - (1 + 1));// remove head and tail "
							kvDict[keyEnum] = val;
						} catch (Exception e) {
							Debug.LogError("at tag:" + tag + ", found attribute:" + keyStr + " is not supported yet, e:" + e);
						}
					}
				}
			}
			
			// foreach (var dict in kvDict) {
			// 	Debug.LogError("kv:" + dict.Key + " val:" + dict.Value);
			// }

			return kvDict;
		}

		private class TagPoint {
			public readonly VirtualGameObject vGameObject;
			public readonly Tag tag;
			public readonly Tag[] depth;
			public readonly string originalTagName;
			public TagPoint (Tag tag, string parentRawTag, Tag[] depth, Dictionary<KV_KEY, string> kv, string originalTagName, int charIndex, string s) {
				this.tag = tag;
				this.depth = depth;
				this.originalTagName = originalTagName;

				
				var prefabName = string.Empty;
				switch (tag) {
					case Tag._CONTENT: {
						prefabName = parentRawTag;
						break;
					}
					
					case Tag.H:
					case Tag.P:
					case Tag.A:
					case Tag.UL:
					case Tag.LI: {// these are container.
						prefabName = originalTagName.ToUpper() + "Container";
						break;
					}

					default: {
						prefabName = originalTagName.ToUpper();
						break;
					}
				}

				if (string.IsNullOrEmpty(prefabName)) {
					Debug.LogError("charIndex:" + charIndex + " s:" + s + " parentRawTag:" + parentRawTag + " originalTagName:" + originalTagName);
				} 
				
				this.vGameObject = new VirtualGameObject(tag, depth, kv, prefabName);
			}
		}

		private Tag IsTag (string data, ref int index) {
			foreach (var tag in Enum.GetValues(typeof(Tag))) {
				var tagStr = "<" + tag.ToString();

				if (data.Length <= index + tagStr.Length) {
					continue;
				}

				if (data.Substring(index, tagStr.Length).ToUpper() == tagStr) {
					index = index + tagStr.ToString().Length;
					return (Tag)tag;
				}
			}

			// no tag found.
			index = index + 1;
			return Tag.NO_TAG_FOUND;
		}
    }
}