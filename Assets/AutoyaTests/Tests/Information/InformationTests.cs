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

public class InformationTests : MiyamasuTestRunner {
	[MTest] public void ParseSmallMarkdown () {
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
				tokenizer.Materialize();

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

			public readonly Tag tag;
			
			private VirtualTransform _transform;
			
			public VirtualTransform transform {
                get {
					return _transform;
				}
            }

			public Dictionary<string, string> kv = new Dictionary<string, string>();
			public string content = string.Empty;

			public VirtualGameObject (Tag tag) {
				this.tag = tag;
				this._transform = new VirtualTransform(this);
			}

			/**
				return generated game object.
			*/
			public GameObject Materialize (Tag tag, GameObject parent) {
				Debug.LogError("Materialize tag:" + tag);
				this._gameObject = new GameObject(tag.ToString());// オブジェクトプールがあるといいのでは的な。
				this._gameObject.transform.SetParent(parent.transform);

				switch (tag) {
					case Tag.ROOT: {
						var childlen = this.transform.GetChildlen();
						foreach (var child in childlen) {
							child.Materialize(child.tag, this._gameObject);
						}
						break;
					}

					case Tag.P:
					case Tag.H1:
					case Tag.H2:
					case Tag.H3:
					case Tag.H4:
					case Tag.H5: {
						{
							GameObject tDummyObj = GameObject.Find("dummy");
								
							Text tDummy;
							if (tDummyObj == null) {
								tDummyObj = new GameObject("dummy");
								tDummy = tDummyObj.AddComponent<Text>();
							} else {
								tDummy = tDummyObj.GetComponent<Text>();
							}

							var generator = new TextGenerator();
							var v2 = tDummy.GetComponent<RectTransform>().sizeDelta;
							generator.Populate(content, tDummy.GetGenerationSettings(v2));
							foreach(var l in generator.lines){
								Debug.Log("ch index:" + l.startCharIdx);
							}
						}

						// 親 -> 子、とだんだん幅を狭めていく感じでviewを実装。
						// 高さは計算後に全部並べる感じか。結局遅延させられなさそう？
						// 画面外になったら書かない、とかか。上下で超過した最大一個をdisableして、それ以降はskipとか。
						// 仮でGameObjectを一つ持って、折り返しを計測するのに使える感じ。
						this._gameObject.AddComponent<CanvasRenderer>();
						var t = this._gameObject.AddComponent<Text>();
						t.text = content;

						var childlen = this.transform.GetChildlen();
						foreach (var child in childlen) {
							child.Materialize(child.tag, this._gameObject);
						}
						break;
					}
					default: {
						// ここで、仮のUIパーツをいっぱい作って、いざ表示範囲指定の時に、型にあてていけばいいか。
						// とりあえず組み上げ出してみよう。Pはなんかテキスト。中にリンク(a)が入ったりする。
						break;
					}
				}

				return this._gameObject;
			}
		}

		public class VirtualTransform {
			/*
				このへんに、posとかwidthとか持てば良さそう。
				Rootにコンテンツ全体のサイズを0,0,totalW,totalH,という値を持つような感じで保持して、
				それ以下のコンテンツは下の最大値を持つ、とかしとけばよさげ。二重リストも存在しないし。
			*/
			private readonly VirtualGameObject gameObject;
			private List<VirtualGameObject> _childlen = new List<VirtualGameObject>();
			public VirtualTransform (VirtualGameObject gameObject) {
				this.gameObject = gameObject;
			}

			public void SetParent (VirtualTransform t) {
				t._childlen.Add(this.gameObject);
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
			return rootObject.Materialize(Tag.ROOT, new GameObject("sample"));
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

				// Debug.LogError("readLine:" + readLine);
				
				var foundStartTagPointAndAttr = FindStartTag(lines, index);

				/*
					no <tag> found.
				*/
				if (foundStartTagPointAndAttr.tagPoint.tag == Tag.NO_TAG_FOUND || foundStartTagPointAndAttr.tagPoint.tag == Tag.UNKNOWN) {
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
				
				while (true) {
					if (lines.Length <= index) {// 閉じタグが見つからなかったのでたぶんparseException.
						break;
					}

					readLine = lines[index];
					
					// find end of current tag.
					var foundEndTagPointAndContent = FindEndTag(foundStartTagPointAndAttr.tagPoint, lines, index);
					if (foundEndTagPointAndContent.tagPoint.tag != Tag.NO_TAG_FOUND && foundEndTagPointAndContent.tagPoint.tag != Tag.UNKNOWN) {
						// close tag found. set attr to this closed tag.
						AddChildContentToParent(p, foundStartTagPointAndAttr.tagPoint, foundStartTagPointAndAttr.attrs);

						// content exists. parse recursively.
						Tokenize(foundEndTagPointAndContent.tagPoint, foundEndTagPointAndContent.content);
						break;
					}

					// end tag is not contained in current line.
					index++;
				}

				// p.tagContents.AddRange(foundStartTagPoint.tagContents);
				index++;
			}

			return p.gameObject;
		}

		private void AddChildContentToParent (TagPoint parent, TagPoint child, Dictionary<string, string> kv) {
			var parentObj = parent.gameObject;
			child.gameObject.transform.SetParent(parentObj.transform);
			child.gameObject.kv = kv;
		}
		
		private void AddContentToParent (TagPoint p, string content) {
			p.gameObject.content = content;
		}

		public enum Tag {
			NO_TAG_FOUND,
			UNKNOWN,
			ROOT,
			H1, 
			H2, 
			H3, 
			H4, 
			H5, 
			P, 
			IMG, 
			UL, 
			OL,
			LI, 
			A
		}

		public class TagPoint {
			public readonly VirtualGameObject gameObject;

			public readonly int lineIndex;
			public readonly Tag tag;
			
			public TagPoint (int lineIndex, Tag tag) {
				if (tag == Tag.NO_TAG_FOUND || tag == Tag.UNKNOWN) {
					return;
				}

				this.lineIndex = lineIndex;
				this.tag = tag;
				this.gameObject = new VirtualGameObject(tag);	
			}

			public GameObject Materialize (GameObject parent) {
				return this.gameObject.Materialize(tag, parent);
			}
		}


		private struct TagPointAndAttr {
			public readonly TagPoint tagPoint;
			public readonly Dictionary<string, string> attrs;
			
			public TagPointAndAttr (TagPoint tagPoint, Dictionary<string, string> attrs) {
				this.tagPoint = tagPoint;
				this.attrs = attrs;
			}
			public TagPointAndAttr (TagPoint tagPoint) {
				this.tagPoint = tagPoint;
				this.attrs = new Dictionary<string, string>();
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
		private TagPointAndAttr FindStartTag (string[] lines, int lineIndex) {
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
				
				try {
					var tagEnum = (Tag)Enum.Parse(typeof(Tag), tagName, true);
					return new TagPointAndAttr(new TagPoint(lineIndex, tagEnum), kvDict);
				} catch (Exception e) {
					return new TagPointAndAttr(new TagPoint(lineIndex, Tag.UNKNOWN), kvDict);
				}
			}
			return new TagPointAndAttr(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
		}

		private TagPointAndAttr FindSingleTag (string[] lines, int lineIndex) {
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
					return new TagPointAndAttr(new TagPoint(lineIndex, tagEnum), kvDict);
				} catch (Exception e) {
					return new TagPointAndAttr(new TagPoint(lineIndex, Tag.UNKNOWN), kvDict);
				}
			}
			return new TagPointAndAttr(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
		}

		private TagPointAndContent FindEndTag (TagPoint p, string[] lines, int lineIndex) {
			var line = lines[lineIndex];
			var endTag = "</" + p.tag.ToString().ToLower() + ">";

			var endTagIndex = -1;
			if (p.lineIndex == lineIndex) {
				// check start from next point of close point of start tag.
				endTagIndex = line.IndexOf(endTag, 1 + p.tag.ToString().Length + 1);
			} else {
				endTagIndex = line.IndexOf(endTag);
			}
			
			if (endTagIndex == -1) {
				// no end tag contained.
				return new TagPointAndContent(new TagPoint(lineIndex, Tag.NO_TAG_FOUND));
			}
			
			var contentsStrLines = lines.Where((i,l) => p.lineIndex <= l && l <= lineIndex).ToArray();
			
			// modify the line which contains start or end tag. exclude tag expression.
			contentsStrLines[0] = contentsStrLines[0].Substring(1 + p.tag.ToString().Length + 1);
			contentsStrLines[contentsStrLines.Length-1] = contentsStrLines[contentsStrLines.Length-1].Substring(0, contentsStrLines[contentsStrLines.Length-1].Length - endTag.Length);
			
			var contentsStr = string.Join("\n", contentsStrLines);

			return new TagPointAndContent(p, contentsStr);
		}

		public GameObject Materialize (int index) {
			return null;
		}
	}

    [MTest] public void ParseLargeMarkdown () {
        var largeMd = "";
	}

	[MTest] public void DrawParsedMarkdown () {
		
	}
}
