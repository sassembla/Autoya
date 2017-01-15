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
            send authentication data to server at first boot.
        */
        private static string OnBootAuthRequested () {
            // set boot authentication data.
            var bootKey = AuthSettings.AUTH_BOOT;
            var base64Str = Base64.FromBytes(bootKey);
            return base64Str;
        }

        /**
            received first boot authentication result.
        */
        private void OnBootReceived (Dictionary<string, string> responseHeader, string data) {
            var isValidResponse = true;
            if (isValidResponse) {
                Autoya.Persist_Update(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME, data);
            } else {
                // failsafe here.
            }
        }

        /**
            received 401. should authenticate again.
        */
        private string OnTokenRefreshRequested () {
            // return refresh token for re-authenticate.
            var refreshToken = Autoya.Persist_Load(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME);
            return refreshToken;
        }

        /**
            received refreshed token.
        */
        private void OnTokenRefreshReceived (Dictionary<string, string> headers, string data) {
           var isValidResponse = true;
            if (isValidResponse) {
                Autoya.Persist_Update(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME, data);
            } else {
                // failsafe here.
            }
        }
    }
}