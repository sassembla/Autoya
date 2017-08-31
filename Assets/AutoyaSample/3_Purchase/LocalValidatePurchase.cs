using System;
using System.Collections;
using AutoyaFramework;
using AutoyaFramework.Purchase;
using UnityEngine;

/**
	Purchase example of local validate based purchase.
*/
public class LocalValidatePurchase : MonoBehaviour {
	
	void Start () {
		// set all products in app directly.
		var productInfos = new ProductInfo[] {
			new ProductInfo("100_gold_coins", "100_gold_coins_iOS", true, "one hundled of coins."),
			new ProductInfo("1000_gold_coins", "1000_gold_coins_iOS", true, "one ton of coins."),
			new ProductInfo("10000_gold_coins", "10000_gold_coins_iOS", false, "ten tons of coins."),// this product setting is example of not allow to buy for this player, disable to buy but need to be displayed.
		};

		foreach (var product in productInfos) {
			Debug.Log("productId:" + product.productId + " info:" + product.info + " avaliable:" + product.isAvailableToThisPlayer);
		}
		
		var localPurchaseRouter = new LocalPurchaseRouter(
			productInfos, 
			() => {
				Debug.Log("ready purchase.");
			}, 
			(err, reason, status) => {
				Debug.LogError("failed to ready purchase. error:" + err + " reason:" + reason);
			}, 
			alreadyPurchasedProductId => {
				/*
					this action will be called when the IAP feature found non-completed purchase record &&
					the validate result of that is OK.

					need to deploy product to user.
				 */
				// deploy purchased product to user here.
			}
		);

		// it's convenient to set purchase id for each purchase. because purchase feature is async.
		var purchaseId = "myPurchaseId_" + Guid.NewGuid().ToString();
		
		localPurchaseRouter.Purchase(
			purchaseId, 
			"100_gold_coins", 
			purchasedId => {
				Debug.Log("purchase succeeded, purchasedId:" + purchasedId + " purchased item id:" + "100_gold_coins");
				// deploy purchased product to user here.
			}, 
			(purchasedId, error, reason, status) => {
				Debug.LogError("failed to purchase Id:" + purchasedId + " failed, error:" + error + " reason:" + reason);
			}
		);
	}
	
}
