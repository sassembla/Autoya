using System;
using System.Collections;
using AutoyaFramework.Purchase;
using UnityEngine;

/**
    まだAutoya本体とは独立した状態の課金実装。というかこのまま使ってもほぼ問題ない感じ。
    Autoyaからラップされるのは、その独自のRequestHeadersとか、ErrorHandleFlow部分で、これはまあ理想的な感じ。
*/
public class Purchase : MonoBehaviour {
	
    private PurchaseRouter purchaseRouter;
    
    
    void Awake () {
        purchaseRouter = new PurchaseRouter(
            iEnum => {
                StartCoroutine(iEnum);
            },
            () => {
                    Debug.Log("ready for purchase.");
            },
            (err, reason, autoyaStatus) => {
                Debug.LogError("failed to ready purchaseRouter. err:" + err + " reason:" + reason + " autoyaStatus:" + autoyaStatus);
            }
        );
    }

	// Use this for initialization
	IEnumerator Start () {
        /*
            this code can wait until purchaseRouter is ready.
        */
        while (!purchaseRouter.IsPurchaseReady()) {
            yield return null;
        }

        /*
            let's purchase.
        */

        // it's convenient for taking purchase id for each purchase. because purchase feature is async.
        var purchaseId = "myPurchaseId_" + Guid.NewGuid().ToString();
        
        var purchaseCoroutine = purchaseRouter.PurchaseAsync(
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

        StartCoroutine(purchaseCoroutine);
	}
	
}
