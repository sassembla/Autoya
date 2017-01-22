using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.Purchase;
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
            detect if already authenticated or not.
            if not first boot, you can load your token here.
        */
        private bool IsFirstBoot () {
            var tokenCandidatePaths = _autoyaFilePersistence.FileNamesInDomain(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN);
            return tokenCandidatePaths.Length == 0;
        }

        /**
            send authentication data to server at first boot.
        */
        private IEnumerator OnBootAuthRequest (Action<Dictionary<string, string>, string> setHeaderAndDataToRequest) {
            // set boot body data for Http.Post to server.(if empty, this framework use Http.Get for sending data to server.)
            var data = "some boot data";

            // set boot authentication header.
            var bootKey = AuthSettings.AUTH_BOOT;
            var base64Str = Base64.FromBytes(bootKey);

            var bootRequestHeader = new Dictionary<string, string> {
                {"Authorization", base64Str}
            };

            setHeaderAndDataToRequest(bootRequestHeader, data);
            yield break;
        }

        /**
            received first boot authentication result.
        */
        private IEnumerator OnBootAuthResponse (Dictionary<string, string> responseHeader, string data) {
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
        private IEnumerator OnTokenRefreshRequest (Action<Dictionary<string, string>, string> setHeaderToRequest) {
            // set refresh body data for Http.Post to server.(if empty, this framework use Http.Get for sending data to server.)
            var data = "some refresh data";

            // return refresh token for re-authenticate.
            var refreshToken = Autoya.Persist_Load(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME);

            var refreshRequestHeader = new Dictionary<string, string> {
                {"Authorization", refreshToken}
            };

            setHeaderToRequest(refreshRequestHeader, data);
            yield break;
        }

        /**
            received refreshed token.
        */
        private IEnumerator OnTokenRefreshResponse (Dictionary<string, string> responseHeader, string data) {
            var isValidResponse = true;
            if (isValidResponse) {
                Autoya.Persist_Update(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME, data);
            } else {
                // failsafe here.
            }

            yield break;
        }

        
        /*
            standard http request & response handler.
        */

        /**
            fire when generating http request, via Autoya.Http_X.
            you can add some kind of authorization parameter to request header.
        */
        private Dictionary<string, string> OnHttpRequest (HttpMethod method, string url, Dictionary<string, string> requestHeader, string data) {
            var accessToken = Autoya.Persist_Load(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME);
            requestHeader["Authorization"] = Base64.FromString(accessToken);
            
            return requestHeader;
        }

        /**
            fire when reveived http response from server, via Autoya.Http_X.
            you can verify response data & header parameter.

            accepted http code is 200 ~ 299.

            if everything looks good, return string.Empty. 
                then "succeeded" action of Autoya.Http_X will be raised.

            else, return your origial error message. 
                then "failed" action of Autoya.Http_X will be raised with your message.
        */
        private string OnValidateHttpResponse (HttpMethod method, string url, Dictionary<string, string> responseHeader, object data) {
            if (true) {
                return string.Empty;
            }
        }


        /*
            purchase feature handler.
        */


        /**
            fire when the server returns product datas for this app.
            these datas should return platform-specific data.

            e,g, if player is iOS, should return iOS item data.
        */
        private ProductInfo[] OnLoadProductsResponse (string responseData) {
            /*
                get ProductInfo[] data from this responseData.
                server should return ProductInfos data type.

                below is generating products data for exsample.
                responseData is ignored.

                let's change.
            */
            return new ProductInfo[] {
                new ProductInfo("100_gold_coins", "100_gold_coins_iOS"),
                new ProductInfo("1000_gold_coins", "1000_gold_coins_iOS")
            };
        }
        
        /**
            purchase feature is succeeded to load.
        */
        private void OnPurchaseReady () {
            // do something if need.
        }

        /**
            fire when failed to ready purchase feature.

            e,g, show dialog to player. show "reloading purchase feature... please wait amoment" or other message by error.
            this err parameter includes "player can not available purchase feature" and other many situations are exists.
            see Purchase.PurchaseRouter.PurchaseError enum.

            purchase feature is failed to load. but Autoya retries to load store feature in background automatically.
            when success, OnPurchaseReady will be called.
        */
        private void OnPurchaseReadyFailed (Purchase.PurchaseRouter.PurchaseError err, string reason, AutoyaStatus autoyaStatus) {
            // do something if need. 
        }

        /**
            received ticket data for purchasing product via Autoya.Purchase.
            you can modify received ticket data string to desired data.
            returned string will be send to the server for item-deploy information of this purchase.
        */
        private string OnTicketResponse (string ticketData) {
            return ticketData;
        }
    }
}