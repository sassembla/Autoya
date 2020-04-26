using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AutoyaFramework.AppManifest
{
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
    public class AppManifestStoreSettings
    {
        public const string BUILDMANIFEST_PATH = "Assets/Autoya/Manifest/ShouldGitIgnore/Resources/BuildManifest.json";

        public static string Prettify(string jsonVal)
        {
            return jsonVal.Replace("\",", "\",\n\t").Replace("{\"", "{\t\n\t\"").Replace("\"}", "\"\n}");
        }
    }

    public interface IRuntimeManifestBase
    {
        void UpdateFromStoredJson(string storedString);
    }

    public class AppManifestStore<RuntimeManifestType, BuildManifestType> where RuntimeManifestType : IRuntimeManifestBase, new() where BuildManifestType : new()
    {

        private RuntimeManifest<RuntimeManifestType> runtimeManifest;
        private readonly BuildManifest<BuildManifestType> buildManifest;
        private readonly Func<string> loadFunc;
        private readonly Func<string, bool> overwriteFunc;

        public AppManifestStore(Func<string, bool> overwriteFunc, Func<string> loadFunc)
        {
            this.buildManifest = new BuildManifest<BuildManifestType>();

            this.loadFunc = loadFunc;
            this.overwriteFunc = overwriteFunc;

            // load or renew runtimeManifest.
            ReloadFromStorage();
        }

        public void ReloadFromStorage()
        {
            RuntimeManifestType runtimeManifestObj;
            {
                var runtimeManifestObjStr = loadFunc();

                // loaded str is null or empty. need crate new file.
                if (string.IsNullOrEmpty(runtimeManifestObjStr))
                {
                    runtimeManifestObj = new RuntimeManifestType();
                    var jsonStr = JsonUtility.ToJson(runtimeManifestObj);
                    overwriteFunc(jsonStr);
                }
                else
                {
                    // get persisted and compare to coded one.
                    runtimeManifestObj = new RuntimeManifestType();
                    runtimeManifestObj.UpdateFromStoredJson(runtimeManifestObjStr);
                }
            }

            this.runtimeManifest = new RuntimeManifest<RuntimeManifestType>(runtimeManifestObj);
        }

        private Dictionary<string, string> buildManifestDict;
        public Dictionary<string, string> GetParamDict()
        {
            if (buildManifestDict == null)
            {
                buildManifestDict = buildManifest.buildParamDict;
                var runtimeManifestDict = runtimeManifest.GetDict();

                // add/overwrite buildManifest by runtimeManifest.
                foreach (var runtimeManifestDictItem in runtimeManifestDict)
                {
                    buildManifestDict[runtimeManifestDictItem.Key] = runtimeManifestDictItem.Value;
                }
            }

            return buildManifestDict;
        }

        public BuildManifestType GetRawBuildManifest()
        {
            return buildManifest.Obj;
        }

        public RuntimeManifestType GetRuntimeManifest()
        {
            return runtimeManifest.Obj();
        }

        public bool UpdateRuntimeManifest(RuntimeManifestType newOne)
        {

            var jsonStr = JsonUtility.ToJson(newOne);
            var result = overwriteFunc(jsonStr);

            if (result)
            {
                runtimeManifest.UpdateObject(newOne);
            }

            buildManifestDict = null;
            return result;
        }


#if UNITY_EDITOR
        private static BuildManifestType LoadBuildManifest()
        {
            var targetPath = AppManifestStoreSettings.BUILDMANIFEST_PATH;
            if (!System.IO.File.Exists(targetPath))
            {
                return new BuildManifestType();
            }

            using (var sr = new System.IO.StreamReader(targetPath))
            {
                var jsonStr = sr.ReadToEnd();
                if (string.IsNullOrEmpty(jsonStr))
                {
                    // not found.
                    return new BuildManifestType();
                }
                return JsonUtility.FromJson<BuildManifestType>(jsonStr);
            }
        }

        public static void UpdateBuildManifest(Func<BuildManifestType, BuildManifestType, BuildManifestType> update)
        {
            var current = LoadBuildManifest();
            var source = new BuildManifestType();
            var updated = update(current, source);

            // overwrite resource file.
            var targetPath = AppManifestStoreSettings.BUILDMANIFEST_PATH;
            var jsonStr = JsonUtility.ToJson(updated);

            jsonStr = AppManifestStoreSettings.Prettify(jsonStr);

            using (var sw = new System.IO.StreamWriter(targetPath))
            {
                sw.WriteLine(jsonStr);
            }

            UnityEditor.AssetDatabase.Refresh();
        }

#endif
    }

    public class RuntimeManifest<RuntimeManifestType> where RuntimeManifestType : new()
    {
        private RuntimeManifestType obj;

        public RuntimeManifest(RuntimeManifestType obj)
        {
            this.obj = obj;
        }

        public Dictionary<string, string> GetDict()
        {
            return obj.GetType()
                .GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(prop => prop.Name, prop => prop.GetValue(obj).ToString());
        }

        public RuntimeManifestType Obj()
        {
            return obj;
        }

        public void UpdateObject(RuntimeManifestType obj)
        {
            this.obj = obj;
        }
    }


    public class BuildManifest<BuildManifestType> where BuildManifestType : new()
    {
        public readonly Dictionary<string, string> buildParamDict;
        public readonly BuildManifestType Obj;

        public BuildManifest()
        {
            BuildManifestType obj;
            this.buildParamDict = LoadBuildParamDict(out obj);
            this.Obj = obj;

#if UNITY_EDITOR
            {
                // もしエディタで、かつファイルが存在しなかったら、jsonを吐き出す。
                var targetPath = AppManifestStoreSettings.BUILDMANIFEST_PATH;
                if (!System.IO.File.Exists(targetPath))
                {
                    var jsonStr = JsonUtility.ToJson(new BuildManifestType());
                    jsonStr = AppManifestStoreSettings.Prettify(jsonStr);
                    using (var sw = new System.IO.StreamWriter(targetPath))
                    {
                        sw.WriteLine(jsonStr);
                    }
                }
                UnityEditor.AssetDatabase.Refresh();
            }
#endif

            // set parameter from application.
            buildParamDict["unityVersion"] = Application.unityVersion;

#if UNITY_CLOUD_BUILD
            {
                // overwrite by cloud build parameter if exist.
                var cloudBuildManifestStr = Resources.Load<TextAsset>("UnityCloudBuildManifest.json").text;
                var cloudBuildManifest = JsonUtility.FromJson<CloudBuildManifest>(cloudBuildManifestStr);
                
                var cloudBuildManifestDict = cloudBuildManifest.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.Public).ToArray()
                    .ToDictionary(prop => prop.Name, prop => (string)prop.GetValue(cloudBuildManifest));
                
                // overwrite.
                foreach (var cloudBuildManifestDictItem in cloudBuildManifestDict) {
                    var key = cloudBuildManifestDictItem.Key;
                    var val = cloudBuildManifestDictItem.Value;
                    buildParamDict[key] = val;
                }
            }
#endif
        }

        private Dictionary<string, string> LoadBuildParamDict(out BuildManifestType obj)
        {
            // load BuildManifest from Resources.
            var baseBuildManifestAsset = Resources.Load<TextAsset>("BuildManifest");

            if (baseBuildManifestAsset == null)
            {
                obj = new BuildManifestType();
                return new Dictionary<string, string>();
            }


            var jsonStr = baseBuildManifestAsset.text;
            if (string.IsNullOrEmpty(jsonStr))
            {
                obj = new BuildManifestType();
                return new Dictionary<string, string>();
            }

            var manifestObj = JsonUtility.FromJson<BuildManifestType>(jsonStr);
            obj = manifestObj;

            return manifestObj.GetType()
                .GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(prop => prop.Name, prop => (string)prop.GetValue(manifestObj));
        }
    }
}

[Serializable]
public class CloudBuildManifest
{
    [SerializeField] public string scmCommitId;
    [SerializeField] public string scmBranch;
    [SerializeField] public string buildNumber;
    [SerializeField] public string buildStartTime;
    [SerializeField] public string projectId;
    [SerializeField] public string bundleId;
    [SerializeField] public string unityVersion;
    [SerializeField] public string xcodeVersion;
    [SerializeField] public string cloudBuildTargetName;
}
