using System;
using System.Reflection;
using UnityEngine.Networking;
using UnityEngine.UDP.Common;

namespace UnityEngine.UDP.Editor.Analytics
{
    public static class EditorAnalyticsApi
    {
        public const string k_API_URL = "/udp/api/cdp/event";
        public const string k_ENDPOINT = BuildConfig.CONNECT_ENDPOINT;

        public static string orgIdCache;
        public static string userIdCache;

        #region Event Names

        internal const string k_ImportSDKEventName = "editorImportSDK";
        internal const string k_ClientCreateEventName = "editorClientCreate";
        internal const string k_ClientUpdateEventName = "editorClientUpdate";
        internal const string k_IapCreateEventName = "editorIapCreate";
        internal const string k_IapUpdateEventName = "editorIapUpdate";
        internal const string k_ProjectBuildEventName = "editorProjectBuild";
        internal const string k_AppCreateEventName = "editorAppCreate";
        internal const string k_AppUpdateEventName = "editorAppUpdate";
        internal const string k_ProjectOpenEventName = "editorProjectOpen";

        #endregion

        public static UnityWebRequest ImportSdk()
        {
            var parameters = Common.GetCommonParams();
            EventRequest request = new EventRequest
            {
                type = k_ImportSDKEventName,
                msg = MiniJson.JsonEncode(parameters),
            };

            return AssembleAndSendWebRequest(request);
        }

        public static UnityWebRequest ProjectOpened()
        {
            var parameters = Common.GetCommonParams();
            EventRequest request = new EventRequest
            {
                type = k_ProjectOpenEventName,
                msg = MiniJson.JsonEncode(parameters),
            };

            return AssembleAndSendWebRequest(request);
        }

        // clientCreate or clientUpdate
        public static UnityWebRequest ClientEvent(string eventName, string clientId, string failedReason)
        {
            var parameters = Common.GetCommonParams();

            bool successful = failedReason == null;
            parameters.Add(Common.k_Successful, successful);
            if (successful)
            {
                parameters.Add(Common.k_ClientId, clientId);
            }
            else
            {
                parameters.Add(Common.k_FailedReason, failedReason);
            }

            EventRequest request = new EventRequest
            {
                type = eventName,
                msg = MiniJson.JsonEncode(parameters),
            };

            return AssembleAndSendWebRequest(request);
        }

        // iapCreate && iapUpdate
        public static UnityWebRequest IapEvent(string eventName, string clientId, IapItem item, string failedReason)
        {
            var parameters = Common.GetCommonParams();
            parameters.Add(Common.k_ClientId, clientId);

            if (failedReason != null)
            {
                parameters.Add(Common.k_FailedReason, failedReason);
            }

            bool successful = failedReason == null;
            parameters.Add(Common.k_Successful, successful);

            if (successful)
            {
                parameters.Add(Common.k_Consumable, item.consumable);
                parameters.Add(Common.k_ItemId, item.id);
                parameters.Add(Common.k_ItemType, "inapp");
                var priceList = item.priceSets.PurchaseFee.priceMap.DEFAULT;
                parameters.Add(Common.k_PriceList, priceList);

                parameters.Add(Common.k_ProductId, item.slug);
                parameters.Add(Common.k_OwnerId, item.ownerId);
                parameters.Add(Common.k_OwnerType, item.ownerType);
            }

            EventRequest request = new EventRequest
            {
                type = eventName,
                msg = MiniJson.JsonEncode(parameters),
            };

            return AssembleAndSendWebRequest(request);
        }

        public static UnityWebRequest AppEvent(string eventName, string clientId, AppItemResponse appItem,
            string failedReason)
        {
            var parameters = Common.GetCommonParams();
            bool successful = failedReason == null;

            parameters.Add(Common.k_Successful, successful);

            if (!successful)
            {
                parameters.Add(Common.k_FailedReason, failedReason);
            }
            else
            {
                parameters.Add(Common.k_ClientId, appItem.clientId);
                parameters.Add(Common.k_Revision, appItem.revision);
                parameters.Add(Common.k_AppName, appItem.name);
                parameters.Add(Common.k_AppSlug, appItem.slug);
                parameters.Add(Common.k_AppType, appItem.type);
                parameters.Add(Common.k_OwnerId, appItem.ownerId);
                parameters.Add(Common.k_OwnerType, appItem.ownerType);
            }

            EventRequest request = new EventRequest
            {
                type = eventName,
                msg = MiniJson.JsonEncode(parameters),
            };

            return AssembleAndSendWebRequest(request);
        }

        private static UnityWebRequest AssembleAndSendWebRequest(EventRequest request)
        {
            return AppStoreOnboardApi.asyncRequest(UnityWebRequest.kHttpVerbPOST, k_ENDPOINT, k_API_URL, null, request);
        }

        public static UnityWebRequest ProjectBuildEvent()
        {
            var parameters = Common.GetCommonParams();
            EventRequest request = new EventRequest
            {
                type = k_ProjectBuildEventName,
                msg = MiniJson.JsonEncode(parameters)
            };

            return AssembleAndSendWebRequest(request);
        }
    }

    #region models

    [Serializable]
    public class EventRequest
    {
        public string type;
        public string msg; // json string of payload
    }

    [Serializable]
    public class EventRequestResponse : GeneralResponse
    {
    }

    #endregion
}