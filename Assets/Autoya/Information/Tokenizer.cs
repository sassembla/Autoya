using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AutoyaFramework.Information {
    public struct ContentWidthAndHeight {
        public float width;
        public int totalHeight;
        public ContentWidthAndHeight (float width, int totalHeight) {
            this.width = width;
            this.totalHeight = totalHeight;
        }
    }
    
    public struct HandlePoint {
        public float nextLeftHandle;
        public float nextTopHandle;

        public float viewWidth;
        public float viewHeight;

        public HandlePoint (float nextLeftHandle, float nextTopHandle, float width, float height) {
            this.nextLeftHandle = nextLeftHandle;
            this.nextTopHandle = nextTopHandle;
            this.viewWidth = width;
            this.viewHeight = height;
        }

        public Vector2 Position () {
            return new Vector2(nextLeftHandle, nextTopHandle);
        }

        public Vector2 Size () {
            return new Vector2(viewWidth, viewHeight);
        }
    }
    
    public class TagPoint {
        public readonly string id;
        public readonly VirtualGameObject vGameObject;

        public readonly int lineIndex;
        public readonly int tagEndPoint;

        public readonly Tag tag;
        public readonly Tag[] depth;

        public readonly string originalTagName;
        
        public TagPoint (int lineIndex, int tagEndPoint, Tag tag, Tag[] depth, string originalTagName) {
            this.id = Guid.NewGuid().ToString();

            this.lineIndex = lineIndex;
            this.tagEndPoint = tagEndPoint;

            this.tag = tag;
            this.depth = depth;
            this.originalTagName = originalTagName;
            this.vGameObject = new VirtualGameObject(tag, depth);	
        }
        public TagPoint (int lineIndex, Tag tag) {
            this.id = Guid.NewGuid().ToString();
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

    public class VirtualGameObject {
        public GameObject _gameObject;
        public string prefabName;

        public InformationRootMonoBehaviour rootInstance;

        public readonly Tag tag;
        public readonly Tag[] depth;
        public Padding padding;
        
        private VirtualTransform vTransform;
        
        public VirtualRectTransform rectTransform = new VirtualRectTransform();

        public VirtualGameObject parent;
        
        public VirtualTransform transform {
            get {
                return vTransform;
            }
        }

        public Vector2 PaddedRightBottomPoint () {
            return rectTransform.anchoredPosition + rectTransform.sizeDelta + new Vector2(padding.PadWidth(), padding.PadHeight());
        }

        public VirtualGameObject (Tag tag, Tag[] depth) {
            this.tag = tag;
            this.depth = depth;
            this.padding = new Padding();
            this.vTransform = new VirtualTransform(this);
        }

        public VirtualGameObject GetRootGameObject () {
            if (vTransform.Parent() != null) {
                return vTransform.Parent().GetRootGameObject();
            }

            // no parent. return this vGameObject.
            return this;
        }

        public Dictionary<KV_KEY, string> keyValueStore = new Dictionary<KV_KEY, string>();

        private ContentWidthAndHeight GetContentWidthAndHeight (string prefabName, string text, float contentWidth, float contentHeight) {
            // このあたりをhttpリクエストに乗っけるようなことができるとなおいいのだろうか。AssetBundleともちょっと違う何か、的な。
            /*
                ・Resourcesに置ける
                ・AssetBundle化できる
            */
            var prefab = LoadPrefab(prefabName);
            var textComponent = prefab.GetComponent<Text>();
            
            // set content height.
            return CalculateTextContent(textComponent, text, new Vector2(contentWidth, contentHeight));
        }

        private ContentWidthAndHeight CalculateTextContent (Text textComponent, string text, Vector2 sizeDelta) {
            textComponent.text = text;

            var generator = new TextGenerator();
            generator.Populate(textComponent.text, textComponent.GetGenerationSettings(sizeDelta));

            var height = 0;
            foreach(var l in generator.lines){
                // LogError("ch topY:" + l.topY);
                // LogError("ch index:" + l.startCharIdx);
                // LogError("ch height:" + l.height);
                height += l.height;
            }
            
            var width = textComponent.preferredWidth;

            if (1 < generator.lines.Count) {
                width = sizeDelta.x;
            }

            // reset.
            textComponent.text = string.Empty;

            return new ContentWidthAndHeight(width, height);
        }
        
        private HandlePoint LayoutTagContent(string prefabName, Tag tag, HandlePoint contentHandlePoint) {
            var rectTrans = rectTransform;

            // set y start pos.
            rectTrans.anchoredPosition = new Vector2(rectTrans.anchoredPosition.x + contentHandlePoint.nextLeftHandle, rectTrans.anchoredPosition.y + contentHandlePoint.nextTopHandle);
            
            var contentWidth = 0f;
            var contentHeight = 0f;

            // set kv.
            switch (tag) {
                case Tag.A: {
                    // do nothing.
                    break;
                }
                case Tag.IMG: {
                    // set basic size from prefab.
                    var prefab = LoadPrefab(prefabName);
                    if (prefab != null) {
                        var rectTransform = prefab.GetComponent<RectTransform>();
                        contentWidth = rectTransform.sizeDelta.x;
                        contentHeight = rectTransform.sizeDelta.y;
                    }

                    foreach (var kv in this.keyValueStore) {
                        var key = kv.Key;
                        switch (key) {
                            case KV_KEY.WIDTH: {
                                var width = Convert.ToInt32(kv.Value);;
                                contentWidth = width;
                                break;
                            }
                            case KV_KEY.HEIGHT: {
                                var height = Convert.ToInt32(kv.Value);
                                contentHeight = height;
                                break;
                            }
                            case KV_KEY.SRC: {
                                // ignore on layout.
                                break;
                            }
                            case KV_KEY.ALT: {
                                // do nothing yet.
                                break;
                            }
                            default: {
                                break;
                            }
                        }
                    }
                    break;
                }
                
                case Tag._CONTENT: {
                    // set text if exist.
                    foreach (var kvs in this.keyValueStore) {
                        var key = kvs.Key;
                        switch (key) {
                            case KV_KEY.CONTENT: {
                                var text = kvs.Value;
                        
                                var contentWidthAndHeight = GetContentWidthAndHeight(prefabName, text, contentHandlePoint.viewWidth, contentHandlePoint.viewHeight);

                                contentWidth = contentWidthAndHeight.width;
                                contentHeight = contentWidthAndHeight.totalHeight;
                                break;
                            }
                            default: {
                                // ignore.
                                break;
                            }
                        }
                    }
                    
                    break;
                }
                
                default: {
                    // do nothing.
                    break;
                }
            }

            // set content size.
            rectTrans.sizeDelta = new Vector2(contentWidth, contentHeight);
            return contentHandlePoint;
        }
        
        /**
            return generated game object.
        */
        public GameObject MaterializeRoot (string viewName, Vector2 viewPort, Tokenizer.OnLayoutDelegate onLayoutDel, Tokenizer.OnMaterializeDelegate onMaterializeDel) {
            var rootHandlePoint = new HandlePoint(0, 0, viewPort.x, viewPort.y);

            // 事前計算、ここでコンテンツの一覧を返すようにすればいいかな。要素単位で。
            Layout(this, rootHandlePoint, onLayoutDel);


            this._gameObject = new GameObject(viewName + Tag.ROOT.ToString());
            
            this.rootInstance = this._gameObject.AddComponent<InformationRootMonoBehaviour>();
            var rectTrans = this._gameObject.AddComponent<RectTransform>();
            rectTrans.anchorMin = Vector2.up;
            rectTrans.anchorMax = Vector2.up;
            rectTrans.pivot = Vector2.up;
            rectTrans.position = Vector2.zero;
            rectTrans.sizeDelta = viewPort;
            
            // 範囲指定してGOを充てる、ということがしたい。
            Materialize(this, onMaterializeDel);

            return this._gameObject;
        }
        
        /**
            layout contents.
        */
        private HandlePoint Layout (VirtualGameObject parent, HandlePoint handlePoint, Tokenizer.OnLayoutDelegate onLayoutDel) {
            switch (this.tag) {
                case Tag.ROOT: {
                    // do nothing.
                    break;
                }
                default: {
                    handlePoint = LayoutTagContent(prefabName, tag, handlePoint);
                    break;
                }
            }

            // parent layout is done. will be resize by child, then padding.

            var childlen = this.transform.GetChildlen();
            
            // layout -> resize -> padding of childlen.
            if (0 < childlen.Count) {
                var layoutLine = new List<VirtualGameObject>();

                foreach (var child in childlen) {
                    handlePoint = child.Layout(this, handlePoint, onLayoutDel);
                    
                    // 子のタグによって、layoutLineに加えなくてもいい、という感じ。現在のtagがPで、子供が_Contentな場合のみ、という。
                    if (this.tag == Tag.P) {
                        switch (child.tag) {
                            case Tag.IMG:
                            case Tag._CONTENT: {
                                // pass.
                                break;
                            }
                            default: {
                                Debug.LogError("あーーここにLIとかが。 child.tag:" + child.tag);
                                break;
                            }
                        }
                    } else {
                        continue;
                    }

                    // width over.
                    if (handlePoint.viewWidth < handlePoint.nextLeftHandle) {
                        if (0 < layoutLine.Count) {
                            handlePoint = SortByLayoutLine(layoutLine, handlePoint);

                            // forget current line.
                            layoutLine.Clear();

                            // move current child content to next line head.
                            child.rectTransform.anchoredPosition = new Vector2(handlePoint.nextLeftHandle + child.padding.left, handlePoint.nextTopHandle + child.padding.top);
                    
                            // set next handle.
                            handlePoint.nextLeftHandle = handlePoint.nextLeftHandle + child.padding.left + child.rectTransform.sizeDelta.x + child.padding.right;
                        }
                    }

                    // in viewpoint width.

                    layoutLine.Add(child);

                    // if <br /> is contained.
                    if (child.keyValueStore.ContainsKey(KV_KEY.ENDS_WITH_BR)) {
                        handlePoint = SortByLayoutLine(layoutLine, handlePoint);

                        // forget current line.
                        layoutLine.Clear();

                        // set next line.
                        handlePoint.nextLeftHandle = 0;
                        handlePoint.nextTopHandle = child.PaddedRightBottomPoint().y;
                    }
                }

                // if layoutLine content is exist, put all in 1 line.
                if (0 < layoutLine.Count) {
                    handlePoint = SortByLayoutLine(layoutLine, handlePoint);
                }
                
                // set parent size to wrapping childlen.
                {
                    var rightBottomPoint = Vector2.zero;
                    // fit most large bottom-right point. largest point of width and y.
                    foreach (var child in childlen) {
                        var paddedRightBottomPoint = child.PaddedRightBottomPoint();

                        if (rightBottomPoint.x < paddedRightBottomPoint.x) {
                            rightBottomPoint.x = paddedRightBottomPoint.x;
                        }
                        if (rightBottomPoint.y < paddedRightBottomPoint.y) {
                            rightBottomPoint.y = paddedRightBottomPoint.y;
                        }
                    }

                    // fit size to wrap all child contents.
                    rectTransform.sizeDelta = rightBottomPoint - rectTransform.anchoredPosition;
                }

                // layout and padding and orientation of child tags are done.
            }
            
            /*
                set padding if need.
                default padding is 0.
            */
            onLayoutDel(this.tag, this.depth, this.padding, new Dictionary<KV_KEY, string>(this.keyValueStore));

            /*
                adopt padding to this content.
            */
            {
                // translate anchor position of content.(child follows parent.)
                rectTransform.anchoredPosition += padding.LeftTopPoint();
                
                handlePoint.nextLeftHandle += padding.PadWidth();
                handlePoint.nextTopHandle += padding.PadHeight();
            }

            /*
                set next left-top point by this tag && the parent tag kind.
            */
            switch (parent.tag) {
                case Tag.H:
                case Tag.P: {
                    // next content is planned to layout to the next of this content.
                    handlePoint.nextLeftHandle += this.rectTransform.sizeDelta.x + this.padding.PadWidth();
                    break;
                }
                case Tag.UL:
                case Tag.OL:
                case Tag.ROOT: {
                    // CRLF
                    handlePoint.nextLeftHandle = 0;
                    handlePoint.nextTopHandle = this.rectTransform.anchoredPosition.y + this.rectTransform.sizeDelta.y + this.padding.PadHeight();
                    break;
                }
            }
            
            return handlePoint;
        }

        /**
            create line of contents -> sort all content by base line.
        */
        private HandlePoint SortByLayoutLine (List<VirtualGameObject> layoutLine, HandlePoint handlePoint) {
            // find tallest content in layoutLine.
            var targetHeightObjArray = layoutLine.OrderByDescending(c => c.rectTransform.sizeDelta.y + c.padding.PadHeight()).ToArray();
            
            if (0 < targetHeightObjArray.Length) {
                var tallestContent = targetHeightObjArray[0];
                
                // get tallest padded height. this will be this layoutLine's bottom line.
                var paddedHighestHeightInLine = tallestContent.rectTransform.sizeDelta.y + tallestContent.padding.PadHeight();
            
                // other child content will be moved.
                foreach (var childInLine in layoutLine) {
                    if (childInLine == tallestContent) {// ignore tallest content itself.
                        continue;
                    }

                    var childPaddedHeight = childInLine.rectTransform.sizeDelta.y + childInLine.padding.PadHeight();
                    var heightDiff = paddedHighestHeightInLine - childPaddedHeight;
                    childInLine.rectTransform.anchoredPosition += new Vector2(0, heightDiff);
                }

                // set next line head.
                handlePoint.nextLeftHandle = 0;
                handlePoint.nextTopHandle += paddedHighestHeightInLine;
            }

            return handlePoint;
        }


        private void Materialize (VirtualGameObject parent, Tokenizer.OnMaterializeDelegate onMaterializeDel) {
            switch (this.tag) {
                case Tag.ROOT: {
                    // do nothing.
                    break;
                }
                default: {
                    this._gameObject = MaterializeTagContent(prefabName, tag);
                    this._gameObject.transform.SetParent(parent._gameObject.transform);
                    break;
                }
            }
            
            foreach (var child in this.transform.GetChildlen()) {
                child.Materialize(this, onMaterializeDel);
            }
            
            onMaterializeDel(this._gameObject, this.tag, this.depth, new Dictionary<KV_KEY, string>(this.keyValueStore));
        }

        private GameObject MaterializeTagContent (string prefabName, Tag tag) {
            var prefab = LoadPrefab(prefabName);
            if (prefab == null) {
                return new GameObject("missing prefab:" + prefabName);
            }

            var obj = LoadGameObject(prefab);

            var vRectTrans = rectTransform;

            var rectTrans = obj.GetComponent<RectTransform>();

            // set position. convert layout position to uGUI position system.
            rectTrans.anchoredPosition = new Vector2(vRectTrans.anchoredPosition.x, -vRectTrans.anchoredPosition.y);
            rectTrans.sizeDelta = vRectTrans.sizeDelta;

            // set parameters.
            switch (tag) {
                case Tag.A: {
                    foreach (var kvs in keyValueStore) {
                        var key = kvs.Key;
                        switch (key) {
                            case KV_KEY.HREF: {
                                var href = kvs.Value;

                                // add button component.
                                var rootObject = GetRootGameObject();
                                var rootMBInstance = rootObject.rootInstance;
                                
                                AddButton(obj, () => rootMBInstance.OnLinkTapped(tag, href));
                                break;
                            }
                            default: {
                                // do nothing.
                                break;
                            }
                        }
                    }
                    break;
                }
                case Tag.IMG: {
                    foreach (var kv in keyValueStore) {
                        var key = kv.Key;
                        switch (key) {
                            case KV_KEY.SRC: {
                                var src = kv.Value;
                                
                                // add button component.
                                var rootObject = GetRootGameObject();
                                var rootMBInstance = rootObject.rootInstance;
                                
                                AddButton(obj, () => rootMBInstance.OnImageTapped(tag, src));
                                break;
                            }
                            default: {
                                // do nothing.
                                break;
                            }
                        }
                    }
                    break;
                }
                
                case Tag._CONTENT: {
                    foreach (var kvs in keyValueStore) {
                        switch (kvs.Key) {
                            case KV_KEY.CONTENT:{
                                var text = kvs.Value;
                                if (!string.IsNullOrEmpty(text)) {
                                    var textComponent = obj.GetComponent<Text>();
                                    textComponent.text = text;
                                }
                                break;
                            }
                            case KV_KEY.PARENTTAG: {
                                break;
                            }
                            default: {
                                // ignore.
                                break;
                            }
                        }
                    }
                    
                    break;
                }
                
                default: {
                    // do nothing.
                    break;
                }
            }

            return obj;
        }

        private GameObject LoadPrefab (string prefabName) {
            Debug.LogWarning("辞書にできる");
            return Resources.Load(prefabName) as GameObject;
        }

        private GameObject LoadGameObject (GameObject prefab) {
            Debug.LogWarning("ここを後々、プールからの取得に変える。タグ単位でGameObjectのプールを作るか。");
            return GameObject.Instantiate(prefab);
        }


        private void AddButton (GameObject obj, UnityAction param) {
            var button = obj.GetComponent<Button>();
            if (button == null) {
                button = obj.AddComponent<Button>();
            }

            if (Application.isPlaying) {
                /*
                    this code can set action to button. but it does not appear in editor inspector.
                */
                button.onClick.AddListener(
                    param
                );
            } else {
                try {
                    button.onClick.AddListener(// 現状、エディタでは、Actionをセットする方法がわからん。関数単位で何かを用意すればいけそう = ButtonをPrefabにするとかしとけば行けそう。
                        param
                    );
                    // UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                    // 	button.onClick,
                    // 	() => rootMBInstance.OnImageTapped(tagPoint.tag, src)
                    // );

                    // // 次の書き方で、固定の値をセットすることはできる。エディタにも値が入ってしまう。
                    // インスタンスというか、Prefabを作りまくればいいのか。このパーツのインスタンスを用意して、そこに値オブジェクトを入れて、それが着火する、みたいな。
                    // UnityEngine.Events.UnityAction<String> callback = new UnityEngine.Events.UnityAction<String>(rootMBInstance.OnImageTapped);
                    // UnityEditor.Events.UnityEventTools.AddStringPersistentListener(
                    // 	button.onClick, 
                    // 	callback,
                    // 	src
                    // );
                } catch (Exception e) {
                    Debug.LogError("e:" + e);
                }
            }
        }
    }

    public class VirtualTransform {
        private readonly VirtualGameObject vGameObject;
        private List<VirtualGameObject> _childlen = new List<VirtualGameObject>();
        public VirtualTransform (VirtualGameObject gameObject) {
            this.vGameObject = gameObject;
        }

        public void SetParent (VirtualTransform t) {
            t._childlen.Add(this.vGameObject);
            this.vGameObject.parent = t.vGameObject;
        }
        
        public VirtualGameObject Parent () {
            return this.vGameObject.parent;
        }

        public List<VirtualGameObject> GetChildlen () {
            return _childlen;
        }
    }

    public class VirtualRectTransform {
        public Vector2 anchoredPosition = Vector2.zero;
        public Vector2 sizeDelta = Vector2.zero;
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