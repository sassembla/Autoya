// #define ENABLE_XIAOMIPACKAGENAMEEXTENSION
#if ENABLE_XIAOMIPACKAGENAMEEXTENSION
#if UNITY_5_6_OR_NEWER && !UNITY_5_6_0
using UnityEditor;
using UnityEngine;
using UnityEditor.Build;

namespace AppStoresSupport
{
    /// <summary>
    /// Automatic generation of Xiaomi-compatible package identifier during Android build.
    /// </summary>
    public class XiaomiPackageNameExtension : IPreprocessBuild, IPostprocessBuild
    {
        public int callbackOrder
        {
            get { return 0; }
        }

        private const string XiaomiPostfix = ".mi"; // The postfix requested by Xiaomi.
        private bool IsXiaomiPostfixAdded = false;

        public void OnPreprocessBuild(BuildTarget target, string path)
        {
            // Check if the current package name has Xiaomi postfix.
            var originalPackageName = PlayerSettings.applicationIdentifier;
            if (EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.Android && !originalPackageName.EndsWith(XiaomiPostfix))
            {
                Debug.Log(originalPackageName);
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, originalPackageName + XiaomiPostfix);
                IsXiaomiPostfixAdded = true;
            }
        }

        public void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (IsXiaomiPostfixAdded)
            {
                var packageName = PlayerSettings.applicationIdentifier;
                Debug.Log(packageName);
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, packageName.Remove(packageName.Length - XiaomiPostfix.Length));
                IsXiaomiPostfixAdded = false;
            }
        }
    }
}
#endif
#endif // ENABLE_XIAOMIPACKAGENAMEEXTENSION
