using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if UNITY_5_6_OR_NEWER && !UNITY_5_6_0
using UnityEngine;
using UnityEngine.Store;

namespace AppStoresSupport
{
    [System.Serializable]
    public class AppStoreSetting 
    {
        public string AppID = "";
        public string AppKey = "";
        public bool IsTestMode = false;
    }

    [System.Serializable]
    public class AppStoreSettings : ScriptableObject
    {
        public string UnityClientID = "";
        public string UnityClientKey = "";
        public string UnityClientRSAPublicKey = "";

        public AppStoreSetting XiaomiAppStoreSetting = new AppStoreSetting();
        
        public AppInfo getAppInfo() {
            AppInfo appInfo = new AppInfo();
            appInfo.clientId = UnityClientID;
            appInfo.clientKey = UnityClientKey;
            appInfo.appId = XiaomiAppStoreSetting.AppID;
            appInfo.appKey = XiaomiAppStoreSetting.AppKey;
            appInfo.debug = XiaomiAppStoreSetting.IsTestMode;
            return appInfo;
        }

#if UNITY_EDITOR
        [MenuItem("Assets/Create/App Store Settings", false, 1011)]
        static void CreateAppStoreSettingsAsset()
        {
            const string appStoreSettingsAssetFolder = "Assets/Plugins/UnityChannel/XiaomiSupport/Resources";
            const string appStoreSettingsAssetPath = appStoreSettingsAssetFolder + "/AppStoreSettings.asset";
            if (File.Exists(appStoreSettingsAssetPath))
                return;

            if (!Directory.Exists(appStoreSettingsAssetFolder))
                Directory.CreateDirectory(appStoreSettingsAssetFolder);

            var appStoreSettings = CreateInstance<AppStoreSettings>();
            AssetDatabase.CreateAsset(appStoreSettings, appStoreSettingsAssetPath);
        }
#endif
    }
}
#endif