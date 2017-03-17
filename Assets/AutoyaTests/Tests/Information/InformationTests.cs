using Miyamasu;
using MarkdownSharp;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;
using AutoyaFramework.Information;

public class InformationTests : MiyamasuTestRunner {
	[MTest] public void ParseSmallMarkdown () {

		var sampleMd = @"
# Autoya

small, thin framework for Unity.  
which contains essential game features.

>>>>>>> dev_information
ver 0.8.4

![loading](https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true)

まずは4連になるケース(部分改行を無視する)
あ
い
ううう
<img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true' width='100' height='200' />
small, thin framework for Unit1.
<img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' height='200' />

<img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true' width='100' height='200' />
smal<Br Unit2.  
<img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' height='200' />

<img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='301' height='20' />

test
<img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='301' height='20' />
test2



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
		
		sampleMd = @"
An h1 header
============

Paragraphs are separated by a blank line.

2nd paragraph. *Italic*, **bold**, and `monospace`. Itemized lists
look like:

  * this one
  * that one
  * the other one

Note that --- not considering the asterisk --- the actual text
content starts at 4-columns in.

> Block quotes are
> written like so.
>
> They can span multiple paragraphs,
> if you like.

Use 3 dashes for an em-dash. Use 2 dashes for ranges (ex., 'it's all
in chapters 12--14'). Three dots ... will be converted to an ellipsis.
Unicode is supported. ☺



An h2 header
------------

Here's a numbered list:

 1. first item
 2. second item
 3. third item

Note again how the actual text starts at 4 columns in (4 characters
from the left side). Here's a code sample:

    # Let me re-iterate ...
    for i in 1 .. 10 { do-something(i) }

As you probably guessed, indented 4 spaces. By the way, instead of
indenting the block, you can use delimited blocks, if you like:

~~~
define foobar() {
    print 'Welcome to flavor country!';
}
~~~

(which makes copying & pasting easier). You can optionally mark the
delimited block for Pandoc to syntax highlight it:

~~~python
import time
# Quick, count to ten!
for i in range(10):
    # (but not *too* quick)
    time.sleep(0.5)
    print i
~~~



### An h3 header ###

Now a nested list:

 1. First, get these ingredients:

      * carrots
      * celery
      * lentils

 2. Boil some water.

 3. Dump everything in the pot and follow
    this algorithm:

        find wooden spoon
        uncover pot
        stir
        cover pot
        balance wooden spoon precariously on pot handle
        wait 10 minutes
        goto first step (or shut off burner when done)

    Do not bump wooden spoon or it will fall.

Notice again how text always lines up on 4-space indents (including
that last line which continues item 3 above).

Here's a link to [a website](http://foo.bar), to a [local
doc](local-doc.html), and to a [section heading in the current
doc](#an-h2-header). Here's a footnote [^1].

[^1]: Footnote text goes here.

Tables can look like this:

size  material      color
----  ------------  ------------
9     leather       brown
10    hemp canvas   natural
11    glass         transparent

Table: Shoes, their sizes, and what they're made of

(The above is the caption for the table.) Pandoc also supports
multi-line tables:

--------  -----------------------
keyword   text
--------  -----------------------
red       Sunsets, apples, and
          other red or reddish
          things.

green     Leaves, grass, frogs
          and other things it's
          not easy being.
--------  -----------------------

A horizontal rule follows.

***

Here's a definition list:

apples
  : Good for making applesauce.
oranges
  : Citrus!
tomatoes
  : There's no 'e' in tomatoe.

Again, text is indented 4 spaces. (Put a blank line between each
term/definition pair to spread things out more.)

Here's a 'line block':

| Line one
|   Line too
| Line tree

and images can be specified like so:

![example image](example-image.jpg 'An exemplary image')

Inline math equations go in like so: $\omega = d\phi / dt$. Display
math should get its own line and be put in in double-dollarsigns:

$$I = \int \rho R^{2} dV$$

And note that you can backslash-escape any punctuation characters
which you wish to be displayed literally, ex.: \`foo\`, \*bar\*, etc.
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

			描画範囲に対する生成が未完成。
		*/
		RunOnMainThread(
			() => {
				var tokenizer = new Tokenizer(text);

				// 全体を生成
				var root = tokenizer.Materialize(
					"test",
					new Rect(0, 0, 1024, 400),
					/*
						要素ごとにpaddingを変更できる。
					*/
					(tag, depth, padding, kv) => {
						// padding.left += 10;
						// padding.top += 41;
						// padding.bottom += 31;
					},
					/*
						要素ごとに表示に使われているgameObjectへの干渉ができる。
					 */
					(go, tag, depth, kv) => {
						
					}
				);
				

				var canvas = GameObject.Find("Canvas");
				if (canvas != null) {
					root.transform.SetParent(canvas.transform);
				}

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

				// GameObject.DestroyImmediate(root);
			}
		);
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
