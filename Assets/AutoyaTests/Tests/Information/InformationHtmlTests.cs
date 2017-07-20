// using Miyamasu;
// using UnityEngine;
// using System.Collections.Generic;
// using System;
// using System.Linq;
// using UnityEngine.UI;
// using UnityEngine.Events;
// using AutoyaFramework.Information;
// using System.Collections;

// /**
// 	test for html based view.
//  */
// public class InformationHtmlTests : MiyamasuTestRunner {
// 	[MSetup] public void Setup () {

// 		// GetTexture(url) runs only Play mode.
// 		if (!IsTestRunningInPlayingMode()) {
// 			SkipCurrentTest("Information feature should run on MainThread.");
// 		};
// 	}

// 	[MTest] public void NoTagContained () {
// 		var sample = @"";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
// 	}
	
// 	[MTest] public void NoValidTagContained () {
// 		var sample = @"
// <!DOCTYPE html>
// "
// ;
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
// 	}

// 	[MTest] public void EmptyContents () {
// 		var sample = @"
// <head>something</head>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
// 	}

// 	[MTest] public void BodyContents () {
// var sample = @"
// <head>something</head>
// <body>else</body>
// ";

// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
// 	}

// 	// このテストはParserのテストに行くべき。
// // 	[MTest] public void LoadInvalidView () {
// // 		var sample = @"
// // something.
// // ";
// // 		try {
// // 			DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
// // 			Assert(false, "should not come here.");
// // 		} catch (Exception e) {
// // 			// do nothng.
// // 		}
// // 	}

// 	[MTest] public void LoadHtmlView () {
// 		var sample = @"
// <!DOCTYPE html>
// <title>Small HTML 5</title>
// <p>Hello world</p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
// 	}

// 	[MTest] public void LoadSmallHtmlView () {
// 		var sample = @"
// <!DOCTYPE html>
// <head></head>
// <body>
// <p>something
// <p>else</p>
// </p>
// </body>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
// 	}

// 	[MTest] public void CommentOnlyView () {
// 		var sample = @"
// <!--comment-->
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
// 	}


// 	[MTest] public void CommentWithContent () {
// 		var sample = @"
// <!--comment-->
// <body>
// something.
// </body>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
// 	}

// 	[MTest] public void LoadSpecificView_MyView_NestedDiv_PContainerContained () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/MyView/DepthAssetList)-->
// <p>
// <div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' /><p>something</p></div>
// </p>
// <p>
// <div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' /><p>something</p></div>
// </p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "MyView");
// 	}


// 	[MTest] public void LoadSpecificView_MyView_NestedDiv_PContainerContainedWithSameRoot () {
// 		/*
// 			同根の場合と同じように、2つ、同じ根をもつ連続したコンテンツがある時、その描画が綺麗にズレる。
// 		 */
// 		var sample = @"
// <!--depth asset list url(resources://Views/MyView/DepthAssetList)-->
// <p>
// <div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' /><p>something</p></div>
// <div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' /><p>something</p></div>
// </p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "MyView");
// 	}


// 	[MTest] public void HttpSchemeCommentAsDepthAssetListUrl () {
// 		var sample = @"
// <!--depth asset list url(http://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/Information/InformationResources/Resources/Views/MyView/DepthAssetList.txt)-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' /><p>something</p></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "MyView");
// 	}

// 	[MTest] public void HttpsSchemeCommentAsDepthAssetListUrl () {
// 		var sample = @"
// <!--depth asset list url(https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/Information/InformationResources/Resources/Views/MyView/DepthAssetList.txt)-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' /><p>something</p></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "MyView");
// 	}




// 	/*
// 		単体コンテンツでのアンカーセット
// 	 */
// 	[MTest] public void LeftTopAncheredView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/LeftTopAncheredView/DepthAssetList)-->
// <!--サイズを指定して出す、画像の原点を左上アンカーでセットしてる。pivotは0,1なので左上-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='10' height='10' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "LeftTopAncheredView");
// 	}

// 	[MTest] public void PivotView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/PivotView/DepthAssetList)-->
// <!--サイズを指定して出す、画像の原点を左上アンカーでセットしてる。pivotは0.5、0.5で、画像のposは0,0で、これだと画像はpivotを反映して表示する。-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='10' height='10' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "PivotView");
// 	}

// 	[MTest] public void LeftTopPivotView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/LeftTopPivotView/DepthAssetList)-->
// <!--サイズを指定して出す、画像の原点を左上アンカーでセットしてる。pivotは0,1なので左上-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='10' height='10' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "LeftTopPivotView");
// 	}

// 	[MTest] public void LeftTopPivotCenterAnchoredView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/LeftTopPivotCenterAnchoredView/DepthAssetList)-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='10' height='10' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "LeftTopPivotCenterAnchoredView");
// 	}

// 	[MTest] public void CenterPivotCenterAnchoredView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/CenterPivotCenterAnchoredView/DepthAssetList)-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='10' height='10' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "CenterPivotCenterAnchoredView");
// 	}

// 	[MTest] public void CenterPivotLeftAnchoredView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/CenterPivotLeftAnchoredView/DepthAssetList)-->
// <!--アンカーのxが0 -> 0.5になっていて、左に寄り付く性質がある。-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='10' height='10' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "CenterPivotLeftAnchoredView");
// 	}

// // 	unsupported pattern
// // 	[MTest] public void CenterPivotRightAnchoredView () {
// // 		var sample = @"
// // <!--depth asset list url(resources://Views/CenterPivotRightAnchoredView/DepthAssetList)-->
// // <!--アンカーのxが0.5 -> 1になっていて、右に寄り付く性質がある。このタイプのやつはまだよくわからん。viewのサイズを自己増幅させないといけない？-->
// // <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='10' height='10' /></div></p>
// // ";
// // 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "CenterPivotRightAnchoredView");
// // 	}


// 	[MTest] public void LeftTopPivotXRelativeView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/LeftTopPivotXRelativeView/DepthAssetList)-->
// <!--xアンカーが0-1なのでxに対してrelative、yアンカーは1-1-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='10' height='10' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "LeftTopPivotXRelativeView");
// 	}

// 	[MTest] public void LeftTopPivotXRelativeWithBottomEdgeView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/LeftTopPivotXRelativeWithBottomEdgeView/DepthAssetList)-->
// <!--xアンカーが0-1なのでxに対してrelative、yアンカーは0-0-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='10' height='10' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "LeftTopPivotXRelativeWithBottomEdgeView");
// 	}

// 	[MTest] public void LeftTopPivotYRelativeView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/LeftTopPivotYRelativeView/DepthAssetList)-->
// <!--yアンカーが0-1なのでyに対してrelative、xアンカーは0-0-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='10' height='10' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "LeftTopPivotYRelativeView");
// 	}

// 	[MTest] public void LeftTopPivotYRelativeWithRightEdgeView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/LeftTopPivotYRelativeWithRightEdgeView/DepthAssetList)-->
// <!--yアンカーが0-1なのでyに対してrelative、xアンカーは1-1-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='10' height='10' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "LeftTopPivotYRelativeWithRightEdgeView");
// 	}

// 	[MTest] public void LeftTopPivotRelativeView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/LeftTopPivotRelativeView/DepthAssetList)-->
// <!--アンカーがx,y共に0-1-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='10' height='10' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "LeftTopPivotRelativeView");
// 	}

// 	[MTest] public void LeftTopPivotXAnchoredWithXLeftMerginView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/LeftTopPivotXAnchoredWithXLeftMerginView/DepthAssetList)-->
// <!--アンカーがxは0-1、左merginが2あり、一辺が10になる。-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='8' height='10' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "LeftTopPivotXAnchoredWithXLeftMerginView");
// 	}

// 	[MTest] public void LeftTopPivotXAnchoredWithXRightMerginView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/LeftTopPivotXAnchoredWithXRightMerginView/DepthAssetList)-->
// <!--アンカーがxは0-1、右merginが7あり、画像幅3+mergin7で、親のwidthが10になる。-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='3' height='10' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "LeftTopPivotXAnchoredWithXRightMerginView");
// 	}

// 	[MTest] public void LeftTopPivotXAnchoredWithXSideMerginView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/LeftTopPivotXAnchoredWithXSideMerginView/DepthAssetList)-->
// <!--アンカーがxは0-1、右merginが3あり、左3があり、画像幅4で、親のwidthが10になる。-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='4' height='10' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "LeftTopPivotXAnchoredWithXSideMerginView");
// 	}


// 	[MTest] public void LeftTopPivotXAnchoredWithYTopMerginView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/LeftTopPivotXAnchoredWithYTopMerginView/DepthAssetList)-->
// <!--アンカーがyは0-1、上merginが3あり、一辺が10になる。-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='10' height='7' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "LeftTopPivotXAnchoredWithYTopMerginView");
// 	}

// 	[MTest] public void LeftTopPivotXAnchoredWithYBottomMerginView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/LeftTopPivotXAnchoredWithYBottomMerginView/DepthAssetList)-->
// <!--アンカーがxは0-1、下merginが7あり、画像高さ3+merginの7で、親のheightが10になる。-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='10' height='3' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "LeftTopPivotXAnchoredWithYBottomMerginView");
// 	}

// 	[MTest] public void LeftTopPivotXAnchoredWithYSideMerginView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/LeftTopPivotXAnchoredWithYSideMerginView/DepthAssetList)-->
// <!--アンカーがxは0-1、上merginが3あり、下5があり、画像高さ2で、親のwidthが10になる。-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='10' height='2' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "LeftTopPivotXAnchoredWithYSideMerginView");
// 	}


// 	[MTest] public void LeftTopPivotXYMerginView () {
// 		var sample = @"
// <!--depth asset list url(resources://Views/LeftTopPivotXYMerginView/DepthAssetList)-->
// <!--アンカーがx、yともに0-1、上merginが3あり、下6があり、左1、右6で、親のwidthが10になる。-->
// <p><div><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='3' height='1' /></div></p>
// ";
// 		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "LeftTopPivotXYMerginView");
// 	}

//     /*
//         コンテナを便利に扱いたい。独自タグの実装からビジュアル反映を綺麗にいい感じにやる、ってやつ。
//      */


// 	private static int index;

// 	private void DrawHtml (string html, int width=800, int height=1000, Action<double> progress=null, Action done=null, string viewName=InformationConstSettings.VIEWNAME_DEFAULT) {
// 		RunEnumeratorOnMainThread(
// 			RunEnum(viewName, html, width, height, progress, done)
// 		);
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
// 			html, 
// 			new ViewBox(width, height, 0), 
//             layoutedRect => {},
// 			progress, 
// 			done
// 		);

// 		var rect = root.GetComponent<RectTransform>();
// 		rect.anchoredPosition = new Vector2(index, 0);

// 		var canvas = GameObject.Find("Canvas");
// 		if (canvas != null) {
// 			root.transform.SetParent(canvas.transform);
// 		}

// 		// サンプルを探して表示する
// 		if (viewName != InformationConstSettings.VIEWNAME_DEFAULT) {
// 			var obj = GameObject.Find("Canvas/"+viewName);
// 			if (obj != null) {
// 				var rectTrans = obj.GetComponent<RectTransform>();
// 				rectTrans.anchoredPosition = new Vector2(index, rect.anchoredPosition.y);
// 			}
// 		}

// 		index+=width;


// 		while (coroutineCount != 0) {
// 			yield return null;
// 		}
// 	}
// }
