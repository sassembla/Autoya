#if UNITY_5_6_OR_NEWER && !UNITY_5_6_0
using System;
using System.Collections.Generic;

namespace AppStoreModel
{
	public class UnityClientInfo
	{
		public string ClientId { get; set; }
		public string ClientKey { get; set; }
		public string ClientRSAPublicKey { get; set; }
		public string ClientSecret {get; set;}
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

		public UnityClient() {
			this.scopes = new List<string> ();
		}
	}

	[Serializable]
	public class UnityChannel
	{
		public XiaomiSettings xiaomi;
		public string projectGuid;
		public string callbackUrl;
	}

	[Serializable]
	public class XiaomiSettings
	{
		public string appId;
		public string appKey;
		public string appSecret;
	}

	[Serializable]
	public class UnityClientResponseWrapper: GeneralResponse
	{
		public UnityClientResponse[] array;
	}

	[Serializable]
	public class UnityClientResponse: GeneralResponse
	{
		public string client_id;
		public string client_secret;
		public UnityChannelResponse channel;
		public string rev;
	}

	[Serializable]
	public class UnityChannelResponse
	{
		public List<ThirdPartySettingsResponse> thirdPartySettings;
		public string projectGuid;
		public string callbackUrl;
		public string publicRSAKey;
		public string channelSecret;

		public UnityChannelResponse() {
			this.thirdPartySettings = new List<ThirdPartySettingsResponse> ();
		}
	}

	[Serializable]
	public class ThirdPartySettingsResponse
	{
		public string appId;
		public string appKey;
		public string appSecret;
		public string appType;
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
	public class TokenInfo: GeneralResponse
	{
		public string access_token;
		public string refresh_token;
	}

	[Serializable]
	public class UserIdResponse: GeneralResponse
	{
		public string sub;
	}

	[Serializable]
	public class OrgIdResponse: GeneralResponse
	{
		public string org_foreign_key;
	}

	[Serializable]
	public class OrgRoleResponse: GeneralResponse
	{
		public List<string> roles;
	}

	[Serializable]
	public class GeneralResponse
	{
		public string message;
	}
}
#endif
