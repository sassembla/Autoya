using Miyamasu;
using MarkdownSharp;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.UI;

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

<img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true' width='100' height='200' />
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
					(go, tag, depth, padding, kv) => {
						// padding.left += 10;
						// padding.top += 10;
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
			public GameObject _gameObject;

			public InformationRootMonoBehaviour @class;

			public readonly Tag tag;
			public readonly Tag[] depth;
			public Padding padding;
			
			private VirtualTransform vTransform;

			public VirtualGameObject parent;
			
			public VirtualTransform transform {
                get {
					return vTransform;
				}
            }

			public OnMaterializeDelegate onMaterialize;

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

			public Dictionary<KV_KEY, string> kv = new Dictionary<KV_KEY, string>();

			public Func<HandlePoint, HandlePoint> materializeContents = null;
			
			
			/**
				return generated game object.
			*/
			public GameObject MaterializeRoot (Vector2 viewPort, OnMaterializeDelegate onMaterialize) {
				var rootHandlePoint = new HandlePoint(0, 0, viewPort.x, viewPort.y);

				this._gameObject = new GameObject(Tag.ROOT.ToString());
				
				this.@class = this._gameObject.AddComponent<InformationRootMonoBehaviour>();
				var rectTrans = this._gameObject.AddComponent<RectTransform>();
				rectTrans.anchorMin = Vector2.up;
				rectTrans.anchorMax = Vector2.up;
				rectTrans.pivot = Vector2.up;
				rectTrans.position = rootHandlePoint.Position();
				rectTrans.sizeDelta = rootHandlePoint.Size();
						
				// set limit size.
				Materialize(rootHandlePoint, onMaterialize, this._gameObject);

				return this._gameObject;
			}
			
			private HandlePoint Materialize (HandlePoint handlePoint, OnMaterializeDelegate onMaterialize, GameObject parent) {
				// update materialize delegate.
				this.onMaterialize = onMaterialize;

				switch (this.tag) {
					case Tag.ROOT: {
						// do nothing.
						break;
					}
					default: {
						if (materializeContents != null) {
							handlePoint = materializeContents(handlePoint);;
						} else {
							this._gameObject = new GameObject("not ready");
						}
						
						if (parent != null) {
							this._gameObject.transform.SetParent(parent.transform);
						}
						break;
					}
				}

				// ここで、機転のyは設定できている。
				// 高さを測るために0にする。
				handlePoint.contentHeight = 0;
				var contentHeight = 0f;


				var childlen = this.transform.GetChildlen();
				foreach (var child in childlen) {
					handlePoint = child.Materialize(handlePoint, onMaterialize, this._gameObject);
					// コンテンツの高さが個別にゲットできる。幅とかもか。
					// contentHeight += handlePoint.contentHeight;
					
					// handlePoint.nextTopHandle += handlePoint.contentHeight;
					// handlePoint.contentHeight = 0;
					
					// xについてもここで処理すれば良さそう。回り込めるかどうか、か。
					// ここで、handlePointのy値によってはキャンセルするとかが可能。
				}

				// ここで、全子供の合計の高さサイズが取得できてる。
				Debug.LogError("contentHeight:" + contentHeight);

				// 自分自身の高さに指定する。

				/*
					set padding if need.
					default padding is 0.
				*/
				onMaterialize(this._gameObject, this.tag, this.depth, this.padding, new Dictionary<KV_KEY, string>(this.kv));

				/*
					adopt padding.
				*/
				{
					var rectTrans = this._gameObject.GetComponent<RectTransform>();
					rectTrans.anchoredPosition += padding.Position();

					handlePoint.nextLeftHandle += padding.PaddingWidth();
					handlePoint.nextTopHandle += padding.PaddingHeight();
				}
				
				return handlePoint;
			}
		}

		public delegate void OnMaterializeDelegate (GameObject obj, Tag tag, Tag[] depth, Padding padding, Dictionary<InformationTests.Tokenizer.KV_KEY, string> keyValue);

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

		private readonly VirtualGameObject rootObject;

		public Tokenizer (string source) {
			var root = new TagPoint(0, Tag.ROOT, new Tag[0], string.Empty);
			rootObject = Tokenize(root, source);
		}

		public GameObject Materialize (Rect viewport, OnMaterializeDelegate del) {
			var rootObj = rootObject.MaterializeRoot(viewport.size, del);
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
				
				while (true) {
					if (lines.Length <= index) {// 閉じタグが見つからなかったのでたぶんparseException.
						break;
					}

					readLine = lines[index];
					
					// find end of current tag.
					var foundEndTagPointAndContent = FindEndTag(foundStartTagPointAndAttrAndOption.tagPoint, foundStartTagPointAndAttrAndOption.option, lines, index);
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
			
			var child = new TagPoint(p.lineIndex, Tag._CONTENT, p.depth.Concat(new Tag[]{Tag._CONTENT}).ToArray(), p.originalTagName + " Content");
			child.vGameObject.transform.SetParent(parentObj.transform);
			child.vGameObject.kv[KV_KEY.CONTENT] = content;
			child.vGameObject.kv[KV_KEY.PARENTTAG] = p.originalTagName;

			SetupMaterializeAction(child);

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

		private static GameObject tDummyObj;
		
		/*
			こいつは、ある限界幅に対して文字列がどのように折り返されるか、を返してきてるので、折り返しがある = 幅コンテンツが限界になってる、という感じで、
			heightだけだと折り返されてるかどうかは判断できない。

			手順をまとめると、
			・規定幅が与えられる
			・幅に対して文字列(フォント、サイズ付き)を渡す
			・改行位置、コンポーネントの高さが求められる
			・
		*/
		private LineCountAndHeight Populate (Text textComponent) {
			if (tDummyObj == null) tDummyObj = GameObject.Find("dummy");
			var v2 = textComponent.GetComponent<RectTransform>().sizeDelta;

			var generator = new TextGenerator();
			generator.Populate(textComponent.text, textComponent.GetGenerationSettings(v2));

			var height = 0;
			foreach(var l in generator.lines){
				// Debug.LogError("ch topY:" + l.topY);
				// Debug.LogError("ch index:" + l.startCharIdx);
				// Debug.LogError("ch height:" + l.height);
				height += l.height;
			}
			return new LineCountAndHeight(generator.lines.Count, height);//generator.rectExtents;// 要素の全体が入る四角形。あーなるほど、オリジナルの数値を引っ張ってるな。
		}

		private void SetupMaterializeAction (TagPoint tagPoint) {
			// set only once.
			if (tagPoint.vGameObject.materializeContents != null) {
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
				set on materialize function.
			*/
			tagPoint.vGameObject.materializeContents = positionData => {
				var prefab = Resources.Load(prefabName) as GameObject;
				if (prefab == null) {
					Debug.LogError("missing prefab:" + prefabName);
					return positionData;
				}

				// instantiage gameObject. ここを後々、プールからの取得に変える。同じ種類のオブジェクトがあればそれで良さそう。それか設定をアレコレできればいいのか。
				tagPoint.vGameObject._gameObject = GameObject.Instantiate(prefab);

				return MaterializeTagContent(tagPoint, positionData);
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

			public float contentHeight;

			public HandlePoint (float nextLeftHandle, float nextTopHandle, float width, float height) {
				this.nextLeftHandle = nextLeftHandle;
				this.nextTopHandle = nextTopHandle;
				this.width = width;
				this.height = height;
				this.contentHeight = 0;
			}

			public Vector2 Position () {
				return new Vector2(nextLeftHandle, nextTopHandle);
			}

			public Vector2 Size () {
				return new Vector2(width, height);
			}

			public void AddContentHeight (float height) {
				contentHeight += height;
			}
		}

		/**
			materialize contents of tag.
			ready resource size. width and height.
			in order to implement arounding of contents in P tag, HandlePoint will be changed flexibly.
		*/
		private HandlePoint MaterializeTagContent (TagPoint tagPoint, HandlePoint contentHandlePoint) {
			var obj = tagPoint.vGameObject._gameObject;
			var rectTrans = obj.GetComponent<RectTransform>();

			// set y start pos.
			rectTrans.anchoredPosition = new Vector2(rectTrans.anchoredPosition.x, rectTrans.anchoredPosition.y -contentHandlePoint.nextTopHandle);

			// set kv.
			switch (tagPoint.tag) {
				case Tag.IMG: {					
					foreach (var kv in tagPoint.vGameObject.kv) {
						var key = kv.Key;
						
						switch (key) {
							case KV_KEY.WIDTH: {
								var val = Convert.ToInt32(kv.Value);
								rectTrans.sizeDelta = new Vector2(val, rectTrans.sizeDelta.y);
								break;
							}
							case KV_KEY.HEIGHT: {
								var val = Convert.ToInt32(kv.Value);
								rectTrans.sizeDelta = new Vector2(rectTrans.sizeDelta.x, val);
								break;
							}
							case KV_KEY.SRC: {
								var src = kv.Value;
								// add event component.
								var button = obj.GetComponent<Button>();
								if (button == null) {
									button = obj.AddComponent<Button>();
								}

								var rootObject = tagPoint.vGameObject.GetRootGameObject();
								var rootMBInstance = rootObject.@class;
								
								if (Application.isPlaying) {
									/*
										this code can set action to button. but it does not appear in editor inspector.
									*/
									button.onClick.AddListener(
										() => rootMBInstance.OnImageTapped(tagPoint.tag, src)
									);
								} else {
									
									try {
										button.onClick.AddListener(// エディタでは、Actionをセットすることができない。関数単位で何かを用意すればいけそう = ButtonをPrefabにするとかしとけば行けそう。
											() => rootMBInstance.OnImageTapped(tagPoint.tag, src)
										);
										// UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
										// 	button.onClick,
										// 	() => rootMBInstance.OnImageTapped(tagPoint.tag, src)
										// );

										// // 次の書き方で、固定の値をセットすることはできる。エディタにも値が入ってしまう。
										// インスタンスを作りまくればいいのか。このパーツのインスタンスを用意して、そこに値オブジェクトを入れて、それが着火する、みたいな。
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
								
								break;
							}
						}
					}

					contentHandlePoint.nextTopHandle += rectTrans.rect.height;
					break;
				}
				
				case Tag._CONTENT: {
					rectTrans.sizeDelta = new Vector2(contentHandlePoint.width, contentHandlePoint.height);

					var contentWidth = 0f;
					var contentHeight = 0;

					// set text if exist.
					if (tagPoint.vGameObject.kv.ContainsKey(KV_KEY.CONTENT)) {
						var text = tagPoint.vGameObject.kv[KV_KEY.CONTENT];
						
						if (!string.IsNullOrEmpty(text)) {
							var textComponent = obj.GetComponent<Text>();
							if (textComponent != null) { 
								textComponent.text = text;
								
								// set content height.
								var contentLineCountAndHeight = Populate(textComponent);
								contentHeight = contentLineCountAndHeight.totalHeight;

								// set content width.
								var lineCount = contentLineCountAndHeight.lineCount;
								if (1 < lineCount) {// content has multiple lines. content width is equal to window width.
									contentWidth = contentHandlePoint.width;
								} else {
									contentWidth = textComponent.preferredWidth;
								}
							}
						}
					}
					
					// adjust height to contents text height.
					rectTrans.sizeDelta = new Vector2(contentWidth, contentHeight);

					contentHandlePoint.AddContentHeight(contentHeight);
					contentHandlePoint.nextTopHandle += contentHeight;
					break;
				}

				default: {
					// do nothing.
					break;
				}
			}

			return contentHandlePoint;
		}

		
		public class TagPoint {
			public readonly string id;
			public readonly VirtualGameObject vGameObject;

			public readonly int lineIndex;
			public readonly Tag tag;
			public readonly Tag[] depth;

			public readonly string originalTagName;
			
			public TagPoint (int lineIndex, Tag tag, Tag[] depth=null, string originalTagName="empty.") {
				if (tag == Tag.NO_TAG_FOUND || tag == Tag.UNKNOWN) {
					return;
				}

				this.id = Guid.NewGuid().ToString();

				this.lineIndex = lineIndex;
				this.tag = tag;
				this.depth = depth;
				this.originalTagName = originalTagName;
				this.vGameObject = new VirtualGameObject(tag, depth);	
			}
		}


		private struct TagPointAndAttrAndHeadingNumber {
			public readonly TagPoint tagPoint;
			public readonly Dictionary<KV_KEY, string> attrs;
			public readonly int option;
			public TagPointAndAttrAndHeadingNumber (TagPoint tagPoint, Dictionary<KV_KEY, string> attrs, int option) {
				this.tagPoint = tagPoint;
				this.attrs = attrs;
				this.option = option;
			}
			public TagPointAndAttrAndHeadingNumber (TagPoint tagPoint, Dictionary<KV_KEY, string> attrs) {
				this.tagPoint = tagPoint;
				this.attrs = attrs;
				this.option = 0;
			}
			public TagPointAndAttrAndHeadingNumber (TagPoint tagPoint) {
				this.tagPoint = tagPoint;
				this.attrs = new Dictionary<KV_KEY, string>();
				this.option = 0;
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
		private TagPointAndAttrAndHeadingNumber FindStartTag (Tag[] parentDepth, string[] lines, int lineIndex) {
			var line = lines[lineIndex];

			// find <X>something...
			if (line.StartsWith("<")) {
				var closeIndex = line.IndexOf(">");

				if (closeIndex == -1) {
					return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
				}

				// check found tag end has closed tag mark or not.
				if (line[closeIndex-1] == '/') {
					// closed tag detected.
					return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
				}

				var originalTagName = string.Empty;
				var kvDict = new Dictionary<KV_KEY, string>();

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
							return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, Tag.UNKNOWN), kvDict);
						}
					}
					// var kvs = kvDict.Select(i => i.Key + " " + i.Value).ToArray();
					// Debug.LogError("tag:" + tagName + " contains kv:" + string.Join(", ", kvs));
				} else {
					originalTagName = line.Substring(1, closeIndex - 1);
				}

				var tagName = originalTagName;
				var numbers = string.Join(string.Empty, originalTagName.ToCharArray().Where(c => Char.IsDigit(c)).Select(t => t.ToString()).ToArray());
				var headingNumber = 0;
				
				if (!string.IsNullOrEmpty(numbers) && tagName.EndsWith(numbers)) {
					var index = tagName.IndexOf(numbers);
					tagName = tagName.Substring(0, index);
					
					headingNumber = Convert.ToInt32(numbers);
				}
				
				try {
					var tagEnum = (Tag)Enum.Parse(typeof(Tag), tagName, true);
					if (headingNumber != 0) {
						return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, tagEnum, parentDepth.Concat(new Tag[]{tagEnum}).ToArray(), originalTagName), kvDict, headingNumber);	
					}
					return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, tagEnum, parentDepth.Concat(new Tag[]{tagEnum}).ToArray(), originalTagName), kvDict);
				} catch {
					return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, Tag.UNKNOWN), kvDict);
				}
			}
			return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
		}

		private TagPointAndAttrAndHeadingNumber FindSingleTag (Tag[] parentDepth, string[] lines, int lineIndex) {
			var line = lines[lineIndex];

			// find <X>something...
			if (line.StartsWith("<")) {
				var closeIndex = line.IndexOf(" />");

				if (closeIndex == -1) {
					return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
				}

				var contents = line.Substring(1, closeIndex - 1).Split(' ');

				if (contents.Length == 0) {
					return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
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
						return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, Tag.UNKNOWN), kvDict);
					}
				}
				// var kvs = kvDict.Select(i => i.Key + " " + i.Value).ToArray();
				// Debug.LogError("tag:" + tagName + " contains kv:" + string.Join(", ", kvs));
				
				try {
					var tagEnum = (Tag)Enum.Parse(typeof(Tag), tagName, true);
					return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, tagEnum, parentDepth.Concat(new Tag[]{tagEnum}).ToArray(), tagName), kvDict);
				} catch {
					return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, Tag.UNKNOWN), kvDict);
				}
			}
			return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
		}

		private TagPointAndContent FindEndTag (TagPoint p, int headingNumber, string[] lines, int lineIndex) {
			var line = lines[lineIndex];
			
			var sizeStr = string.Empty;
			if (headingNumber != 0) {
				sizeStr = headingNumber.ToString();
			}
			var endTagStr = p.tag.ToString().ToLower() + sizeStr;
			var endTag = "</" + endTagStr + ">";
			
			var endTagIndex = -1;
			if (p.lineIndex == lineIndex) {
				// check start from next point of close point of start tag.
				endTagIndex = line.IndexOf(endTag, 1 + endTagStr.Length + 1);
			} else {
				endTagIndex = line.IndexOf(endTag);
			}
			
			if (endTagIndex == -1) {
				// no end tag contained.
				return new TagPointAndContent(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
			}
			
			var contentsStrLines = lines.Where((i,l) => p.lineIndex <= l && l <= lineIndex).ToArray();
			
			// modify the line which contains start or end tag. exclude tag expression.
			contentsStrLines[0] = contentsStrLines[0].Substring(1 + endTagStr.Length + 1);
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
