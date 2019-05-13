using System;
using System.Collections.Generic;

namespace UnityEngine.UDP.Editor
{
    public class UnityClientInfo
    {
        public string ClientId { get; set; }
        public string ClientKey { get; set; }
        public string ClientRSAPublicKey { get; set; }
        public string ClientSecret { get; set; }
    }

    public class RolePermission
    {
        public bool owner { get; set; }
        public bool manager { get; set; }
        public bool user { get; set; }

        public RolePermission()
        {
            owner = false;
            manager = false;
            user = false;
        }
    }

    public class TestAccount
    {
        public string email;
        public string password;
        public string playerId;
        public bool isUpdate;

        public TestAccount()
        {
            this.isUpdate = false;
            this.email = "";
            this.password = "";
            this.playerId = "";
        }
    }

    [Serializable]
    public class UnityClient
    {
        public string client_id;
        public string client_secret;
        public string client_name;
        public List<string> scopes;
        public UnityChannel channel;
        public string rev;
        public string owner;
        public string ownerType;

        public UnityClient()
        {
            this.scopes = new List<string>();
        }
    }

    [Serializable]
    public class UnityChannel
    {
        public string projectGuid;
        public ThirdPartySetting[] thirdPartySettings;
        public string callbackUrl;
        public string bundleIdentifier = "UDP";
    }

    [Serializable]
    public class ThirdPartySetting
    {
        public String appId;
        public String appType;
        public String appKey;
        public String appSecret;
        public ExtraProperties extraProperties;
    }

    [Serializable]
    public class ExtraProperties
    {
        public String pubKey;
        public String performIapCallbacks;
    }

    [Serializable]
    public class UnityClientResponseWrapper : GeneralResponse
    {
        public UnityClientResponse[] array;
    }

    [Serializable]
    public class UnityClientResponse : GeneralResponse
    {
        public string client_id;
        public string client_secret;
        public UnityChannelResponse channel;
        public string rev;
    }

    [Serializable]
    public class UnityChannelResponse
    {
        public string projectGuid;
        public ThirdPartySetting[] thirdPartySettings;
        public string callbackUrl;
        public string publicRSAKey;
        public string channelSecret;
    }

    [Serializable]
    public class TokenRequest
    {
        public string code;
        public string client_id;
        public string client_secret;
        public string grant_type;
        public string redirect_uri;
        public string refresh_token;
    }

    [Serializable]
    public class TokenInfo : GeneralResponse
    {
        public string access_token;
        public string refresh_token;
    }

    [Serializable]
    public class UserIdResponse : GeneralResponse
    {
        public string sub;
    }

    [Serializable]
    public class OrgIdResponse : GeneralResponse
    {
        public string org_foreign_key;
    }

    [Serializable]
    public class OrgRoleResponse : GeneralResponse
    {
        public List<string> roles;
    }

    [Serializable]
    public class GeneralResponse
    {
        public string message;
    }

    [Serializable]
    public class IapItemSearchResponse : GeneralResponse
    {
        public int total;
        public List<IapItem> results;
    }

    [Serializable]
    public class IapItem
    {
        public string id;
        public string slug;
        public string name;
        public string masterItemSlug;
        public bool consumable = true;
        public string type = "IAP";
        public string status = "STAGE";
        public string ownerId;
        public string ownerType = "ORGANIZATION";
        public PriceSets priceSets;
        //public Locales locales;

        public Properties properties;
//		public string refresh_token;

        public int CheckValidation()
        {
            if (slug == null)
            {
                return -1;
            }
            if (name == null)
            {
                return -2;
            }
            if (masterItemSlug == null)
            {
                return -3;
            }
            if (priceSets == null || priceSets.PurchaseFee == null || priceSets.PurchaseFee.priceMap == null || priceSets.PurchaseFee.priceMap.DEFAULT.Count.Equals(0))
            {
                return -4;
            }
            if (properties == null || string.IsNullOrEmpty(properties.description))
            {
                return -6;
            }
            return 0;
        }
    }

    [Serializable]
    public class PriceSets
    {
        public PurchaseFee PurchaseFee;
    }

    [Serializable]
    public class PurchaseFee
    {
        public string priceType;
        public PriceMap priceMap;
    }

    [Serializable]
    public class PriceMap
    {
        public List<PriceDetail> DEFAULT;
    }

    [Serializable]
    public class PriceDetail
    {
        public string price;
        public string currency;
    }

    [Serializable]
    public class Locales
    {
        public Locale thisShouldBeENHyphenUS;
        public Locale thisShouldBeZHHyphenCN;
    }

    [Serializable]
    public class Locale
    {
        public string name;
        public string shortDescription;
        public string longDescription;
    }

    [Serializable]
    public class Player
    {
        public string email;
        public string password;
        public string clientId;
    }

    [Serializable]
    public class PlayerChangePasswordRequest
    {
        public string password;
        public string playerId;
    }

    [Serializable]
    public class PlayerResponse : GeneralResponse
    {
        public string nickName;
        public string id;
    }

    [Serializable]
    public class PlayerResponseWrapper : GeneralResponse
    {
        public int total;
        public PlayerResponse[] results;
    }

    [Serializable]
    public class AppItem
    {
        public string id;
        public string type;
        public string slug;
        public string name;
        public string status;
        public string ownerId;
        public string ownerType;
        public string clientId;
        public string packageName;
        public string revision;
    }

    [Serializable]
    public class Properties
    {
        public string description;
    }

    [Serializable]
    public class AppItemResponse : GeneralResponse
    {
        public string slug;
        public string name;
        public string id;
        public string status;
        public string type;
        public string ownerId;
        public string ownerType;
        public string clientId;
        public string packageName;
        public string revision;
    }

    [Serializable]
    public class AppItemResponseWrapper : GeneralResponse
    {
        public int total;
        public AppItemResponse[] results;
    }

    [Serializable]
    public class AppItemPublishResponse : GeneralResponse
    {
        public string revision;
    }

    [Serializable]
    public class PlayerVerifiedResponse : GeneralResponse
    {
    }

    [Serializable]
    public class PlayerDeleteResponse : GeneralResponse
    {
    }

    [Serializable]
    public class IapItemDeleteResponse : GeneralResponse
    {
    }

    [Serializable]
    public class ErrorResponse : GeneralResponse
    {
        public string code;
        public ErrorDetail[] details;
    }

    [Serializable]
    public class ErrorDetail
    {
        public string field;
        public string reason;
    }

    [Serializable]
    public class PlayerChangePasswordResponse : GeneralResponse
    {
    }
}