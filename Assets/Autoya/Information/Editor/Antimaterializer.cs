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
            
            /*
                root -> container or content -> container or content...
             */
            for (var i = 0; i < target.transform.childCount; i++) {
                var child = target.transform.GetChild(i);
                AntimaterializeChildlen(child.gameObject, new List<string>{viewName});
            }
            
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
            // このレイヤーにあるものに対して、まずコピーを生成し、そのコピーに対して処理を行う。
            var currentLayerInstance = GameObject.Instantiate(source);
            currentLayerInstance.name = source.name;


            var layerName = currentLayerInstance.name;
            
            var depth = viewName + "/" + layerName;

            Debug.LogError("currentLayerInstance:" + currentLayerInstance);

            var childrenDesc = new List<BoxConstraint>();
            var children = new List<GameObject>();

            // インスタンスに対して、子供達を取得し、それらのRectTransform以外のコンポーネントを全て消す。
            foreach (Transform childTrans in currentLayerInstance.transform) {
                var childObj = childTrans.gameObject;

                var boxName = layerName + "_" + childObj.name;
                childObj.name = boxName;

                var childRectTrans = childObj.GetComponent<RectTransform>();

                // この階層のdescにこのchildの情報を追加
                var box = new BoxConstraint(boxName, new RectTransDesc(childRectTrans));
                childrenDesc.Add(box);

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
            Debug.LogError("prefabPath:" + prefabPath);
            var dirPath = Path.GetDirectoryName(prefabPath);
            
            /*
                create prefab.
             */
            FileController.CreateDirectoryRecursively(dirPath);
            PrefabUtility.CreatePrefab(prefabPath, currentLayerInstance);


            // 自身の削除
            GameObject.DestroyImmediate(currentLayerInstance);
            


            // descを追加
            currentConstraints.Add(new BoxConstraints(layerName, childrenDesc.ToArray()));
            


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

                CollectConstraints(depth, child, currentConstraints);
            }

            // 全ての複製childの削除
            for (var i = 0; i < children.Count; i++) {
                var child = children[i];
                GameObject.DestroyImmediate(child);
            }
        }

        /**
            標準に存在しないタグをprefabへと分解する。
         */
        private static void AntimaterializeChildlen (GameObject source, List<string> depthSource) {
            var contentName = source.name;
            
            var currentDepthSource = new List<string>(depthSource);
            currentDepthSource.Add(contentName);

            var rectTrans = source.GetComponent<RectTransform>();
            var isContainer = source.name.EndsWith(InformationConstSettings.NAME_PREFAB_CONTAINER);
            
            var currentDepth = string.Join("/", currentDepthSource.ToArray());
            
            
            if (isContainer) {
                /*
                    Containerならば、単に0,0とかComponentなしとかその辺の比較だけがあればいい感じ。
                 */
                var reason = string.Empty;

                var useThisContainer = false;


                // diff by anchordPosition.
                if (rectTrans.anchoredPosition != Vector2.zero) {
                    reason += "position,";
                    useThisContainer = true;
                }

                // diff by anchor setting.
                if (
                    rectTrans.anchorMin.x == 0 &&
                    rectTrans.anchorMin.y == 1 &&
                    rectTrans.anchorMax.x == 0 &&
                    rectTrans.anchorMax.y == 1 &&
                    rectTrans.pivot.x == 0 &&
                    rectTrans.pivot.y == 1
                ) {
                    // pass.
                } else {
                    reason += "anchor,";
                    useThisContainer = true;
                }

                // RectTransform以外にもComponentがついてる
                var components = source.GetComponents<Component>();
                if (components.Where(c => c.GetType() != typeof(RectTransform)).Any()) {
                    reason += "additional component,";
                    useThisContainer = true;
                }

                if (useThisContainer) {
                    CreatePrefab(currentDepth, source);
                }
            } else {
                var reason = string.Empty;
                
                var useThisContent = false;
                
                // 既存のDefaultから探す
                if (!defaultTagStrs.Contains(contentName)) {
                    reason += "new tag,";
                    useThisContent = true;
                }

                // diff by anchordPosition.
                if (rectTrans.anchoredPosition != Vector2.zero) {
                    reason += "position,";
                    useThisContent = true;
                }

                // diff by anchor setting.
                if (
                    rectTrans.anchorMin.x == 0 &&
                    rectTrans.anchorMin.y == 1 &&
                    rectTrans.anchorMax.x == 0 &&
                    rectTrans.anchorMax.y == 1 &&
                    rectTrans.pivot.x == 0 &&
                    rectTrans.pivot.y == 1
                ) {
                    // pass.
                } else {
                    reason += "anchor,";
                    useThisContent = true;
                }

                // RectTransform以外にもComponentがついてる
                var components = source.GetComponents<Component>();
                if (components.Where(c => c.GetType() != typeof(RectTransform)).Any()) {

                    if (!useThisContent && defaultTagStrs.Contains(contentName)) {

                        // この時点でまだ使う決定がなされてない = 新種や位置違いのcontentではない。
                        // componentの構成差を見る。
                        
                        useThisContent = CheckComponent(contentName, components, out reason);
                    }
                }

                if (useThisContent) {
                    CreatePrefab(currentDepth, source);
                }
            }
            
            for (var i = 0; i < source.transform.childCount; i++) {
                var child = source.transform.GetChild(i);
                AntimaterializeChildlen(child.gameObject, currentDepthSource);
            }
        }

        private static void CreatePrefab (string depth, GameObject sourceObject) {
            /*
                copy gameobject for deleting all child, then create prefab.
                in this phase, prefab should not have child gameObject.
                */
            var targetPrefabSource = GameObject.Instantiate(sourceObject);
            var children = new List<GameObject>();
            foreach (Transform t in targetPrefabSource.transform) {
                children.Add(t.gameObject);
            }
            children.ForEach(child => GameObject.DestroyImmediate(child));

            var prefabPath = InformationConstSettings.FULLPATH_INFORMATION_RESOURCE + depth + ".prefab";
            
            var dirPath = Path.GetDirectoryName(prefabPath);
            
            /*
                create prefab.
                */
            FileController.CreateDirectoryRecursively(dirPath);
            PrefabUtility.CreatePrefab(prefabPath, targetPrefabSource);

            // delete unnecessary copied prefab source.
            GameObject.DestroyImmediate(targetPrefabSource);
        }

        private static bool CheckComponent (string contentName, Component[] components, out string reason) {
            reason = string.Empty;
            
            var prefabPath = InformationConstSettings.PREFIX_PATH_INFORMATION_RESOURCE + InformationConstSettings.VIEWNAME_DEFAULT + "/" + contentName;
            var prefab = Resources.Load(prefabPath) as GameObject;

            var prefabComponents = prefab.GetComponents<Component>();

            // check component count.
            if (prefabComponents.Length != components.Length) {
                reason += "additional component,";
                return true;
            }
            
            foreach (var component in components) {
                
                if (component is Text) {
                    var text = component as Text;
                    var prefabText = prefab.GetComponent<Text>();

                    // フォント比較
                    {
                        var contentFont = text.font.name;
                        var prefabFont = prefabText.font.name;
                        
                        if (contentFont != prefabFont) {
                            reason += "font mismatch. default:" + prefabFont + " vs " + contentFont + ",";
                            return true;
                        }
                    }

                    // フォントサイズ比較
                    {
                        var contentFontSize = text.fontSize;
                        var prefabFontSize = prefabText.fontSize;

                        if (contentFontSize != prefabFontSize) {
                            reason += "font size mismatch. default fontSize:" + prefabFontSize + " vs content fontSize:" + contentFontSize + ",";
                            return true;
                        }
                    }
                }

                if (component is Image) {
                    var image = component as Image;
                    var prefabImage = prefab.GetComponent<Image>();

                    // raycast比較
                    {
                        var contentRayCast = image.raycastTarget;
                        var prefabRayCast = prefabImage.raycastTarget;
                        
                        if (contentRayCast != prefabRayCast) {
                            reason += "raycast of image mismatch. default:" + prefabRayCast + " vs content:" + contentRayCast + ",";
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}