using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using System.IO;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;

public static class BuildPostprocess_SignInWIthApple
{
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target == BuildTarget.iOS)
        {
            // pbxプロジェクトファイルのパス
            string xcodeProjPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";

            // プロジェクトファイルの読み込み
            PBXProject proj = new PBXProject();
            string projFile = File.ReadAllText(xcodeProjPath);
            proj.ReadFromString(projFile);
            var targetName = PBXProject.GetUnityTargetName();
            var guid = proj.TargetGuidByName(targetName);

            proj.AddFrameworkToProject(guid, "AuthenticationServices.framework", true);

            // SignInWithApple有効化
            var productName = proj.GetBuildPropertyForAnyConfig(guid, "PRODUCT_NAME");
            var entitlementsPath = targetName + "/" + productName + ".entitlements";
            proj.SetBuildProperty(guid, "CODE_SIGN_ENTITLEMENTS", entitlementsPath);

            var entPath = path + "/" + entitlementsPath;
            {
                var plist = new PlistDocument();
                if (File.Exists(entPath))
                {
                    plist.ReadFromFile(entPath);
                }

                var rootDict = plist.root;

                var valueArray = new PlistElementArray();
                valueArray.AddString("Default");
                rootDict.values["com.apple.developer.applesignin"] = valueArray;

                File.WriteAllText(entPath, plist.WriteToString());
            }

            // 設定したデータを書き出す
            proj.WriteToFile(xcodeProjPath);
        }
    }
}
#endif