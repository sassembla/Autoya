using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AutoyaFramework.Information {
    public class Antimaterializer {
        [MenuItem("Window/Information/Antimaterialize")] public static void Antimaterialize () {
            /*
                ここでの処理は、ResourcesにdepthAssetListを吐き出して、リスト自体をResourcesから取得し、そのリストにはResourcesからdepthAssetを読み込むパスがかいてある、
                という構成に対応している状態。

                今後、リストをWebからDLしたり、AssetBundleからロードしたりとかにも対応する。
             */

            // 選択対象がHirearchy内のやつでないとだめ、とかどうやって絞ればいいんだろね。まあいいか。
            if (Selection.activeGameObject == null) {
                Debug.Log("no game object selected. select GameObject in Canvas. e,g, Canvas/SOMETHING");
                return;
            }
            
            var target = Selection.activeGameObject;
            if (target.transform.parent.name != "Canvas") {
                return;
            }

            var viewName = target.name;
            var exportBasePath = InformationConstSettings.PATH_INFORMATION_RESOURCE_FULLPATH + viewName;
            Debug.Log("start antimaterialize:" + viewName + ". view name is:" + target + " export file path:" + exportBasePath);

            // remove all files in exportBasePath.
            FileController.RemakeDirectory(exportBasePath);
            
            /*
                root -> container or val -> container or val...
             */
            for (var i = 0; i < target.transform.childCount; i++) {
                var child = target.transform.GetChild(i);
                AntimaterializeChildlen(child.gameObject, new List<string>{viewName});
            }
            
            /*
                create depthAssetList.
             */
            var exportedFiles = FileController.FilePathsInFolder(exportBasePath);
            var depthAssetInfoList = new List<DepthAssetInfo>();

            foreach (var exportedFilePath in exportedFiles) {
                // Debug.LogError("exportedFilePath:" + exportedFilePath);
                
                var depthAssetNameIndex = exportedFilePath.IndexOf("/Resources/Views/") + "/Resources/Views/".Length;
                var depthAssetNameWithExtension = exportedFilePath.Substring(depthAssetNameIndex);
                var depthAssetName = depthAssetNameWithExtension.Substring(0, depthAssetNameWithExtension.Length - Path.GetExtension(depthAssetNameWithExtension).Length);

                // Assets/InformationResources/Resources/Views/MyView/P_CONTAINER/P_CONTAINER.prefab
                var resourceBasePathIndex = exportedFilePath.IndexOf("/Resources/") + "/Resources/".Length;
                var resourcePathWithExtension = exportedFilePath.Substring(resourceBasePathIndex);
                var resourcePath = "resources://" + resourcePathWithExtension.Substring(0, resourcePathWithExtension.Length - Path.GetExtension(resourcePathWithExtension).Length);

                var depthAssetInfo = new DepthAssetInfo(depthAssetName, resourcePath);
                depthAssetInfoList.Add(depthAssetInfo);
            }

            var listFileName = "DepthAssetList.txt";
            var depthAssetList = new DepthAssetList(depthAssetInfoList.ToArray());

            var jsonStr = JsonUtility.ToJson(depthAssetList);
            using (var sw = new StreamWriter(Path.Combine(exportBasePath, listFileName))) {
                sw.WriteLine(jsonStr);
            }
            AssetDatabase.Refresh();
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

                // アンカー設定が異なる
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
                    reason += "component,";
                    useThisContainer = true;
                }

                if (useThisContainer) {
                    /*
                        copy gameobject for deleting all child, then create prefab.
                        in this phase, prefab should not have child gameObject.
                     */
                    var targetPrefabSource = GameObject.Instantiate(source);
                    var children = new List<GameObject>();
                    foreach (Transform t in targetPrefabSource.transform) {
                        children.Add(t.gameObject);
                    }
                    children.ForEach(child => GameObject.DestroyImmediate(child));

                    var prefabPath = InformationConstSettings.PATH_INFORMATION_RESOURCE_FULLPATH + currentDepth + ".prefab";
                    
                    var dirPath = Path.GetDirectoryName(prefabPath);
                    
                    /*
                        create prefab.
                     */
                    FileController.CreateDirectoryRecursively(dirPath);
                    PrefabUtility.CreatePrefab(prefabPath, targetPrefabSource);

                    // delete unnecessary copied prefab source.
                    GameObject.DestroyImmediate(targetPrefabSource);
                }
            } else {
                /*
                    containerではない要素の比較の場合、単体のtagとしての比較を行うことになる。
                    component単位で差分を見る？
                 */

            }
            
            for (var i = 0; i < source.transform.childCount; i++) {
                var child = source.transform.GetChild(i);
                AntimaterializeChildlen(child.gameObject, currentDepthSource);
            }
        }
    }
}