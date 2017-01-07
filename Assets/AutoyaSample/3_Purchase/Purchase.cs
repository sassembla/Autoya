using System;
using System.Collections;
using AutoyaFramework.Purchase;
using UnityEngine;

/**
    まだAutoya本体とは独立した状態の課金実装。というかこのまま使ってもほぼ問題ない感じ。
    Autoyaからラップされるのは、その独自のRequestHeadersとか、ErrorHandleFlow部分で、これはまあ理想的な感じ。
*/
public class Purchase : MonoBehaviour {
	
    private PurchaseRouter purshaceRouter;
    
    
    void Awake () {
        purshaceRouter = new PurchaseRouter(
            () => {
                Debug.Log("ready for purchase.");
            },
            (err, reason) => {
                Debug.LogError("failed to ready purchaseRouter. err:" + err + " reason:" + reason);
            },
            iEnum => StartCoroutine(iEnum)
        );
    }

	// Use this for initialization
	IEnumerator Start () {
        /*
            this code can wait until purshaceRouter is ready.
        */
        while (!purshaceRouter.IsPurchaseReady()) {
            yield return null;
        }

        /*
            let's purchase.
        */

        // it's convenient for taking purchase id for each purchase. because purchase feature is async.
        var purchaseId = "myPurchaseId_" + Guid.NewGuid().ToString();
        purshaceRouter.PurchaseAsync(
            purchaseId, 
            "100_gold_coins", 
            pId => {
                Debug.Log("succeeded to purchase. id:" + pId);
            },
            (pId, err, reason) => {
                Debug.LogError("failed to purchase, id:" + pId + " err:" + err + " reason:" + reason);
            }
        );
	}
	
}
