// using System;
// using System.Collections;
// using System.Collections.Generic;
// using AutoyaFramework.Purchase;
// using UnityEngine;

// namespace AutoyaFramework {
// 	public partial class Autoya {
//         /*
//             Loader
//         */
//         private PurchaseRouter _purchaseRouter;
        
//         public static void Purchase_Start (string purchaseId,string productId, Action<string> done, Action<string, PurchaseRouter.PurchaseError, string, AutoyaStatus> failed) {
//             autoya.mainthreadDispatcher.Commit(Purchase(purchaseId, productId, done, failed));
//         }

//         private static IEnumerator Purchase (string purchaseId, string productId, Action<string> done, Action<string, PurchaseRouter.PurchaseError, string, AutoyaStatus> failed) {
//             if (autoya._purchaseRouter == null) {
//                 var purchaseReadyFailed = false;
//                 autoya._purchaseRouter = new PurchaseRouter(
//                     iEnum => {
//                         autoya.mainthreadDispatcher.Commit(iEnum);
//                     },
//                     () => {
//                         Debug.Log("準備できた。FWが知れればそれでいいんで、このブロック全体が別のとこに書きそう。");
//                     }, 
//                     (err, reason, autoyaStatus) => {
//                         purchaseReadyFailed = true;
//                     },
//                     autoya.httpRequestHeaderDelegate, 
//                     autoya.httpResponseHandlingDelegate
//                 );

//                 var cor = autoya._purchaseRouter.Ready(
                    
//                 );

//                 while (cor.MoveNext()) {
//                     yield return null;
//                 }

//                 if (purchaseReadyFailed) {
//                     Debug.LogError("課金機構の準備に失敗した。通信なし状態とかのリトライは内部でやられてるので、うーんっていう。");
//                     yield break;
//                 }
//             }

//             var purchaseCoroutine = autoya._purchaseRouter.PurchaseAsync(purchaseId, productId, done, failed);
//             while (purchaseCoroutine.MoveNext()) {
//                 yield return null;
//             }
//         }
//     }
// }