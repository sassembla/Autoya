using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework;
using AutoyaFramework.Purchase;
using Miyamasu;
using UnityEngine;

/**
	tests for Autoya Purchase
*/
public class PurchaseRouterTests : MiyamasuTestRunner {
	/**
		Unity 5.5以降対応のpurchaseのテスト。以下のようなことをまるっとやっている。

		{アイテム一覧取得処理}
			・アイテム一覧を取得する。

		{アップデート処理} 
			・起動時処理(勝手に購買処理が完了したりするはず)
			・チケットがない場合の購入完了処理
			・チケットがある場合の購入完了処理

		{購入処理} 
			・事前通信
			・購買処理
			・チケットの保存
			・購買完了通信
			
			・購入成功チケットの削除
			・購入キャンセルチケットの削除
			・購入失敗チケットの処理
		
		非消費アイテム、レストアとかは対応しないぞ。まだ。

		特定のUnityのIAPの ConfigurationBuilder.Instance メソッドが、Playing中でないとProgressしない。そのため、このテストをEditorで走らせることができない。
		ちょっと回避しようがない。
	*/
	private PurchaseRouter router;

	private TestMBRunner runner;
	
	[MSetup] public IEnumerator Setup () {
		var done = false;
		
		// overwrite Autoya instance for test purchase feature.
		yield return WaitPurchaseFeatureOfAutoya(
			() => {
				done = true;
			}
		);
		
		yield return WaitUntil(
			() => done,
			() => {throw new TimeoutException("failed to ready.");}
		);

		if (!done) {
			Fail("Purchase feature test setup is failed to ready.");
			yield break;
		}

		// shutdown purchase feature for get valid result from Unity IAP.
		Autoya.Purchase_Shutdown();
		
		var purchaseRunner = new GameObject("PurchaseTestRunner");
		runner = purchaseRunner.AddComponent<TestMBRunner>();

		router = new PurchaseRouter(
			iEnum => {
				runner.StartCoroutine(iEnum);
			},
			productData => {
				// dummy response.
				return new ProductInfo[] {
					new ProductInfo("100_gold_coins", "100_gold_coins_iOS", true, "one hundled of coins."),
					new ProductInfo("1000_gold_coins", "1000_gold_coins_iOS", true, "one ton of coins.")
				};
			},
			ticketData => ticketData,
			() => {},
			(err, reason, status) => {}
		);
	
		yield return WaitUntil(() => router.IsPurchaseReady(), () => {throw new TimeoutException("failed to ready.");});
	}

	[MTeardown] public void Teardown () {
		GameObject.Destroy(runner.gameObject);
	}

	private IEnumerator WaitPurchaseFeatureOfAutoya (Action done) {
		Autoya.TestEntryPoint(Application.persistentDataPath);
		
		while (!Autoya.Purchase_IsReady()) {
			yield return null;
		}
		done();
	}

	[MTest] public IEnumerator ShowProductInfos () {
		var products = router.ProductInfos();
		True(products.Length == 2, "not match.");
		yield break;
	}


	[MTest] public IEnumerator Purchase () {
		var purchaseId = "dummy purchase Id";
		var productId = "100_gold_coins";

		var purchaseDone = false;
		var purchaseSucceeded = false;
		var failedReason = string.Empty;

		yield return router.PurchaseAsync(
			purchaseId,
			productId,
			pId => {
				purchaseDone = true;
				purchaseSucceeded = true;
			},
			(pId, err, reason, autoyaStatus) => {
				purchaseDone = true;
				failedReason = reason;
			}
		);

		yield return WaitUntil(() => purchaseDone, () => {throw new TimeoutException("failed to purchase async.");});
		True(purchaseSucceeded, "purchase failed. reason:" + failedReason);
	}

	[MTest] public IEnumerator ReloadPurchaseRouter () {
		Debug.LogWarning("自発的にストアのリロードを行いたい。");
		yield break;
	}

	[MTest] public IEnumerator PurchaseCancell () {
		Debug.LogWarning("購入キャンセルのテストがしたい");
		yield break;
	}


	[MTest] public IEnumerator Offline () {
		Debug.LogWarning("多段階時のオフラインのテストがしたい");
		yield break;
	}
	

	private bool forceFailResponse = false;
	private int forceFailCount = 0;
	private void DummyResponsehandlingDelegate (string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed) {
		if (forceFailResponse) {
			forceFailCount++;
			failed(connectionId, httpCode, "expected failure in test.", new AutoyaStatus());
			return;
		}

		if (200 <= httpCode && httpCode < 299) {
			succeeded(connectionId, data);
			return;
		}

		failed(connectionId, httpCode, errorReason, new AutoyaStatus());
	}
	
	[MTest] public IEnumerator RetryPurchaseThenFail () {
		forceFailResponse = false;

		// routerをhandling付きで生成すればいい。
		
		router = new PurchaseRouter(
			iEnum => {
				runner.StartCoroutine(iEnum);
			},
			productData => {
				// dummy response.
				return new ProductInfo[] {
					new ProductInfo("100_gold_coins", "100_gold_coins_iOS", true, "one hundled of coins."),
					new ProductInfo("1000_gold_coins", "1000_gold_coins_iOS", true, "one ton of coins.")
				};
			},
			ticketData => ticketData,
			() => {},
			(err, reason, status) => {},
			null,
			DummyResponsehandlingDelegate
		);
	
		yield return WaitUntil(() => router.IsPurchaseReady(), () => {throw new TimeoutException("failed to ready.");});

		var purchaseId = "dummy purchase Id";
		var productId = "100_gold_coins";

		var purchaseDone = false;
		var failedReason = string.Empty;

		var cor = router.PurchaseAsync(
			purchaseId,
			productId,
			pId => {
				// never success.
			},
			(pId, err, reason, autoyaStatus) => {
				purchaseDone = true;
			}
		);

		while (cor.MoveNext()) {
			yield return null;
		}

		yield return WaitUntil(
			() => {
				var state = router.State();
				if (state == PurchaseRouter.RouterState.Purchasing) {
					// httpが強制的に失敗するようにする。
					forceFailResponse = true;
				}
				return purchaseDone;
			},
			() => {throw new TimeoutException("timeout.");},
			15
		);
	}

	[MTest] public IEnumerator RetryPurchaseThenFinallySuccess () {
		forceFailResponse = false;

		// routerをhandling付きで生成すればいい。
		
		router = new PurchaseRouter(
			iEnum => {
				runner.StartCoroutine(iEnum);
			},
			productData => {
				// dummy response.
				return new ProductInfo[] {
					new ProductInfo("100_gold_coins", "100_gold_coins_iOS", true, "one hundled of coins."),
					new ProductInfo("1000_gold_coins", "1000_gold_coins_iOS", true, "one ton of coins.")
				};
			},
			ticketData => ticketData,
			() => {},
			(err, reason, status) => {},
			null,
			DummyResponsehandlingDelegate
		);
	
		yield return WaitUntil(() => router.IsPurchaseReady(), () => {throw new TimeoutException("failed to ready.");});


		var purchaseId = "dummy purchase Id";
		var productId = "100_gold_coins";

		var purchaseDone = false;
		var failedReason = string.Empty;

		var cor = router.PurchaseAsync(
			purchaseId,
			productId,
			pId => {
				purchaseDone = true;
			},
			(pId, err, reason, autoyaStatus) => {
				// never fail.
			}
		);

		while (cor.MoveNext()) {
			yield return null;
		}

		yield return WaitUntil(
			() => {
				var state = router.State();
				if (state == PurchaseRouter.RouterState.Purchasing) {
					// httpが強制的に失敗するようにする。
					forceFailResponse = true;
				}

				if (forceFailCount == PurchaseSettings.PURCHASED_MAX_RETRY_COUNT - 1) {
					forceFailResponse = false;
				}

				return purchaseDone;
			},
			() => {throw new TimeoutException("timeout.");},
			10
		);
	}

	/*
		意図的にbeforeを出す方法が無いかな〜。
		Listenerを複数作って、ランダムにどのインスタンスかがレスポンスを得る、っていうのは見つけたんだけど、イレギュラーすぎて安定させられる気がしない。
		また状況も結構異なる。
	*/
	
	/*
		このへんどうやって書き直そうかな〜〜
		errorFlowやMonoBehaviourを渡せるようになったんだけど、おかげで切り替えられなくなってテストができない。
	*/

	/**
		force fail initialize of router.
	*/
	// [MTest] public IEnumerator ReloadUnreadyStore () {
	//     if (router == null) {
	//         MarkSkipped();
	//         return;
	//     }

	//     // renew router.
	//     
	//         () => {
	//             router = new PurchaseRouter();
	//         }
	//     );
		
	//     try {
	//         yield return WaitUntil(() => router.IsPurchaseReady(), 2, "failed to ready.");
	//     } catch {
	//         // catch timeout. do nothing.
	//     }

	//     True(!router.IsPurchaseReady(), "not intended.");
		
	//     // すでにnewされているrouterのハンドラを更新しないとダメか、、
	//     router.httpGet = (url, successed, failed) => {
	//         Autoya.Http_Get(url, successed, failed);
	//     };

	//     router.httpPost = (url, data, successed, failed) => {
	//         Autoya.Http_Post(url, data, successed, failed);
	//     };

	//     var ready = false;
	//     router.Reload(
	//         () => {
	//             ready = true;
	//         },
	//         (err, reason) => {}
	//     );

	//     yield return WaitUntil(() => ready, 5, "not get ready.");
	//     True(router.IsPurchaseReady(), "not ready.");
	// }

	// [MTest] public IEnumerator ReloadUnreadyStoreThenPurchase () {
	//     if (router == null) {
	//         MarkSkipped();
	//         return;
	//     }

	//     Action<string, Action<string, string>, Action<string, int, string>> httpGet = (url, successed, failed) => {
	//         // empty http get. will be timeout.
	//     };

	//     Action<string, string, Action<string, string>, Action<string, int, string>> httpPost = (url, data, successed, failed) => {
	//         // empty http post. will be timeout.
	//     };

	//     // renew router.
	//     
	//         () => {
	//             router = new PurchaseRouter(httpGet, httpPost);
	//         }
	//     );
		
	//     try {
	//         yield return WaitUntil(() => router.IsPurchaseReady(), 2, "failed to ready.");
	//     } catch {
	//         // catch timeout. do nothing.
	//     }

	//     True(!router.IsPurchaseReady(), "not intended.");
		
	//     // すでにnewされているrouterのハンドラを更新しないとダメか、、
	//     router.httpGet = (url, successed, failed) => {
	//         Autoya.Http_Get(url, successed, failed);
	//     };

	//     router.httpPost = (url, data, successed, failed) => {
	//         Autoya.Http_Post(url, data, successed, failed);
	//     };

	//     var ready = false;
	//     router.Reload(
	//         () => {
	//             ready = true;
	//         },
	//         (err, reason) => {}
	//     );

	//     yield return WaitUntil(() => ready, 5, "not get ready.");
	//     True(router.IsPurchaseReady(), "not ready.");

	//     var purchaseId = "dummy purchase Id";
	//     var productId = "100_gold_coins";

	//     var purchaseDone = false;
	//     var purchaseSucceeded = false;
	//     var failedReason = string.Empty;

	//     
	//         () => {
	//             router.PurchaseAsync(
	//                 purchaseId,
	//                 productId,
	//                 pId => {
	//                     purchaseDone = true;
	//                     purchaseSucceeded = true;
	//                 },
	//                 (pId, err, reason) => {
	//                     purchaseDone = true;
	//                     failedReason = reason;
	//                 }
	//             );
	//         }
	//     );

	//     yield return WaitUntil(() => purchaseDone, 10, "failed to purchase async.");
	//     True(purchaseSucceeded, "purchase failed. reason:" + failedReason);
	// }
}
