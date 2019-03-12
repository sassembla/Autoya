using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AssetBundlesPackager
{
    public static void UnityPackage()
    {
        var assetPaths = new List<string>();

        var packagePath = "Assets/Autoya/AssetBundle";
        CollectPathRecursive(packagePath, assetPaths);

        assetPaths.Add("Assets/Autoya/Settings/AssetBundlesSettings.cs");
        assetPaths.Add("Assets/Autoya/Backyard/BackyardSettings.cs");
        assetPaths.Add("Assets/Autoya/AutoyaDependencies.cs");

        assetPaths.Add("Assets/Packager/AssetBundles/AssetBundlesLoadSample.cs");

        AssetDatabase.ExportPackage(assetPaths.ToArray(), "Autoya.AssetBundles.unitypackage", ExportPackageOptions.IncludeDependencies);
    }

    private static void CollectPathRecursive(string path, List<string> collectedPaths)
    {
        var filePaths = Directory.GetFiles(path);
        foreach (var filePath in filePaths)
        {
            collectedPaths.Add(filePath);
        }

        var modulePaths = Directory.GetDirectories(path);
        foreach (var folderPath in modulePaths)
        {
            CollectPathRecursive(folderPath, collectedPaths);
        }
    }

    static AssetBundlesPackager()
    {
        // create unitypackage if compiled.
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            UnityPackage();
        }
    }
}