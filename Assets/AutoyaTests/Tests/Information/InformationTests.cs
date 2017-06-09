// using Miyamasu;
// using MarkdownSharp;
// using UnityEngine;
// using System.Collections.Generic;
// using System;
// using System.Linq;
// using UnityEngine.UI;
// using UnityEngine.Events;
// using AutoyaFramework.Information;
// using System.Collections;
// using UnityEditor;

// public class InformationTests : MiyamasuTestRunner {
// 	[MSetup] public void Setup () {

// 		// GetTexture(url) runs only Play mode.
// 		if (!IsTestRunningInPlayingMode()) {
// 			SkipCurrentTest("Information feature should run on MainThread.");
// 		};
// 	}

// 	[MTest] public void ParseSmallMarkdown () {
// 		var sampleMd = @"
// An h1 header
// ============

// Paragraphs are separated by a blank line.

// 2nd paragraph. *Italic*, **bold**, and `monospace`. Itemized lists
// look like:

//   * this one
//   * that one
//   * the other one

// Note that --- not considering the asterisk --- the actual text
// content starts at 4-columns in.

// > Block quotes are
// > written like so.
// >
// > They can span multiple paragraphs,
// > if you like.

// Use 3 dashes for an em-dash. Use 2 dashes for ranges (ex., 'it's all
// in chapters 12--14'). Three dots ... will be converted to an ellipsis.
// Unicode is supported. ☺



// An h2 header
// ------------

// Here's a numbered list:

//  1. first item
//  2. second item
//  3. third item

// Note again how the actual text starts at 4 columns in (4 characters
// from the left side). Here's a code sample:

//     # Let me re-iterate ...
//     for i in 1 .. 10 { do-something(i) }

// As you probably guessed, indented 4 spaces. By the way, instead of
// indenting the block, you can use delimited blocks, if you like:

// ~~~
// define foobar() {
//     print 'Welcome to flavor country!';
// }
// ~~~

// (which makes copying & pasting easier). You can optionally mark the
// delimited block for Pandoc to syntax highlight it:

// ~~~python
// import time
// # Quick, count to ten!
// for i in range(10):
//     # (but not *too* quick)
//     time.sleep(0.5)
//     print i
// ~~~



// ### An h3 header ###

// Now a nested list:

//  1. First, get these ingredients:

//       * carrots
//       * celery
//       * lentils

//  2. Boil some water.

//  3. Dump everything in the pot and follow
//     this algorithm:

//         find wooden spoon
//         uncover pot
//         stir
//         cover pot
//         balance wooden spoon precariously on pot handle
//         wait 10 minutes
//         goto first step (or shut off burner when done)

//     Do not bump wooden spoon or it will fall.

// Notice again how text always lines up on 4-space indents (including
// that last line which continues item 3 above).

// Here's a link to [a website](http://foo.bar), to a [local
// doc](local-doc.html), and to a [section heading in the current
// doc](#an-h2-header). Here's a footnote [^1].

// [^1]: Footnote text goes here.

// Tables can look like this:

// size  material      color
// ----  ------------  ------------
// 9     leather       brown
// 10    hemp canvas   natural
// 11    glass         transparent

// Table: Shoes, their sizes, and what they're made of

// (The above is the caption for the table.) Pandoc also supports
// multi-line tables:

// --------  -----------------------
// keyword   text
// --------  -----------------------
// red       Sunsets, apples, and
//           other red or reddish
//           things.

// green     Leaves, grass, frogs
//           and other things it's
//           not easy being.
// --------  -----------------------

// A horizontal rule follows.

// ***

// Here's a definition list:

// apples
//   : Good for making applesauce.
// oranges
//   : Citrus!
// tomatoes
//   : There's no 'e' in tomatoe.

// Again, text is indented 4 spaces. (Put a blank line between each
// term/definition pair to spread things out more.)

// Here's a 'line block':

// | Line one
// |   Line too
// | Line tree

// and images can be specified like so:

// ![example image](example-image.jpg 'An exemplary image')

// Inline math equations go in like so: $\omega = d\phi / dt$. Display
// math should get its own line and be put in in double-dollarsigns:

// $$I = \int \rho R^{2} dV$$

// And note that you can backslash-escape any punctuation characters
// which you wish to be displayed literally, ex.: \`foo\`, \*bar\*, etc.
// 		";

// 		// Create new markdown instance
// 		Markdown mark = new Markdown();

// 		// Run parser
// 		string text = mark.Transform(sampleMd);
// 		Debug.LogError("text:" + text);


// 		/*
// 			次のようなhtmlが手に入るので、
// 			hX, p, img, ul, li, aとかのタグを見て、それぞれを「行単位の要素」に変形、uGUIの要素に変形できれば良さそう。
// 			tokenizer書くか。

// 			・tokenizerでいろんなコンポーネントに分解
// 			・tokenizerで分解したコンポーネントを、コンポーネント単位で生成
// 			・生成したコンポーネントを配置、描画
			
// 			で、これらの機能は、「N行目から」みたいな指定で描画できるようにしたい。

// 			描画範囲に対する生成が未完成。
// 		*/
// 		Debug.LogWarning("一個めの要素の描画抑制中。");
// 		// RunOnMainThread(
// 		// 	() => {
// 		// 		var tokenizer = new Tokenizer(text);

// 		// 		Action<IEnumerator> executor = i => {
// 		// 			var s = new GameObject("test0");
// 		// 			s.AddComponent<MainThreadRunner>().StartCoroutine(i);
// 		// 		};
				
// 		// 		// 全体を生成
// 		// 		var root = tokenizer.Materialize(
// 		// 			"test",
// 		// 			executor,
// 		// 			new Rect(0, 0, 1024, 400),
// 		// 			/*
// 		// 				要素ごとにpaddingを変更できる。
// 		// 			*/
// 		// 			(tag, depth, padding, kv) => {
// 		// 				// padding.left += 10;
// 		// 				// padding.top += 41;
// 		// 				// padding.bottom += 31;
// 		// 			},
// 		// 			/*
// 		// 				要素ごとに表示に使われているgameObjectへの干渉ができる。
// 		// 			 */
// 		// 			(go, tag, depth, kv) => {
						
// 		// 			}
// 		// 		);
				

// 		// 		var canvas = GameObject.Find("Canvas");
// 		// 		if (canvas != null) {
// 		// 			root.transform.SetParent(canvas.transform);
// 		// 		}

// 		// 		// var childlen = obj.transform.GetChildlen();

// 		// 		// for (var i = 0; i < tokenizer.ComponentCount(); i++) {
// 		// 		// 	tokenizer.Materialize(i);
// 		// 		// 	// これがuGUIのコンポーネントになってる
// 		// 		// }

// 		// 		/*
// 		// 			位置を指定して書き出すことができる(一番上がずれる = cursorか。)
// 		// 		*/
// 		// 		// for (var i = 1; i < tokenizer.ComponentCount(); i++) {
// 		// 		// 	var component = tokenizer.GetComponentAt(i);
// 		// 		// 	// これがuGUIのコンポーネントになってる
// 		// 		// }

// 		// 		// GameObject.DestroyImmediate(root);
// 		// 	}
// 		// );
// 	}

// 	// シンプルなヘッダひとつ
// 	[MTest] public void DrawParsedSimpleHeader () {
// 		var sample = @"
// # h1 Heading 8-)
// 		";
// 		Draw(sample);
// 	}

	
// 	// h1を2つ
// 	[MTest] public void DrawParsedSimpleContinuedSameHeaders () {
// 		var sample = @"
// # h1 Heading 8-)
// # h1-2 Heading
// 		";
// 		Draw(sample);
// 	}

// 	// br区切りのP
// 	[MTest] public void DrawParsedSimpleContinuedPTagsByBR () {
// 		var sample = @"
// p1 Heading 8-)  
// p2 Heading
// 		";
// 		Draw(sample);
// 	}

// 	// 連続するヘッダ
// 	[MTest] public void DrawParsedSimpleContinuedHeaders () {
// 		var sample = @"
// # h1 Heading 8-)
// ## h2 Heading
// 		";
// 		Draw(sample);
// 	}

// 	// headers
// 	[MTest] public void DrawParsedSimpleHeaders () {
// 		var sample = @"
// # h1 Heading 8-)
// ## h2 Heading
// ### h3 Heading
// #### h4 Heading
// ##### h5 Heading
// ###### h6 Heading
// 		";
// 		Draw(sample);
// 	}

// 	[MTest] public void DrawSingleLongContent () {
// 		var sample = @"
// aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
// ";
// 		Draw(sample);
// 	}
// 	[MTest] public void DrawMultipleLongContent () {
// 		var sample = @"
// __testlen__ aaaaaaaaaaaaEndOfL0 bbbbbbbbbbbbbbbbbbbbbbbbbbbbbEndOfL1
// ";
// 		Draw(sample);
// 	}

// 	[MTest] public void DrawMultipleLongContent2 () {
// 		var sample = @"
// __testlen__ aaaaaaaaaaaaEndOfL0 bbbbbbbbbbbbbbbbbbbbbbbbbbbbbEndOfL1 iiicdefgEndOfL2
// ";
// 		Draw(sample);
// 	}

// 	[MTest] public void DrawUListSingle () {
// 		var sample = @"
// - test  fmm __hom__ hehe
// ";
// 		Draw(sample);
// 	}

// 	[MTest] public void DrawUListWithA () {
// 		var sample = @"
// - __[title](https://url/)__ - high quality and fast image resize in browser.
// ";
// 		Draw(sample);
// 	}

// 	[MTest] public void DrawPreWithA () {
// 		var sample = @"
// 	- __[title](https://url/)__ - high quality and fast image(br)  
// resize in browser.
// ";
// 		Draw(sample);
// 	}

// 	[MTest] public void DrawNearestToWidthLimit () {
// 		var sample = @"
// aidaa https://octodex.github.com/images/dojocat.jpg  'The Dojocat'";
// 		Draw(sample);
// 	}

// 	[MTest] public void DrawSmallMarkdown () {
// 		var sample = @"
// ---
// testtest!  
// something  

// __Advertisement :)__
//  fmmm


// - test  fmm __hom__ hehe
// - __[pica](https://nodeca.github.io/pica/demo/)__ - high quality and fast image resize in browser.
// - __[babelfish](https://github.com/nodeca/babelfish/)__ - developer friendly i18n with plurals support and easy syntax.

// You will like those projects!

// ---

// # h1 Heading 8-)
// ## h2 Heading
// ### h3 Heading
// #### h4 Heading
// ##### h5 Heading
// ###### h6 Heading
// 		";
// 		Draw(sample);
// 	}

// 	[MTest] public void DrawUnorderedList () {
// 		var sample = @"
// ## Lists

// Unordered

// + Create a list by starting a line with `+`, `-`, or `*`
// + Sub-lists are made by indenting 2 spaces:
//   - Marker character change forces new list start:
//     * Ac tristique libero volutpat at
//     + Facilisis in pretium nisl aliquet
//     - Nulla volutpat aliquam velit
// + Very easy!
// ";
// 		Draw(sample);
// 	}

// 	[MTest]	public void DrawOrderedList () {
// 		var sample = @"
// Ordered

// 1. Lorem ipsum dolor sit amet
// 2. Consectetur adipiscing elit
// 3. Integer molestie lorem at massa

// 1. You can use sequential numbers...
// 1. ...or keep all the numbers as `1.`
// ";
// 		Draw(sample);
// 	}

// 	[MTest]	public void DrawNumbererdList () {
// 		var sample = @"
// Start numbering with offset:

// 57. foo
// 1. bar
// ";
// 		Draw(sample);
// 	}

// 	[MTest] public void DrawBlockQuote () {
// 		var sample = @"
// ## Blockquotes

// > Blockquotes can also be nested...
// ";
// 		Draw(sample);
// 	}

// 	[MTest] public void DrawNestedBlockQuote () {
// 		var sample = @"
// ## Blockquotes

// > Blockquotes can also be nested...
// >> double bq,
// ";
// 		Draw(sample);
// 	}
	
// 	[MTest] public void DrawMoreNestedBlockQuote () {
// 		var sample = @"
// ## Blockquotes

// > Blockquotes can also be nested...
// >> double bq,
// > > > triple bq.
// ";
// 		Draw(sample);
// 	}




	
// 	[MTest] public void DrawMiddleSizeMarkdown () {
// 		var sample = @"
// # Autoya
// ver 0.8.4

// ![loading](https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true)

// small, thin framework for Unity.  
// which contains essential game features.

// ## Features
// * Authentication handling
// * AssetBundle load/management
// * HTTP/TCP/UDP Connection feature
// * Maintenance changed handling
// * Purchase/IAP feature
// * Notification(local/remote)
// * Information


// ## Motivation
// Unity already contains these feature's foundation, but actually we need more codes for using it in app.

// This framework can help that.

// ## License
// see below.  
// [LICENSE](./LICENSE)


// ## Progress

// ### automatic Authentication
// already implemented.

// ###AssetBundle list/preload/load
// already implemented.

// ###HTTP/TCP/UDP Connection feature
// | Protocol        | Progress     |
// | ------------- |:-------------:|
// | http/1 | done | 
// | http/2 | not yet | 
// | tcp      | not yet      | 
// | udp	| not yet      |  


// ###app-version/asset-version/server-condition changed handles
// already implemented.

// ###Purchase/IAP flow
// already implemented.

// ###Notification(local/remote)
// in 2017 early.

// ###Information
// in 2017 early.


// ## Tests
// implementing.


// ## Installation
// unitypackage is ready!

// 1. use Autoya.unitypackage.
// 2. add Purchase plugin via Unity Services.
// 3. done!

// ## Usage
// all example usage is in Assets/AutoyaSamples folder.

// yes,(2spaces linebreak)  
// 2s break line will be expressed with [br /].

// then,(hard break)
// hard break will appear without [br /].

// 		";
// 		Draw(sample);
// 	}

// 	[MTest] public void DrawMiddleSizeMarkdown2 () {
// 		var sample = @"
// # Autoya

// small, thin framework for Unity.  
// which contains essential game features.

// ver 0.8.4

// ![loading](https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true)

// まずは4連になるケース(部分改行を無視する)
// あ
// い
// ううう
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true' width='100' height='200' />
// small, thin framework for Unit1.
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' height='200' />

// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true' width='100' height='200' />
// small Br Unit2.  
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' height='200' />

// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='301' height='20' />

// test
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='301' height='20' />
// test2



// ## Features
// * Authentication handling
// * AssetBundle load/management
// * HTTP/TCP/UDP Connection feature
// * Maintenance changed handling
// * Purchase/IAP feature
// * Notification(local/remote)
// * Information

// ## Motivation
// Unity already contains these feature's foundation, but actually we need more codes for using it in app.

// This framework can help that.

// ## License


// see below.  
// [LICENSE](./LICENSE)

// ## Progress

// ### automatic Authentication
// already implemented.

// ###AssetBundle list/preload/load
// already implemented.

// ###HTTP/TCP/UDP Connection feature
// | Protocol        | Progress     |
// | ------------- |:-------------:|
// | http/1 | done | 
// | http/2 | not yet | 
// | tcp      | not yet      | 
// | udp	| not yet      |  


// ###app-version/asset-version/server-condition changed handles
// already implemented.

// ###Purchase/IAP flow
// already implemented.

// ###Notification(local/remote)
// in 2017 early.

// ###Information
// in 2017 early.


// ## Tests
// implementing.


// ## Installation
// unitypackage is ready!

// 1. use Autoya.unitypackage.
// 2. add Purchase plugin via Unity Services.
// 3. done!

// ## Usage
// all example usage is in Assets/AutoyaSamples folder.

// yes,(2spaces linebreak)  
// 2s break line will be expressed with <br />.

// then,(hard break)
// hard break will appear without <br />.
// 		";
// 		Draw(sample);
// 	}

// 	[MTest] public void DrawVeryLargeMarkdown () {
// 		var sample = @"
// ---
// __Advertisement :)__

// - __[pica](https://nodeca.github.io/pica/demo/)__ - high quality and fast image 
// resize in browser.
// - __[babelfish](https://github.com/nodeca/babelfish/)__ - developer friendly
//   i18n with plurals support and easy syntax.

// You will like those projects!

// ---

// # h1 Heading 8-)
// ## h2 Heading
// ### h3 Heading
// #### h4 Heading
// ##### h5 Heading
// ###### h6 Heading


// ## Horizontal Rules

// ___

// ---

// ***


// ## Typographic replacements

// Enable typographer option to see result.

// (c) (C) (r) (R) (tm) (TM) (p) (P) +-

// test.. test... test..... test?..... test!....

// !!!!!! ???? ,,  -- ---

// 'Smartypants, double quotes' and 'single quotes'


// ## Emphasis

// **This is bold text**

// __This is bold text__

// *This is italic text*

// _This is italic text_

// ~~Strikethrough~~


// ## Blockquotes


// > Blockquotes can also be nested...
// >> ...by using additional greater-than signs right next to each other...
// > > > ...or with spaces between arrows.


// ## Lists

// Unordered

// + Create a list by starting a line with `+`, `-`, or `*`
// + Sub-lists are made by indenting 2 spaces:
//   - Marker character change forces new list start:
//     * Ac tristique libero volutpat at
//     + Facilisis in pretium nisl aliquet
//     - Nulla volutpat aliquam velit
// + Very easy!

// Ordered

// 1. Lorem ipsum dolor sit amet
// 2. Consectetur adipiscing elit
// 3. Integer molestie lorem at massa


// 1. You can use sequential numbers...
// 1. ...or keep all the numbers as `1.`

// Start numbering with offset:

// 57. foo
// 1. bar


// ## Code

// Inline `code`

// Indented code

//     // Some comments
//     line 1 of code
//     line 2 of code
//     line 3 of code


// Block code 'fences'

// ```
// Sample text here...
// ```

// Syntax highlighting

// ``` js
// var foo = function (bar) {
//   return bar++;
// };

// console.log(foo(5));
// ```

// ## Tables

// | Option | Description |
// | ------ | ----------- |
// | data   | path to data files to supply the data that will be passed into templates. |
// | engine | engine to be used for processing templates. Handlebars is the default. |
// | ext    | extension to be used for dest files. |

// Right aligned columns

// | Option | Description |
// | ------:| -----------:|
// | data   | path to data files to supply the data that will be passed into templates. |
// | engine | engine to be used for processing templates. Handlebars is the default. |
// | ext    | extension to be used for dest files. |


// ## Links

// [link text](http://dev.nodeca.com)

// [link with title](http://nodeca.github.io/pica/demo/ 'title text!')

// Autoconverted link https://github.com/nodeca/pica (enable linkify to see)


// ## Images

// ![Minion](https://octodex.github.com/images/minion.png)
// ![Stormtroopocat](https://octodex.github.com/images/stormtroopocat.jpg 'The Stormtroopocat')

// Like links, Images also have a footnote style syntax

// ![Alt text][id]

// With a reference later in the document defining the URL location:

// [id]: https://octodex.github.com/images/dojocat.jpg  'The Dojocat'


// ## Plugins

// The killer feature of `markdown-it` is very effective support of
// [syntax plugins](https://www.npmjs.org/browse/keyword/markdown-it-plugin).


// ### [Emojies](https://github.com/markdown-it/markdown-it-emoji)

// > Classic markup: :wink: :crush: :cry: :tear: :laughing: :yum:
// >
// > Shortcuts (emoticons): :-) :-( 8-) ;)

// see [how to change output](https://github.com/markdown-it/markdown-it-emoji#change-output) with twemoji.


// ### [Subscript](https://github.com/markdown-it/markdown-it-sub) / [Superscript](https://github.com/markdown-it/markdown-it-sup)

// - 19^th^
// - H~2~O


// ### [\<ins>](https://github.com/markdown-it/markdown-it-ins)

// ++Inserted text++


// ### [\<mark>](https://github.com/markdown-it/markdown-it-mark)

// ==Marked text==


// ### [Footnotes](https://github.com/markdown-it/markdown-it-footnote)

// Footnote 1 link[^first].

// Footnote 2 link[^second].

// Inline footnote^[Text of inline footnote] definition.

// Duplicated footnote reference[^second].

// [^first]: Footnote **can have markup**

//     and multiple paragraphs.

// [^second]: Footnote text.


// ### [Definition lists](https://github.com/markdown-it/markdown-it-deflist)

// Term 1

// :   Definition 1
// with lazy continuation.

// Term 2 with *inline markup*

// :   Definition 2

//         { some code, part of Definition 2 }

//     Third paragraph of definition 2.

// _Compact style:_

// Term 1
//   ~ Definition 1

// Term 2
//   ~ Definition 2a
//   ~ Definition 2b


// ### [Abbreviations](https://github.com/markdown-it/markdown-it-abbr)

// This is HTML abbreviation example.

// It converts 'HTML', but keep intact partial entries like 'xxxHTMLyyy' and so on.

// *[HTML]: Hyper Text Markup Language

// ### [Custom containers](https://github.com/markdown-it/markdown-it-container)

// ::: warning
// *here be dragons*
// :::
// 		";
// 		Draw(sample);
// 	}

// 	// test case from bug.
// 	[MTest] public void AvoidFakeTag () {
// 		var sample = @"smal<Br Unit2.  ";
// 		Draw(sample);
// 	}

// 	// tagに見える文字列がタグとして処理される。
// 	[MTest] public void AvoidFakeTag2 () {
// 		var sample = @"
// # h1 Heading 8-)
// smal<Br Unit2.  
// ## h2 Heading
// 		";
// 		Draw(sample);
// 	}

// 	[MTest] public void AvoidFakeTag3 () {
// 		var sample = @"
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true' width='100' height='20' />
// smal<Br Unit2.  
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' height='20' />
// 		";
// 		Draw(sample);
// 	}

// 	[MTest] public void ResizeLargeImage () {
// 		var sample = @"
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='301' height='20' />

// test
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='301' height='20' />
// test2		
// ";
// 		Draw(sample);
// 	}

// 	[MTest] public void LoadLocalImage () {
// 		// load image from Resources/informationTest/icon.png
// 		var sample = @"
// <img src='informationTest/icon.png' width='366' height='366' />
// ";
// 		Draw(sample);
// 	}

// 	[MTest] public void LoadAssetBundleImage () {
// 		Debug.LogError("not yet applied.");
// 		// load image from AssetBundle which contains informationTest/icon.png
// 		var sample = @"
// <img src='assetbundle://informationTest/icon.png' width='301' height='20' />
// ";
// 		Draw(sample);
// 	}

// 	[MTest] public void LoadAssetBundleImageWithRelativePath () {
// 		// load image from Resources/informationTest/icon.png
// 		var sample = @"
// <img src='./informationTest/icon.png' width='301' height='20' />
// ";
// 		Draw(sample);
// 	}
	
// 	[MTest] public void DrawTable () {
// 		var sample = @"
// <table>
// <thead>
// <tr>
// <th>row1</th>
// <th>row2</th>
// </tr>
// </thead>
// <tbody>
// <tr>
// <td>col1 ~~~~~~~~~~~~~~~~~~~~~~</td>
// <td>col2 ~~~~~~~~~~~~ .</td>
// </tr>
// <tr>
// <td>col1-2 ~~~~~~~ </td>
// <td>col2-2 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~ </td>
// </tr>
// <tr>
// <td>col1-3</td>
// <td>col2-3</td>
// </tr>
// </tbody>
// </table>
// ";

// 		Draw(sample, 500);
// 	}

// 	[MTest] public void DrawTableWhichContainsImages () {
// 		var sample = @"
// <table>
// <thead>
// <tr>
// <th>row1--------------------------------------</th>
// <th>row2----------------------------------------------</th>
// </tr>
// </thead>
// <tbody>
// <tr>
// <td>fmm <a href='test'>test</a> 
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true' width='100' height='10' />
// </td>
// <td>col2 ~~~~~~~~~~~~ .</td>
// </tr>
// <tr>
// <td>col1-2 ~~~~~~~ </td>
// <td>col2-2 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~ </td>
// </tr>
// <tr>
// <td>col1-3</td>
// <td>col2-3</td>
// </tr>
// </tbody>
// </table>
// ";
// 		Draw(sample, 800);
// 	}

// 	[MTest] public void WellDesignedView () {
// 		var sample = @"
		
// 		";

// 		Draw(sample, 800);
// 	}

// 	private static int index;

// 	private void Draw (string sample, int width=300) {
// 		// Create new markdown instance
// 		Markdown mark = new Markdown();

// 		var text = mark.Transform(sample);
		
// 		RunEnumeratorOnMainThread(
// 			RunEnum(text, width)
// 		);

// 		index+=width;
// 	}

// 	private IEnumerator RunEnum (string text, int width) {
// 		var tokenizer = new Tokenizer(text);
		
// 		var coroutineCount = 0;

// 		Action<IEnumerator> executor = i => {
// 			coroutineCount++;
			
// 			EditorApplication.CallbackFunction d = null;

// 			d = () => {
// 				var result = i.MoveNext();
// 				if (!result) {
// 					EditorApplication.update -= d;
// 					coroutineCount--;
// 				} 
// 			};

// 			// start running.
// 			EditorApplication.update += d;
// 		};

// 		var root = tokenizer.Materialize(
// 			"test",
// 			executor,
// 			new View(width, 4000, 0),
// 			(tag, depth, padding, kv) => {},
// 			(go, tag, depth, kv) => {}
// 		);

// 		var rect = root.GetComponent<RectTransform>();

// 		// 横にずらす
// 		rect.anchoredPosition = new Vector2(index, 0);

// 		var canvas = GameObject.Find("Canvas");
// 		if (canvas != null) {
// 			root.transform.SetParent(canvas.transform);
// 		}

// 		while (coroutineCount != 0) {
// 			yield return null;
// 		}
// 	}
// }
