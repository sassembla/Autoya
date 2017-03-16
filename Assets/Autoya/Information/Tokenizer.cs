using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoyaFramework.Information
{
    public enum Tag {
        NO_TAG_FOUND,
        _CONTENT,
        ROOT,
        H, 
        P, 
        IMG, 
        UL, 
        OL,
        LI, 
        A,
        BR
    }

    public struct ContentWidthAndHeight {
        public float width;
        public int totalHeight;
        public ContentWidthAndHeight (float width, int totalHeight) {
            this.width = width;
            this.totalHeight = totalHeight;
        }
    }
    
    // public class TagPoint {
    //     // public readonly VirtualGameObject vGameObject;

    //     // public readonly int lineIndex;
    //     // public readonly int tagEndPoint;

    //     // public readonly Tag tag;
    //     // public readonly Tag[] depth;

    //     // public readonly string originalTagName;
        
    //     // public TagPoint (int lineIndex, int tagEndPoint, Tag tag, Tag[] depth, string originalTagName) {
    //     //     this.lineIndex = lineIndex;
    //     //     this.tagEndPoint = tagEndPoint;

    //     //     this.tag = tag;
    //     //     this.depth = depth;
    //     //     this.originalTagName = originalTagName;
    //     //     this.vGameObject = new VirtualGameObject(tag, depth);	
    //     // }
    //     // public TagPoint (int lineIndex, Tag tag) {
    //     //     this.lineIndex = lineIndex;
    //     //     this.tag = tag;
    //     // }
    // }

    // public struct TagPointAndContent {
    //     public readonly TagPoint tagPoint;
    //     public readonly string content;
        
    //     public TagPointAndContent (TagPoint tagPoint, string content) {
    //         this.tagPoint = tagPoint;
    //         this.content = content;
    //     }
    //     public TagPointAndContent (TagPoint tagPoint) {
    //         this.tagPoint = tagPoint;
    //         this.content = string.Empty;
    //     }
    // }

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
            var root = new TagPoint2(Tag.ROOT, string.Empty, new Tag[0], new Dictionary<KV_KEY, string>(), string.Empty);
            rootObject = Tokenize(root, source);
        }

        public GameObject Materialize (string viewName, Rect viewport, OnLayoutDelegate onLayoutDel, OnMaterializeDelegate onMaterializeDel) {
            var rootObj = rootObject.MaterializeRoot(viewName, viewport.size, onLayoutDel, onMaterializeDel);
            rootObj.transform.position = viewport.position;
            return rootObj;
        }

        private VirtualGameObject Tokenize (TagPoint2 parentTagPoint, string data) {
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
								var contentTagPoint = new TagPoint2(Tag._CONTENT, parentTagPoint.originalTagName, parentTagPoint.depth.Concat(new Tag[]{Tag._CONTENT}).ToArray(), new Dictionary<KV_KEY, string>(), string.Empty);
								contentTagPoint.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
								contentTagPoint.vGameObject.keyValueStore[KV_KEY.CONTENT] = lineReplacedStr;
								SetPrefabName(contentTagPoint, parentTagPoint.originalTagName);
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
									
									kv = GetAttr(attrStr);
									
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
							var tagPoint = new TagPoint2(tag, parentTagPoint.originalTagName, parentTagPoint.depth.Concat(new Tag[]{tag}).ToArray(), kv, rawTagName);
							tagPoint.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
							SetPrefabName(tagPoint, parentTagPoint.originalTagName);
						} else {
							// Debug.LogError("end tag is not closed yet:" + tag);
							// charIndexから、endTagまでがcontents。
							var endTag = "</" + rawTagName.ToLower() + ">";
							var endTagIndex = data.IndexOf(endTag, charIndex);
							// Debug.LogError("endTagIndex:" + endTagIndex + " endTag:" + endTag);

							if (endTagIndex == -1) {
								throw new Exception("parse error. failed to find end-tag of:" + tag);
							}
							
							var contents = data.Substring(charIndex, endTagIndex - charIndex);
							
							var tagPoint = new TagPoint2(tag, parentTagPoint.originalTagName, parentTagPoint.depth.Concat(new Tag[]{tag}).ToArray(), kv, rawTagName);
							tagPoint.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
							SetPrefabName(tagPoint, parentTagPoint.originalTagName);

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
					var contentTagPoint = new TagPoint2(Tag._CONTENT, parentTagPoint.originalTagName, parentTagPoint.depth.Concat(new Tag[]{Tag._CONTENT}).ToArray(), new Dictionary<KV_KEY, string>(), string.Empty);
					contentTagPoint.vGameObject.transform.SetParent(parentTagPoint.vGameObject.transform);
					contentTagPoint.vGameObject.keyValueStore[KV_KEY.CONTENT] = restStr;
					SetPrefabName(contentTagPoint, parentTagPoint.originalTagName);
				}
			}

            return parentTagPoint.vGameObject;
        }
        
		private Dictionary<KV_KEY, string> GetAttr (string source) {
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
							Debug.LogError("attribute:" + keyStr + " is not supported, e:" + e);
						}
					}
				}
			}
			
			// foreach (var dict in kvDict) {
			// 	Debug.LogError("kv:" + dict.Key + " val:" + dict.Value);
			// }

			return kvDict;
		}

		private class TagPoint2 {
			public readonly VirtualGameObject vGameObject;
			public readonly Tag tag;
			public readonly string parentRawTag;
			public readonly Tag[] depth;
			public readonly string originalTagName;
			public TagPoint2 (Tag tag, string parentRawTag, Tag[] depth, Dictionary<KV_KEY, string> kv, string originalTagName) {
				this.vGameObject = new VirtualGameObject(tag, depth, kv);
				this.tag = tag;
				this.parentRawTag = parentRawTag;
				this.depth = depth;
				this.originalTagName = originalTagName;
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


        // private void AddChildContentToParent (TagPoint parent, TagPoint child, Dictionary<KV_KEY, string> kvs) {
        //     var parentObj = parent.vGameObject;
        //     child.vGameObject.transform.SetParent(parentObj.transform);

        //     // append attribute as kv.
        //     foreach (var kv in kvs) {
        //         child.vGameObject.keyValueStore[kv.Key] = kv.Value;
        //     }

        //     SetPrefabName(child);
        // }

        // private void AddContentToParent (TagPoint parentPoint, string contentOriginal) {
        //     if (contentOriginal.EndsWith(BRTagStr)) {
        //         var content = contentOriginal.Substring(0, contentOriginal.Length - BRTagStr.Length);
        //         AddChildContentWithBR(parentPoint, content, true);
        //     } else {
        //         AddChildContentWithBR(parentPoint, contentOriginal, false);
        //     }

        //     SetPrefabName(parentPoint);
        // }

        // private void AddChildContentWithBR (TagPoint parentPoint, string content, bool endsWithBR) {
        //     var	parentObj = parentPoint.vGameObject;
            
        //     var child = new TagPoint2(parentPoint.lineIndex, parentPoint.tagEndPoint, Tag._CONTENT, parentPoint.depth.Concat(new Tag[]{Tag._CONTENT}).ToArray(), parentPoint.originalTagName + " Content");
        //     child.vGameObject.transform.SetParent(parentObj.transform);
        //     child.vGameObject.keyValueStore[KV_KEY.CONTENT] = content;
        //     child.vGameObject.keyValueStore[KV_KEY.PARENTTAG] = parentPoint.originalTagName;
        //     if (endsWithBR) {
        //         child.vGameObject.keyValueStore[KV_KEY.ENDS_WITH_BR] = "true";
        //     }

        //     SetPrefabName(child);
        // }

        private void SetPrefabName (TagPoint2 tagPoint, string parentOriginalTagName) {
			Debug.LogWarning("綺麗なコードではないので後で書き直す。");
            /*
                set name of required prefab.
                    content -> parent's tag name.

                    parent tag -> tag + container name.

                    single tag -> tag name.
            */
            switch (tagPoint.tag) {
                case Tag._CONTENT: {
                    tagPoint.vGameObject.prefabName = parentOriginalTagName;
					break;
                }
                
                case Tag.H:
                case Tag.P:
                case Tag.A:
                case Tag.UL:
                case Tag.LI: {// these are container.
                    tagPoint.vGameObject.prefabName = tagPoint.originalTagName.ToUpper() + "Container";
					break;
                }

                default: {
                    tagPoint.vGameObject.prefabName = tagPoint.originalTagName.ToUpper();
					break;
                }
            }
        }


        /**
            find tag if exists.
        */
        // private TagPointAndAttr FindStartTag (Tag[] parentDepth, string line, int lineIndex) {
        //     // find <X>something...
        //     if (line.StartsWith("<")) {
        //         var closeIndex = line.IndexOf(">");

        //         if (closeIndex == -1) {
        //             return new TagPointAndAttr(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
        //         }

        //         // check found tag end has closed tag mark or not.
        //         if (line[closeIndex-1] == '/') {
        //             // closed tag detected.
        //             return new TagPointAndAttr(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
        //         }

        //         var originalTagName = string.Empty;
        //         var kvDict = new Dictionary<KV_KEY, string>();
        //         var tagEndPoint = closeIndex;

        //         // not closed tag. contains attr or not.
        //         if (line[closeIndex-1] == '"') {// <tag something="else">
        //             var tagNameAndAttr = ParseAttribute(line, closeIndex);

        //             originalTagName = tagNameAndAttr.tag;
        //             kvDict = tagNameAndAttr.attr;
        //             // var kvs = kvDict.Select(i => i.Key + " " + i.Value).ToArray();
        //             // LogError("originalTagName:" + originalTagName + " contains kv:" + string.Join(", ", kvs));
        //         } else {
        //             originalTagName = line.Substring(1, closeIndex - 1);
        //         }

        //         var tagName = originalTagName;
        //         var numbers = string.Join(string.Empty, originalTagName.ToCharArray().Where(c => Char.IsDigit(c)).Select(t => t.ToString()).ToArray());
                
        //         if (!string.IsNullOrEmpty(numbers) && tagName.EndsWith(numbers)) {
        //             var index = tagName.IndexOf(numbers);
        //             tagName = tagName.Substring(0, index);
        //         }
                
        //         try {
        //             var tagEnum = (Tag)Enum.Parse(typeof(Tag), tagName, true);
        //             return new TagPointAndAttr(new TagPoint(lineIndex, tagEndPoint, tagEnum, parentDepth.Concat(new Tag[]{tagEnum}).ToArray(), originalTagName), kvDict);
        //         } catch {
        //             return new TagPointAndAttr(new TagPoint(lineIndex, Tag.UNKNOWN), kvDict);
        //         }
        //     }
        //     return new TagPointAndAttr(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
        // }



        // private TagNameAndAttr ParseAttribute (string line, int tagCloseCharIndex) {
        //     var tagContent = line.Substring(1, tagCloseCharIndex - 1).Split(' ');

        //     if (tagContent.Length == 0) {
        //         Debug.LogError("なんかやばそう");
        //     }
            
        //     var tagName = tagContent[0];
            
        //     var kvDict = new Dictionary<KV_KEY, string>();
        //     for (var i = 1; i < tagContent.Length; i++) {
        //         var kv = tagContent[i].Split(new char[]{'='}, 2);
                
        //         if (kv.Length < 2) {
        //             continue;
        //         }

        //         var keyStr = kv[0];
        //         try {
        //             var key = (KV_KEY)Enum.Parse(typeof(KV_KEY), keyStr, true);
                        
        //             var val = kv[1].Substring(1, kv[1].Length - (1 + 1));

        //             kvDict[key] = val;
        //         } catch (Exception e) {
        //             Debug.LogError("attribute:" + keyStr + " does not supported, e:" + e);
        //             // return new TagPointAndAttr(new TagPoint(lineIndex, Tag.UNKNOWN), kvDict);
        //         }
        //     }
        //     return new TagNameAndAttr(tagName, kvDict);
        // }


        // private struct TagNameAndAttr {
        //     public string tag;
        //     public Dictionary<KV_KEY, string> attr;
        //     public TagNameAndAttr (string tag, Dictionary<KV_KEY, string> attr) {
        //         this.tag = tag;
        //         this.attr = attr;
        //     }
        // }

        // private TagPointAndContent FindEndTag (TagPoint p, string[] lines, int lineIndex) {
        //     var line = lines[lineIndex];

        //     var endTagStr = p.originalTagName;
        //     var endTag = "</" + endTagStr + ">";

        //     var endTagIndex = -1;
        //     if (p.lineIndex == lineIndex) {
        //         // check from next point to end of start tag.
        //         endTagIndex = line.IndexOf(endTag, 1 + endTagStr.Length + 1);
        //     } else {
        //         endTagIndex = line.IndexOf(endTag);
        //     }
            
        //     if (endTagIndex == -1) {
        //         // no end tag contained.
        //         return new TagPointAndContent(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
        //     }
            

        //     // end tag was found!. get tagged contents from lines.

        //     var contentsStrLines = lines.Where((i,l) => p.lineIndex <= l && l <= lineIndex).ToArray();
            
        //     // remove start tag from start line.
        //     contentsStrLines[0] = contentsStrLines[0].Substring(p.tagEndPoint+1);
            
        //     // remove found end-tag from last line.
        //     contentsStrLines[contentsStrLines.Length-1] = contentsStrLines[contentsStrLines.Length-1].Substring(0, contentsStrLines[contentsStrLines.Length-1].Length - endTag.Length);
            
        //     var contentsStr = string.Join("\n", contentsStrLines);
        //     return new TagPointAndContent(p, contentsStr);
        // }

    }
}