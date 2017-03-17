// using System;
// using System.Collections;
// using AutoyaFramework;
// using AutoyaFramework.Purchase;
// using Miyamasu;
// using UnityEngine;

// /**
// 	tests for Autoya Purchase
// */
// public class PurchaseRouterTests : MiyamasuTestRunner {
// 	/**
// 		Unity 5.5対応のpurchaseのテスト。以下のようなことをまるっとやっている。

// 		{アイテム一覧取得処理}
// 			・アイテム一覧を取得する。

// 		{アップデート処理} 
// 			・起動時処理(勝手に購買処理が完了したりするはず)
// 			・チケットがない場合の購入完了処理
// 			・チケットがある場合の購入完了処理

// 		{購入処理} 
// 			・事前通信
// 			・購買処理
// 			・チケットの保存
// 			・購買完了通信
			
// 			・購入成功チケットの削除
// 			・購入キャンセルチケットの削除
// 			・購入失敗チケットの処理
		
// 		非消費アイテム、レストアとかは対応しないぞ。まだ。

// 		特定のUnityのIAPの ConfigurationBuilder.Instance メソッドが、Playing中でないとProgressしない。そのため、このテストをEditorで走らせることができない。
// 		ちょっと回避しようがない。
// 	*/
// 	private PurchaseRouter router;
	
// 	[MSetup] public void Setup () {
// 		if (!IsTestRunningInPlayingMode()) {
// 			SkipCurrentTest("Purchase feature should run on MainThread.");
// 		};

// 		var done = false;
		
// 		// overwrite Autoya instance for test purchase feature.
// 		RunEnumeratorOnMainThread(
// 			WaitPurchaseFeatureOfAutoya(
// 				() => {
// 					done = true;
// 				}
// 			),
// 			false
// 		);

// 		WaitUntil(
// 			() => done,
// 			5,
// 			"failed to ready."
// 		);

// 		if (!done) {
// 			SkipCurrentTest("Purchase feature test setup is failed to ready.");
// 			return;
// 		}

// 		// shutdown purchase feature for get valid result from Unity IAP.
// 		Autoya.Purchase_Shutdown();
		
// 		RunOnMainThread(
// 			() => {
// 				router = new PurchaseRouter(
// 					iEnum => {
// 						RunEnumeratorOnMainThread(iEnum, false);
// 					},
// 					productData => {
// 						// dummy response.
// 						return new ProductInfo[] {
// 							new ProductInfo("100_gold_coins", "100_gold_coins_iOS", true, "one hundled of coins."),
// 							new ProductInfo("1000_gold_coins", "1000_gold_coins_iOS", true, "one ton of coins.")
// 						};
// 					},
// 					ticketData => ticketData,
// 					() => {},
// 					(err, reason, status) => {}
// 				);
// 			}
// 		);
		
// 		WaitUntil(() => router.IsPurchaseReady(), 5, "failed to ready.");
// 	}

// 	private IEnumerator WaitPurchaseFeatureOfAutoya (Action done) {
// 		Autoya.TestEntryPoint(Application.persistentDataPath);
		
// 		while (!Autoya.Purchase_IsReady()) {
// 			yield return null;
// 		}
// 		done();
// 	}

// 	[MTest] public void ShowProductInfos () {
// 		var products = router.ProductInfos();
// 		Assert(products.Length == 2, "not match.");
// 	}


// 	[MTest] public void Purchase () {
// 		var purchaseId = "dummy purchase Id";
// 		var productId = "100_gold_coins";

// 		var purchaseDone = false;
// 		var purchaseSucceeded = false;
// 		var failedReason = string.Empty;

// 		RunEnumeratorOnMainThread(
// 			router.PurchaseAsync(
// 				purchaseId,
// 				productId,
// 				pId => {
// 					purchaseDone = true;
// 					purchaseSucceeded = true;
// 				},
// 				(pId, err, reason, autoyaStatus) => {
// 					purchaseDone = true;
// 					failedReason = reason;
// 				}
// 			)
// 		);

// 		WaitUntil(() => purchaseDone, 10, "failed to purchase async.");
// 		Assert(purchaseSucceeded, "purchase failed. reason:" + failedReason);
// 	}

// 	[MTest] public void PurchaseCancell () {
// 		Debug.LogWarning("購入キャンセルのテストがしたい");
// 	}


// 	[MTest] public void Offline () {
// 		Debug.LogWarning("多段階時のオフラインのテストがしたい");
// 	}
	
// 	/*
// 		意図的にbeforeを出す方法が無いかな〜。
// 		Listenerを複数作って、ランダムにどのインスタンスかがレスポンスを得る、っていうのは見つけたんだけど、イレギュラーすぎて安定させられる気がしない。
// 		また状況も結構異なる。
// 	*/
	
// 	/*
// 		このへんどうやって書き直そうかな〜〜
// 		errorFlowやMonoBehaviourを渡せるようになったんだけど、おかげで切り替えられなくなってテストができない。
// 	*/

// 	/**
// 		force fail initialize of router.
// 	*/
// 	// [MTest] public void ReloadUnreadyStore () {
// 	//     if (router == null) {
// 	//         MarkSkipped();
// 	//         return;
// 	//     }

// 	//     // renew router.
// 	//     RunOnMainThread(
// 	//         () => {
// 	//             router = new PurchaseRouter();
// 	//         }
// 	//     );
		
// 	//     try {
// 	//         WaitUntil(() => router.IsPurchaseReady(), 2, "failed to ready.");
// 	//     } catch {
// 	//         // catch timeout. do nothing.
// 	//     }

// 	//     Assert(!router.IsPurchaseReady(), "not intended.");
		
// 	//     // すでにnewされているrouterのハンドラを更新しないとダメか、、
// 	//     router.httpGet = (url, successed, failed) => {
// 	//         Autoya.Http_Get(url, successed, failed);
// 	//     };

// 	//     router.httpPost = (url, data, successed, failed) => {
// 	//         Autoya.Http_Post(url, data, successed, failed);
// 	//     };

// 	//     var ready = false;
// 	//     router.Reload(
// 	//         () => {
// 	//             ready = true;
// 	//         },
// 	//         (err, reason) => {}
// 	//     );

// 	//     WaitUntil(() => ready, 5, "not get ready.");
// 	//     Assert(router.IsPurchaseReady(), "not ready.");
// 	// }

// 	// [MTest] public void ReloadUnreadyStoreThenPurchase () {
// 	//     if (router == null) {
// 	//         MarkSkipped();
// 	//         return;
// 	//     }

// 	//     Action<string, Action<string, string>, Action<string, int, string>> httpGet = (url, successed, failed) => {
// 	//         // empty http get. will be timeout.
// 	//     };

// 	//     Action<string, string, Action<string, string>, Action<string, int, string>> httpPost = (url, data, successed, failed) => {
// 	//         // empty http post. will be timeout.
// 	//     };

// 	//     // renew router.
// 	//     RunOnMainThread(
// 	//         () => {
// 	//             router = new PurchaseRouter(httpGet, httpPost);
// 	//         }
// 	//     );
		
// 	//     try {
// 	//         WaitUntil(() => router.IsPurchaseReady(), 2, "failed to ready.");
// 	//     } catch {
// 	//         // catch timeout. do nothing.
// 	//     }

// 	//     Assert(!router.IsPurchaseReady(), "not intended.");
		
// 	//     // すでにnewされているrouterのハンドラを更新しないとダメか、、
// 	//     router.httpGet = (url, successed, failed) => {
// 	//         Autoya.Http_Get(url, successed, failed);
// 	//     };

// 	//     router.httpPost = (url, data, successed, failed) => {
// 	//         Autoya.Http_Post(url, data, successed, failed);
// 	//     };

// 	//     var ready = false;
// 	//     router.Reload(
// 	//         () => {
// 	//             ready = true;
// 	//         },
// 	//         (err, reason) => {}
// 	//     );

// 	//     WaitUntil(() => ready, 5, "not get ready.");
// 	//     Assert(router.IsPurchaseReady(), "not ready.");

// 	//     var purchaseId = "dummy purchase Id";
// 	//     var productId = "100_gold_coins";

// 	//     var purchaseDone = false;
// 	//     var purchaseSucceeded = false;
// 	//     var failedReason = string.Empty;

// 	//     RunOnMainThread(
// 	//         () => {
// 	//             router.PurchaseAsync(
// 	//                 purchaseId,
// 	//                 productId,
// 	//                 pId => {
// 	//                     purchaseDone = true;
// 	//                     purchaseSucceeded = true;
// 	//                 },
// 	//                 (pId, err, reason) => {
// 	//                     purchaseDone = true;
// 	//                     failedReason = reason;
// 	//                 }
// 	//             );
// 	//         }
// 	//     );

// 	//     WaitUntil(() => purchaseDone, 10, "failed to purchase async.");
// 	//     Assert(purchaseSucceeded, "purchase failed. reason:" + failedReason);
// 	// }
// }
