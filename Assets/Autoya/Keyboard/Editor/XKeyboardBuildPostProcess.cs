using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Linq;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;

[InitializeOnLoad]
public static class XKeyboardBuildPostProcess
{
    // static LiveKeyboardBuildPostProcess()
    // {
    //     Debug.Log("きてる");
    //     OnPostProcessBuild(BuildTarget.iOS, "/Users/franchouchou/Desktop/milive.client/iOS2");
    // }

    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string projectBasePath)
    {
        if (buildTarget != BuildTarget.iOS)
        {
            return;
        }

        var projectPath = projectBasePath + "/Unity-iPhone.xcodeproj/project.pbxproj";

        PBXProject pbxProject = new PBXProject();
        pbxProject.ReadFromFile(projectPath);

        var iOSBuildTargetName = PBXProject.GetUnityTargetName();
        Debug.Log("Defaultのレイヤーをなんとかする");
        var imageAssetResourcePath = $"{Application.dataPath}/Autoya/Keyboard/Plugins/iOS/KeyboardXibs/Default/defaultKeyboard.imageset";
        var xcAssetsFolderPath = Path.Combine(projectBasePath, iOSBuildTargetName, "Images.xcassets");

        if (Directory.Exists(xcAssetsFolderPath))
        {
            var imageSetPath = Path.Combine(xcAssetsFolderPath, "defaultKeyboard.imageset");
            if (Directory.Exists(imageSetPath) && 0 < Directory.GetFiles(imageSetPath).Length)
            {
                return;
            }

            // dirを作る。
            Directory.CreateDirectory(imageSetPath);

            var localFilePaths = Directory.GetFiles(imageAssetResourcePath).Where(p => Path.GetExtension(p) != ".meta");
            foreach (var filePath in localFilePaths)
            {
                var fileName = Path.GetFileName(filePath);
                var targetFilePath = Path.Combine(imageSetPath, fileName);

                if (File.Exists(targetFilePath))
                {
                    continue;
                }

                // コピーする
                File.Copy(filePath, targetFilePath);
            }

            return;
        }

        Debug.LogError("failed to find xcAssetsFolderPath:" + xcAssetsFolderPath);
    }
}
#endif