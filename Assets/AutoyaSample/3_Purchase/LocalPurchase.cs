using System;
using System.Collections;
using AutoyaFramework;
using AutoyaFramework.Purchase;
using UnityEngine;

/**
	Purchase example of local validate based purchase.
*/
public class LocalPurchase : MonoBehaviour {
	
	private LocalPurchaseRouter localPurchaseRouter;

	void Start () {
		localPurchaseRouter = new LocalPurchaseRouter(
			PurchaseSettings.IMMUTABLE_PURCHASE_ITEM_INFOS.productInfos,
			() => {
				Debug.Log("ready purchase.");
				purchasable = true;
			}, 
			(err, reason) => {
				Debug.LogError("failed to ready purchase. error:" + err + " reason:" + reason);
			}, 
			alreadyPurchasedProductId => {
				/*
					this action will be called when 
						the IAP feature found non-completed purchase record
							&&
						the validate result of that is OK.

					need to deploy product to user.
				 */
				
				// deploy purchased product to user here.
			}
		);
	}

	bool purchasable = false;
	bool purchasing = false;

	void Update () {
		if (purchasable && !purchasing) {
			purchasing = true;

			// display all products.
			var products = localPurchaseRouter.ProductInfos();
			foreach (var product in products) {
				Debug.Log("productId:" + product.productId + " info:" + product.info + " avaliable:" + product.isAvailableToThisPlayer);
			}

			// it's convenient to set purchase id for each purchase. because purchase feature is async.
			var purchaseId = "myPurchaseId_" + Guid.NewGuid().ToString();
			
			localPurchaseRouter.PurchaseAsync(
				purchaseId, 
				"100_gold_coins", 
				purchasedId => {
					Debug.Log("purchase succeeded, purchasedId:" + purchasedId + " purchased item id:" + "100_gold_coins");
					// deploy purchased product to user here.
				}, 
				(purchasedId, error, reason) => {
					Debug.LogError("failed to purchase Id:" + purchasedId + " failed, error:" + error + " reason:" + reason);
				}
			);
		}
		
	}
	
}
