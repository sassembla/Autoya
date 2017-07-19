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

// /**
// 	test for markdown based view.
//  */
// public class InformationMarkdownTests : MiyamasuTestRunner {
// 	[MSetup] public void Setup () {

// 		// GetTexture(url) runs only Play mode.
// 		if (!IsTestRunningInPlayingMode()) {
// 			SkipCurrentTest("Information feature should run on MainThread.");
// 		};
// 	}

// 	[MTest] public void CommentOnly () {
// 		var sample = @"
// <!-- + text + -->
// ";
//  		DrawMarkdown(sample);
// 	}

// 	[MTest] public void MarkdownWithComment () {
// var sample = @"
// <!-- + text + -->
// hi!
//  ";
//  		DrawMarkdown(sample);
// 	}

// 	// シンプルなヘッダひとつ
// 	[MTest] public void DrawParsedSimpleHeader () {
// 		var sample = @"
// # h1 Heading 8-)
// 		";
// 		DrawMarkdown(sample);
// 	}

	
// 	// h1を2つ
// 	[MTest] public void DrawParsedSimpleContinuedSameHeaders () {
// 		var sample = @"
// # h1 Heading 8-)
// # h1-2 Heading
// 		";
// 		DrawMarkdown(sample);
// 	}

// 	// br区切りのP
// 	[MTest] public void DrawParsedSimpleContinuedPTagsByBR () {
// 		var sample = @"
// p1 Heading 8-)  
// p2 Heading
// 		";
// 		DrawMarkdown(sample);
// 	}

// 	// 連続するヘッダ
// 	[MTest] public void DrawParsedSimpleContinuedHeaders () {
// 		var sample = @"
// # h1 Heading 8-)
// ## h2 Heading
// 		";
// 		DrawMarkdown(sample);
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
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void DrawSingleLongContent () {
// 		var sample = @"
// aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
// ";
// 		DrawMarkdown(sample);
// 	}
// 	[MTest] public void DrawMultipleLongContent () {
// 		var sample = @"
// __testlen__ aaaaaaaaaaaaEndOfL0 bbbbbbbbbbbbbbbbbbbbbbbbbbbbbEndOfL1
// ";
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void DrawMultipleLongContent2 () {
// 		var sample = @"
// __testlen__ aaaaaaaaaaaaEndOfL0 bbbbbbbbbbbbbbbbbbbbbbbbbbbbbEndOfL1 iiicdefgEndOfL2
// ";
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void DrawUListSingle () {
// 		var sample = @"
// - test  fmm __hom__ hehe
// ";
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void DrawUListWithA () {
// 		var sample = @"
// - __[title](https://url/)__ - high quality and fast image resize in browser.
// ";
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void DrawPreWithA () {
// 		var sample = @"
// 	- __[title](https://url/)__ - high quality and fast image(br)  
// resize in browser.
// ";
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void DrawNearestToWidthLimit () {
// 		var sample = @"
// aidaa https://octodex.github.com/images/dojocat.jpg  'The Dojocat'";
// 		DrawMarkdown(sample);
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
// 		DrawMarkdown(sample);
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
// 		DrawMarkdown(sample);
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
// 		DrawMarkdown(sample);
// 	}

// 	[MTest]	public void DrawNumbererdList () {
// 		var sample = @"
// Start numbering with offset:

// 57. foo
// 1. bar
// ";
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void DrawBlockQuote () {
// 		var sample = @"
// ## Blockquotes

// > Blockquotes can also be nested...
// ";
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void DrawNestedBlockQuote () {
// 		var sample = @"
// ## Blockquotes

// > Blockquotes can also be nested...
// >> double bq,
// ";
// 		DrawMarkdown(sample);
// 	}
	
// 	[MTest] public void DrawMoreNestedBlockQuote () {
// 		var sample = @"
// ## Blockquotes

// > Blockquotes can also be nested...
// >> double bq,
// > > > triple bq.
// ";
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void DrawImage () {
// 		var sample = @"
// ![loading](https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true)
// ";		
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void DrawNotExistImage () {
// 		var sample = @"
// ![loading](https://github.com/sassembla/Autoya/blob/master/doc/scr_notexist.png?raw=true)
// ";		
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void DrawImageWithPercentWidthAndHeight () {
// 		var sample = @"
// ![loading](https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true width='100%' height='100%')
// ";		
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void DrawImageWithPercentWidthOnly () {
// 		var sample = @"
// ![loading](https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true width='100%')
// ";		
// 		DrawMarkdown(sample);
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
// 		DrawMarkdown(sample);
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
// 		DrawMarkdown(sample);
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

// 100. foo
// 2. bar
// 4. bar

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

// ### [\ins](https://github.com/markdown-it/markdown-it-ins)

// ++Inserted text++


// ### [\mark](https://github.com/markdown-it/markdown-it-mark)

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
// 		DrawMarkdown(sample);
// 	}

// 	// test case from bug.
// 	[MTest] public void AvoidFakeTag () {
// 		var sample = @"smal<Br Unit2.  ";
// 		DrawMarkdown(sample);
// 	}

// 	// tagに見える文字列がタグとして処理される。
// 	[MTest] public void AvoidFakeTag2 () {
// 		var sample = @"
// # h1 Heading 8-)
// smal<Br Unit2.  
// ## h2 Heading
// 		";
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void AvoidFakeTag3 () {
// 		var sample = @"
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true' width='100' height='20' />
// smal<Br Unit2.  
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' height='20' />
// 		";
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void ResizeLargeImage () {
// 		var sample = @"
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='301' height='20' />

// test
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='301' height='20' />
// test2		
// ";
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void SetImageWidthByPer () {
// 		var sample = @"
// ratioTest(100% x 20%)
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100%' height='20%' />

// ratioTest(100% x 30%)
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100%' height='30%' />

// ratioTest(100% width)
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100%' />

// ratioTest(100% height)
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' height='100%' />

// ";
// 		DrawMarkdown(
// 			sample, 
// 			300, 
// 			300, 
// 			progress => {
// 				Debug.Log("progress:" + progress);
// 			},
// 			() => {
// 				Debug.Log("done.");
// 			}
// 		);
// 	}

// 	[MTest] public void LoadLocalImage () {
// 		// load image from Resources/informationTest/icon.png
// 		var sample = @"
// <img src='informationTest/icon.png' width='366' height='366' />
// ";
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void LoadAssetBundleImage () {
// 		Debug.LogError("not yet applied.");
// 		// load image from AssetBundle which contains informationTest/icon.png
// 		var sample = @"
// <img src='assetbundle://informationTest/icon.png' width='301' height='20' />
// ";
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void LoadAssetBundleImageWithRelativePath () {
// 		// load image from Resources/informationTest/icon.png
// 		var sample = @"
// <img src='./informationTest/icon.png' width='301' height='20' />
// ";
// 		DrawMarkdown(sample);
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

// 		DrawMarkdown(sample, 500);
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
// 		DrawMarkdown(sample, 800);
// 	}

// 	[MTest] public void BugFix_IlligalPreferredWidth () {
// 		/*
// 			幅指定300で折り返しサイズを測ろうとするも、preferredWidthが301とかを返してくるコーナーケースが存在する。
// 			レイアウトの結果画面幅を超えるpreferredWidthが来た際、その微差を丸め込むという処理で対処した。
// 		 */
// 		var sample = @"
// With a reference later in the document defining the URL location xxxxx:
// 		";

// 		DrawMarkdown(sample, 300);
// 	}

// 	[MTest] public void GetWrongRoot () {
// 		var sample = @"
// <p start='100'>test</p><h2>my h2</h2>
// 		";
// 		DrawMarkdown(sample);
// 	}

// 	[MTest] public void GetProgress () {
// 		var sample = @"
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' height='100%' />
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' height='100%' />
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' height='100%' />
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' height='100%' />
// <img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' height='100%' />
// 		";
// 		DrawMarkdown(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."));
// 	}

// 	[MTest] public void LoadSpecificView_Default () {
// 		// should fail.
// 		var sample = @"
// <!--depth asset list url(resources://somewhere)-->
// ";
// 		try {
// 			DrawMarkdown(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
// 			Assert(false, "should not reach here.");
// 		} catch {
// 			// pass.
// 		}
// 	}

// 	[MTest] public void LoadSpecificView_MyView_DepthNotContained () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/MyView/DepthAssetList)-->
// something.
// ";	
// 		/*
// 			markdownとして解釈されるので、somethingは<p>something</p>になる。
// 			このdepthはp/somethingになるんだけど、これはdepthAssetListに含まれていない。
// 		 */
// 		DrawMarkdown(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "MyView");
// 	}
	
	
// 	private static int index;

// 	private void DrawMarkdown (string sample, int width=800, int height=1000, Action<double> progress=null, Action done=null, string viewName="Default") {
// 		// get head comment tag from source. then set it to head of generated html from markdown.
// 		var outsideCommentCode = GetHeadCommentCode(sample);

// 		Markdown mark = new Markdown();
// 		var html = outsideCommentCode + mark.Transform(sample);
		
// 		RunEnumeratorOnMainThread(
// 			RunEnum(viewName, html, width, height, progress, done)
// 		);

// 		index+=width;
// 	}

// 	private string GetHeadCommentCode (string source) {
// 		var commentTagStartIndex = source.IndexOf("<!--");
// 		if (commentTagStartIndex == -1) {
// 			return string.Empty;
// 		}

// 		var commentTagEndBeganIndex = source.IndexOf("-->", commentTagStartIndex);
// 		if (commentTagEndBeganIndex == -1) {
// 			throw new Exception("failed to parse comment tag in markdown. index:" + commentTagStartIndex);
// 		}

// 		var commentTagEndIndex = commentTagEndBeganIndex + "-->".Length;
// 		var commentCode = source.Substring(commentTagStartIndex, commentTagEndIndex - commentTagStartIndex);

// 		return commentCode;
// 	}

// 	private IEnumerator RunEnum (string viewName, string html, int width, int height, Action<double> progress, Action done) {
// 		/*
// 			実行ブロック。
// 			変換器自体を作り、それにビューとテキストを渡すとインスタンスを返してくる。
// 		 */
// 		var coroutineCount = 0;

// 		Action<IEnumerator> executor = i => {
// 			coroutineCount++;
// 			#if UNITY_EDITOR
// 			{
// 				UnityEditor.EditorApplication.CallbackFunction d = null;

// 				d = () => {
// 					var result = i.MoveNext();
// 					if (!result) {
// 						UnityEditor.EditorApplication.update -= d;
// 						coroutineCount--;
// 					} 
// 				};

// 				// start running.
// 				UnityEditor.EditorApplication.update += d;
// 			}
// 			#else 
// 			Debug.LogError("do same things with runtime.");
// 			// {
// 			// 	UnityEditor.EditorApplication.CallbackFunction d = null;

// 			// 	d = () => {
// 			// 		var result = i.MoveNext();
// 			// 		if (!result) {
// 			// 			UnityEditor.EditorApplication.update -= d;
// 			// 			coroutineCount--;
// 			// 		} 
// 			// 	};

// 			// 	// start running.
// 			// 	UnityEditor.EditorApplication.update += d;
// 			// }
// 			#endif
// 		};

// 		var v = new ViewGenerator(executor);
// 		var root = v.GenerateViewFromSource(
// 			viewName, 
// 			html,
// 			new ViewBox(width, height, 0), 
// 			progress, 
// 			done
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
