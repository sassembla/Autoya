using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class AutoyaEditorTool
{
    [MenuItem("Window/Autoya/Clean Cached AssetBundles")]
    public static void CleanCache()
    {
        Caching.ClearCache();
    }


    [MenuItem("Window/Autoya/Update UnityPackage")]
    public static void UnityPackage()
    {
        var assetPaths = new List<string>();

        var frameworkPath = "Assets/Autoya";
        CollectPathRecursive(frameworkPath, assetPaths);

        AssetDatabase.ExportPackage(assetPaths.ToArray(), "Autoya.unitypackage", ExportPackageOptions.IncludeDependencies);

        var assetGraphPath = "Assets/Editor";
        CollectPathRecursive(assetGraphPath, assetPaths);
        AssetDatabase.ExportPackage(assetPaths.ToArray(), "Autoya+AssetGraph.unitypackage", ExportPackageOptions.IncludeDependencies);
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

    static AutoyaEditorTool()
    {
        // create unitypackage if compiled.
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            UnityPackage();
        }
    }
}