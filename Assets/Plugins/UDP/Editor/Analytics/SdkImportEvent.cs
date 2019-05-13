using UnityEditor;
using UnityEngine.Networking;

#if (UNITY_5_6_OR_NEWER && !UNITY_5_6_0)
namespace UnityEngine.UDP.Editor.Analytics
{
    [InitializeOnLoad]
    public static class SdkImportEvent
    {
        private const string k_SdkImportPlayerPref = "UnityUdpSdkImported";

        static SdkImportEvent()
        {
            if (!PlayerPrefs.HasKey(k_SdkImportPlayerPref))
            {
                PlayerPrefs.SetInt(k_SdkImportPlayerPref, 1);

                UnityWebRequest request = EditorAnalyticsApi.ImportSdk();
                EditorAnalyticsReqStruct reqStruct = new EditorAnalyticsReqStruct
                {
                    eventName = EditorAnalyticsApi.k_ImportSDKEventName,
                    webRequest = request
                };

                // Send the request
                WebRequestQueue.Enqueue(reqStruct);
            }
        }
    }
}
#endif