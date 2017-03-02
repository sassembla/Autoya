using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoyaFramework.Information
{
    public enum Tag {
        NO_TAG_FOUND,
        UNKNOWN,
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
    
    public class TagPoint {
        public readonly VirtualGameObject vGameObject;

        public readonly int lineIndex;
        public readonly int tagEndPoint;

        public readonly Tag tag;
        public readonly Tag[] depth;

        public readonly string originalTagName;
        
        public TagPoint (int lineIndex, int tagEndPoint, Tag tag, Tag[] depth, string originalTagName) {
            this.lineIndex = lineIndex;
            this.tagEndPoint = tagEndPoint;

            this.tag = tag;
            this.depth = depth;
            this.originalTagName = originalTagName;
            this.vGameObject = new VirtualGameObject(tag, depth);	
        }
        public TagPoint (int lineIndex, Tag tag) {
            this.lineIndex = lineIndex;
            this.tag = tag;
        }
    }

    public struct TagPointAndContent {
        public readonly TagPoint tagPoint;
        public readonly string content;
        
        public TagPointAndContent (TagPoint tagPoint, string content) {
            this.tagPoint = tagPoint;
            this.content = content;
        }
        public TagPointAndContent (TagPoint tagPoint) {
            this.tagPoint = tagPoint;
            this.content = string.Empty;
        }
    }

    public enum KV_KEY {
        CONTENT,
        PARENTTAG,

        WIDTH,
        HEIGHT,
        SRC,
        ALT,
        HREF,
        ENDS_WITH_BR
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
            var root = new TagPoint(0, 0, Tag.ROOT, new Tag[0], string.Empty);
            rootObject = Tokenize(root, source);
        }

        public GameObject Materialize (string viewName, Rect viewport, OnLayoutDelegate onLayoutDel, OnMaterializeDelegate onMaterializeDel) {
            var rootObj = rootObject.MaterializeRoot(viewName, viewport.size, onLayoutDel, onMaterializeDel);
            rootObj.transform.position = viewport.position;
            return rootObj;
        }

        private VirtualGameObject Tokenize (TagPoint parentTagPoint, string data) {
            var lines = data.Split('\n');
            
            var index = 0;

            while (true) {
                if (lines.Length <= index) {
                    break;
                }

                var readLine = lines[index];
                if (string.IsNullOrEmpty(readLine)) {
                    index++;
                    continue;
                }

                var foundStartTagPointAndAttrAndOption = FindStartTag(parentTagPoint.depth, lines, index);

                /*
                    no <tag> found.
                */
                if (foundStartTagPointAndAttrAndOption.tagPoint.tag == Tag.NO_TAG_FOUND || foundStartTagPointAndAttrAndOption.tagPoint.tag == Tag.UNKNOWN) {					
                    // detect single closed tag. e,g, <tag something />.
                    var foundSingleTagPointWithAttr = FindSingleTag(parentTagPoint.depth, lines, index);

                    if (foundSingleTagPointWithAttr.tagPoint.tag != Tag.NO_TAG_FOUND && foundSingleTagPointWithAttr.tagPoint.tag != Tag.UNKNOWN) {
                        // closed <tag /> found. add this new closed tag to parent tag.
                        AddChildContentToParent(parentTagPoint, foundSingleTagPointWithAttr.tagPoint, foundSingleTagPointWithAttr.attrs);
                    } else {
                        // not tag contained in this line. this line is just contents of parent tag.
                        AddContentToParent(parentTagPoint, readLine);
                    }
                    
                    index++;
                    continue;
                }
                
                // tag opening found.
                var attr = foundStartTagPointAndAttrAndOption.attrs;
                

                // find end tag.
                while (true) {
                    if (lines.Length <= index) {// 閉じタグが見つからなかったのでたぶんparseException.
                        break;
                    }

                    readLine = lines[index];
                    
                    // find end of current tag.
                    var foundEndTagPointAndContent = FindEndTag(foundStartTagPointAndAttrAndOption.tagPoint, lines, index);
                    if (foundEndTagPointAndContent.tagPoint.tag != Tag.NO_TAG_FOUND && foundEndTagPointAndContent.tagPoint.tag != Tag.UNKNOWN) {
                        // close tag found. set attr to this closed tag.
                        AddChildContentToParent(parentTagPoint, foundEndTagPointAndContent.tagPoint, attr);

                        // content exists. parse recursively.
                        Tokenize(foundEndTagPointAndContent.tagPoint, foundEndTagPointAndContent.content);
                        break;
                    }

                    // end tag is not contained in current line.
                    index++;
                }

                index++;
            }

            return parentTagPoint.vGameObject;
        }
        
        private void AddChildContentToParent (TagPoint parent, TagPoint child, Dictionary<KV_KEY, string> kvs) {
            var parentObj = parent.vGameObject;
            child.vGameObject.transform.SetParent(parentObj.transform);

            // append attribute as kv.
            foreach (var kv in kvs) {
                child.vGameObject.keyValueStore[kv.Key] = kv.Value;
            }

            SetupMaterializeAction(child);
        }

        private const string BRTagStr = "<br />";
        
        private void AddContentToParent (TagPoint parentPoint, string contentOriginal) {
            if (contentOriginal.EndsWith(BRTagStr)) {
                var content = contentOriginal.Substring(0, contentOriginal.Length - BRTagStr.Length);
                AddChildContentWithBR(parentPoint, content, true);
            } else {
                AddChildContentWithBR(parentPoint, contentOriginal);
            }

            SetupMaterializeAction(parentPoint);
        }

        private void AddChildContentWithBR (TagPoint parentPoint, string content, bool endsWithBR=false) {
            var	parentObj = parentPoint.vGameObject;
            
            var child = new TagPoint(parentPoint.lineIndex, parentPoint.tagEndPoint, Tag._CONTENT, parentPoint.depth.Concat(new Tag[]{Tag._CONTENT}).ToArray(), parentPoint.originalTagName + " Content");
            child.vGameObject.transform.SetParent(parentObj.transform);
            child.vGameObject.keyValueStore[KV_KEY.CONTENT] = content;
            child.vGameObject.keyValueStore[KV_KEY.PARENTTAG] = parentPoint.originalTagName;
            if (endsWithBR) {
                child.vGameObject.keyValueStore[KV_KEY.ENDS_WITH_BR] = "true";
            }

            SetupMaterializeAction(child);
        }

        private void SetupMaterializeAction (TagPoint tagPoint) {
            // set only once.
            if (!string.IsNullOrEmpty(tagPoint.vGameObject.prefabName)) {
                return;
            }
            
            var prefabNameCandidate = string.Empty;

            /*
                set name of required prefab.
                    content -> parent's tag name.

                    parent tag -> tag + container name.

                    single tag -> tag name.
            */
            switch (tagPoint.tag) {
                case Tag._CONTENT: {
                    prefabNameCandidate = tagPoint.vGameObject.keyValueStore[KV_KEY.PARENTTAG];
                    break;
                }
                
                case Tag.H:
                case Tag.P:
                case Tag.A:
                case Tag.UL:
                case Tag.LI: {// these are container.
                    prefabNameCandidate = tagPoint.originalTagName.ToUpper() + "Container";
                    break;
                }

                default: {
                    prefabNameCandidate = tagPoint.originalTagName.ToUpper();
                    break;
                }
            }
            
            tagPoint.vGameObject.prefabName = prefabNameCandidate;
        }
        
        private struct TagPointAndAttr {
            public readonly TagPoint tagPoint;
            public readonly Dictionary<KV_KEY, string> attrs;
            public TagPointAndAttr (TagPoint tagPoint, Dictionary<KV_KEY, string> attrs) {
                this.tagPoint = tagPoint;
                this.attrs = attrs;
            }
            
            public TagPointAndAttr (TagPoint tagPoint) {
                this.tagPoint = tagPoint;
                this.attrs = new Dictionary<KV_KEY, string>();
            }
        }


        /**
            find tag if exists.
        */
        private TagPointAndAttr FindStartTag (Tag[] parentDepth, string[] lines, int lineIndex) {
            var line = lines[lineIndex];

            // find <X>something...
            if (line.StartsWith("<")) {
                var closeIndex = line.IndexOf(">");

                if (closeIndex == -1) {
                    return new TagPointAndAttr(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
                }

                // check found tag end has closed tag mark or not.
                if (line[closeIndex-1] == '/') {
                    // closed tag detected.
                    return new TagPointAndAttr(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
                }

                var originalTagName = string.Empty;
                var kvDict = new Dictionary<KV_KEY, string>();
                var tagEndPoint = closeIndex;

                // not closed tag. contains attr or not.
                if (line[closeIndex-1] == '"') {// <tag something="else">
                    var contents = line.Substring(1, closeIndex - 1).Split(' ');
                    originalTagName = contents[0];
                    for (var i = 1; i < contents.Length; i++) {
                        var kv = contents[i].Split(new char[]{'='}, 2);
                        
                        if (kv.Length < 2) {
                            continue;
                        }

                        var keyStr = kv[0];
                        try {
                            var key = (KV_KEY)Enum.Parse(typeof(KV_KEY), keyStr, true);
                            var val = kv[1].Substring(1, kv[1].Length - (1 + 1));

                            kvDict[key] = val;
                        } catch (Exception e) {
                            Debug.LogError("attribute:" + keyStr + " does not supported, e:" + e);
                            return new TagPointAndAttr(new TagPoint(lineIndex, Tag.UNKNOWN), kvDict);
                        }
                    }
                    // var kvs = kvDict.Select(i => i.Key + " " + i.Value).ToArray();
                    // LogError("originalTagName:" + originalTagName + " contains kv:" + string.Join(", ", kvs));
                } else {
                    originalTagName = line.Substring(1, closeIndex - 1);
                }

                var tagName = originalTagName;
                var numbers = string.Join(string.Empty, originalTagName.ToCharArray().Where(c => Char.IsDigit(c)).Select(t => t.ToString()).ToArray());
                
                if (!string.IsNullOrEmpty(numbers) && tagName.EndsWith(numbers)) {
                    var index = tagName.IndexOf(numbers);
                    tagName = tagName.Substring(0, index);
                }
                
                try {
                    var tagEnum = (Tag)Enum.Parse(typeof(Tag), tagName, true);
                    return new TagPointAndAttr(new TagPoint(lineIndex, tagEndPoint, tagEnum, parentDepth.Concat(new Tag[]{tagEnum}).ToArray(), originalTagName), kvDict);
                } catch {
                    return new TagPointAndAttr(new TagPoint(lineIndex, Tag.UNKNOWN), kvDict);
                }
            }
            return new TagPointAndAttr(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
        }

        private TagPointAndAttr FindSingleTag (Tag[] parentDepth, string[] lines, int lineIndex) {
            var line = lines[lineIndex];

            // find <X>something...
            if (line.StartsWith("<")) {
                var closeIndex = line.IndexOf(" />");

                if (closeIndex == -1) {
                    return new TagPointAndAttr(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
                }

                var contents = line.Substring(1, closeIndex - 1).Split(' ');

                if (contents.Length == 0) {
                    return new TagPointAndAttr(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
                }
                
                var tagName = contents[0];
                
                var kvDict = new Dictionary<KV_KEY, string>();
                for (var i = 1; i < contents.Length; i++) {
                    var kv = contents[i].Split(new char[]{'='}, 2);
                    
                    if (kv.Length < 2) {
                        continue;
                    }

                    var keyStr = kv[0];
                    try {
                        var key = (KV_KEY)Enum.Parse(typeof(KV_KEY), keyStr, true);
                            
                        var val = kv[1].Substring(1, kv[1].Length - (1 + 1));

                        kvDict[key] = val;
                    } catch (Exception e) {
                        Debug.LogError("attribute:" + keyStr + " does not supported, e:" + e);
                        return new TagPointAndAttr(new TagPoint(lineIndex, Tag.UNKNOWN), kvDict);
                    }
                }
                // var kvs = kvDict.Select(i => i.Key + " " + i.Value).ToArray();
                // LogError("tag:" + tagName + " contains kv:" + string.Join(", ", kvs));
                
                try {
                    var tagEnum = (Tag)Enum.Parse(typeof(Tag), tagName, true);
                    return new TagPointAndAttr(new TagPoint(lineIndex, -1, tagEnum, parentDepth.Concat(new Tag[]{tagEnum}).ToArray(), tagName), kvDict);
                } catch {
                    return new TagPointAndAttr(new TagPoint(lineIndex, Tag.UNKNOWN), kvDict);
                }
            }
            return new TagPointAndAttr(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
        }

        private TagPointAndContent FindEndTag (TagPoint p, string[] lines, int lineIndex) {
            var line = lines[lineIndex];

            var endTagStr = p.originalTagName;
            var endTag = "</" + endTagStr + ">";

            var endTagIndex = -1;
            if (p.lineIndex == lineIndex) {
                // check from next point to end of start tag.
                endTagIndex = line.IndexOf(endTag, 1 + endTagStr.Length + 1);
            } else {
                endTagIndex = line.IndexOf(endTag);
            }
            
            if (endTagIndex == -1) {
                // no end tag contained.
                return new TagPointAndContent(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
            }
            

            // end tag was found!. get tagged contents from lines.

            var contentsStrLines = lines.Where((i,l) => p.lineIndex <= l && l <= lineIndex).ToArray();
            
            // remove start tag from start line.
            contentsStrLines[0] = contentsStrLines[0].Substring(p.tagEndPoint+1);
            
            // remove found end-tag from last line.
            contentsStrLines[contentsStrLines.Length-1] = contentsStrLines[contentsStrLines.Length-1].Substring(0, contentsStrLines[contentsStrLines.Length-1].Length - endTag.Length);
            
            var contentsStr = string.Join("\n", contentsStrLines);
            return new TagPointAndContent(p, contentsStr);
        }

    }
}