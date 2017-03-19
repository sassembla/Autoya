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

    public struct ContentAndWidthAndHeight {
		public string content;
		public float width;
        public float totalHeight;
        public ContentAndWidthAndHeight (string content, float width, float totalHeight) {
			this.content = content;
			this.width = width;
            this.totalHeight = totalHeight;
        }
    }

    public enum KV_KEY {
        _CONTENT,
        _PARENTTAG,
		_STYLE,

        WIDTH,
        HEIGHT,
        SRC,
        ALT,
        HREF,
		START,
		TITLE
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
            var root = new TagPoint(Tag.ROOT, string.Empty, new Tag[0], new Dictionary<KV_KEY, string>(), string.Empty);
            rootObject = Tokenize(root, source.Replace("\n", string.Empty));
        }

        public GameObject Materialize (string viewName, Rect viewport, OnLayoutDelegate onLayoutDel, OnMaterializeDelegate onMaterializeDel) {
            var rootObj = rootObject.MaterializeRoot(viewName, viewport.size, onLayoutDel, onMaterializeDel);
            rootObj.transform.position = viewport.position;
            return rootObj;
        }

        private VirtualGameObject Tokenize (TagPoint parentTagPoint, string data) {
			Debug.LogError("data:" + data);
			var charIndex = 0;
			var readPoint = 0;
			
            while (true) {
				// consumed.
				if (data.Length <= charIndex) {
					break;
				}

				var chr = data[charIndex];
				// Debug.LogError("chr:" + chr);

				
				if (chr == '<') {
					var foundTag = IsTag(data, charIndex);

					if (foundTag == Tag.NO_TAG_FOUND) {
						// no tag found. go to next char.
						charIndex++;
						continue;
					}

					// Debug.LogError("foundTag:" + foundTag + " cont:" + data.Substring(charIndex));

					if (readPoint < charIndex) {
						// Debug.LogError("readPoint:" + readPoint + " vs charIndex:" + charIndex);
						var length = charIndex - readPoint;
						var str = data.Substring(readPoint, length);
						
						// Debug.LogError("1 str:" + str + " parentTagPoint:" + parentTagPoint.tag + " current tag:" + foundTag);

						if (!string.IsNullOrEmpty(str)) {
							var contentTagPoint = new TagPoint(
								Tag._CONTENT, 
								parentTagPoint.originalTagName, 
								parentTagPoint.depth.Concat(new Tag[]{Tag._CONTENT}).ToArray(), 
								new Dictionary<KV_KEY, string>(), 
								string.Empty
							);
							contentTagPoint.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
							contentTagPoint.vGameObject.keyValueStore[KV_KEY._CONTENT] = str;
						}
					}

					var rawTagName = foundTag.ToString();
					if (foundTag == Tag.H) {
						// get Hx(number). e.g. H -> H1
						rawTagName = rawTagName + data[charIndex + 1/*length ouf '<'*/ + 1/*length of 'H'*/];
					}
					
					// set tag.
					var tag = foundTag;

					// set to next char index. after '<tag'
					charIndex = charIndex + ("<" + rawTagName).Length;
					
					// collect attr and find start-tag end.
					{
						var kv = new Dictionary<KV_KEY, string>();
						switch (data[charIndex]) {
							case ' ': {// <tag [attr]/> or <tag [attr]>
								// Debug.LogError("' ' found at tag:" + tag);
								var tagEndIndex = data.IndexOf(">", charIndex);
								
								var attrStr = data.Substring(charIndex + 1, tagEndIndex - charIndex - 1);
								
								kv = GetAttr(tag, attrStr);
								
								// tag closed point is tagEndIndex. next point is tagEndIndex + 1.
								charIndex = tagEndIndex + 1;
								readPoint = charIndex;

								// Debug.LogError("data[tagEndIndex - 1]:" + data[tagEndIndex - 1]);
								if (data[tagEndIndex - 1] == '/') {// <tag [attr]/>
									// Debug.LogError("-1 is / @tag:" + tag);

									// 閉じタグが見つかっていて、すでにcharIndexがセットできてる
									var tagPoint2 = new TagPoint(tag, parentTagPoint.originalTagName, parentTagPoint.depth.Concat(new Tag[]{tag}).ToArray(), kv, rawTagName);
									tagPoint2.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
									continue;
								}

								// Debug.LogError("not closed tag:" + tag);

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
								
								var tagPoint = new TagPoint(tag, parentTagPoint.originalTagName, parentTagPoint.depth.Concat(new Tag[]{tag}).ToArray(), kv, rawTagName);
								tagPoint.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
								
								// Debug.LogError("contents1:" + contents);
								Tokenize(tagPoint, contents);

								charIndex = endTagIndex + endTag.Length;
								readPoint = charIndex;
								
								charIndex++;
								continue;;
							}
							case '>': {// <tag> start tag is closed.
								// Debug.LogError("> found at tag:" + tag);

								// set to next char.
								charIndex = charIndex + 1;

								/*
									finding end-tag of this tag.
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
								
								var tagPoint = new TagPoint(tag, parentTagPoint.originalTagName, parentTagPoint.depth.Concat(new Tag[]{tag}).ToArray(), kv, rawTagName);
								tagPoint.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
								
								// Debug.LogError("contents2:" + contents);
								Tokenize(tagPoint, contents);

								charIndex = endTagIndex + endTag.Length;
								readPoint = charIndex;
								continue;
							}
							default: {
								throw new Exception("parse error. unknown keyword found:" + data[charIndex] + " at tag:" + tag);
							}
						}
					}
				}
				charIndex++;
            }

			if (readPoint < data.Length) { 
				var restStr = data.Substring(readPoint);
				// Debug.LogError("2 restStr:" + restStr + " parentTagPoint:" + parentTagPoint.tag);
				if (!string.IsNullOrEmpty(restStr)) {
					var contentTagPoint = new TagPoint(Tag._CONTENT, parentTagPoint.originalTagName, parentTagPoint.depth.Concat(new Tag[]{Tag._CONTENT}).ToArray(), new Dictionary<KV_KEY, string>(), string.Empty);
					contentTagPoint.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
					contentTagPoint.vGameObject.keyValueStore[KV_KEY._CONTENT] = restStr;
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
			public TagPoint (Tag tag, string parentRawTag, Tag[] depth, Dictionary<KV_KEY, string> kv, string originalTagName) {
				this.tag = tag;
				this.depth = depth;
				this.originalTagName = originalTagName;

				
				var prefabName = string.Empty;
				switch (tag) {
					case Tag._CONTENT: {
						prefabName = parentRawTag;
						break;
					}

					// these are not container, not content, only parameters.
					case Tag.EM:
					case Tag.STRONG: 
					// {
					// 	// こいつが実体を持たないようにしたい。
					// 	prefabName = Tag.P.ToString();
					// 	kv[KV_KEY._STYLE] = tag.ToString();
						
					// 	break;
					// }

					case Tag.PRE:
					case Tag.BLOCKQUOTE:
					case Tag.CODE:
					case Tag.H:
					case Tag.P:
					case Tag.A:
					case Tag.UL:
					case Tag.LI:
					case Tag.OL: {// these are container.
						prefabName = originalTagName.ToUpper() + "Container";
						break;
					}

					default: {
						prefabName = originalTagName.ToUpper();
						break;
					}
				}
				
				this.vGameObject = new VirtualGameObject(tag, depth, kv, prefabName);
			}
		}

		private Tag IsTag (string data, int index) {
			var length = Tag.NO_TAG_FOUND.ToString().Length;
			if (data.Length - index < length) {
				length = data.Length - index;
			}

			var sample = data.Substring(index, length).ToUpper();
			foreach (var tag in Enum.GetValues(typeof(Tag))) {
				var tagStr = "<" + tag.ToString();

				if (data.Length <= index + tagStr.Length) {
					continue;
				}

				if (sample.StartsWith(tagStr)) {
					return (Tag)tag;
				}
			}

			return Tag.NO_TAG_FOUND;
		}
    }
}