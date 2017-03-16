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

				GameObject.DestroyImmediate(root);
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
