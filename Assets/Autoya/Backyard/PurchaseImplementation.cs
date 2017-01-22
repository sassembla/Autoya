using System;
using System.Collections;
using AutoyaFramework.Purchase;
using UnityEngine;

namespace AutoyaFramework {
    public enum PurchaseFeatureState {
        None,
        Loading,
        Ready,
        Reloading,
        Closed
    }
    
	public partial class Autoya {
        /*
            Purchase implementation.
        */
        private PurchaseRouter _purchaseRouter;
        
        private PurchaseFeatureState purchaseFeatureState = PurchaseFeatureState.None;
        
        private void ReloadPurchasability () {
            purchaseFeatureState = PurchaseFeatureState.Loading;

            _purchaseRouter = new PurchaseRouter(
                mainthreadDispatcher.Commit,
                productSourceData => {
                    /*
                        handle received product datas to OverridePoint.
                    */
                    return OnLoadProductsResponse(productSourceData);
                },
                ticketData => {
                    return OnTicketResponse(ticketData);
                },
                () => {
                    purchaseFeatureState = PurchaseFeatureState.Ready;
                    OnPurchaseReady();
                }, 
                (err, reason, autoyaStatus) => {
                    purchaseFeatureState = PurchaseFeatureState.Reloading;
                    OnPurchaseReadyFailed(err, reason, autoyaStatus);
                    
                    // start reloading.
                    mainthreadDispatcher.Commit(ReloadPurchaseFeature());
                },
                httpRequestHeaderDelegate, 
                httpResponseHandlingDelegate
            );
        }

        private IEnumerator ReloadPurchaseFeature () {
            ReloadPurchasability();

            while (_purchaseRouter.IsPurchaseReady()) {
                yield return null;
            }
        }


        /*
            public apis.
        */

        public static PurchaseFeatureState Purchase_State () {
            return autoya.purchaseFeatureState;
        }
        
        public static bool Purchase_IsReady () {
            if (autoya == null) {
                return false;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
                return false;
			}

            if (autoya.purchaseFeatureState != PurchaseFeatureState.Ready) {
                return false;
            }
            
            if (!autoya._purchaseRouter.IsPurchaseReady()) {
                return false;
            }

            return true;
        }

        public static ProductInfo[] Purchase_ProductInfos () {
            if (autoya == null) {
                return new ProductInfo[]{};
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
                return new ProductInfo[]{};
			}

            return autoya._purchaseRouter.ProductInfos();
        }

        
        
        /**
            purchase item in asynchronously.
            
            string purchaseId: you can set id for this purchase. this param will back in done or failed handler.
            string productId: platform-shard product id string.
            Action<string> done: fire when purchase is done in succeessful. string is purchaseId.
            Action<string, PurchaseRouter.PurchaseError, string, AutoyaStatus> failed: fire when purchase is failed. 1st string is purchaseId.
        */
        public static void Purchase (string purchaseId, string productId, Action<string> done, Action<string, PurchaseRouter.PurchaseError, string, AutoyaStatus> failed) {
            if (autoya == null) {
                Debug.LogWarning("not yet. 1");
				// var cor = new AssetBundleLoadErrorInstance(assetName, "Autoya is null.", loadFailed).Coroutine();
				// autoya.mainthreadDispatcher.Commit(cor);
				return;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
                Debug.LogWarning("not yet. 2");
				// var cor = new AssetBundleLoadErrorInstance(assetName, "not authenticated.", loadFailed).Coroutine();
				// autoya.mainthreadDispatcher.Commit(cor);				
				return;
			}

            if (autoya.purchaseFeatureState != PurchaseFeatureState.Ready) {
                Debug.LogWarning("not yet. 3");
                return;
            }

            var purchasability = autoya._purchaseRouter.IsPurchaseReady();
            if (!purchasability) {
                Debug.LogWarning("not yet. 4");
                return;
            }

            autoya.mainthreadDispatcher.Commit(autoya._purchaseRouter.PurchaseAsync(purchaseId, productId, done, failed));
        }

        /**
            do not use this method in actual use.
            this method is only for testing.
        */
        public static void Purchase_Shutdown () {
            if (autoya == null) {
                return;
			}

            autoya.purchaseFeatureState = PurchaseFeatureState.Closed;
            autoya._purchaseRouter = null;
        }
    }
}