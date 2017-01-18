using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.Representation.Base64;
using AutoyaFramework.Settings.Auth;
using UnityEngine;

/**
    modify this class for your authentication dataflow.
*/
namespace AutoyaFramework {

    public partial class Autoya {
        /**
            return if server is under maintenance or not.
        */
        private bool IsUnderMaintenance (int httpCode, Dictionary<string, string> responseHeader) {
            return httpCode == BackyardSettings.MAINTENANCE_CODE;
        }

        
        /**
            send authentication data to server at first boot.
        */
        private IEnumerator OnBootAuthKeyRequested (Action<Dictionary<string, string>> setHeaderToRequest) {
            // set boot authentication data.
            var bootKey = AuthSettings.AUTH_BOOT;
            var base64Str = Base64.FromBytes(bootKey);

            var bootRequestHeader = new Dictionary<string, string> {
                {"Authorization", base64Str}
            };

            setHeaderToRequest(bootRequestHeader);
            yield break;
        }

        /**
            received first boot authentication result.
        */
        private IEnumerator OnBootReceived (Dictionary<string, string> responseHeader, string data) {
            var isValidResponse = true;
            if (isValidResponse) {
                Autoya.Persist_Update(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME, data);
            } else {
                // failsafe here.
            }
            yield break;
        }

        /**
            check if server response is unauthorized or not.
        */
        private bool IsUnauthorized (int httpCode, Dictionary<string, string> responseHeader) {
            return httpCode == AuthSettings.AUTH_HTTP_CODE_UNAUTHORIZED;
        }

        /**
            received 401. should authenticate again.
        */
        private IEnumerator OnTokenRefreshRequested (Action<Dictionary<string, string>> setHeaderToRequest) {
            // return refresh token for re-authenticate.
            var refreshToken = Autoya.Persist_Load(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME);

            var refreshRequestHeader = new Dictionary<string, string> {
                {"Authorization", refreshToken}
            };

            setHeaderToRequest(refreshRequestHeader);
            yield break;
        }

        /**
            received refreshed token.
        */
        private IEnumerator OnTokenRefreshReceived (Dictionary<string, string> responseHeader, string data) {
            var isValidResponse = true;
            if (isValidResponse) {
                Autoya.Persist_Update(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME, data);
            } else {
                // failsafe here.
            }

            yield break;
        }


        /*
            http request header delegates.
        */
        public enum HttpMethod {
            Get,
            Post,
            Put,
            Delete
        }
        
        private Dictionary<string, string> OnHttpRequest (HttpMethod method, string url, Dictionary<string, string> requestHeader, string data) {
            var accessToken = Autoya.Persist_Load(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME);
            requestHeader["Authorization"] = Base64.FromString(accessToken);
            
            return requestHeader;
        }
    }
}