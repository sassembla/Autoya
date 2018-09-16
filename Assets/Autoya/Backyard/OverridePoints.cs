using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Purchase;
using AutoyaFramework.Representation.Base64;
using AutoyaFramework.Settings.AssetBundles;
using AutoyaFramework.Settings.Auth;
using UnityEngine;
using AutoyaFramework.Settings.App;
using System.Linq;
using System.IO;

/**
    modify this class for your app's authentication, purchase, assetBundles, appManifest dataflow.
*/
namespace AutoyaFramework
{

    public partial class Autoya
    {
        /**
            you can do something before Autoya boot.
         */
        private IEnumerator OnBootApplication()
        {
            yield break;
        }

        /*
            maintenance handlers.
         */

        /**
            return if server is under maintenance or not.
        */
        private bool IsUnderMaintenance(int httpCode, Dictionary<string, string> responseHeader)
        {
            return httpCode == BackyardSettings.MAINTENANCE_CODE;
        }


        /*
            authentication handlers.
         */

        /**
            return true if already authenticated, return false if not.
            you can load your authenticated data (kind of Token) here.
        */
        private IEnumerator IsFirstBoot(Action<bool> result)
        {
            var tokenCandidatePaths = _autoyaFilePersistence.FileNamesInDomain(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN);
            var isFirstBoot = tokenCandidatePaths.Length == 0;

            if (!isFirstBoot)
            {
                // load saved data and hold it for after use.
                result(false);
                yield break;
            }

            result(true);
            yield break;
        }

        /**
            send authentication data to server at first boot.
        */
        private IEnumerator OnBootAuthRequest(Action<Dictionary<string, string>, string> setHeaderAndDataToRequest)
        {
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
            if failed to validate response, call bootAuthFailed(int errorCode, string reason).
                this bootAuthFailed method raises the notification against Autoya.Auth_SetOnBootAuthFailed() handler.
        */
        private IEnumerator OnBootAuthResponse(Dictionary<string, string> responseHeader, string data, Action<int, string> bootAuthFailed)
        {
            var isValidResponse = true;
            if (isValidResponse)
            {
                Autoya.Persist_Update(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME, data);
            }
            else
            {
                bootAuthFailed(-1, "failed to boot validation.");
            }
            yield break;
        }

        /**
            check if server response is unauthorized or not.
        */
        private bool IsUnauthorized(int httpCode, Dictionary<string, string> responseHeader)
        {
            return httpCode == AuthSettings.AUTH_HTTP_CODE_UNAUTHORIZED;
        }

        /**
            received Unauthorized code from server. then, should authenticate again.
            set header and data for refresh token.
        */
        private IEnumerator OnTokenRefreshRequest(Action<Dictionary<string, string>, string> setHeaderToRequest)
        {
            // set refresh body data for Http.Post to server.(if empty, this framework use Http.Get for sending data to server.)
            var data = "some refresh data";

            // return refresh token for re-authenticate.
            var refreshToken = Autoya.Persist_Load(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME);

            var base64Str = Base64.FromString(refreshToken);

            var refreshRequestHeader = new Dictionary<string, string> {
                {"Authorization", base64Str}
            };

            setHeaderToRequest(refreshRequestHeader, data);
            yield break;
        }

        /**
            received refreshed token.
            if failed to validate response, call refreshFailed(int errorCode, string reason).
                this refreshFailed method raises the notification against Autoya.Auth_SetOnRefreshAuthFailed() handler.
        */
        private IEnumerator OnTokenRefreshResponse(Dictionary<string, string> responseHeader, string data, Action<int, string> refreshFailed)
        {
            var isValidResponse = true;
            if (isValidResponse)
            {
                Autoya.Persist_Update(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME, data);
            }
            else
            {
                // failsafe here.


                // set result as failure.
                refreshFailed(-1, "failed to refresh token.");
            }

            yield break;
        }

        private IEnumerator OnLogout(Action succeeded, Action<string> failed)
        {
            var result = Autoya.Persist_Delete(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME);
            if (result)
            {
                succeeded();
            }
            else
            {
                failed("failed to delete token.");
            }

            yield break;
        }


        /*
            authorized http request & response handlers.
        */

        /**
            fire when generating http request, via Autoya.Http_X.
            you can add some kind of authorization parameter to request header.
        */
        private Dictionary<string, string> OnHttpRequest(string method, string url, Dictionary<string, string> requestHeader, object data)
        {
            var accessToken = Autoya.Persist_Load(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME);
            requestHeader["Authorization"] = Base64.FromString(accessToken);

            return requestHeader;
        }

        /**
            fire when received http response from server, via Autoya.Http_X.
            you can verify response data & header parameter.

            accepted http code is 200 ~ 299. and these code is already fixed.

            if everything looks good, return true.
                although need to set reason(will not be used.)
                then "succeeded" action of Autoya.Http_X will be raised.

            else, set reason.
                then "failed" action of Autoya.Http_X will be raised with code 200 ~ 299 with the reason which you set.
        */

        // string version.
        private bool OnValidateHttpResponse(string method, string url, Dictionary<string, string> responseHeader, string data, out string reason)
        {
            // let's validate http response if need.
            var isValid = true;
            if (isValid)
            {
                reason = string.Empty;
                return true;
            }
            else
            {
                reason = "run over by a bicycle.";
                return false;
            }
        }

        // byte[] version.
        private bool OnValidateHttpResponse(string method, string url, Dictionary<string, string> responseHeader, byte[] data, out string reason)
        {
            // let's validate http response if need.
            var isValid = true;
            if (isValid)
            {
                reason = string.Empty;
                return true;
            }
            else
            {
                reason = "run over by a bicycle.";
                return false;
            }
        }



        /*
            app version and resource version handlers.
         */

        private string OnAppVersionRequired()
        {
            return Autoya.Manifest_GetBuildManifest().appVersion;
        }

        private string OnResourceVersionRequired()
        {
            var manifest = Autoya.Manifest_LoadRuntimeManifest();
            return string.Join(",", manifest.resourceInfos.Select(info => info.listIdentity + ":" + info.listVersion).ToArray());
        }



        /*
            purchase feature handlers.
        */

        /**
            fire when the server returns product datas for this app.
            these datas should return platform-specific data.

            e,g, if player is iOS, should return iOS item data.
        */
        private ProductInfo[] OnLoadProductsResponse(string responseData)
        {
            /*
                get ProductInfo[] data from this responseData.
                server should return ProductInfos data type.

                consider convert response data to productInfo[].
                e.g.
                    string responseData -> JsonUtility.FromJson<ProductInfos>(responseData) -> productInfos.


                below is reading products data from settings for example.
                responseData is ignored.
            */
            var productInfos = PurchaseSettings.IMMUTABLE_PURCHASE_ITEM_INFOS;
            return productInfos.productInfos;
        }

        /**
            purchase feature is succeeded to load.
        */
        private void OnPurchaseReady()
        {
            // do something if need.
        }

        /**
            called when failed to ready the purchase feature.
            
            offline, server returned error, or failed to ready IAPFeature.

            e,g, show dialog to player. for example "reloading purchase feature... please wait a moment" or other message of error.
                this err parameter includes "player can not available purchase feature".

                see Purchase.PurchaseRouter.PurchaseReadyError enum.

            then, you can retry with Purchase_AttemptReady() method.
            when success, OnPurchaseReady will be called.
        */
        private IEnumerator OnPurchaseReadyFailed(Purchase.PurchaseRouter.PurchaseReadyError err, int code, string reason, AutoyaStatus status)
        {
            // do something if need. 
            yield break;
        }

        /**
            called after purchase started.
            choosedProductId is player choosed product id.
            this method's result will be send(http POST) to your server.

            you can modify request parameter to the expected request data format here.

            request url is defined at PurchaseSettings.cs/PURCHASE_URL_TICKET.
         */
        private string OnTicketRequest(string choosedProductId)
        {
            // by default, choosedProductId will be send as raw string.
            // when you change this like "{"productId":choosedProductId}", Tikcet request will be contains json representation.
            // please modify here if you need.
            return choosedProductId;
        }

        /**
            called when received ticket data for purchasing product via Autoya.Purchase.
            you can modify received ticket data string to desired data.
            returned string will be send to the server for item-deploy information of this purchase.
        */
        private string OnTicketResponse(string ticketData)
        {
            // modify if need.
            return ticketData;
        }

        /**
            called when uncompleted purchase is done in background.

            you can handle the completion of failed purchase in this method.

            server received "paid" information and returned response code 200,
            after that, framework complete uncompleted purchase then fire this method.
         */
        private void onPaidPurchaseDoneInBackground(string backgroundPurchasedProductId)
        {
            // server deployed some products for this player. update player's parameter if need.
        }



        /*
            AssetBundles handlers.
        */

        // assetBundleList store controls.
        private AssetBundleList[] LoadAssetBundleListsFromStorage()
        {
            // load stored assetBundleList then return it.
            var filePaths = Autoya.Persist_FileNamesInDomain(AssetBundlesSettings.ASSETBUNDLES_LIST_STORED_DOMAIN);
            return filePaths.Select(
                path => JsonUtility.FromJson<AssetBundleList>(
                    Autoya.Persist_Load(
                        AssetBundlesSettings.ASSETBUNDLES_LIST_STORED_DOMAIN, Path.GetFileName(path)
                    )
                )
            ).ToArray();
        }
        private bool StoreAssetBundleListToStorage(AssetBundleList list)
        {
            var listStr = JsonUtility.ToJson(list);
            var result = _autoyaFilePersistence.Update(AssetBundlesSettings.ASSETBUNDLES_LIST_STORED_DOMAIN, list.identity, listStr);
            return result;
        }
        private bool DeleteAssetBundleListsFromStorage()
        {
            var result = Autoya.Persist_DeleteByDomain(AssetBundlesSettings.ASSETBUNDLES_LIST_STORED_DOMAIN);
            return result;
        }

        /**
            should return assetBundleList-identity & version pair.

            the source is the value of responseHeader[AuthSettings.AUTH_RESPONSEHEADER_RESVERSION].
         */
        private KeyValuePair<string, string>[] GetListIdentityAndNewVersionDescriptions(string source)
        {
            var listInfosStrs = source.Split(',');

            var identityAndNewVersionPairs = new KeyValuePair<string, string>[listInfosStrs.Length];
            for (var i = 0; i < listInfosStrs.Length; i++)
            {
                var listInfosStr = listInfosStrs[i];
                var identityAndVersion = listInfosStr.Split(':');
                identityAndNewVersionPairs[i] = new KeyValuePair<string, string>(identityAndVersion[0], identityAndVersion[1]);
            }

            return identityAndNewVersionPairs;
        }

        /**
            should return identities of AssetBundleLists from persisted place.
         */
        private string[] LoadAppUsingAssetBundleListIdentities()
        {
            return Autoya.Manifest_LoadRuntimeManifest().resourceInfos.Select(info => info.listIdentity).ToArray();
        }

        /**
            should return the url for downloading assetBundleList.
         */
        private string OnAssetBundleListDownloadUrlRequired(string listIdentity)
        {
            var targetListInfo = Autoya.Manifest_LoadRuntimeManifest().resourceInfos.Where(info => info.listIdentity == listIdentity).FirstOrDefault();
            if (targetListInfo == null)
            {
                throw new Exception("failed to detect bundle info from runtime manifest. requested listIdentity:" + listIdentity + " is not contained in runtime manifest.");
            }

            var url = targetListInfo.listDownloadUrl + "/" + targetListInfo.listIdentity + "/" + AssetBundlesSettings.PLATFORM_STR + "/" + targetListInfo.listVersion + "/";
            return url;
        }

        /**
            should return the url for downloading assetBundle.
         */
        private string OnAssetBundleDownloadUrlRequired(string listIdentity)
        {
            var targetListInfo = Autoya.Manifest_LoadRuntimeManifest().resourceInfos.Where(info => info.listIdentity == listIdentity).FirstOrDefault();
            if (targetListInfo == null)
            {
                throw new Exception("failed to detect bundle info from runtime manifest. requested listIdentity:" + listIdentity + " is not contained in runtime manifest.");
            }

            var url = targetListInfo.listDownloadUrl + "/" + targetListInfo.listIdentity + "/" + AssetBundlesSettings.PLATFORM_STR + "/" + targetListInfo.listVersion + "/";
            return url;
        }

        /**
            fire when the server returned new AssetBundleList version via responseHeader.

            return yes & url: start downloading new assetBundleList from url.
            return no: cancel downloading new assetBundleList.

            fire when you received new assetBundleList version parameter from authenticated http response's response header.

            basepath is from: runtimeManifest. see AutoyaRuntimeManifestObject.cs.
            receivedNewAssetBundleIdentity is from: response header of some http connection.
            rceivedNewAssetBundleListVersion is from : response header of some http connection.
         */
        private Func<string, string, ShouldRequestOrNot> OnRequestNewAssetBundleList = (string receivedNewAssetBundleListIdentity, string rceivedNewAssetBundleListVersion) =>
        {
            var targetListInfo = Autoya.Manifest_LoadRuntimeManifest().resourceInfos.Where(info => info.listIdentity == receivedNewAssetBundleListIdentity).FirstOrDefault();
            if (targetListInfo == null)
            {
                throw new Exception("failed to detect bundle info from runtime manifest. requested listIdentity:" + receivedNewAssetBundleListIdentity + " is not contained in runtime manifest.");
            }

            var url = targetListInfo.listDownloadUrl + receivedNewAssetBundleListIdentity + "/" + rceivedNewAssetBundleListVersion + "/" + receivedNewAssetBundleListIdentity + ".json";

            return ShouldRequestOrNot.Yes(url);
            // return ShouldRequestOrNot.No();
        };

        /**
            fire when you received new assetBundleList.

            condition 
                the condition parameter tells you "current using assets are changed in the new AssetBundleList or not."
                
            return true:
                AssetBundleList will be updated. runtime manifest's resVersion will be changed to new one.
                changed assets will be loaded when you load these assets again.

                current loaded && changed assets is not effected anything.

            return false:
                AssetBundleList will be ignored.
                this means "Postpone updating assetBundleList to latest one." 

                current loaded && changed(in ignored new list) assets is nothing changed.
                internal list state will become "pending update assetBundleList" state.
         */
        private Action<CurrentUsingBundleCondition, Action, Action> ShouldUpdateToNewAssetBundleList = (CurrentUsingBundleCondition condition, Action proceed, Action cancel) =>
        {
            /*
                according to your app's state & value of condition,
                please select true(update assetBundleList) or false(cancel update assetBundleList now).

                e,g, 
                    when your app's state is under battle, and you determine that no need to change asset now,
                    should execute
                        cancel(); 
                    list update will be postponed.


                    otherwise when your app's state is good state to update assets,
                    should execute
                        proceed();

                    for update assetBundleList.

                    app's assetBundleList will be updated to the latest and ready for load/download latest assetBundles.
                    using "Preload" feature on the beginning of app's state will help updating latest assets.
             */
            proceed();
        };

        /**
            fire when update stored AssetBundleList version parameter from old to new version.
         */
        private void OnUpdateToNewAssetBundleList(string updatedAssetBundleListIdentity, string newVersion)
        {
            var runtimeManifest = Autoya.Manifest_LoadRuntimeManifest();
            foreach (var resInfo in runtimeManifest.resourceInfos)
            {
                if (resInfo.listIdentity == updatedAssetBundleListIdentity)
                {
                    resInfo.listVersion = newVersion;
                    break;
                }
            }
            Autoya.Manifest_UpdateRuntimeManifest(runtimeManifest);
        }

        /**
            fire when AssetBundleList is updated to the new received one.
            you can get AssetBundle condition and need to fire ready() when you finish after doing post process.
         */
        private Action<CurrentUsingBundleCondition, Action> OnAssetBundleListUpdated = (condition, ready) =>
        {
            // do something. e,g, Preload updated AssetBundles from updated AssetBundleList.
            // finally, fire ready() for progress.
            ready();
        };

        /**
            fire when failed to store new AssetBundleList to storage.
            just show failed reason and what should be do for success next time.
         */
        private IEnumerator OnNewAssetBundleListStoreFailed(string reason)
        {
            yield break;
        }

        private string[] OnAssetBundleListUrlsRequired()
        {
            return Autoya.Manifest_LoadRuntimeManifest().resourceInfos.Select(info => autoya.OnAssetBundleListDownloadUrlRequired(info.listIdentity) + info.listIdentity + ".json").ToArray();
        }

        /**
            return request headers for getting AssetBundleList.
         */
        private Dictionary<string, string> OnAssetBundleListGetRequest(string url, Dictionary<string, string> requestHeader)
        {
            return requestHeader;
        }

        /**
            return request headers for getting AssetBundlePreloadList.
         */
        private Dictionary<string, string> OnAssetBundlePreloadListGetRequest(string url, Dictionary<string, string> requestHeader)
        {
            return requestHeader;
        }

        /**
            return request headers for getting AssetBundles.
         */
        private Dictionary<string, string> OnAssetBundleGetRequest(string url, Dictionary<string, string> requestHeader)
        {
            return requestHeader;
        }


        /*
            Application version control.
         */

        /**
            do something for server requested client to download latest app from store.
         */
        private Action<string> OnNewAppRequested = newAppVersion =>
        {
            Debug.Log("new app version:" + newAppVersion + " is ready on store!");
        };


        /*
            ApplicationManifest handlers.
        */

        /**
            called when runtimeManifest should overwrite.
            please set the mechanism for store runtime manifest in this device.
         */
        private bool OnOverwriteRuntimeManifest(string data)
        {
            return _autoyaFilePersistence.Update(AppSettings.APP_STORED_RUNTIME_MANIFEST_DOMAIN, AppSettings.APP_STORED_RUNTIME_MANIFEST_FILENAME, data);
        }

        /**
            called when runtimeManifest should load.
            please set the mechanism for load runtime manifest in this device.
         */
        private string OnLoadRuntimeManifest()
        {
            return _autoyaFilePersistence.Load(AppSettings.APP_STORED_RUNTIME_MANIFEST_DOMAIN, AppSettings.APP_STORED_RUNTIME_MANIFEST_FILENAME);
        }

        /**
            called when runtimeManifest should be restore.
         */
        private void OnRestoreRuntimeManifest()
        {
            _autoyaFilePersistence.Delete(AppSettings.APP_STORED_RUNTIME_MANIFEST_DOMAIN, AppSettings.APP_STORED_RUNTIME_MANIFEST_FILENAME);
            _appManifestStore.ReloadFromStorage();
        }
    }
}