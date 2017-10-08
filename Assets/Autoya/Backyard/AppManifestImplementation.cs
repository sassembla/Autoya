
using System;
using System.Collections.Generic;
using AutoyaFramework.AppManifest;

namespace AutoyaFramework {
	public partial class Autoya {
        private AppManifestStore<RuntimeManifestObject, BuildManifestObject> manifestStore;

        private void InitializeAppManifest () {
            manifestStore = new AppManifestStore<RuntimeManifestObject, BuildManifestObject>(OnOverwriteRuntimeManifest, OnLoadRuntimeManifest);
        }

        /*
            public functions
         */
        public static Dictionary<string, string> Manifest_GetAppManifest () {
            return autoya.manifestStore.GetParamDict();
        }

        public static bool Manifest_UpdateRuntimeManifest (RuntimeManifestObject updated) {
            return autoya.manifestStore.UpdateRuntimeManifest(updated);
        }

        public static RuntimeManifestObject Manifest_LoadRuntimeManifest () {
            return autoya.manifestStore.GetRuntimeManifest();
        }
    }
    

    [UnityEditor.InitializeOnLoad] public class OnBuildEntryPoint {
		static OnBuildEntryPoint () {
			if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                OnBuild();
            }
		}
        
        public static void OnBuild () {

            // コマンドラインから実行された場合、buildCommentを読み出す。例えばSlackからビルドコメント付きでビルドさせたりするのに役立つ。

            AppManifestStore<RuntimeManifestObject, BuildManifestObject>.UpdateBuildManifest(
                current => {
                    // countup build count.
                    var buildCountStr = current.buildCount;
                    var buildCountNum = Convert.ToInt64(buildCountStr) + 1;
                    current.buildCount = buildCountNum.ToString();

                    return current;
                }
            );
        }
	}
}