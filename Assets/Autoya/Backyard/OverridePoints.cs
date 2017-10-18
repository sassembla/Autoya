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

/**
	modify this class for your app's authentication, purchase, assetBundles, appManifest dataflow.
*/
namespace AutoyaFramework {

	public partial class Autoya {
		
		/*
			maintenance handlers.
		 */

		/**
			return if server is under maintenance or not.
		*/
		private bool IsUnderMaintenance (int httpCode, Dictionary<string, string> responseHeader) {
			return httpCode == BackyardSettings.MAINTENANCE_CODE;
		}


		/*
			authentication handlers.
		 */

		/**
			return true if already authenticated, return false if not.
			you can load your authenticated data (kind of Token) here.
		*/
		private bool IsFirstBoot () {
			var tokenCandidatePaths = _autoyaFilePersistence.FileNamesInDomain(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN);
			var isFirstBoot = tokenCandidatePaths.Length == 0;
			if (!isFirstBoot) {
				// load saved data and hold it for after use.
				return false;
			}
			return true;
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
			if failed to validate response, call bootAuthFailed(int errorCode, string reason).
				this bootAuthFailed method raises the notification against Autoya.Auth_SetOnBootAuthFailed() handler.
		*/
		private IEnumerator OnBootAuthResponse (Dictionary<string, string> responseHeader, string data, Action<int, string> bootAuthFailed) {
			var isValidResponse = true;
			if (isValidResponse) {
				Autoya.Persist_Update(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME, data);
			} else {
				bootAuthFailed(-1, "failed to boot validation.");
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
			received Unauthorized code from server. then, should authenticate again.
			set header and data for refresh token.
		*/
		private IEnumerator OnTokenRefreshRequest (Action<Dictionary<string, string>, string> setHeaderToRequest) {
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
		private IEnumerator OnTokenRefreshResponse (Dictionary<string, string> responseHeader, string data, Action<int, string> refreshFailed) {
			var isValidResponse = true;
			if (isValidResponse) {
				Autoya.Persist_Update(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN, AuthSettings.AUTH_STORED_TOKEN_FILENAME, data);
			} else {
				// failsafe here.


				// set result as failure.
				refreshFailed(-1, "failed to refresh token.");
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
		private Dictionary<string, string> OnHttpRequest (string method, string url, Dictionary<string, string> requestHeader, string data) {
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
		private bool OnValidateHttpResponse (string method, string url, Dictionary<string, string> responseHeader, string data, out string reason) {
			// let's validate http response if need.
			var isValid = true;
			if (isValid) {
				reason = string.Empty;
				return true;
			} else {
				reason = "run over by a bicycle.";
				return false;
			}
		}

		// byte[] version.
		private bool OnValidateHttpResponse (string method, string url, Dictionary<string, string> responseHeader, byte[] data, out string reason) {
			// let's validate http response if need.
			var isValid = true;
			if (isValid) {
				reason = string.Empty;
				return true;
			} else {
				reason = "run over by a bicycle.";
				return false;
			}
		}



		/*
			purchase feature handlers.
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
		private void OnPurchaseReady () {
			// do something if need.
		}

		/**
			fire when failed to ready the purchase feature.
			
			offline, server returned error, or failed to ready IAPFeature.

			e,g, show dialog to player. for example "reloading purchase feature... please wait a moment" or other message of error.
				this err parameter includes "player can not available purchase feature".

				see Purchase.PurchaseRouter.PurchaseReadyError enum.

			then, you can retry with Purchase_AttemptReady() method.
			when success, OnPurchaseReady will be called.
		*/
		private IEnumerator OnPurchaseReadyFailed (Purchase.PurchaseRouter.PurchaseReadyError err, int code, string reason, AutoyaStatus status) {
			// do something if need. 
			yield break;
		}

		/**
			received ticket data for purchasing product via Autoya.Purchase.
			you can modify received ticket data string to desired data.
			returned string will be send to the server for item-deploy information of this purchase.
		*/
		private string OnTicketResponse (string ticketData) {
			// modify if need.
			return ticketData;
		}



		/*
			AssetBundles handlers.
		*/

		private string AssetBundleListDownloadUrl () {
			var targetListVersion = Autoya.Manifest_LoadRuntimeManifest().resVersion;
			if (string.IsNullOrEmpty(targetListVersion)) {
				return string.Empty;
			}
			
			return AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST + targetListVersion + "/AssetBundles.StandaloneOSXIntel64_" + targetListVersion.Replace(".", "_") + ".json";
		}

		private AssetBundleList LoadAssetBundleListFromStorage () {
			// load stored assetBundleList then return it.
			var listStr = _autoyaFilePersistence.Load(AssetBundlesSettings.ASSETBUNDLES_LIST_STORED_DOMAIN, AssetBundlesSettings.ASSETBUNDLES_LIST_FILENAME);
			if (string.IsNullOrEmpty(listStr)) {
				return new AssetBundleList();
			}
			
			return JsonUtility.FromJson<AssetBundleList>(listStr);
		}
		private bool StoreAssetBundleListToStorage (AssetBundleList list) {
			var listStr = JsonUtility.ToJson(list);
			var result = _autoyaFilePersistence.Update(AssetBundlesSettings.ASSETBUNDLES_LIST_STORED_DOMAIN, AssetBundlesSettings.ASSETBUNDLES_LIST_FILENAME, listStr);
			return result;
		}
		private bool DeleteAssetBundleListFromStorage () {
			var result = _autoyaFilePersistence.Delete(AssetBundlesSettings.ASSETBUNDLES_LIST_STORED_DOMAIN, AssetBundlesSettings.ASSETBUNDLES_LIST_FILENAME);
			return result;
		}

		/**
			fire when you received new assetBundleList version parameter from authenticated http response's response header.
		 */
		private Func<string, ShouldRequestOrNot> OnRequestNewAssetBundleList = (string rceivedNewAssetBundleListVersion) => {
			var url = AssetBundlesSettings.ASSETBUNDLES_URL_DOWNLOAD_ASSETBUNDLELIST + rceivedNewAssetBundleListVersion + "/AssetBundles.StandaloneOSXIntel64_" + rceivedNewAssetBundleListVersion.Replace(".", "_") + ".json";

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
		private Func<CurrentUsingBundleCondition, bool> ShouldUpdateToNewAssetBundleList = (CurrentUsingBundleCondition condition) => {
			/*
				according to your app's state & value of condition,
				please select true(update assetBundleList) or false(cancel update assetBundleList now).

				e,g, 
					when your app's state is under battle, and you determine that no need to change asset now,
					should 
						retrun false. 
					list update will be postponed.


					otherwise when your app's state is good state to update assets,
					should 
						retrun true
					for update assetBundleList.
					app's assetBundleList will be updated to the latest and ready for load/download latest assetBundles.
					using "Preload" feature on the beginning of app's state will help updating latest assets.
			 */
			return true;
		};

		/**
			return request headers for getting AssetBundleList.
		 */
		private Dictionary<string, string> OnAssetBundleListGetRequest (string url, Dictionary<string, string> requestHeader) {
			return requestHeader;
		}

		/**
			return request headers for getting AssetBundlePreloadList.
		 */
		private Dictionary<string, string> OnAssetBundlePreloadListGetRequest (string url, Dictionary<string, string> requestHeader) {
			return requestHeader;
		}
		
		/**
			return request headers for getting AssetBundles.
		 */
		private Dictionary<string, string> OnAssetBundleGetRequest (string url, Dictionary<string, string> requestHeader) {
			return requestHeader;
		}


		/*
			Application version control.
		 */

		/**
			do something for server requested client to download latest app from store.
		 */
		private Action<string> OnNewAppRequested = newAppVersion => {
			Debug.Log("new app version:" + newAppVersion + " is ready on store!");
		};


		/*
			ApplicationManifest handlers.
		*/

		/**
			called when runtimeManifest should overwrite.
			please set the mechanism for store runtime manifest in this device.
		 */
        private bool OnOverwriteRuntimeManifest (string data) {
            return _autoyaFilePersistence.Update(AppSettings.APP_STORED_RUNTIME_MANIFEST_DOMAIN, AppSettings.APP_STORED_RUNTIME_MANIFEST_FILENAME, data);
        }

		/**
			called when runtimeManifest should load.
			please set the mechanism for load runtime manifest in this device.
		 */
        private string OnLoadRuntimeManifest () {
            return _autoyaFilePersistence.Load(AppSettings.APP_STORED_RUNTIME_MANIFEST_DOMAIN, AppSettings.APP_STORED_RUNTIME_MANIFEST_FILENAME);
        }
	}
}