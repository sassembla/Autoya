using Miyamasu;
using MarkdownSharp;
using UnityEngine;
using System.Xml;
using System.Collections.Generic;
using System;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine.UI;

public enum Tag {
	NO_TAG_FOUND,
	UNKNOWN,
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

		";
    

        // Create new markdown instance
        Markdown mark = new Markdown();

        // Run parser
        string text = mark.Transform(sampleMd);
        Debug.LogError("text:" + text);

		// 取れそうな戦略：
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
				tokenizer.Materialize();// これを、Rootが画面に乗った時にやったほうがいいのかも

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
		public struct UIAndPos {
			public readonly GameObject gameObject;
			public readonly Rect rect;
			public UIAndPos (GameObject gameObject, Rect rect) {
				this.gameObject = gameObject;
				this.rect = rect;
			}
		}
		
		public class VirtualGameObject {
			public GameObject _gameObject;

			public InformationRootMonoBehaviour @class;

			public readonly Tag tag;
			
			private VirtualTransform vTransform;

			public VirtualGameObject parent;
			
			public VirtualTransform transform {
                get {
					return vTransform;
				}
            }

			public VirtualGameObject GetRootGameObject () {
				if (vTransform.Parent() != null) {
					return vTransform.Parent().GetRootGameObject();
				}

				// no parent. return this vGameObject.
				return this;
			}

			public void OnButtonTapped () {

			}

			public Dictionary<string, string> kv = new Dictionary<string, string>();
			public List<string> contents = new List<string>();

			public Func<string, Rect, UIAndPos> onMaterialize = null;
			
			public VirtualGameObject (Tag tag) {
				this.tag = tag;
				this.vTransform = new VirtualTransform(this);
			}

			/**
				return generated game object.
			*/
			public GameObject MaterializeRoot () {
				Materialize(new Rect(0, 0, 300, 100));
				return this._gameObject;
			}

			private Rect Materialize (Rect rect, GameObject parent=null) {
				switch (this.tag) {
					case Tag.ROOT: {
						this._gameObject = new GameObject("ROOT");

						this.@class = this._gameObject.AddComponent<InformationRootMonoBehaviour>();

						var rectTrans = this._gameObject.AddComponent<RectTransform>();
						rectTrans.anchorMin = Vector2.up;
						rectTrans.anchorMax = Vector2.up;
						rectTrans.pivot = Vector2.up;
						rectTrans.position = rect.position;
						rectTrans.sizeDelta = new Vector2(rect.width, rect.height);
						
						rect = rectTrans.rect;
						break;
					}
					default: {
						if (onMaterialize != null) {
							var gameObjectAndPos = onMaterialize(tag.ToString(), rect);
							this._gameObject = gameObjectAndPos.gameObject;
							rect = gameObjectAndPos.rect;
						} else {
							this._gameObject = new GameObject("not ready");
						}
						
						if (parent != null) {
							this._gameObject.transform.SetParent(parent.transform);
						}
						break;
					}
				}
				
				var childlen = this.transform.GetChildlen();
				foreach (var child in childlen) {
					rect = child.Materialize(rect, this._gameObject);
				}

				return rect;
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

		private readonly VirtualGameObject rootObject;

		public Tokenizer (string source) {
			var root = new TagPoint(0, Tag.ROOT);
			rootObject = Tokenize(root, source);
		}

		public GameObject Materialize () {
			// Rootタグのオブジェクトが作られればそれでいい感じがする。んで、そいつがスクロールを持っていれば。
			return rootObject.MaterializeRoot();
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

				var foundStartTagPointAndAttrAndOption = FindStartTag(lines, index);

				/*
					no <tag> found.
				*/
				if (foundStartTagPointAndAttrAndOption.tagPoint.tag == Tag.NO_TAG_FOUND || foundStartTagPointAndAttrAndOption.tagPoint.tag == Tag.UNKNOWN) {					
					// detect single closed tag. e,g, <tag something />.
					var foundSingleTagPointWithAttr = FindSingleTag(lines, index);

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
		
		private void AddChildContentToParent (TagPoint parent, TagPoint child, Dictionary<string, string> kvs) {
			var parentObj = parent.vGameObject;
			child.vGameObject.transform.SetParent(parentObj.transform);

			// append attribute as kv.
			foreach (var kv in kvs) {
				child.vGameObject.kv[kv.Key] = kv.Value;
			}

			SetupOnMaterializeAction(child);
		}
		
		private void AddContentToParent (TagPoint p, string content) {
			p.vGameObject.contents.Add(content);
			
			SetupOnMaterializeAction(p);
		}

		private int Populate (Text text) {
			GameObject tDummyObj = GameObject.Find("dummy");
			
			var generator = new TextGenerator();
			var v2 = text.GetComponent<RectTransform>().sizeDelta;
			generator.Populate(text.text, text.GetGenerationSettings(v2));

			var height = 0;
			foreach(var l in generator.lines){
				// Debug.LogError("ch topY:" + l.topY);
				// Debug.LogError("ch index:" + l.startCharIdx);
				// Debug.LogError("ch height:" + l.height);
				height += l.height;
			}
			return height;//generator.rectExtents;// 要素の全体が入る四角形。あーなるほど、オリジナルの数値を引っ張ってるな。
		}

		private void SetupOnMaterializeAction (TagPoint tagPoint) {
			// set only once.
			if (tagPoint.vGameObject.onMaterialize != null) {
				return;
			}
			
			var prefabName = string.Empty;

			// name
			switch (tagPoint.tag) {
				case Tag.H: {
					prefabName = "H" + tagPoint.vGameObject.kv["headingNumber"];	
					break;
				}
				default: {
					prefabName = tagPoint.tag.ToString().ToUpper();
					break;
				}
			}

			/*
				set on materialize function.
			*/
			tagPoint.vGameObject.onMaterialize = (name, endEdgeRect) => {
				var prefab = Resources.Load(prefabName) as GameObject;
				if (prefab == null) {
					return new UIAndPos(new GameObject("prefab " + prefabName + " is not found."), endEdgeRect);
				}
				
				var obj = GameObject.Instantiate(prefab);
				var rectTrans = obj.GetComponent<RectTransform>();

				// set y pos.
				rectTrans.anchoredPosition = new Vector2(0, -endEdgeRect.yMax);

				switch (tagPoint.tag) {
					case Tag.IMG: {
						
						foreach (var kv in tagPoint.vGameObject.kv) {
							var key = kv.Key;
							
							switch (key) {
								case "width": {
									var val = Convert.ToInt32(kv.Value);
									rectTrans.sizeDelta = new Vector2(val, rectTrans.sizeDelta.y);
									break;
								}
								case "height": {
									var val = Convert.ToInt32(kv.Value);
									rectTrans.sizeDelta = new Vector2(rectTrans.sizeDelta.x, val);
									break;
								}
								case "src": {
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
										// UnityEditor.Events.UnityEventTools.AddObjectPersistentListener<GameObject> (
										// 	button.onClick, 
										// 	(s) => {
										// 		rootMBInstance.OnImageTapped(tagPoint.tag, src);
										// 	},
										// 	rootMBInstance.gameObject
										// );
									}
									
									break;
								}
							}
						}

						endEdgeRect.height += rectTrans.rect.height;
						break;
					}
					default: {
						rectTrans.sizeDelta = new Vector2(endEdgeRect.width, endEdgeRect.height);

						// set text if exist.
						var text = string.Join("\n", tagPoint.vGameObject.contents.ToArray());
						var contentHeight = 0;

						if (!string.IsNullOrEmpty(text)) {
							var textComponent = obj.GetComponent<Text>();
							textComponent.text = text;
							
							// set content size.
							contentHeight = Populate(textComponent);// populate自体の返せるパラメータはもっといっぱいありそう。
						}
						
						// adjust height to contents text height.
						rectTrans.sizeDelta = new Vector2(rectTrans.sizeDelta.x, contentHeight);

						endEdgeRect.height += contentHeight;
						break;
					}
				}

				return new UIAndPos(obj, endEdgeRect);
			};
		}

		
		public class TagPoint {
			public readonly string id;
			public readonly VirtualGameObject vGameObject;

			public readonly int lineIndex;
			public readonly Tag tag;
			
			public TagPoint (int lineIndex, Tag tag) {
				if (tag == Tag.NO_TAG_FOUND || tag == Tag.UNKNOWN) {
					return;
				}

				this.id = Guid.NewGuid().ToString();

				this.lineIndex = lineIndex;
				this.tag = tag;
				this.vGameObject = new VirtualGameObject(tag);	
			}
		}


		private struct TagPointAndAttrAndHeadingNumber {
			public readonly TagPoint tagPoint;
			public readonly Dictionary<string, string> attrs;
			public readonly int option;
			public TagPointAndAttrAndHeadingNumber (TagPoint tagPoint, Dictionary<string, string> attrs, int option) {
				this.tagPoint = tagPoint;
				this.attrs = attrs;
				this.option = option;
			}
			public TagPointAndAttrAndHeadingNumber (TagPoint tagPoint, Dictionary<string, string> attrs) {
				this.tagPoint = tagPoint;
				this.attrs = attrs;
				this.option = 0;
			}
			public TagPointAndAttrAndHeadingNumber (TagPoint tagPoint) {
				this.tagPoint = tagPoint;
				this.attrs = new Dictionary<string, string>();
				this.option = 0;
			}
		}

		private struct TagPointAndContent {
			public readonly TagPoint tagPoint;
			public readonly string content;
			
			public TagPointAndContent (TagPoint tagPoint, int headingNumber, string content) {
				this.tagPoint = tagPoint;
				this.tagPoint.vGameObject.kv["headingNumber"] = headingNumber.ToString();
				this.content = content;
			}
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
		private TagPointAndAttrAndHeadingNumber FindStartTag (string[] lines, int lineIndex) {
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

				var tagName = string.Empty;
				var kvDict = new Dictionary<string, string>();

				// not closed tag. contains attr or not.
				if (line[closeIndex-1] == '"') {// <tag something="else">
					var contents = line.Substring(1, closeIndex - 1).Split(' ');
					tagName = contents[0];
					for (var i = 1; i < contents.Length; i++) {
						var kv = contents[i].Split(new char[]{'='}, 2);
						
						if (kv.Length < 2) {
							continue;
						}

						var key = kv[0];
						var val = kv[1].Substring(1, kv[1].Length - (1 + 1));

						kvDict[key] = val;
					}
					// var kvs = kvDict.Select(i => i.Key + " " + i.Value).ToArray();
					// Debug.LogError("tag:" + tagName + " contains kv:" + string.Join(", ", kvs));
				} else {
					tagName = line.Substring(1, closeIndex - 1);
				}

				
				var numbers = string.Join(string.Empty, tagName.ToCharArray().Where(c => Char.IsDigit(c)).Select(t => t.ToString()).ToArray());
				var headingNumber = 0;
				
				if (!string.IsNullOrEmpty(numbers) && tagName.EndsWith(numbers)) {
					var index = tagName.IndexOf(numbers);
					tagName = tagName.Substring(0, index);
					
					headingNumber = Convert.ToInt32(numbers);
				}
				
				try {
					var tagEnum = (Tag)Enum.Parse(typeof(Tag), tagName, true);
					if (headingNumber != 0) {
						return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, tagEnum), kvDict, headingNumber);	
					}
					return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, tagEnum), kvDict);
				} catch {
					return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, Tag.UNKNOWN), kvDict);
				}
			}
			return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
		}

		private TagPointAndAttrAndHeadingNumber FindSingleTag (string[] lines, int lineIndex) {
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
				
				var kvDict = new Dictionary<string, string>();
				for (var i = 1; i < contents.Length; i++) {
					var kv = contents[i].Split(new char[]{'='}, 2);
					
					if (kv.Length < 2) {
						continue;
					}

					var key = kv[0];
					var val = kv[1].Substring(1, kv[1].Length - (1 + 1));

					kvDict[key] = val;
				}
				// var kvs = kvDict.Select(i => i.Key + " " + i.Value).ToArray();
				// Debug.LogError("tag:" + tagName + " contains kv:" + string.Join(", ", kvs));
				
				try {
					var tagEnum = (Tag)Enum.Parse(typeof(Tag), tagName, true);
					return new TagPointAndAttrAndHeadingNumber(new TagPoint(lineIndex, tagEnum), kvDict);
				} catch (Exception e) {
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

			if (headingNumber != 0) {
				return new TagPointAndContent(p, headingNumber, contentsStr);
			}
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
