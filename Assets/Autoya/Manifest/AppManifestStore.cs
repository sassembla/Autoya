using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEngine.CloudBuild {// dummy.
    public class BuildManifestObject : ScriptableObject {
        public string scmCommitId;
        public string scmBranch;
        public string buildNumber;
        public string buildStartTime;
        public string projectId;
        public string bundleId;
        public string unityVersion;
        public string xcodeVersion;
        public string cloudBuildTargetName;
    }
}

namespace AutoyaFramework.Versioning.App {
    /**
        アプリケーションの動的なパラメータに関する型情報。
        アプリケーション内にストアされる可能性がある。

        この部分を動的に書き込む機会がある。
        読み込むのはオンメモリも更新保持してるんで大丈夫、という作りにする。

        この型情報部分を外から与えらえるようにしたい。型パラメータとかそんな感じで。
    */
    [Serializable] public class RuntimeManifestObject {
        [SerializeField] public string resVersion;
    }
    

    /**
        アプリケーションのマニフェストを保持、出力、更新するための機構。

        AppManifest
            <- RuntimeManifest
            <- BuildManifest
                <- OriginalBuildManfest
                    +
                <- CloudBuildManifest

        AppManifest
            アプリケーション全体のパラメータの集合。
            RuntimeManifestとBuildManifestの集合を返す。

        RuntimeManifest
            実行時に更新がかかる可能性のあるパラメータの集合。
                resourceVersion

        BuildManifest
            実行時に更新がかからず、ビルド時に固定
            特定のResourcesフォルダに置いてあるjsonを使用。
                BuildManifestObject.json
            UnityCloudBuildでビルドした場合はそのパラメータも使用。
                UnityEngine.CloudBuild.BuildManifestObject

     */
    public class AppManifestStore<T> where T : new() {
        private readonly RuntimeManifest<T> runtimeManifest;
        private readonly BuildManifest buildManifest;

        private readonly Func<string, bool> overwriteFunc;
        private readonly Func<string> loadFunc;

        public AppManifestStore (Func<string, bool> overwriteFunc, Func<string> loadFunc) {
            var runtimeManifestObjStr = loadFunc();

            T runtimeManifestObj;
            if (string.IsNullOrEmpty(runtimeManifestObjStr)) {
                runtimeManifestObj = new T();
                var jsonStr = JsonUtility.ToJson(runtimeManifestObj);
                overwriteFunc(jsonStr);
            } else {
                runtimeManifestObj = JsonUtility.FromJson<T>(runtimeManifestObjStr);
            }

            this.runtimeManifest = new RuntimeManifest<T>(runtimeManifestObj);
            this.buildManifest = new BuildManifest();
            this.overwriteFunc = overwriteFunc;
            this.loadFunc = loadFunc;
        }

        
        public Dictionary<string, string> GetParamDict () {
            var buildManifestDict = buildManifest.buildParamDict;
            var runtimeManifestDict = runtimeManifest.GetDict();
            
            // overwrite buildManifest by runtimeManifest.
            foreach (var runtimeManifestDictItem in runtimeManifestDict) {
                buildManifestDict[runtimeManifestDictItem.Key] = runtimeManifestDictItem.Value;
            }
            
            return buildManifestDict;
        }

        public T GetRuntimeManifest () {
            return runtimeManifest.Obj();
        }

        public bool UpdateRuntimeManifest (T newOne) {

            var jsonStr = JsonUtility.ToJson(newOne);
            var result = overwriteFunc(jsonStr);
            
            if (result) {
                runtimeManifest.UpdateObject(newOne);
            }

            return result;
        }


        public class RuntimeManifest<T> where T : new() {
            private T obj;

            public RuntimeManifest (T obj) {
                this.obj = obj;
            }

            public Dictionary<string, string> GetDict () {
                return obj.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .ToDictionary(prop => prop.Name, prop => (string)prop.GetValue(obj, null));
            }

            public T Obj () {
                return obj;
            }

            public void UpdateObject (T obj) {
                this.obj = obj;
            }
        }




        /**
            ビルド時に書き込まれて以降変化しないデータの型情報。
            リードオンリー。一回読んだらそれで終わり。

            この型情報部分を外から与えらえるようにしたい。
        */ 
        [Serializable] public class BuildManifestObject {
            public string scmCommitId;
            public string scmBranch;
            public string buildNumber;
            public string buildStartTime;
            public string projectId;
            public string bundleId;
            public string unityVersion;
            public string xcodeVersion;
            public string cloudBuildTargetName;
        }

        public class BuildManifest {
            public readonly Dictionary<string, string> buildParamDict;

            public BuildManifest () {
                // load BuildManifest from Resources.
                var baseBuildManifestAsset = Resources.Load<TextAsset>("BuildManifest");
                
                if (baseBuildManifestAsset == null) {
                    buildParamDict = new Dictionary<string, string>();
                } else {
                    var jsonStr = baseBuildManifestAsset.text;
                    if (string.IsNullOrEmpty(jsonStr)) {
                        buildParamDict = new Dictionary<string, string>();
                    } else {
                        var manifestObj = JsonUtility.FromJson<BuildManifestObject>(jsonStr);
                        buildParamDict = manifestObj.GetType()
                            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .ToDictionary(prop => prop.Name, prop => (string)prop.GetValue(buildParamDict, null));
                    }
                }

                // set parameter from application.
                buildParamDict["version"] = Application.version;
                buildParamDict["unityVersion"] = Application.unityVersion;
            
                #if UNITY_EDITOR
                {
                    // もしエディタで、かつファイルが存在しなかったら、jsonを吐き出す。
                    var targetPath = "Assets/Autoya/Manifest/Resources/BuildManifest.json";
                    if (!System.IO.File.Exists(targetPath)) {
                        var jsonStr = JsonUtility.ToJson(new BuildManifestObject());
                        using (var sw = new System.IO.StreamWriter(targetPath)) {
                            sw.WriteLine(jsonStr);
                        }
                    }
                }
                #endif

                #if UNITY_CLOUD_BUILD
                {
                    // overwrite by cloud build if exist.
                    var cloudBuildManifest = Resources.Load<UnityEngine.CloudBuild.BuildManifestObject>("UnityCloudBuildManifest.scriptable");
                    var cloudBuildManifestDict = cloudBuildManifest.GetType()
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .ToDictionary(prop => prop.Name, prop => (string)prop.GetValue(buildParamDict, null));

                    foreach (var cloudBuildManifestDictItem in cloudBuildManifestDict) {
                        var key = cloudBuildManifestDictItem.Key;
                        var val = cloudBuildManifestDictItem.Value;
                        buildParamDict[key] = val;
                    }
                }
                #endif
            }
        }
    }
}