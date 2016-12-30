using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Bundlizer {
	[MenuItem("Window/Autoya/GenerateTestAssetBundles")] public static void BuildAssetBundles () {
		BuildPipeline.BuildAssetBundles("AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneOSXIntel64);
	}
}
