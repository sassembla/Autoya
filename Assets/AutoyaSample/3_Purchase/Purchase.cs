using System;
using System.Collections;
using AutoyaFramework;
using UnityEngine;

/**
	Purchase example.
*/
public class Purchase : MonoBehaviour {
	
	IEnumerator Start () {
		while (!Autoya.Purchase_IsReady()) {
			yield return null;
		}

		// display all products.
		var products = Autoya.Purchase_ProductInfos();
		foreach (var product in products) {
			Debug.Log("productId:" + product.productId + " info:" + product.info + " avaliable:" + product.isAvailableToThisPlayer);
		}
		

		// it's convenient for taking purchase id for each purchase. because purchase feature is async.
		var purchaseId = "myPurchaseId_" + Guid.NewGuid().ToString();
		
		Autoya.Purchase(
			purchaseId, 
			"100_gold_coins", 
			pId => {
				Debug.Log("succeeded to purchase. id:" + pId);
			}, 
			(pId, err, reason, autoyaStatus) => {
				if (autoyaStatus.isAuthFailed) {
					Debug.LogError("failed to auth.");
					return;
				} 
				if (autoyaStatus.inMaintenance) {
					Debug.LogError("failed, service is under maintenance.");
					return;
				}
				Debug.LogError("failed to purchase, id:" + pId + " err:" + err + " reason:" + reason);
			}
		);
	}
	
}
