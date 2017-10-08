using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AutoyaFramework.AppManifest {
    /**
        アプリケーションのマニフェスト(パラメータの集合)を保持、出力、更新するための機構。

        AppManifest
            <- BuildManifest
                <- BuildManfestType
                    +
                CloudBuildManifest
            <- RuntimeManifestType

        AppManifest
            アプリケーション全体のパラメータの集合。
            RuntimeManifestTypeとBuildManifestTypeの集合を返す。

        BuildManifestType
            実行時に更新がかからず、ビルド時に固定されるパラメータの集合。
            特定のResourcesフォルダに置いてあるjsonを使用。
                以下に保存される。
                Assets/Autoya/Manifest/Resources/BuildManifestObject.json

            UnityCloudBuildでビルドした場合はそのパラメータも併用される。
                BuildManifestTypeに同名のパラメータがあった場合、こちらのパラメータで上書きされる。
        
        RuntimeManifestType
            実行時に更新がかかる可能性のあるパラメータの集合。
                運用に応じて遠隔で変更することができる。
                BuildManifestTypeに同名のパラメータがあった場合、こちらのパラメータで上書きされる。

     */
    public class AppManifestStoreSettings {
        public const string BUILDMANIFEST_PATH = "Assets/Autoya/Manifest/ShouldGitIgnore/Resources/BuildManifest.json";
    }

    public class AppManifestStore<RuntimeManifestType, BuildManifestType> where RuntimeManifestType : new() where BuildManifestType : new() {
        
        private readonly RuntimeManifest<RuntimeManifestType> runtimeManifest;
        private readonly BuildManifest<BuildManifestType> buildManifest;

        private readonly Func<string, bool> overwriteFunc;
        private readonly Func<string> loadFunc;

        public AppManifestStore (Func<string, bool> overwriteFunc, Func<string> loadFunc) {
            this.buildManifest = new BuildManifest<BuildManifestType>();

            // load or renew runtimeManifest.
            RuntimeManifestType runtimeManifestObj;
            {
                var runtimeManifestObjStr = loadFunc();

                // loaded str is null or empty. need crate new file.
                if (string.IsNullOrEmpty(runtimeManifestObjStr)) {
                    runtimeManifestObj = new RuntimeManifestType();
                    var jsonStr = JsonUtility.ToJson(runtimeManifestObj);
                    overwriteFunc(jsonStr);
                } else {
                    runtimeManifestObj = JsonUtility.FromJson<RuntimeManifestType>(runtimeManifestObjStr);
                }
            }

            this.runtimeManifest = new RuntimeManifest<RuntimeManifestType>(runtimeManifestObj);
            this.overwriteFunc = overwriteFunc;
            this.loadFunc = loadFunc;
        }

        
        public Dictionary<string, string> GetParamDict () {
            var buildManifestDict = buildManifest.buildParamDict;
            var runtimeManifestDict = runtimeManifest.GetDict();
            
            // add/overwrite buildManifest by runtimeManifest.
            foreach (var runtimeManifestDictItem in runtimeManifestDict) {
                buildManifestDict[runtimeManifestDictItem.Key] = runtimeManifestDictItem.Value;
            }
            
            return buildManifestDict;
        }

        public RuntimeManifestType GetRuntimeManifest () {
            return runtimeManifest.Obj();
        }

        public bool UpdateRuntimeManifest (RuntimeManifestType newOne) {

            var jsonStr = JsonUtility.ToJson(newOne);
            var result = overwriteFunc(jsonStr);
            
            if (result) {
                runtimeManifest.UpdateObject(newOne);
            }

            return result;
        }


        #if UNITY_EDITOR
        private static BuildManifestType LoadBuildManifest () {
            var targetPath = AppManifestStoreSettings.BUILDMANIFEST_PATH;
            if (!System.IO.File.Exists(targetPath)) {
                return new BuildManifestType();
            }

            using (var sr = new System.IO.StreamReader(targetPath)) {
                var jsonStr = sr.ReadToEnd();
                if (string.IsNullOrEmpty(jsonStr)) {
                    // not found.
                    return new BuildManifestType();
                }
                return JsonUtility.FromJson<BuildManifestType>(jsonStr);
            }
        }

        public static void UpdateBuildManifest (Func<BuildManifestType, BuildManifestType> update) {
            var current = LoadBuildManifest();

            var updated = update(current);

            // overwrite resource file.
            var targetPath = AppManifestStoreSettings.BUILDMANIFEST_PATH;
            var jsonStr = JsonUtility.ToJson(updated);
            using (var sw = new System.IO.StreamWriter(targetPath)) {
                sw.WriteLine(jsonStr);
            }

            UnityEditor.AssetDatabase.Refresh();
        }
        #endif
    }

    public class RuntimeManifest<RuntimeManifestType> where RuntimeManifestType : new() {
        private RuntimeManifestType obj;

        public RuntimeManifest (RuntimeManifestType obj) {
            this.obj = obj;
        }

        public Dictionary<string, string> GetDict () {
            return obj.GetType()
                .GetFields(BindingFlags.DeclaredOnly |BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(prop => prop.Name, prop => (string)prop.GetValue(obj));
        }

        public RuntimeManifestType Obj () {
            return obj;
        }

        public void UpdateObject (RuntimeManifestType obj) {
            this.obj = obj;
        }
    }
    

    public class BuildManifest<BuildManifestType> where BuildManifestType : new() {
        public readonly Dictionary<string, string> buildParamDict;

        public BuildManifest () {
            buildParamDict = LoadBuildParamDict();

            #if UNITY_EDITOR
            {
                // もしエディタで、かつファイルが存在しなかったら、jsonを吐き出す。
                var targetPath = AppManifestStoreSettings.BUILDMANIFEST_PATH;
                if (!System.IO.File.Exists(targetPath)) {
                    var jsonStr = JsonUtility.ToJson(new BuildManifestType());
                    using (var sw = new System.IO.StreamWriter(targetPath)) {
                        sw.WriteLine(jsonStr);
                    }
                }
                UnityEditor.AssetDatabase.Refresh();
            }
            #endif

            // set parameter from application.
            buildParamDict["version"] = Application.version;
            buildParamDict["unityVersion"] = Application.unityVersion;


            #if UNITY_CLOUD_BUILD
            {
                // overwrite by cloud build if exist.
                var cloudBuildManifest = Resources.Load<UnityEngine.CloudBuild.BuildManifestObject>("UnityCloudBuildManifest.scriptable");
                var cloudBuildManifestDict = cloudBuildManifest.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.Public)
                    .ToDictionary(prop => prop.Name, prop => (string)prop.GetValue(buildParamDict));

                foreach (var cloudBuildManifestDictItem in cloudBuildManifestDict) {
                    var key = cloudBuildManifestDictItem.Key;
                    var val = cloudBuildManifestDictItem.Value;
                    buildParamDict[key] = val;
                }
            }
            #endif
        }

        private Dictionary<string, string> LoadBuildParamDict () {
            // load BuildManifest from Resources.
            var baseBuildManifestAsset = Resources.Load<TextAsset>("BuildManifest");
            
            if (baseBuildManifestAsset == null) {
                return new Dictionary<string, string>();
            }
            

            var jsonStr = baseBuildManifestAsset.text;
            if (string.IsNullOrEmpty(jsonStr)) {
                return new Dictionary<string, string>();
            }

            var manifestObj = JsonUtility.FromJson<BuildManifestType>(jsonStr);
            return manifestObj.GetType()
                .GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(prop => prop.Name, prop => (string)prop.GetValue(manifestObj));
        }
    }
}

// namespace UnityEngine.CloudBuild {// dummy.
//     public class BuildManifestObject : ScriptableObject {
//         public string scmCommitId;
//         public string scmBranch;
//         public string buildNumber;
//         public string buildStartTime;
//         public string projectId;
//         public string bundleId;
//         public string unityVersion;
//         public string xcodeVersion;
//         public string cloudBuildTargetName;
//     }
// }