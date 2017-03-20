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
		_UNKNOWN,

        _CONTENT,
        _PARENTTAG,
		
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

					// Debug.LogError("foundTag:" + foundTag + " cont:" + data.Substring(charIndex));

					var readingPointStartIndex = 0;
					var readingPointLength = 0;

					if (readPoint < charIndex) {
						// Debug.LogError("readPoint:" + readPoint + " vs charIndex:" + charIndex);
						var length = charIndex - readPoint;

						// reserve index and length.
						readingPointStartIndex = readPoint;
						readingPointLength = length;
					}

					var rawTagName = foundTag.ToString();
					if (foundTag == Tag.H) {
						// get Hx(number). e.g. H -> H1
						rawTagName = rawTagName + data[charIndex + 1/*length ouf '<'*/ + 1/*length of 'H'*/];
					}


					// set tag.
					var tag = foundTag;

					{// use tempCharIndex, return charIndex.
						// set to next char index. after '<tag'
						var tempCharIndex = charIndex + ("<" + rawTagName).Length;
						var tempReadPoint = readPoint;

						/*
							collect attr and find start-tag end.
						*/
						{
							var kv = new Dictionary<KV_KEY, string>();
							switch (data[tempCharIndex]) {
								case ' ': {// <tag [attr]/> or <tag [attr]>
									var startTagEndIndex = data.IndexOf(">", tempCharIndex);
									
									if (startTagEndIndex == -1) {
										// start tag never close.
										charIndex++;
										continue;
									}

									// Debug.LogError("' ' found at tag:" + tag + " startTagEndIndex:" + startTagEndIndex);
									var attrStr = data.Substring(tempCharIndex + 1, startTagEndIndex - tempCharIndex - 1);
									
									kv = GetAttr(tag, attrStr);
									
									// tag closed point is tagEndIndex. next point is tagEndIndex + 1.
									tempCharIndex = startTagEndIndex + 1;
									tempReadPoint = tempCharIndex;

									// Debug.LogError("data[tagEndIndex - 1]:" + data[tagEndIndex - 1]);

									/*
										single close tag found.
										this tag content is just closed.
									 */
									if (data[startTagEndIndex - 1] == '/') {// <tag [attr]/>
										// Debug.LogError("-1 is / @tag:" + tag);

										// add content before tag.
										if (0 < readingPointLength) {
											var str = data.Substring(readingPointStartIndex, readingPointLength);
								
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

										var tagPoint2 = new TagPoint(tag, parentTagPoint.originalTagName, parentTagPoint.depth.Concat(new Tag[]{tag}).ToArray(), kv, rawTagName);
										tagPoint2.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);

										charIndex = tempCharIndex;
										readPoint = tempReadPoint;
										continue;
									}

									// Debug.LogError("not closed tag:" + tag);

									/*
										finding end-tag of this tag.
									*/
									var endTag = "</" + rawTagName.ToLower() + ">";
									var cascadedStartTagHead = "<" + rawTagName.ToLower();
									
									var endTagIndex = FindEndTag(endTag, cascadedStartTagHead, data, tempCharIndex);
									
									// add content before tag.
									if (0 < readingPointLength) {
										var str = data.Substring(readingPointStartIndex, readingPointLength);
							
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

									var contents = data.Substring(tempCharIndex, endTagIndex - tempCharIndex);
												
									var tagPoint = new TagPoint(tag, parentTagPoint.originalTagName, parentTagPoint.depth.Concat(new Tag[]{tag}).ToArray(), kv, rawTagName);
									tagPoint.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
									
									// Debug.LogError("contents1:" + contents);
									Tokenize(tagPoint, contents);

									tempCharIndex = endTagIndex + endTag.Length;
									tempReadPoint = tempCharIndex;
									
									tempCharIndex++;

									charIndex = tempCharIndex;
									readPoint = tempReadPoint;
									continue;;
								}
								case '>': {// <tag> start tag is closed.
									// set to next char.
									tempCharIndex = tempCharIndex + 1;

									// Debug.LogError("> found at tag:" + tag + " cont:" + data.Substring(tempCharIndex) + "___ finding end tag of tag:" + tag);

									/*
										finding end-tag of this tag.
									*/
									var endTag = "</" + rawTagName.ToLower() + ">";
									var cascadedStartTagHead = "<" + rawTagName.ToLower();

									var endTagIndex = FindEndTag(endTag, cascadedStartTagHead, data, tempCharIndex);

									// add content before tag.
									if (0 < readingPointLength) {
										var str = data.Substring(readingPointStartIndex, readingPointLength);
							
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

									var contents = data.Substring(tempCharIndex, endTagIndex - tempCharIndex);
									
									var tagPoint = new TagPoint(tag, parentTagPoint.originalTagName, parentTagPoint.depth.Concat(new Tag[]{tag}).ToArray(), kv, rawTagName);
									tagPoint.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
									
									// Debug.LogError("contents2:" + contents);
									Tokenize(tagPoint, contents);

									tempCharIndex = endTagIndex + endTag.Length;
									tempReadPoint = tempCharIndex;
									
									charIndex = tempCharIndex;
									readPoint = tempReadPoint;
									continue;
								}
								default: {
									throw new Exception("parse error. unknown keyword found:" + data[charIndex] + " at tag:" + tag);
								}
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

		private Dictionary<KV_KEY, string> GetAttr (Tag tag, string originalAttrSource) {
			// [src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' height='200' /]
			var source = originalAttrSource.TrimEnd('/');
			// Debug.LogError("source:" + source);

			var kvDict = new Dictionary<KV_KEY, string>();
			
			// k1="v1" k2='v2'
			
			var index = 0;
			while (true) {
				if (source.Length <= index) {
					break;
				}

				var eqIndex = source.IndexOf('=', index);
				if (eqIndex == -1) {
					// no "=" found.
					break;
				}

				// = is found.

				var keyStr = source.Substring(index, eqIndex - index);
				KV_KEY keyEnum = KV_KEY._UNKNOWN;
				try {
					keyEnum = (KV_KEY)Enum.Parse(typeof(KV_KEY), keyStr, true);
				} catch (Exception e) {
					throw new Exception("at tag:" + tag + ", found attribute:" + keyStr + " is not supported yet, e:" + e);
				}

				var valStartIndex = eqIndex + 1;

				var delim = source[valStartIndex];
				var valEndIndex = source.IndexOf(delim, valStartIndex + 1);
				if (valEndIndex == -1) {
					// no delim end found.
					throw new Exception("attribute at tag:" + tag + " contains illigal description. source:" + originalAttrSource);
				}

				var val = source.Substring(valStartIndex + 1, valEndIndex - (valStartIndex + 1));

				kvDict[keyEnum] = val;
				// Debug.LogError("keyEnum:" + keyEnum + " val:" + val);

				var spaceIndex = source.IndexOf(" ", valEndIndex);
				if (spaceIndex == -1) {
					break;
				}

				index = spaceIndex + 1;
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