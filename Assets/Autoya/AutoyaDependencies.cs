using System;
using System.Collections.Generic;

/**
	all modules have compatibility for this dependencies of Autoya.
 */
namespace AutoyaFramework
{
    /*
		delegate for supply http request header generate func for modules.
	*/
    public delegate Dictionary<string, string> HttpRequestHeaderDelegate(string method, string url, Dictionary<string, string> requestHeader, string data);

    /*
		delegate for handle http response for modules.
	 */
    public delegate void HttpResponseHandlingDelegate(string connectionId, Dictionary<string, string> responseHeader, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed);

    /*
		delegate for supply assetBundle get request header geneate func for modules.
	*/
    public delegate Dictionary<string, string> AssetBundleGetRequestHeaderDelegate(string url, Dictionary<string, string> requestHeader);

    /**
		struct for represents Autoya's specific status.
		
		if inMaintenance == true, server is in maintenance mode. == server returned http code for maintenance.
			see OverridePoint.cs "IsUnderMaintenance" method to change this behaviour.

		if isAuthFailed == true, server returned 401.
			see OverridePoint.cs "IsUnauthorized" method to change this behaviour.
	*/
    public struct AutoyaStatus
    {
        public readonly bool inMaintenance;
        public readonly bool isAuthFailed;

        // isResponseValidateFailed reserved.

        public AutoyaStatus(bool inMaintenance, bool isAuthFailed, bool isResponseValidateFailed = false)
        {
            this.inMaintenance = inMaintenance;
            this.isAuthFailed = isAuthFailed;
        }

        public bool HasError()
        {
            return inMaintenance || isAuthFailed;
        }

        public override string ToString()
        {
            return "inMaintenance:" + inMaintenance + " isAuthFailed:" + isAuthFailed;
        }
    }
}