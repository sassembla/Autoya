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

			run();

			GameObject.DestroyImmediate(target);

			// 作り出したview以下のものも消す
			var viewAssetPath = "Assets/InformationResources/Resources/Views/" + testTargetSampleObjName;
			if (Directory.Exists(viewAssetPath)) {
				Debug.LogError("deleting");
				Directory.Delete(viewAssetPath, true);
				AssetDatabase.Refresh();
			}
		}

		[MenuItem("/Window/TestMaterialize")] public static void RunTests () {
			EditorSampleMaterial();
			EditorSampleMaterialWithDepth();
		}

		private static void EditorSampleMaterial () {
			var testTargetSampleObjName = "EditorSampleMaterial";
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
					// 空かどうかチェック
					Debug.Assert(boxConstraintes.Length == 0);
				}
			);
		}

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
					
					// 一つあるかチェック
					Debug.Assert(boxConstraintes.Length == 1);


					// prefabファイルが生成されているかチェック
					var createdAsset = Resources.Load("Views/" + testTargetSampleObjName + "/IMG") as GameObject;
					Debug.Assert(createdAsset != null);

					// 作成されたprefabの配置が左上アンカーになっているかチェック
					var rectTrans = createdAsset.GetComponent<RectTransform>();
					Debug.Assert(rectTrans != null);

					Debug.Assert(rectTrans.anchoredPosition == Vector2.zero);
					
					Debug.LogError("いろんなオフセットも0ではないはず");
				}
			);
		}
	}
}
