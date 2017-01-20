using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad] public class Bundlizer {
	[MenuItem("Window/Autoya/Generate Test AssetBundles")] public static void BuildAssetBundles () {
		BuildPipeline.BuildAssetBundles("AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneOSXIntel64);
	}

	[MenuItem("Window/Autoya/Update UnityPackage")] public static void UnityPackage () {
		var assetPaths = new List<string>();
		var dirPaths = Directory.GetDirectories("Assets/Autoya");
		
		foreach (var dir in dirPaths) {
			var files = Directory.GetFiles(dir);
			foreach (var file in files) {
				assetPaths.Add(file);
			}
		}

		AssetDatabase.ExportPackage(assetPaths.ToArray(), "Autoya.unitypackage", ExportPackageOptions.IncludeDependencies);
	}

	static Bundlizer () {
		UnityPackage();
	}
}
