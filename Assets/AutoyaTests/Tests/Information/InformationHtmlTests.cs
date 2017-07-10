using Miyamasu;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;
using AutoyaFramework.Information;
using System.Collections;

/**
	test for html based view.
 */
public class InformationHtmlTests : MiyamasuTestRunner {
	[MSetup] public void Setup () {

		// GetTexture(url) runs only Play mode.
		if (!IsTestRunningInPlayingMode()) {
			SkipCurrentTest("Information feature should run on MainThread.");
		};
	}

	[MTest] public void NoTagContained () {
		var sample = @"";
		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
	}
	
	[MTest] public void NoValidTagContained () {
		var sample = @"
<!DOCTYPE html>
"
;
		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
	}

	[MTest] public void EmptyContents () {
		var sample = @"
<head>something</head>
";
		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
	}

	[MTest] public void BodyContents () {
var sample = @"
<head>something</head>
<body>else</body>
";

		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
	}

	// このテストはParserのテストに行くべき。
// 	[MTest] public void LoadInvalidView () {
// 		var sample = @"
// something.
// ";
// 		try {
// 			DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
// 			Assert(false, "should not come here.");
// 		} catch (Exception e) {
// 			// do nothng.
// 		}
// 	}

	[MTest] public void LoadHtmlView () {
		var sample = @"
<!DOCTYPE html>
<title>Small HTML 5</title>
<p>Hello world</p>
";
		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
	}

	[MTest] public void LoadSmallHtmlView () {
		var sample = @"
<!DOCTYPE html>
<head></head>
<body>
<p>something
<p>else</p>
</p>
</body>
";
		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
	}

	[MTest] public void CommentOnlyView () {
		var sample = @"
<!--comment-->
";
		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
	}


	[MTest] public void CommentWithContent () {
		var sample = @"
<!--comment-->
<body>
something.
</body>
";
		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "Default");
	}

	[MTest] public void LoadSpecificView_MyView_NestedPContainerContained () {
		var sample = @"
<!--depth asset list url(resources://Views/MyView/DepthAssetList)-->
<div><p>something</p></div>
";
		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "MyView");
	}


	[MTest] public void HttpSchemeCommentAsDepthAssetListUrl () {
		var sample = @"
<!--depth asset list url(http://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/Information/InformationResources/Resources/Views/MyView/DepthAssetList.txt)-->
<div><p>something</p></div>
";
		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "MyView");
	}


	[MTest] public void HttpsSchemeCommentAsDepthAssetListUrl () {
		var sample = @"
<!--depth asset list url(https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/Information/InformationResources/Resources/Views/MyView/DepthAssetList.txt)-->
<div><p>something</p></div>
";
		DrawHtml(sample, 100, 100, progress => Debug.Log("progress:" + progress), () => Debug.Log("done."), "MyView");
	}



	private static int index;

	private void DrawHtml (string html, int width=800, int height=1000, Action<double> progress=null, Action done=null, string viewName="Default") {
		RunEnumeratorOnMainThread(
			RunEnum(viewName, html, width, height, progress, done)
		);

		index+=width;
	}

	private IEnumerator RunEnum (string viewName, string html, int width, int height, Action<double> progress, Action done) {
		/*
			実行ブロック。
			変換器自体を作り、それにビューとテキストを渡すとインスタンスを返してくる。
		 */
		var coroutineCount = 0;

		Action<IEnumerator> executor = i => {
			coroutineCount++;
			#if UNITY_EDITOR
			{
				UnityEditor.EditorApplication.CallbackFunction d = null;

				d = () => {
					var result = i.MoveNext();
					if (!result) {
						UnityEditor.EditorApplication.update -= d;
						coroutineCount--;
					} 
				};

				// start running.
				UnityEditor.EditorApplication.update += d;
			}
			#else 
			Debug.LogError("do same things with runtime.");
			// {
			// 	UnityEditor.EditorApplication.CallbackFunction d = null;

			// 	d = () => {
			// 		var result = i.MoveNext();
			// 		if (!result) {
			// 			UnityEditor.EditorApplication.update -= d;
			// 			coroutineCount--;
			// 		} 
			// 	};

			// 	// start running.
			// 	UnityEditor.EditorApplication.update += d;
			// }
			#endif
		};

		var v = new ViewGenerator(executor);
		var root = v.GenerateViewFromSource(
			viewName, 
			html, 
			new ViewBox(width, height, 0), 
			progress, 
			done
		);

		var rect = root.GetComponent<RectTransform>();

		// 横にずらす
		rect.anchoredPosition = new Vector2(index, 0);

		var canvas = GameObject.Find("Canvas");
		if (canvas != null) {
			root.transform.SetParent(canvas.transform);
		}

		while (coroutineCount != 0) {
			yield return null;
		}
	}
}
