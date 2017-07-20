using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;
using UnityEditor;
using System.IO;

/**
	test for materializer
 */
namespace AutoyaFramework.Information {
	public class MaterializerTests {
		
		private static GameObject editorCanvas;

		private static void Run (string testTargetSampleObjName, Action run) {
			editorCanvas = GameObject.Find("Canvas");
			
			var sample = GameObject.Find("Editor/Canvas/" + testTargetSampleObjName);

			var target = GameObject.Instantiate(sample);
			target.name = sample.name;// set name.

			target.transform.SetParent(editorCanvas.transform, false);

			Selection.activeGameObject = target;

			try {
				run();
			} catch {
				// do nothing.
			}

			GameObject.DestroyImmediate(target);

			// テストのために作り出したview以下のものも消す
			var viewAssetPath = "Assets/InformationResources/Resources/Views/" + testTargetSampleObjName;
			if (Directory.Exists(viewAssetPath)) {
				Directory.Delete(viewAssetPath, true);
				AssetDatabase.Refresh();
			}
		}

		[MenuItem("/Window/TestMaterialize")] public static void RunTests () {
			EditorSampleMaterial();
			EditorSampleMaterialWithDepth();
			CheckIfSameCustomTagInOneView();
		}

		// 階層なしのものを分解する
		private static void EditorSampleMaterial () {
			var testTargetSampleObjName = "EditorSampleMaterial";
			Run(testTargetSampleObjName,
				() => {
					Antimaterializer.Antimaterialize();
					
					// 出力物に対してのチェックを行う。
					var jsonAsset = Resources.Load("Views/" + testTargetSampleObjName + "/DepthAssetList") as TextAsset;
					var jsonStr = jsonAsset.text;
					
					var list = JsonUtility.FromJson<DepthAssetList>(jsonStr);
					Debug.Assert(list.viewName == testTargetSampleObjName);

					var boxConstraintes = list.constraints;

					// 空かどうかチェック
					Debug.Assert(boxConstraintes.Length == 0);
				}
			);
		}

		// 階層付きのものを分解する
		public static void EditorSampleMaterialWithDepth () {
			var testTargetSampleObjName = "EditorSampleMaterialWithDepth";
			Run(testTargetSampleObjName,
				() => {
					Antimaterializer.Antimaterialize();
					/*
						で、吐き出したものが存在していて、そのツリー構造を読み込んで意図とあってれば良し。
					 */

					var jsonAsset = Resources.Load("Views/" + testTargetSampleObjName + "/DepthAssetList") as TextAsset;
					var jsonStr = jsonAsset.text;
					
					var list = JsonUtility.FromJson<DepthAssetList>(jsonStr);
					Debug.Assert(list.viewName == testTargetSampleObjName);

					var boxConstraintes = list.constraints;
					
					// 本体 + imgで2つあるかチェック
					Debug.Assert(boxConstraintes.Length == 2, "boxConstraints:" + boxConstraintes.Length);


					// prefabファイルが生成されているかチェック
					var createdAsset = Resources.Load("Views/" + testTargetSampleObjName + "/MyImgView") as GameObject;
					Debug.Assert(createdAsset != null, "testTargetSampleObjName:" + testTargetSampleObjName);

					// 作成されたprefabのRectTransがあるか
					var rectTrans = createdAsset.GetComponent<RectTransform>();
					Debug.Assert(rectTrans != null);

					// ここはAssetによりけりな感じ。
					Debug.Assert(rectTrans.anchoredPosition == Vector2.zero);
				}
			);
		}

		public static void CheckIfSameCustomTagInOneView () {
			var testTargetSampleObjName = "CheckIfSameCustomTagInOneView";
			Run(testTargetSampleObjName,
				() => {
					Antimaterializer.Antimaterialize();
					
					var jsonAsset = Resources.Load("Views/" + testTargetSampleObjName + "/DepthAssetList") as TextAsset;
					var jsonStr = jsonAsset.text;
					
					var list = JsonUtility.FromJson<DepthAssetList>(jsonStr);
					Debug.Assert(list.viewName == testTargetSampleObjName);

					var boxConstraintes = list.constraints;
					
					// 同一名称のカスタムタグが存在するという違反があるので、途中まで = 1件目のみを吐き出す。
					Debug.Assert(boxConstraintes.Length == 1, "boxConstraints:" + boxConstraintes.Length);
				}
			);
		}

		public static void CheckIfSameCustomTagInOneView_0_1_0 () {
			var testTargetSampleObjName = "CheckIfSameCustomTagInOneView_0_1_0";
			Run(testTargetSampleObjName,
				() => {
					Antimaterializer.Antimaterialize();
					
					var jsonAsset = Resources.Load("Views/" + testTargetSampleObjName + "/DepthAssetList") as TextAsset;
					var jsonStr = jsonAsset.text;
					
					var list = JsonUtility.FromJson<DepthAssetList>(jsonStr);
					Debug.Assert(list.viewName == testTargetSampleObjName);

					var boxConstraintes = list.constraints;
					
					// 同一名称のカスタムタグが存在するという違反があるので、途中まで = 2件目までを吐き出す。
					Debug.Assert(boxConstraintes.Length == 2, "boxConstraints:" + boxConstraintes.Length);
				}
			);
		}
	}
}
