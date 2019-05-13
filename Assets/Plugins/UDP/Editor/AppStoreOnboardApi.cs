using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Networking;

namespace UnityEngine.UDP.Editor
{
    public class AppStoreOnboardApi
    {
        public const string oauthClientId = BuildConfig.CLIENT_ID;
        public const string oauthClientSecret = BuildConfig.CLIENT_SECRET; //staging
        public const string oauthRedirectUri = BuildConfig.ID_ENDPOINT;
        public const string url = BuildConfig.API_ENDPOINT;
        public const string expiredAccessTokenInfo = "Expired Access Token";
        public const string invalidAccessTokenInfo = "Invalid Access Token";
        public const string expiredRefreshTokenInfo = "Expired Refresh Token";
        public const string invalidRefreshTokenInfo = "Invalid Refresh Token";
        public const string forbiddenInfo = "Forbidden";
        public static TokenInfo tokenInfo = new TokenInfo();
        public static ThirdPartySetting[] tps = new ThirdPartySetting[10];
        public static string userId;
        public static string orgId;
        public static string updateRev;
        public static bool loaded = false;

        public const string udpurl = BuildConfig.UDP_ENDPOINT;

        public static UnityWebRequest asyncRequest(string method, string url, string api, string token,
            object postObject)
        {
            UnityWebRequest request = new UnityWebRequest(url + api, method);

            if (postObject != null)
            {
                string postData = HandlePostData(JsonUtility.ToJson(postObject));
                byte[] postDataBytes = Encoding.UTF8.GetBytes(postData);
                request.uploadHandler = (UploadHandler) new UploadHandlerRaw(postDataBytes);
            }
            request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
            // set content-type header
            request.SetRequestHeader("Content-Type", "application/json");
            // set auth header
            if (token != null)
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
            }

            MethodInfo sendWebRequest = request.GetType().GetMethod("SendWebRequest");
            if (sendWebRequest == null)
            {
                sendWebRequest = request.GetType().GetMethod("Send");
            }

            sendWebRequest.Invoke(request, null);

            return request;
        }

        private static string HandlePostData(string oldData)
        {
            string newData = oldData.Replace("thisShouldBeENHyphenUS", "en-US");
            newData = newData.Replace("thisShouldBeZHHyphenCN", "zh-CN");
            Regex re = new Regex("\"\\w+?\":\"\",");
            newData = re.Replace(newData, "");
            re = new Regex(",\"\\w+?\":\"\"");
            newData = re.Replace(newData, "");
            re = new Regex("\"\\w+?\":\"\"");
            newData = re.Replace(newData, "");
            return newData;
        }

        public static UnityWebRequest asyncRequest(string method, string url, string api, string token,
            object postObject, bool isTest)
        {
            UnityWebRequest request = new UnityWebRequest(url + api, method);

            if (postObject != null)
            {
                string postData = HandlePostData(JsonUtility.ToJson(postObject));
                byte[] postDataBytes = Encoding.UTF8.GetBytes(postData);
                request.uploadHandler = (UploadHandler) new UploadHandlerRaw(postDataBytes);
            }
            request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
            // set content-type header
            request.SetRequestHeader("Content-Type", "application/json");
            // set auth header
            if (isTest)
            {
            }
            else if (token != null)
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
            }

            MethodInfo sendWebRequestMethodInfo = request.GetType().GetMethod("SendWebRequest");

            if (sendWebRequestMethodInfo == null)
            {
                sendWebRequestMethodInfo = request.GetType().GetMethod("Send");
            }

            sendWebRequestMethodInfo.Invoke(request, null);
            return request;
        }

        public static UnityWebRequest RefreshToken()
        {
            TokenRequest req = new TokenRequest();
            req.client_id = oauthClientId;
            req.client_secret = oauthClientSecret;
            req.grant_type = "refresh_token";
            req.refresh_token = tokenInfo.refresh_token;
            return asyncRequest(UnityWebRequest.kHttpVerbPOST, url, "/v1/oauth2/token", null, req);
        }

        public static UnityWebRequest GetAccessToken(string authCode)
        {
            TokenRequest req = new TokenRequest();
            req.code = authCode;
            req.client_id = oauthClientId;
            req.client_secret = oauthClientSecret;
            req.grant_type = "authorization_code";
            req.redirect_uri = oauthRedirectUri;
            return asyncRequest(UnityWebRequest.kHttpVerbPOST, url, "/v1/oauth2/token", null, req);
        }

        public static UnityWebRequest GetUserId()
        {
            string token = tokenInfo.access_token;
            string api = "/v1/oauth2/tokeninfo?access_token=" + token;
            return asyncRequest(UnityWebRequest.kHttpVerbGET, url, api, token, null);
        }

        public static UnityWebRequest GetOrgId(string projectGuid)
        {
            string api = "/v1/core/api/projects/" + projectGuid;
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbGET, url, api, token, null);
        }

        public static UnityWebRequest GetOrgRoles()
        {
            string api = "/v1/organizations/" + orgId + "/roles?userId=" + userId;
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbGET, url, api, token, null);
        }

        public static UnityWebRequest GetUnityClientInfo(string projectGuid)
        {
            string api = "/v1/oauth2/user-clients?projectGuid=" + projectGuid;
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbGET, url, api, token, null);
        }

        public static UnityWebRequest GetUnityClientInfoByClientId(string clientId)
        {
            string api = "/v1/oauth2/user-clients/" + clientId;
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbGET, url, api, token, null);
        }

        public static UnityWebRequest GenerateUnityClient(string projectGuid, UnityClientInfo unityClientInfo,
            string callbackUrl)
        {
            return generateOrUpdateUnityClient(projectGuid, UnityWebRequest.kHttpVerbPOST, unityClientInfo,
                callbackUrl);
        }

        public static UnityWebRequest UpdateUnityClient(string projectGuid, UnityClientInfo unityClientInfo,
            string callbackUrl)
        {
            return generateOrUpdateUnityClient(projectGuid, UnityWebRequest.kHttpVerbPUT, unityClientInfo, callbackUrl);
        }

        static UnityWebRequest generateOrUpdateUnityClient(string projectGuid, string method,
            UnityClientInfo unityClientInfo, string callbackUrl)
        {
            UnityChannel channel = new UnityChannel();
            channel.projectGuid = projectGuid;
            channel.callbackUrl = callbackUrl;
            if (tps != null && tps.Length > 0 && tps[0] != null && !String.IsNullOrEmpty(tps[0].appId))
            {
                channel.thirdPartySettings = tps;
                for (int i = 0; i < channel.thirdPartySettings.Length; i++)
                {
                    if (channel.thirdPartySettings[i].appType.Equals("gstore"))
                    {
                        channel.thirdPartySettings[i].appKey = null;
                        channel.thirdPartySettings[i].appSecret = null;
                    }
                    if (channel.thirdPartySettings[i].appType.Equals("xiaomi"))
                    {
                        channel.thirdPartySettings[i].extraProperties = null;    
                    }
                }
            }

            // set necessary client post data
            UnityClient client = new UnityClient();
            client.client_name = projectGuid;
            client.scopes.Add("identity");
            client.channel = channel;

            string api = null;
            if (method.Equals(UnityWebRequest.kHttpVerbPOST, StringComparison.InvariantCultureIgnoreCase))
            {
                api = "/v1/oauth2/user-clients";
            }
            else if (method.Equals(UnityWebRequest.kHttpVerbPUT, StringComparison.InvariantCultureIgnoreCase))
            {
                // if client is not generated or loaded, directly ignore update
                if (unityClientInfo.ClientId == null)
                {
                    Debug.LogError("Please get/generate Unity Client first.");
                    loaded = false;
                    return null;
                }
                if (updateRev == null)
                {
                    Debug.LogError("Please get/generate Unity Client first.");
                    loaded = false;
                    return null;
                }
                client.rev = updateRev;
                if (orgId == null)
                {
                    Debug.LogError("Please get/generate Unity Client first.");
                    loaded = false;
                    return null;
                }
                client.owner = orgId;
                client.ownerType = "ORGANIZATION";
                api = "/v1/oauth2/user-clients/" + unityClientInfo.ClientId;
            }
            else
            {
                return null;
            }

            string token = tokenInfo.access_token;
            return asyncRequest(method, url, api, token, client);
        }

        public static UnityWebRequest UpdateUnityClientSecret(string clientId)
        {
            if (clientId == null)
            {
                return null;
            }
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbPUT, url,
                "/v1/oauth2/user-clients/channel-secret?clientId=" + clientId, token, null);
        }

        public static UnityWebRequest SaveTestAccount(Player player, string clientId)
        {
            string api = "/v1/player";
            string token = tokenInfo.access_token;
            player.clientId = clientId;
            return asyncRequest(UnityWebRequest.kHttpVerbPOST, udpurl, api, token, player, false);
        }

        public static UnityWebRequest VerifyTestAccount(string playerId)
        {
            string api = "/v1/player/" + playerId + "/set-email-verified";
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbPOST, udpurl, api, token, null, false);
        }

        public static UnityWebRequest GetTestAccount(string clientId)
        {
            string api = "/v1/player/0/all?clientId=" + clientId;
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbGET, udpurl, api, token, null, false);
        }

        public static UnityWebRequest DeleteTestAccount(string playerId)
        {
            string api = "/v1/player/" + playerId;
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbDELETE, udpurl, api, token, null, false);
        }

        public static UnityWebRequest UpdateTestAccount(PlayerChangePasswordRequest player)
        {
            string api = "/v1/player/change-password";
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbPOST, udpurl, api, token, player, false);
        }

        public static UnityWebRequest CreateAppItem(AppItem appItem)
        {
            string api = "/v1/store/items";
            string token = tokenInfo.access_token;
            appItem.status = "STAGE";
            appItem.ownerType = "ORGANIZATION";
            appItem.type = "APP";
            appItem.packageName = "com.unity";
            return asyncRequest(UnityWebRequest.kHttpVerbPOST, udpurl, api, token, appItem, false);
        }

        public static UnityWebRequest UpdateAppItem(AppItem appItem)
        {
            string api = "/v1/store/items/" + appItem.id;
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbPUT, udpurl, api, token, appItem, false);
        }

        public static UnityWebRequest PublishAppItem(string appItemId)
        {
            string api = "/v1/store/items/" + appItemId + "/listing";
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbPOST, udpurl, api, token, null, false);
        }

        public static UnityWebRequest GetAppItem(string clientId)
        {
            string api = "/v1/store/items/search?ownerId=" + orgId +
                         "&ownerType=ORGANIZATION&type=APP&start=0&count=1&clientId=" + clientId;
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbGET, udpurl, api, token, null, false);
        }

        public static UnityWebRequest CreateStoreItem(IapItem iapItem)
        {
            string api = "/v1/store/items";
            string token = tokenInfo.access_token;
            iapItem.ownerId = orgId;
            return asyncRequest(UnityWebRequest.kHttpVerbPOST, udpurl, api, token, iapItem, false);
        }

        public static UnityWebRequest UpdateStoreItem(IapItem iapItem)
        {
            string api = "/v1/store/items/" + iapItem.id;
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbPUT, udpurl, api, token, iapItem, false);
        }

        public static UnityWebRequest SearchStoreItem(String appItemSlug)
        {
            string api = "/v1/store/items/search?ownerId=" + orgId +
                         "&ownerType=ORGANIZATION&start=0&count=20&type=IAP&masterItemSlug=" + appItemSlug;
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbGET, udpurl, api, token, null, false);
        }

        public static UnityWebRequest DeleteStoreItem(string iapItemId)
        {
            string api = "/v1/store/items/" + iapItemId;
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbDELETE, udpurl, api, token, null, false);
        }

        public static UnityWebRequest GetAppItemSlugWithId(String appItemId)
        {
            string api = "/v1/store/items/" + appItemId;
            string token = tokenInfo.access_token;
            return asyncRequest(UnityWebRequest.kHttpVerbGET, udpurl, api, token, null, false);
        }
    }
}