
using System.Collections.Generic;
using AutoyaFramework.AppManifest;

namespace AutoyaFramework {
	public partial class Autoya {
        private AppManifestStore<RuntimeManifestObject> manifestStore;

        private void InitializeAppManifest () {
            manifestStore = new AppManifestStore<RuntimeManifestObject>(OnOverwriteRuntimeManifest, OnLoadRuntimeManifest);
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
}