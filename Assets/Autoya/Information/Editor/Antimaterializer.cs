using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AutoyaFramework.Information {
    public class Antimaterializer {
        private static string[] defaultTagStrs;

        private static string[] CollectDefaultTag () {
            var htmlTags = new List<string>();

            var defaultAssetPaths = InformationConstSettings.FULLPATH_DEFAULT_TAGS;
            var filePaths = FileController.FilePathsInFolder(defaultAssetPaths);
            foreach (var filePath in filePaths) {
                // Debug.LogError("filePath:" + filePath + " filenameWithoutExtension:" + Path.GetFileNameWithoutExtension(filePath));
                var tagStr = Path.GetFileNameWithoutExtension(filePath);
                htmlTags.Add(tagStr);
            }

            return htmlTags.ToArray();
        }

        [MenuItem("Window/Information/Antimaterialize")] public static void Antimaterialize () {
            defaultTagStrs = CollectDefaultTag();

            /*
                ここでの処理は、ResourcesにdepthAssetListを吐き出して、リスト自体をResourcesから取得し、そのリストにはResourcesからdepthAssetを読み込むパスがかいてある、
                という構成に対応している状態。

                今後、リストをWebからDLしたり、AssetBundleからロードしたりとかにも対応する。
             */

            // 選択対象がHirearchy内のやつでないとだめ、という限定がしたい。
            if (Selection.activeGameObject == null) {
                Debug.Log("no game object selected. select GameObject in Canvas. e,g, Canvas/SOMETHING");
                return;
            }
            
            var target = Selection.activeGameObject;
            if (target.transform.parent.name != "Canvas") {
                return;
            }

            var viewName = target.name;
            var exportBasePath = InformationConstSettings.FULLPATH_INFORMATION_RESOURCE + viewName;
            Debug.Log("start antimaterialize:" + viewName + ". view name is:" + target + " export file path:" + exportBasePath);

            // remove all files in exportBasePath.
            FileController.RemakeDirectory(exportBasePath);
            
            Debug.LogWarning("そのうち消す");
            var depthAssetInfoList = new List<DepthAssetInfo>();
            
            // childrenがいろいろなタグの根本にあたる。
            var constraints = new List<BoxConstraints>();
            // recursiveに、コンテンツを分解していく。
            for (var i = 0; i < target.transform.childCount; i++) {
                var child = target.transform.GetChild(i);
                
                CollectConstraints(InformationConstSettings.FULLPATH_INFORMATION_RESOURCE + viewName, child.gameObject, constraints);
            }
            
            var listFileName = "DepthAssetList.txt";
            var depthAssetList = new DepthAssetList(viewName, depthAssetInfoList.ToArray(), constraints.ToArray());

            var jsonStr = JsonUtility.ToJson(depthAssetList);
            using (var sw = new StreamWriter(Path.Combine(exportBasePath, listFileName))) {
                sw.WriteLine(jsonStr);
            }
            AssetDatabase.Refresh();
        }

        /**
            存在するパーツ単位でconstraintsを生成する
         */
        private static void CollectConstraints (string viewName, GameObject source, List<BoxConstraints> currentConstraints) {
            var error = false;
            
            // このレイヤーにあるものに対して、まずコピーを生成し、そのコピーに対して処理を行う。
            var currentLayerInstance = GameObject.Instantiate(source);
            currentLayerInstance.name = source.name;


            var layerName = currentLayerInstance.name;
            
            var depth = viewName + "/" + layerName;

            
            var childrenDescDict = new Dictionary<string, RectTransDesc>();
            
            
            var children = new List<GameObject>();
            using (new ViewChildrenUsing(children)) {

                // インスタンスに対して、子供達を取得し、それらのRectTransform以外のコンポーネントを全て消す。
                foreach (Transform childTrans in currentLayerInstance.transform) {
                    var childObj = childTrans.gameObject;
                    var originalChildName = childObj.name;

                    var boxName = layerName + "_" + childObj.name;
                    childObj.name = boxName;

                    var childRectTrans = childObj.GetComponent<RectTransform>();

                    // この階層のdescにこのchildの情報を追加
                    if (childrenDescDict.ContainsKey(boxName)) {
                        Debug.LogWarning("customTag:" + originalChildName + " is already exist in layer:" + layerName + ". please set other name for each customTag on this layer.");
                        error = true;
                    }

                    childrenDescDict[boxName] = new RectTransDesc(childRectTrans);

                    // childを別のリストにまとめて、自身のchildとは分離
                    {
                        // childの新規インスタンスを生成
                        var childNewInstance = GameObject.Instantiate(childObj);

                        // その名称を、boxNameへと変更。
                        // layer_tag になる。
                        childNewInstance.name = boxName;

                        // 保持
                        children.Add(childNewInstance);
                    }

                    // 元になったinstanceからrectTrans以外のcomponentを奪う
                    
                    foreach (var component in childObj.GetComponents<Component>().Reverse()) {
                        if (component is RectTransform) {
                            continue;
                        }

                        // remove not RectTransform component.
                        GameObject.DestroyImmediate(component);
                    }

                }
                
                // 自身のprefab化
                var prefabPath = depth + ".prefab";
                var dirPath = Path.GetDirectoryName(prefabPath);
                
                /*
                    create prefab.
                */
                FileController.CreateDirectoryRecursively(dirPath);
                PrefabUtility.CreatePrefab(prefabPath, currentLayerInstance);


                // 自身の削除
                GameObject.DestroyImmediate(currentLayerInstance);
                


                // descを追加
                currentConstraints.Add(new BoxConstraints(layerName, childrenDescDict.Select((kv, index) => new BoxConstraint(kv.Key, kv.Value)).ToArray()));
                


                // 生成しておいた子供の位置情報を変更して継続
                foreach (var child in children) {
                    var childRectTrans = child.GetComponent<RectTransform>();
                    // このchildから位置情報を奪い、さらに内部へと進む
                    childRectTrans.anchoredPosition = Vector2.zero;
                    childRectTrans.offsetMin = Vector2.zero;
                    childRectTrans.offsetMax = new Vector2(1, 1);
                    childRectTrans.anchorMin = Vector2.zero;
                    childRectTrans.anchorMax = new Vector2(1, 1);
                    childRectTrans.pivot = new Vector2(0, 1);
                }

                if (error) {
                    Debug.LogWarning("cancelled by error. see above.");
                    return;
                }

                // if no error, continue running.
                foreach (var child in children) {
                    CollectConstraints(depth, child, currentConstraints);
                }
            }
        }

        private class ViewChildrenUsing : IDisposable {
			private List<GameObject> children;
			
			public ViewChildrenUsing (List<GameObject> children) {
				this.children = children;
			}

			private bool disposedValue = false;

			protected virtual void Dispose (bool disposing) {
				if (!disposedValue) {
					if (disposing) {
						for (var i = 0; i < children.Count; i++) {
                            var child = children[i];
                            GameObject.DestroyImmediate(child);
                        }
					}
					disposedValue = true;
				}
			}

			void IDisposable.Dispose () {
				Dispose(true);
			}
		}
    }
}