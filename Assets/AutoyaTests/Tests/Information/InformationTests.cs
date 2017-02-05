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
			hX, p, img, ul, li, aタグを見て、それぞれを「行単位の要素」に変形、uGUIの要素に変形できれば良さそう。
			tokenizer書くか。

			・tokenizerでいろんなコンポーネントに分解
			・tokenizerで分解したコンポーネントを、コンポーネント単位で生成
			・生成したコンポーネントを配置、描画
			
			で、これらの機能は、「N行目から」みたいな指定で描画できるようにしたい。
		*/

        var tokenizer = new Tokenizer(text);
        for (var i = 0; i < tokenizer.ComponentCount(); i++) {
			var component = tokenizer.GetComponentAt(i);
			// これがuGUIのコンポーネントになってる
		}

		/*
			位置を指定して書き出すことができる(一番上がずれる = cursorか。)
		*/
		for (var i = 1; i < tokenizer.ComponentCount(); i++) {
			var component = tokenizer.GetComponentAt(i);
			// これがuGUIのコンポーネントになってる
		}



	}
	/**
		parse hX, p, img, ul, ol, li, a tags then returns GameObject for GUI.
	*/
	public class Tokenizer {
		private readonly string source;
		public Tokenizer (string source) {
			this.source = source;
			
			var root = new TagPoint(0, Tag.ROOT);
			Tokenize(root, source);

			// Debug.LogError("root count:" + root.tagContents.Count);
			// foreach (var t in root.tagContents) {
			// 	Debug.LogError("tag:" + t.tag + " content:" + t.Stringify());
			// }
		}

		private void Tokenize (TagPoint p, string data) {
			var lines = data.Split('\n');
			
			var index = 0;

			// 大外のline読み
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
				
				var foundStartTagPoint = FindStartTag(lines, index);
				if (foundStartTagPoint.tag == Tag.NO_TAG_FOUND || foundStartTagPoint.tag == Tag.UNKNOWN) {
					// detect single closed tag. e,g, <tag something />.
					var foundSingleTagPoint = FindSingleTag(lines, index);

					if (foundSingleTagPoint.tag != Tag.NO_TAG_FOUND && foundSingleTagPoint.tag != Tag.UNKNOWN) {
						// closed tag found. add contents.
						// p.tagContents.Add(new TagContent(foundSingleTagPoint.tag, foundSingleTagPoint.kv));	
					} else {
						// not tag contained in this line. this line is just contents only. add to parent tag.
						var content = readLine;
						Debug.LogError("tag:" + p.tag + " content:" + content);
						// p.tagContents.Add(new TagContent(p.tag, content));	
					}
					
					index++;
					continue;
				}
				
				// foundStartTagPointがあるので、要素の開始が確定できた。最上位からの見出し的な要素を保持できるはず。
				
				while (true) {
					if (lines.Length <= index) {// 閉じタグが見つからなかった
						break;
					}

					readLine = lines[index];
					
					// find end of current tag.
					var foundEndTagPoint = FindEndTag(foundStartTagPoint, lines, index);
					if (foundEndTagPoint.tag != Tag.NO_TAG_FOUND && foundStartTagPoint.tag != Tag.UNKNOWN) {
						break;
					}

					// end tag is not contained in current line.
					index++;
				}

				// p.tagContents.AddRange(foundStartTagPoint.tagContents);
				index++;
			}
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

		public struct TagPoint {
			public readonly int lineIndex;
			public readonly Tag tag;
			public TagPoint (int lineIndex, Tag tag) {
				this.lineIndex = lineIndex;
				this.tag = tag;
			}
		}

		public struct TagContent {
			public readonly Tag tag;
			public readonly Dictionary<string, string> kv;
			public readonly string content;
			public TagContent (Tag tag, string content) {
				this.tag = tag;
				this.kv = new Dictionary<string, string>();
				this.content = content;
			}
			public TagContent (Tag tag, Dictionary<string, string> kv) {
				this.tag = tag;
				this.kv = kv;
				this.content = string.Empty;
			}
			public TagContent (Tag tag, Dictionary<string, string> kv, string content) {
				this.tag = tag;
				this.kv = kv;
				this.content = content;
			}
			public string Stringify () {
				var result = string.Empty;
				
				if (0 < kv.Count) {
					var kvArray = kv.Select(i => "key:" + i.Key + " val:" + i.Value).ToArray();
					result = string.Join(", ", kvArray);
				}

				if (!string.IsNullOrEmpty(content)) {
					if (!string.IsNullOrEmpty(result)) {
						result += "\n"; 
					}
					result += content;
				}

				return result;
			}
		}

		private TagPoint FindStartTag (string[] lines, int lineIndex) {
			var line = lines[lineIndex];

			// find <X>something...
			if (line.StartsWith("<")) {
				var closeIndex = line.IndexOf(">");

				if (closeIndex == -1) {
					return new TagPoint(lineIndex, Tag.NO_TAG_FOUND);
				}

				// check found tag end has closed tag mark or not.
				if (line[closeIndex-1] == '/') {
					// closed tag detected.
					return new TagPoint(lineIndex, Tag.NO_TAG_FOUND);
				}

				var tagName = string.Empty;
				var kvDict = new Dictionary<string, string>();

				// not closed tag. contains attr or not.
				if (line[closeIndex-1] == '"') {// <tag something="else">
					var contents = line.Substring(1 - 1).Split(' ');
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
					var kvs = kvDict.Select(i => i.Key + " " + i.Value).ToArray();
					Debug.LogError("tag:" + tagName + " contains kv:" + string.Join(", ", kvs));
					
				} else {
					tagName = line.Substring(1, closeIndex - 1);
				}
					
				switch (tagName) {
					case "h1": {
						return new TagPoint(lineIndex, Tag.H1);
					}
					case "h2": {
						return new TagPoint(lineIndex, Tag.H2);
					}
					case "h3": {
						return new TagPoint(lineIndex, Tag.H3);
					}
					case "h4": {
						return new TagPoint(lineIndex, Tag.H4);
					}
					case "h5": {
						return new TagPoint(lineIndex, Tag.H5);
					}
					case "p": {
						return new TagPoint(lineIndex, Tag.P);
					}
					case "img": {
						return new TagPoint(lineIndex, Tag.IMG);
					}
					case "ul": {
						return new TagPoint(lineIndex, Tag.UL);
					}
					case "ol": {
						return new TagPoint(lineIndex, Tag.OL);
					}
					case "li": {
						return new TagPoint(lineIndex, Tag.LI);
					}
					case "a": {
						return new TagPoint(lineIndex, Tag.A);
					}
					default: {
						return new TagPoint(lineIndex, Tag.UNKNOWN);
					}
				}
			}
			return new TagPoint(lineIndex, Tag.NO_TAG_FOUND);
		}

		private TagPoint FindSingleTag (string[] lines, int lineIndex) {
			var line = lines[lineIndex];

			// find <X>something...
			if (line.StartsWith("<")) {
				var closeIndex = line.IndexOf(" />");

				if (closeIndex == -1) {
					return new TagPoint(lineIndex, Tag.NO_TAG_FOUND);
				}

				var contents = line.Substring(1 - 1).Split(' ');

				if (contents.Length == 0) {
					return new TagPoint(lineIndex, Tag.NO_TAG_FOUND);
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
				
				switch (tagName) {
					case "h1": {
						return new TagPoint(lineIndex, Tag.H1);
					}
					case "h2": {
						return new TagPoint(lineIndex, Tag.H2);
					}
					case "h3": {
						return new TagPoint(lineIndex, Tag.H3);
					}
					case "h4": {
						return new TagPoint(lineIndex, Tag.H4);
					}
					case "h5": {
						return new TagPoint(lineIndex, Tag.H5);
					}
					case "p": {
						return new TagPoint(lineIndex, Tag.P);
					}
					case "img": {
						return new TagPoint(lineIndex, Tag.IMG);
					}
					case "ul": {
						return new TagPoint(lineIndex, Tag.UL);
					}
					case "ol": {
						return new TagPoint(lineIndex, Tag.OL);
					}
					case "li": {
						return new TagPoint(lineIndex, Tag.LI);
					}
					case "a": {
						return new TagPoint(lineIndex, Tag.A);
					}
					default: {
						return new TagPoint(lineIndex, Tag.UNKNOWN);
					}
				}
			}
			return new TagPoint(lineIndex, Tag.NO_TAG_FOUND);
		}

		private TagPoint FindEndTag (TagPoint p, string[] lines, int lineIndex) {
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
				return new TagPoint(lineIndex, Tag.NO_TAG_FOUND);
			}
			
			var contentsStrLines = lines.Where((i,l) => p.lineIndex <= l && l <= lineIndex).ToArray();
			
			// modify the line which contains start or end tag. exclude tag expression.
			contentsStrLines[0] = contentsStrLines[0].Substring(1 + p.tag.ToString().Length + 1);
			contentsStrLines[contentsStrLines.Length-1] = contentsStrLines[contentsStrLines.Length-1].Substring(0, contentsStrLines[contentsStrLines.Length-1].Length - endTag.Length);
			
			var contentsStr = string.Join("\n", contentsStrLines);

			// tokenize recursive.
			Tokenize(p, contentsStr);

			return p;
		}

		public int ComponentCount () {
			return -1;
		}

		public GameObject GetComponentAt (int index) {
			return null;
		}
	}

    [MTest] public void ParseLargeMarkdown () {
        var largeMd = "";
	}

	[MTest] public void DrawParsedMarkdown () {
		
	}
}
