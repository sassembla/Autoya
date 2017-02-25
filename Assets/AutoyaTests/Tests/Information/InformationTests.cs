using Miyamasu;
using MarkdownSharp;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;

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
	A
}

public class InformationTests : MiyamasuTestRunner {
	[MTest] public void ParseSmallMarkdown () {
        var sampleMd = @"
# Autoya
ver 0.8.4

![loading](https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true)
";
var s = @"<img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true' width='100' height='200' />
small, thin framework for Unity−1.  
<img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' height='200' />

small, thin framework for Unity.  
which contains essential game features.

## Features
* Authentication handling
* AssetBundle load/management
* HTTP/TCP/UDP Connection feature
* Maintenance changed handling
* Purchase/IAP feature
* Notification(local/remote)
* Information


## Motivation
Unity already contains these feature's foundation, but actually we need more codes for using it in app.

This framework can help that.

## License
see below.  
[LICENSE](./LICENSE)


## Progress

### automatic Authentication
already implemented.

###AssetBundle list/preload/load
already implemented.

###HTTP/TCP/UDP Connection feature
| Protocol        | Progress     |
| ------------- |:-------------:|
| http/1 | done | 
| http/2 | not yet | 
| tcp      | not yet      | 
| udp	| not yet      |  


###app-version/asset-version/server-condition changed handles
already implemented.

###Purchase/IAP flow
already implemented.

###Notification(local/remote)
in 2017 early.

###Information
in 2017 early.


## Tests
implementing.


## Installation
unitypackage is ready!

1. use Autoya.unitypackage.
2. add Purchase plugin via Unity Services.
3. done!

## Usage
all example usage is in Assets/AutoyaSamples folder.

yes,(2spaces linebreak)  
2s break line will be expressed with <br />.

then,(hard break)
hard break will appear without <br />.

		";
    

        // Create new markdown instance
        Markdown mark = new Markdown();

        // Run parser
        string text = mark.Transform(sampleMd);
        Debug.LogError("text:" + text);

		/*
			次のようなhtmlが手に入るので、
			hX, p, img, ul, li, aとかのタグを見て、それぞれを「行単位の要素」に変形、uGUIの要素に変形できれば良さそう。
			tokenizer書くか。

			・tokenizerでいろんなコンポーネントに分解
			・tokenizerで分解したコンポーネントを、コンポーネント単位で生成
			・生成したコンポーネントを配置、描画
			
			で、これらの機能は、「N行目から」みたいな指定で描画できるようにしたい。
		*/
		RunOnMainThread(
			() => {
				var tokenizer = new Tokenizer(text);

				// とりあえず全体を生成
				tokenizer.Materialize(
					new Rect(0, 0, 300, 400),
					/*
						干渉ポイント。go自体へのコンポーネント追加や、paddingを変更できる。
					*/
					(tag, depth, padding, kv) => {
						// padding.left += 10;
						// padding.top += 10;
					},
					(go, tag, depth, kv) => {
						
					}
				);
				

				// var childlen = obj.transform.GetChildlen();

				// for (var i = 0; i < tokenizer.ComponentCount(); i++) {
				// 	tokenizer.Materialize(i);
				// 	// これがuGUIのコンポーネントになってる
				// }

				/*
					位置を指定して書き出すことができる(一番上がずれる = cursorか。)
				*/
				// for (var i = 1; i < tokenizer.ComponentCount(); i++) {
				// 	var component = tokenizer.GetComponentAt(i);
				// 	// これがuGUIのコンポーネントになってる
				// }

				// GameObject.DestroyImmediate(obj);// とりあえず消す
			}
		);
	}
	/**
		parse hX, p, img, ul, ol, li, a tags then returns GameObject for GUI.
	*/
	public class Tokenizer {
		public class VirtualGameObject {
			public readonly string id;

			public GameObject _gameObject;

			public InformationRootMonoBehaviour @class;

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
			public VirtualGameObject (Tag tag, Tag[] depth) {
				this.id = Guid.NewGuid().ToString();

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

			public Dictionary<KV_KEY, string> kv = new Dictionary<KV_KEY, string>();

			public Func<HandlePoint, HandlePoint> layoutContents = null;
			
			public Func<GameObject> materialize;
			
			/**
				return generated game object.
			*/
			public GameObject MaterializeRoot (string viewName, Vector2 viewPort, OnLayoutDelegate onLayoutDel, OnMaterializeDelegate onMaterializeDel) {
				var rootHandlePoint = new HandlePoint(0, 0, viewPort.x, viewPort.y);

				this._gameObject = new GameObject(viewName + Tag.ROOT.ToString());
				
				this.@class = this._gameObject.AddComponent<InformationRootMonoBehaviour>();
				var rectTrans = this._gameObject.AddComponent<RectTransform>();
				rectTrans.anchorMin = Vector2.up;
				rectTrans.anchorMax = Vector2.up;
				rectTrans.pivot = Vector2.up;
				rectTrans.position = rootHandlePoint.Position();
				rectTrans.sizeDelta = rootHandlePoint.Size();
				
				// 事前計算、コンストラクト時に持っといていい気がする。ここを軽くするのは大事っぽい。
				Layout(this, rootHandlePoint, onLayoutDel);

				// 範囲指定してGOを充てる、ということがしたい。
				Materialize(this, onMaterializeDel);

				return this._gameObject;
			}
			
			/**
				各コンテンツ位置の事前計算を行う。
			 */
			private HandlePoint Layout (VirtualGameObject parent, HandlePoint handlePoint, OnLayoutDelegate onLayoutDel) {
				switch (this.tag) {
					case Tag.ROOT: {
						// do nothing.
						break;
					}
					default: {
						if (layoutContents != null) {
							handlePoint = layoutContents(handlePoint);
						}
						break;
					}
				}

				// 改行にあたる処理。描画位置を左下に持っていく。
				handlePoint.nextTopHandle += handlePoint.contentHeight;
				
				foreach (var child in this.transform.GetChildlen()) {
					handlePoint = child.Layout(this, handlePoint, onLayoutDel);
				}
				
				/*
					set padding if need.
					default padding is 0.
				*/
				onLayoutDel(this.tag, this.depth, this.padding, new Dictionary<KV_KEY, string>(this.kv));

				/*
					adopt padding.
				*/
				{
					var rectTrans = this.rectTransform;
					rectTrans.anchoredPosition += padding.Position();

					handlePoint.nextLeftHandle += padding.PaddingWidth();
					handlePoint.nextTopHandle += padding.PaddingHeight();
				}

				// ここで、子要素とpaddingを含めた高さが取得できる。
				
				return handlePoint;
			}

			private void Materialize (VirtualGameObject parent, OnMaterializeDelegate onMaterializeDel) {
				switch (this.tag) {
					case Tag.ROOT: {
						// do nothing.
						break;
					}
					default: {
						if (materialize != null) {
							this._gameObject = materialize();
						}
						
						this._gameObject.transform.SetParent(parent._gameObject.transform);
						break;
					}
				}
				
				foreach (var child in this.transform.GetChildlen()) {
					child.Materialize(this, onMaterializeDel);
				}
				
				onMaterializeDel(this._gameObject, this.tag, this.depth, new Dictionary<KV_KEY, string>(this.kv));
			}
		}

		public delegate void OnLayoutDelegate (Tag tag, Tag[] depth, Padding padding, Dictionary<InformationTests.Tokenizer.KV_KEY, string> keyValue);
		public delegate void OnMaterializeDelegate (GameObject obj, Tag tag, Tag[] depth, Dictionary<InformationTests.Tokenizer.KV_KEY, string> keyValue);

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
			public Rect rect = Rect.zero;
			public Vector2 anchoredPosition = Vector2.zero;
			public Vector2 sizeDelta = Vector2.zero;
		}

		private readonly VirtualGameObject rootObject;

		public Tokenizer (string source) {
			var root = new TagPoint(0, 0, Tag.ROOT, new Tag[0], string.Empty);
			rootObject = Tokenize(root, source);
		}

		public GameObject Materialize (Rect viewport, OnLayoutDelegate onLayoutDel, OnMaterializeDelegate onMaterializeDel) {
			var rootObj = rootObject.MaterializeRoot("test", viewport.size, onLayoutDel, onMaterializeDel);
			rootObj.transform.position = viewport.position;
			return rootObj;
		}

		private VirtualGameObject Tokenize (TagPoint p, string data) {
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

				var foundStartTagPointAndAttrAndOption = FindStartTag(p.depth, lines, index);

				/*
					no <tag> found.
				*/
				if (foundStartTagPointAndAttrAndOption.tagPoint.tag == Tag.NO_TAG_FOUND || foundStartTagPointAndAttrAndOption.tagPoint.tag == Tag.UNKNOWN) {					
					// detect single closed tag. e,g, <tag something />.
					var foundSingleTagPointWithAttr = FindSingleTag(p.depth, lines, index);

					if (foundSingleTagPointWithAttr.tagPoint.tag != Tag.NO_TAG_FOUND && foundSingleTagPointWithAttr.tagPoint.tag != Tag.UNKNOWN) {
						// closed <tag /> found. add this new closed tag to parent tag.
						AddChildContentToParent(p, foundSingleTagPointWithAttr.tagPoint, foundSingleTagPointWithAttr.attrs);
					} else {
						// not tag contained in this line. this line is just contents of parent tag.
						AddContentToParent(p, readLine);
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
						AddChildContentToParent(p, foundEndTagPointAndContent.tagPoint, attr);

						// content exists. parse recursively.
						Tokenize(foundEndTagPointAndContent.tagPoint, foundEndTagPointAndContent.content);
						break;
					}

					// end tag is not contained in current line.
					index++;
				}

				index++;
			}

			return p.vGameObject;
		}
		
		private void AddChildContentToParent (TagPoint parent, TagPoint child, Dictionary<KV_KEY, string> kvs) {
			var parentObj = parent.vGameObject;
			child.vGameObject.transform.SetParent(parentObj.transform);

			// append attribute as kv.
			foreach (var kv in kvs) {
				child.vGameObject.kv[kv.Key] = kv.Value;
			}

			SetupMaterializeAction(child);
		}
		
		private void AddContentToParent (TagPoint p, string content) {
			var	parentObj = p.vGameObject;
			
			// add content to parent as child.
			{
				var child = new TagPoint(p.lineIndex, p.tagEndPoint, Tag._CONTENT, p.depth.Concat(new Tag[]{Tag._CONTENT}).ToArray(), p.originalTagName + " Content");
				child.vGameObject.transform.SetParent(parentObj.transform);
				child.vGameObject.kv[KV_KEY.CONTENT] = content;
				child.vGameObject.kv[KV_KEY.PARENTTAG] = p.originalTagName;

				SetupMaterializeAction(child);
			}

			SetupMaterializeAction(p);
		}

		public struct LineCountAndHeight {
			public int lineCount;
			public int totalHeight;
			public LineCountAndHeight (int lineCount, int totalHeight) {
				this.lineCount = lineCount;
				this.totalHeight = totalHeight;
			}
		}

		/*
			こいつは、ある限界幅に対して文字列がどのように折り返されるか、を返してきてるので、折り返しがある = 幅コンテンツが限界になってる、という感じで、
			heightだけだと折り返されてるかどうかは判断できない。

			手順をまとめると、
			・規定幅が与えられる
			・幅に対して文字列(フォント、サイズ付き)を渡す
			・改行位置、コンポーネントの高さが求められる
		*/
		private LineCountAndHeight Populate (Text text, Vector2 sizeDelta) {
			var generator = new TextGenerator();
			generator.Populate(text.text, text.GetGenerationSettings(sizeDelta));

			var height = 0;
			foreach(var l in generator.lines){
				// Debug.LogError("ch topY:" + l.topY);
				// Debug.LogError("ch index:" + l.startCharIdx);
				// Debug.LogError("ch height:" + l.height);
				height += l.height;
			}
			
			return new LineCountAndHeight(generator.lines.Count, height);
		}

		private void SetupMaterializeAction (TagPoint tagPoint) {
			// set only once.
			if (tagPoint.vGameObject.layoutContents != null) {
				return;
			}
			
			var prefabName = string.Empty;

			/*
				set name of required prefab.
					content -> parent's tag name.

					parent tag -> tag + container name.

					single tag -> tag name.
			*/
			switch (tagPoint.tag) {
				case Tag._CONTENT: {
					prefabName = tagPoint.vGameObject.kv[KV_KEY.PARENTTAG];
					break;
				}
				
				case Tag.H:
				case Tag.P:
				case Tag.UL:
				case Tag.LI: {// these are container.
					prefabName = tagPoint.originalTagName.ToUpper() + "Container";
					break;
				}

				default: {
					prefabName = tagPoint.originalTagName.ToUpper();
					break;
				}
			}
			
			/*
				set on calcurate function.
			*/
			tagPoint.vGameObject.layoutContents = positionData => {
				return LayoutTagContent(tagPoint, positionData, prefabName);
			};

			tagPoint.vGameObject.materialize = () => {
				var prefab = Resources.Load(prefabName) as GameObject;
				if (prefab == null) {
					Debug.LogError("missing prefab:" + prefabName);
					return new GameObject("missing prefab:" + prefabName);
				}

				// instantiate gameObject. ここを後々、プールからの取得に変える。同じ種類のオブジェクトがあればそれで良さそう。それか設定をアレコレできればいいのか。
				var go = GameObject.Instantiate(prefab);

				var vRectTrans = tagPoint.vGameObject.rectTransform;

				var rectTrans = go.GetComponent<RectTransform>();

				rectTrans.anchoredPosition = vRectTrans.anchoredPosition;
				rectTrans.sizeDelta = vRectTrans.sizeDelta;
				
				Debug.LogError("rectTrans.sizeDelta:" + rectTrans.sizeDelta);
				

				return go;
			};
		}

		public enum KV_KEY {
			CONTENT,
			PARENTTAG,

			WIDTH,
			HEIGHT,
			SRC,
			ALT,
			HREF,
		};

		public class Padding {
			public float top;
			public float right;
			public float bottom; 
			public float left;

			public Vector2 Position () {
				return new Vector2(left, -top);
			}

			public float PaddingWidth () {
				return left + right;
			}
			public float PaddingHeight () {
				return top + bottom;
			}
		}

		public struct HandlePoint {
			public float nextLeftHandle;
			public float nextTopHandle;

			public float width;
			public float height;

			public float contentWidth;
			public float contentHeight;

			public HandlePoint (float nextLeftHandle, float nextTopHandle, float width, float height) {
				this.nextLeftHandle = nextLeftHandle;
				this.nextTopHandle = nextTopHandle;
				this.width = width;
				this.height = height;
				this.contentWidth = 0;
				this.contentHeight = 0;
			}

			public Vector2 Position () {
				return new Vector2(nextLeftHandle, nextTopHandle);
			}

			public Vector2 Size () {
				return new Vector2(width, height);
			}

			public void SetContentWidth (float contentWidth) {
				this.contentWidth = contentWidth;
			}

			public void SetContentHeight (float contentHeight) {
				this.contentHeight = contentHeight;
			}
		}

		/**
			materialize contents of tag.
		*/
		private HandlePoint LayoutTagContent (TagPoint tagPoint, HandlePoint contentHandlePoint, string prefabName) {
			var rectTrans = tagPoint.vGameObject.rectTransform;

			// set y start pos.
			rectTrans.anchoredPosition = new Vector2(rectTrans.anchoredPosition.x, rectTrans.anchoredPosition.y -contentHandlePoint.nextTopHandle);

			var contentWidth = 0f;
			var contentHeight = 0f;

			// set kv.
			switch (tagPoint.tag) {
				case Tag.A: {
					foreach (var kvs in tagPoint.vGameObject.kv) {
						var key = kvs.Key;
						switch (key) {
							case KV_KEY.HREF: {
								var href = kvs.Value;

								// add button component.
								var rootObject = tagPoint.vGameObject.GetRootGameObject();
								var rootMBInstance = rootObject.@class;
								
								// 実際に表示する瞬間まで無視できるのでは。このアクションを持っておけばいいのでは。
								// AddButton(obj, tagPoint, () => rootMBInstance.OnLinkTapped(tagPoint.tag, href));
								break;
							}
							default: {
								Debug.LogError("A tag, unhandled. key:" + key);
								break;
							}
						}
					}
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

					foreach (var kv in tagPoint.vGameObject.kv) {
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
								var src = kv.Value;
								
								// add button component.
								var rootObject = tagPoint.vGameObject.GetRootGameObject();
								var rootMBInstance = rootObject.@class;
								
								// 実際に表示する瞬間まで無視できるのでは。このアクションを持っておけばいいのでは。
								// AddButton(obj, tagPoint, () => rootMBInstance.OnImageTapped(tagPoint.tag, src));
								break;
							}
							case KV_KEY.ALT: {
								// do nothing yet.
								break;
							}
							default: {
								Debug.LogError("IMG tag, unhandled. key:" + key);
								break;
							}
						}
					}
					break;
				}
				
				case Tag._CONTENT: {
					// set to maximum viewPoint size.
					contentWidth = contentHandlePoint.width;
					contentHeight = contentHandlePoint.height;
					
					// set text if exist.
					if (tagPoint.vGameObject.kv.ContainsKey(KV_KEY.CONTENT)) {
						var text = tagPoint.vGameObject.kv[KV_KEY.CONTENT];
						
						if (!string.IsNullOrEmpty(text)) {
							// prefabからComponentは得られないよな〜〜? と思ったが行けそうな気が。いける。Instantiateいらない。なので、この辺を裏で同期的にロードするような機構を作れればいい。
							// このあたりをhttpリクエストに乗っけるようなことができるとなおいいのだろうか。AssetBundleともちょっと違う何か、的な。
							/*
								・Resourcesに置ける
								・AssetBundle化できる
							 */
							var prefab = LoadPrefab(prefabName);
							var txt = prefab.GetComponent<Text>();
							txt.text = text;

							// set content height.
							var contentLineCountAndHeight = Populate(txt, new Vector2(contentWidth, contentHeight));
							contentHeight = contentLineCountAndHeight.totalHeight;

							// set content width.
							var lineCount = contentLineCountAndHeight.lineCount;
							if (1 < lineCount) {// content has multiple lines. content width is equal to window width.
								contentWidth = contentHandlePoint.width;
							} else {
								contentWidth = txt.preferredWidth;
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
			Debug.LogError("set rectTrans.sizeDelta:" + rectTrans.sizeDelta);

			// hold content width and height. これ仮想化できてるんで不要かもしれない。
			contentHandlePoint.SetContentWidth(contentWidth);
			contentHandlePoint.SetContentHeight(contentHeight);
			
			return contentHandlePoint;
		}

		private GameObject LoadPrefab (string prefabName) {
			return Resources.Load(prefabName) as GameObject;
		}

		private void AddButton (GameObject obj, TagPoint tagPoint, UnityAction param) {
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

		private struct TagPointAndContent {
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
					// Debug.LogError("originalTagName:" + originalTagName + " contains kv:" + string.Join(", ", kvs));
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
				// Debug.LogError("tag:" + tagName + " contains kv:" + string.Join(", ", kvs));
				
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

    [MTest] public void ParseLargeMarkdown () {
        var sampleMd = @"
# Autoya
ver 0.8.4

![loading](https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true)

small, thin framework for Unity.  
which contains essential game features.

## Features
* Authentication handling
* AssetBundle load/management
* HTTP/TCP/UDP Connection feature
* Maintenance changed handling
* Purchase/IAP feature
* Notification(local/remote)
* Information


## Motivation
Unity already contains these feature's foundation, but actually we need more codes for using it in app.

This framework can help that.

## License
see below.  
[LICENSE](./LICENSE)


## Progress

### automatic Authentication
already implemented.

###AssetBundle list/preload/load
already implemented.

###HTTP/TCP/UDP Connection feature
| Protocol        | Progress     |
| ------------- |:-------------:|
| http/1 | done | 
| http/2 | not yet | 
| tcp      | not yet      | 
| udp	| not yet      |  


###app-version/asset-version/server-condition changed handles
already implemented.

###Purchase/IAP flow
already implemented.

###Notification(local/remote)
in 2017 early.

###Information
in 2017 early.


## Tests
implementing.


## Installation
unitypackage is ready!

1. use Autoya.unitypackage.
2. add Purchase plugin via Unity Services.
3. done!

## Usage
all example usage is in Assets/AutoyaSamples folder.

yes,(2spaces linebreak)  
2s break line will be expressed with <br />.

then,(hard break)
hard break will appear without <br />.

		";
	}

	[MTest] public void DrawParsedMarkdown () {
		
	}
}
