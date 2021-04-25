#if UNITY_IOS
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using UnityEditor.iOS.Xcode;

public static class BuildPostprocess_SignInWIthApple
{
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        // create PBXProject.
        PBXProject proj = new PBXProject();
        var xcodeProjPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
        proj.ReadFromFile(xcodeProjPath);

        // enable SIWA.
        var mainTargetGuid = proj.GetUnityMainTargetGuid();
        var targetName = "Unity-iPhone";
        var productName = proj.GetBuildPropertyForAnyConfig(mainTargetGuid, "PRODUCT_NAME_APP");
        var entitlementsPath = targetName + "/" + Application.productName + ".entitlements";
        proj.SetBuildProperty(mainTargetGuid, "CODE_SIGN_ENTITLEMENTS", entitlementsPath);

        var frameworkGuid = proj.GetUnityFrameworkTargetGuid();
        proj.AddFrameworkToProject(frameworkGuid, "AuthenticationServices.framework", true);

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

        // writeout project.
        proj.WriteToFile(xcodeProjPath);
    }
}
#endif