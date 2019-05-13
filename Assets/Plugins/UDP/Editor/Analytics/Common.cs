using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine.UDP.Editor.Analytics
{
    public static class Common
    {
        // common keys
        private const string k_OrgId = "org_id";
        private const string k_EventType = "event_type";
        private const string k_ProjectId = "project_id";
        private const string k_ts = "ts";
        private const string k_UserId = "user_id";
        private const string k_EventTypeValue = "editor";
        private const string k_UnityVersion = "unity_version";
        private const string k_SdkVersion = "sdk_version";
        private const string k_DeviceId = "deviceID";

        // specific keys
        public const string k_AppName = "app_name";
        public const string k_AppSlug = "app_slug";
        public const string k_AppType = "app_type";
        public const string k_ClientId = "client_id";
        public const string k_Consumable = "consumable";
        public const string k_FailedReason = "failed_reason";
        public const string k_ItemId = "item_id";
        public const string k_ItemType = "item_type";
        public const string k_OwnerId = "owner_id";
        public const string k_OwnerType = "owner_type";
        public const string k_PriceList = "price_list";
        public const string k_ProductId = "product_id";
        public const string k_Revision = "revision";
        public const string k_Successful = "successful";

        private static string s_OrganizationId;
        private static string s_UserId;
        private static object s_UnityConnectInstance;

        internal static IDictionary<string, object> GetCommonParams()
        {
            Dictionary<string, object> dic = new Dictionary<string, object>
            {
                {k_ProjectId, Application.cloudProjectId},
                {k_EventType, k_EventTypeValue},
                {k_ts, GetCurrentMillisecondsInUTC()},
                {k_UnityVersion, Application.unityVersion},
                {k_SdkVersion, BuildConfig.VERSION},
                {k_OrgId, GetOrganizationId()},
                {k_UserId, GetUserId()},
                {k_DeviceId, SystemInfo.deviceUniqueIdentifier},
            };

            return dic;
        }

        // UnityEditor.Connect.UnityConnect.instance.GetOrganizationId()
        private static string GetOrganizationId()
        {
            if (string.IsNullOrEmpty(s_OrganizationId))
            {
                Type UnityConnectType = Utils.FindTypeByName("UnityEditor.Connect.UnityConnect");
                MethodInfo getOrgMethod = UnityConnectType.GetMethod("GetOrganizationId");
                s_OrganizationId = (string) getOrgMethod.Invoke(GetUnityConnectInstance(), null);
            }

//            Debug.Log("[Debug] Organization Id: " + s_OrganizationId);
            return s_OrganizationId;
        }

        // UnityEditor.Connect.UnityConnect.instance.GetUserId()
        private static string GetUserId()
        {
            if (string.IsNullOrEmpty(s_UserId))
            {
                Type UnityConnectType = Utils.FindTypeByName("UnityEditor.Connect.UnityConnect");
                MethodInfo getUserIdMethod = UnityConnectType.GetMethod("GetUserId");
                s_UserId = (string) getUserIdMethod.Invoke(GetUnityConnectInstance(), null);
            }

//            Debug.Log("[Debug]Debug User Id: " + s_UserId);
            return s_UserId;
        }

        private static object GetUnityConnectInstance()
        {
            if (s_UnityConnectInstance == null)
            {
                Type UnityConnectType = Utils.FindTypeByName("UnityEditor.Connect.UnityConnect");
                s_UnityConnectInstance = UnityConnectType.GetProperty("instance").GetValue(null, null);
            }

//            Debug.Log("[Debug] UnityConnect Instance is null?: " + (s_UnityConnectInstance == null));
            return s_UnityConnectInstance;
        }

        static UInt64 GetCurrentMillisecondsInUTC()
        {
            UInt64 timeStamp = (UInt64) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds);
            return timeStamp;
        }
    }
}