using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.Purchase;
using UnityEngine;

namespace AutoyaFramework {
	public partial class Autoya {
        /*
            Purchase implementation.
        */
        private PurchaseRouter _purchaseRouter;
        private bool isPurchaseReady;
        
        public static bool Purchase_IsReady () {
            if (autoya == null) {
                return false;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
                return false;
			}

            return autoya._purchaseRouter.IsPurchaseReady();
        }
        
        public static void Purchase (string purchaseId,string productId, Action<string> done, Action<string, PurchaseRouter.PurchaseError, string, AutoyaStatus> failed) {
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

            if (!autoya.isPurchaseReady) {
				return;
			}

            autoya.mainthreadDispatcher.Commit(autoya._purchaseRouter.PurchaseAsync(purchaseId, productId, done, failed));
        }
    }
}