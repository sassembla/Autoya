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
			} catch (Exception e) {
				Debug.LogError("e:" + e);
			}

			GameObject.DestroyImmediate(target);

			// テストのために作り出したview以下のものも消す
			var viewAssetPath = "Assets/InformationResources/Resources/Views/" + testTargetSampleObjName;
			// if (Directory.Exists(viewAssetPath)) {
			// 	Directory.Delete(viewAssetPath, true);
			// 	AssetDatabase.Refresh();
			// }
		}

		[MenuItem("/Window/TestMaterialize")] public static void RunTests () {
			EditorSampleMaterial();
			EditorSampleMaterialWithDepth();
			CheckIfSameCustomTagInOneView();
			EditorSampleMaterialWithMoreDepth();
			ExportedCustomTagPrefabHasZeroPos();
			ExportedCustomTagPrefabHasLeftTopFixedAnchor();
			ExportedCustomTagPrefabHasOriginalSize();
			ExportedCustomTagBoxHasOriginalAnchor();
			ExportedCustomTagChildHasZeroPos();
		}

		// 階層なしのものを分解する
		private static void EditorSampleMaterial () {
			var testTargetSampleObjName = "EditorSampleMaterial";
			Run(testTargetSampleObjName,
				() => {
					Antimaterializer.Antimaterialize();
					
					// 出力物に対してのチェックを行う。カラであれば生成しない
					var jsonAsset = Resources.Load("Views/" + testTargetSampleObjName + "/DepthAssetList");
					Debug.Assert(!jsonAsset);
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
					
					var list = JsonUtility.FromJson<CustomTagList>(jsonStr);
					Debug.Assert(list.viewName == testTargetSampleObjName);

					var boxConstraintes = list.constraints;
					
					// 本体 + MyImgItemの2レイヤーで2
					Debug.Assert(boxConstraintes.Length == 2, "boxConstraints:" + boxConstraintes.Length);

					// prefabファイルが生成されているかチェック
					var createdAsset = Resources.Load("Views/" + testTargetSampleObjName + "/MyImgItem") as GameObject;
					Debug.Assert(createdAsset != null, "createdAsset:" + createdAsset + " is null.");

					// 作成されたprefabのRectTransがあるか
					var rectTrans = createdAsset.GetComponent<RectTransform>();
					Debug.Assert(rectTrans != null);

					// 原点を指しているか
					Debug.Assert(rectTrans.anchoredPosition == Vector2.zero);
				}
			);
		}

		public static void CheckIfSameCustomTagInOneView () {
			// 同一名称のカスタムタグが存在するという違反があるので、キャンセルされる。
			var testTargetSampleObjName = "CheckIfSameCustomTagInOneView";
			Run(testTargetSampleObjName,
				() => {
					try {
						Antimaterializer.Antimaterialize();
						Debug.Assert(false, "never done.");
					} catch {
						// pass.
					}
				}
			);
		}

		public static void EditorSampleMaterialWithMoreDepth () {
			var testTargetSampleObjName = "EditorSampleMaterialWithMoreDepth";
			Run(testTargetSampleObjName,
				() => {
					Antimaterializer.Antimaterialize();

					var jsonAsset = Resources.Load("Views/" + testTargetSampleObjName + "/DepthAssetList") as TextAsset;
					var jsonStr = jsonAsset.text;
					
					var list = JsonUtility.FromJson<CustomTagList>(jsonStr);
					Debug.Assert(list.viewName == testTargetSampleObjName);

					var boxConstraints = list.constraints;
					
					// MyImgAndTextItem, IMG, Text_CONTAINER, Text の4つが吐かれる
					Debug.Assert(boxConstraints.Length == 4, "boxConstraints:" + boxConstraints.Length);
				}
			);
		}
		

		public static void ExportedCustomTagPrefabHasZeroPos () {
			var testTargetSampleObjName = "EditorSampleMaterialWithMoreDepth";
			Run(testTargetSampleObjName,
				() => {
					Antimaterializer.Antimaterialize();
					
					// EditorSampleMaterialWithMoreDepthプレファブの原点が0
					var prefab = Resources.Load("Views/" + testTargetSampleObjName + "/MyImgAndTextItem") as GameObject;
					if (prefab != null) {
						var rectTrans = prefab.GetComponent<RectTransform>();
						Debug.Assert(rectTrans.anchoredPosition == Vector2.zero, "not zero, pos:" + rectTrans.anchoredPosition);
					}
				}
			);
		}

		public static void ExportedCustomTagPrefabHasLeftTopFixedAnchor () {
			var testTargetSampleObjName = "EditorSampleMaterialWithMoreDepth";
			Run(testTargetSampleObjName,
				() => {
					Antimaterializer.Antimaterialize();
					
					var prefab = Resources.Load("Views/" + testTargetSampleObjName + "/MyImgAndTextItem") as GameObject;
					if (prefab != null) {
						var rectTrans = prefab.GetComponent<RectTransform>();
						
						Debug.Assert(rectTrans.anchorMin == new Vector2(0,1) && rectTrans.anchorMax == new Vector2(0,1), "not match.");
					}
				}
			);
		}

		public static void ExportedCustomTagPrefabHasOriginalSize () {
			var testTargetSampleObjName = "EditorSampleMaterialWithMoreDepth";
			Run(testTargetSampleObjName,
				() => {
					var original = GameObject.Find("MyImgAndTextItem");
					
					Antimaterializer.Antimaterialize();
					
					var prefab = Resources.Load("Views/" + testTargetSampleObjName + "/MyImgAndTextItem") as GameObject;
					if (prefab != null) {
						var rectTrans = prefab.GetComponent<RectTransform>();
						var originalRectTrans = original.GetComponent<RectTransform>();

						Debug.Assert(rectTrans.sizeDelta == originalRectTrans.sizeDelta, "not match.");
					}
				}
			);
		}

		public static void ExportedCustomTagBoxHasOriginalAnchor () {
			var testTargetSampleObjName = "EditorSampleMaterialWithMoreDepth";
			Run(testTargetSampleObjName,
				() => {
					var original = GameObject.Find("MyImgAndTextItem");
					var originalChildlen = new List<RectTransform>();
					foreach (Transform childTrans in original.transform) {
						var rectTrans = childTrans.gameObject.GetComponent<RectTransform>();
						originalChildlen.Add(rectTrans);
					}
					
					Antimaterializer.Antimaterialize();
					
					var prefab = Resources.Load("Views/" + testTargetSampleObjName + "/MyImgAndTextItem") as GameObject;
					if (prefab != null) {
						var childlen = new List<RectTransform>();
						foreach (Transform childTrans in prefab.transform) {
							var rectTrans = childTrans.gameObject.GetComponent<RectTransform>();
							childlen.Add(rectTrans);
						}
						
						Debug.Assert(originalChildlen.Count == childlen.Count, "not match.");
						for (var i = 0; i < originalChildlen.Count; i++) {
							Debug.Assert(originalChildlen[i].anchoredPosition == childlen[i].anchoredPosition, "not match.");
							Debug.Assert(originalChildlen[i].offsetMin == childlen[i].offsetMin, "not match.");
							Debug.Assert(originalChildlen[i].offsetMax == childlen[i].offsetMax, "not match.");
						}
					}
				}
			);
		}

		public static void ExportedCustomTagChildHasZeroPos () {
			var testTargetSampleObjName = "EditorSampleMaterialWithMoreDepth";
			Run(testTargetSampleObjName,
				() => {
					Antimaterializer.Antimaterialize();
					
					// EditorSampleMaterialWithMoreDepthプレファブの原点が0
					var prefab = Resources.Load("Views/" + testTargetSampleObjName + "/IMG") as GameObject;
					if (prefab != null) {
						var rectTrans = prefab.GetComponent<RectTransform>();
						Debug.Assert(rectTrans.anchoredPosition == Vector2.zero, "not zero, pos:" + rectTrans.anchoredPosition);
					}
				}
			);
		}

	}
}
