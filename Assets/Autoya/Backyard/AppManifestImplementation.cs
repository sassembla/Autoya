
using System;
using System.Collections.Generic;
using System.Linq;
using AutoyaFramework.AppManifest;
using UnityEngine;

namespace AutoyaFramework
{
    public partial class Autoya
    {
        private AppManifestStore<RuntimeManifestObject, BuildManifestObject> _appManifestStore;

        private void InitializeAppManifest()
        {
            _appManifestStore = new AppManifestStore<RuntimeManifestObject, BuildManifestObject>(OnOverwriteRuntimeManifest, OnLoadRuntimeManifest);
        }

        /*
            public functions
         */
        public static BuildManifestObject Manifest_GetBuildManifest()
        {
            return autoya._appManifestStore.GetRawBuildManifest();
        }
        public static Dictionary<string, string> Manifest_GetAppManifest()
        {
            return autoya._appManifestStore.GetParamDict();
        }

        public static bool Manifest_UpdateRuntimeManifest(RuntimeManifestObject updated)
        {
            return autoya._appManifestStore.UpdateRuntimeManifest(updated);
        }

        public static RuntimeManifestObject Manifest_LoadRuntimeManifest()
        {
            return autoya._appManifestStore.GetRuntimeManifest();
        }

        public static string Manifest_ResourceVersionDescription()
        {
            var manifest = autoya._appManifestStore.GetRuntimeManifest();
            return string.Join(",", manifest.resourceInfos.Select(info => info.listIdentity + ":" + info.listVersion).ToArray());
        }

        public static void Debug_Manifest_RenewRuntimeManifest()
        {
            var newOne = new RuntimeManifestObject();
            autoya._appManifestStore.UpdateRuntimeManifest(newOne);
        }

        /*
            editor functions
         */
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoad]
        public class BuildEntryPoint
        {
            // ${UNITY_APP} -batchmode -projectPath $(pwd) -quit -executeMethod AutoyaFramework.BuildEntryPoint.Entry -m "herecomes!"

            static BuildEntryPoint()
            {
                var buildMessage = string.Empty;

                var commandLineOptions = System.Environment.GetCommandLineArgs().ToList();
                if (commandLineOptions.Contains("-batchmode"))
                {
                    for (var i = 0; i < commandLineOptions.Count; i++)
                    {
                        var param = commandLineOptions[i];

                        switch (param)
                        {
                            case "-m":
                            case "--message":
                                {
                                    if (i < commandLineOptions.Count - 1)
                                    {
                                        buildMessage = commandLineOptions[i + 1];
                                    }
                                    break;
                                }

                        }
                    }
                }

                if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    OnCompile(buildMessage);
                }
            }

            public static void Entry()
            {
                // 何にもすることがないが、コマンドライン処理をすることはできる。でもまあ、だいたい独自になんかすると思うので、出しゃばる必要はない気がする。
                // 理想的な挙動について考えよう。
            }

            private static void OnCompile(string buildMessage = null)
            {
                AppManifestStore<RuntimeManifestObject, BuildManifestObject>.UpdateBuildManifest(
                    (current, buildTimeLatest) =>
                    {
                        // update build version.
                        current.appVersion = buildTimeLatest.appVersion;

                        // countup build count.
                        var buildNoStr = current.buildNo;
                        if (string.IsNullOrEmpty(buildNoStr))
                        {
                            buildNoStr = "0";
                        }
                        var buildNoNum = Convert.ToInt64(buildNoStr) + 1;
                        current.buildNo = buildNoNum.ToString();

                        // set message if exist.
                        if (!string.IsNullOrEmpty(buildMessage))
                        {
                            current.buildMessage = buildMessage;
                        }

                        current.buildDate = DateTime.Now.ToString();

                        return current;
                    }
                );
            }
        }
#endif
    }
}
