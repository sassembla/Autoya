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

            var defaultAssetPaths = ConstSettings.FULLPATH_DEFAULT_TAGS;
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
            if (Selection.gameObjects != null) {
                foreach (var target in Selection.gameObjects) {
                    if (target.transform.parent.GetComponent<Canvas>() == null) {
                        Debug.Log("skipped antimaterialize:" + target + " because it is not located under Canvas attached GameObject. let's select object under Canvas.");
                        continue;
                    }
                    
                    Antimaterialize(target);
                }
            }
        }

        private static void Antimaterialize (GameObject target) {
            var viewName = target.name;
            var exportBasePath = ConstSettings.FULLPATH_INFORMATION_RESOURCE + viewName;
            Debug.Log("start antimaterialize:" + viewName + ". view name is:" + target + " export file path:" + exportBasePath);

            // remove all files in exportBasePath.
            FileController.RemakeDirectory(exportBasePath);
            
            // childrenがいろいろなタグの根本にあたる。
            var constraints = new List<LayerInfo>();
            var contents = new List<ContentInfo>();

            // recursiveに、コンテンツを分解していく。
            for (var i = 0; i < target.transform.childCount; i++) {
                var child = target.transform.GetChild(i);
                
                CollectConstraintsAndContents(viewName, child.gameObject, constraints, contents);
            }
            
            var listFileName = "DepthAssetList.txt";
            var depthAssetList = new CustomTagList(viewName, contents.ToArray(), constraints.ToArray());

            
            var jsonStr = JsonUtility.ToJson(depthAssetList);
            using (var sw = new StreamWriter(Path.Combine(exportBasePath, listFileName))) {
                sw.WriteLine(jsonStr);
            }
            AssetDatabase.Refresh();
            Debug.Log("finished antimaterialize:" + viewName + ". view name is:" + target + " export file path:" + exportBasePath);
        }

        /**
            存在するパーツ単位でconstraintsを生成する
         */
        private static void CollectConstraintsAndContents (string viewName, GameObject source, List<LayerInfo> currentConstraints, List<ContentInfo> currentContents) {
            // このレイヤーにあるものに対して、まずコピーを生成し、そのコピーに対して処理を行う。
            var currentTargetInstance = GameObject.Instantiate(source);
            currentTargetInstance.name = source.name;
            
            // ここでは、同じ名前のcontentがすでにあればエラーを出して終了する。
            if (currentContents.Where(c => c.contentName == currentTargetInstance.name).Any()) {
                throw new Exception("duplicate content:" + currentTargetInstance.name + ". do not duplicate.");
            }

            if (currentConstraints.Where(c => c.layerName == currentTargetInstance.name).Any()) {
                throw new Exception("duplicate layer:" + currentTargetInstance.name + ". do not duplicate.");
            }
            

            using (new GameObjectDeleteUsing(currentTargetInstance)) {
                switch (currentTargetInstance.transform.childCount) {
                    case 0: {
                        // target is content.
                        ModifyContentInstance(viewName, currentTargetInstance, currentContents);
                        break;
                    }
                    default: {
                        // target is layer.
                        ModifyLayerInstance(viewName, currentTargetInstance, currentConstraints);
                        break;
                    }
                }
            }
        }

        private static void ModifyContentInstance (string viewName, GameObject currentContentInstance, List<ContentInfo> currentContents) {
            var contentName = currentContentInstance.name.ToLower();
            
            // 画像か文字を含んでいるコンテンツで、コードも可。でもボタンの実行に関しては画像に対してボタンが勝手にセットされる。ボタンをつけたらエラー。
            // 文字のリンク化も勝手に行われる。というかあれもボタンだっけ。
            // ボタンコンポーネントが付いていたら外す。
            if (currentContentInstance.GetComponent<Button>() != null) {
                throw new Exception("do not attach Button component directory. Button component will be attached automatically.");
            }

            var components = currentContentInstance.GetComponents<Component>();
            if (components.Length < 3) {
                throw new Exception("should have at least 3 component. 1st is rectTransform, 2nd is canvasRendererm 3rd is image or text.");
            }

            // foreach (var s in components) {
            //     Debug.LogError("s:" + s.GetType().Name);
            // }
            
            // rectTrans, canvasRenderer 以外を採用する。
            var currentFirstComponent = components[2];
            var type = TreeType.Content_Img;
            switch (currentFirstComponent.GetType().Name) {
                case "Image": {
                    type = TreeType.Content_Img;
                    break;
                }
                case "Text": {
                    type = TreeType.Container;// not Content_Text. this is container.
                    break;
                }
                default: {
                    throw new Exception("unsupported second component on content. found component type:" + currentFirstComponent);
                }
            }

            // 名前を登録する
            currentContents.Add(new ContentInfo(contentName, type, "resources://" + ConstSettings.PREFIX_PATH_INFORMATION_RESOURCE + viewName + "/" + contentName.ToUpper()));

            // このコンポーネントをprefab化する
            {
                var prefabPath = "Assets/InformationResources/Resources/Views/" + viewName + "/" + contentName.ToUpper() + ".prefab";
                var dirPath = Path.GetDirectoryName(prefabPath);
                
                FileController.CreateDirectoryRecursively(dirPath);
                PrefabUtility.CreatePrefab(prefabPath, currentContentInstance);

                // 自体の削除
                GameObject.DestroyImmediate(currentContentInstance);
            }
        }

        private static void ModifyLayerInstance (string viewName, GameObject currentLayerInstance, List<LayerInfo> currentConstraints) {
            // このインスタンスのポジションを0,0 leftTopAnchor、左上pivotにする。
            // レイヤーのインスタンスは、インスタンス化時に必ず親のサイズにフィットするように変形される。
            var rectTrans = currentLayerInstance.GetComponent<RectTransform>();
            rectTrans.anchoredPosition = new Vector2(0,0);
            rectTrans.anchorMin = new Vector2(0,1);
            rectTrans.anchorMax = new Vector2(0,1);
            rectTrans.pivot = new Vector2(0,1);


            var layerName = currentLayerInstance.name.ToLower();

            var childrenConstraintDict = new Dictionary<string, BoxPos>();
            var copiedChildList = new List<GameObject>();
            
            /*
                元々のchildrenを別GameObjectとして分離
            */
            {
                foreach (Transform component in currentLayerInstance.transform) {
                    var childGameObject = component.gameObject;

                    var newChildGameObject = GameObject.Instantiate(childGameObject);
                    newChildGameObject.name = childGameObject.name;

                    copiedChildList.Add(newChildGameObject);
                }
            }
            
            using(new GameObjectDeleteUsing(copiedChildList.ToArray())) {
                /*
                    box情報を生成
                */
                { 
                    foreach (Transform component in currentLayerInstance.transform) {
                        var boxObject = component.gameObject;
                        var boxRectTrans = boxObject.GetComponent<RectTransform>();
                        
                        var boxName = layerName + "_" + boxObject.name;
                        
                        if (childrenConstraintDict.ContainsKey(boxName)) {
                            throw new Exception("another box:" + boxName + " is already exist in layer:" + layerName + ". please set other name for each customTag on this layer.");
                        }

                        childrenConstraintDict[boxName] = new BoxPos(boxRectTrans);
                    }
                }

                /*
                    layer内のboxの削除(レイアウトの動的な伸張、変更を実行時に動的に行ないたいため、jsonにして吐き出す。実態がないほうがいい)
                */
                {
                    var list = new List<GameObject>();

                    for (var i = 0; i < currentLayerInstance.transform.childCount; i++) {
                        list.Add(currentLayerInstance.transform.GetChild(i).gameObject);
                    }

                    // 取り出してから消す
                    foreach (var childObj in list) {
                        GameObject.DestroyImmediate(childObj);
                    }
                }
                
                /*
                    このprefabはlayer = レイアウトを行う前提の箱として使われる。
                    box = 特定の名前の要素(prefabになる前の子供ゲームオブジェクト名のタグ要素)のみをレイアウトする。
                    その中身にはなんでも入っていい。

                    prefab名は大文字 になる。
                */
                {
                    var prefabPath = "Assets/InformationResources/Resources/Views/" + viewName + "/" + layerName.ToUpper() + ".prefab";
                    var dirPath = Path.GetDirectoryName(prefabPath);
                    
                    FileController.CreateDirectoryRecursively(dirPath);
                    PrefabUtility.CreatePrefab(prefabPath, currentLayerInstance);

                    // layer自体の削除
                    GameObject.DestroyImmediate(currentLayerInstance);
                }

                /*
                    このレイヤーのboxの情報を追加
                */
                {
                    var newChildConstraint = childrenConstraintDict
                        .Select(kv => new BoxConstraint(kv.Key, kv.Value))
                        .ToArray();

                    var newConstraints = new LayerInfo(
                        layerName, 
                        newChildConstraint,
                        "resources://" + ConstSettings.PREFIX_PATH_INFORMATION_RESOURCE + viewName + "/" + layerName.ToUpper()
                    );

                    currentConstraints.Add(newConstraints);
                }

                /*
                    取り出しておいたchildに対して再帰
                */
                foreach (var disposableChild in copiedChildList) {
                    ModifyLayerInstance(viewName, disposableChild, currentConstraints);
                }
            }
        }


        private class GameObjectDeleteUsing : IDisposable {
            private GameObject[] children;
            
            public GameObjectDeleteUsing (params GameObject[] target) {
                this.children = target;
            }

            private bool disposedValue = false;

            protected virtual void Dispose (bool disposing) {
                if (!disposedValue) {
                    if (disposing) {
                        for (var i = 0; i < children.Length; i++) {
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