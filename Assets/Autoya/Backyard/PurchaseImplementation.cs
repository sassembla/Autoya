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

            // このへんに綺麗なハングしないチェックが欲しい(Ienumの中身を変えない形)

            autoya.mainthreadDispatcher.Commit(autoya._Purchase(purchaseId, productId, done, failed));
        }

        private IEnumerator _Purchase (string purchaseId, string productId, Action<string> done, Action<string, PurchaseRouter.PurchaseError, string, AutoyaStatus> failed) {
            var purchaseCoroutine = _purchaseRouter.PurchaseAsync(purchaseId, productId, done, failed);
            while (purchaseCoroutine.MoveNext()) {
                yield return null;
            }
        }
    }
}