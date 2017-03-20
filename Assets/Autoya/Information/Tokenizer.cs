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
        public int totalHeight;
        public ContentAndWidthAndHeight (string content, float width, int totalHeight) {
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
			// Debug.LogError("data:" + data);
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

					var startTagEndIndex = data.IndexOf(">", charIndex);
					
					if (startTagEndIndex == -1) {
						// start tag never close.
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
					

					
					
					/*
					 collect attr and find start-tag end.
					 */
					{
						var kv = new Dictionary<KV_KEY, string>();
						switch (data[charIndex]) {
							case ' ': {// <tag [attr]/> or <tag [attr]>
								// Debug.LogError("' ' found at tag:" + tag);	
								var attrStr = data.Substring(charIndex + 1, startTagEndIndex - charIndex - 1);
								
								kv = GetAttr(tag, attrStr);
								
								// tag closed point is tagEndIndex. next point is tagEndIndex + 1.
								charIndex = startTagEndIndex + 1;
								readPoint = charIndex;

								// Debug.LogError("data[tagEndIndex - 1]:" + data[tagEndIndex - 1]);
								if (data[startTagEndIndex - 1] == '/') {// <tag [attr]/>
									// Debug.LogError("-1 is / @tag:" + tag);

									// 閉じタグが見つかっていて、すでにcharIndexがセットできてる
									var tagPoint2 = new TagPoint(tag, parentTagPoint.originalTagName, parentTagPoint.depth.Concat(new Tag[]{tag}).ToArray(), kv, rawTagName);
									tagPoint2.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
									continue;
								}

								// Debug.LogError("not closed tag:" + tag);

								/*
									finding end-tag of this tag.
								*/
								var endTag = "</" + rawTagName.ToLower() + ">";
								var cascadedStartTagHead = "<" + rawTagName.ToLower();
								
								var endTagIndex = FindEndTag(endTag, cascadedStartTagHead, data, charIndex);
								
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
								// set to next char.
								charIndex = charIndex + 1;

								// Debug.LogError("> found at tag:" + tag + " cont:" + data.Substring(charIndex) + "___ finding end tag of tag:" + tag);

								/*
									finding end-tag of this tag.
								*/
								var endTag = "</" + rawTagName.ToLower() + ">";
								var cascadedStartTagHead = "<" + rawTagName.ToLower();

								var endTagIndex = FindEndTag(endTag, cascadedStartTagHead, data, charIndex);

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

		private int FindEndTag (string endTagStr, string startTagStr, string data, int offset) {
			var cascadedStartTagIndexies = GetIndexiesOf(startTagStr, data, offset);
			var endTagCandidateIndexies = GetIndexiesOf(endTagStr, data, offset);

			// finding pair of start-end tags.
			for (var i = 0; i < endTagCandidateIndexies.Length; i++) {
				var endIndex = endTagCandidateIndexies[i];

				// if start tag exist, this endTag is possible pair.
				if (i < cascadedStartTagIndexies.Length) {
					// start tag exists, 
					var startIndex = cascadedStartTagIndexies[i];

					// endIndex appears faster than startIndex.
					// endIndex is that we expected.
					if (endIndex < startIndex) {
						return endIndex;
					} else {
						// startIndex is faster than endIndex. maybe they are pair.
						// continue to find.
						continue;
					}
				} else {
					// startIndex is exhausted, found endInex is the result.
					return endIndex;
				}
			}
			
			throw new Exception("parse error. failed to find end tag:" + endTagStr + " after charIndex:" + offset);
		}
        
		private int[] GetIndexiesOf (string tagStr, string data, int offset) {
			var resultList = new List<int>();
			var result = -1;
			while (true) {
				result = data.IndexOf(tagStr, offset);
				if (result == -1) {
					break;
				}

				resultList.Add(result);
				offset = result + 1;
			}
			return resultList.ToArray();
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

					// these are container.
					case Tag.EM:
					case Tag.STRONG:
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