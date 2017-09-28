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

		True(router.State() == PurchaseRouter.RouterState.RetryFailed);
		
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

	[MTest] public IEnumerator RetryPurchaseThenFailThenWait () {
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
			ticketData => Guid.NewGuid().ToString(),
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
		Debug.Log("リトライが3回失敗して良い感じになった。" + router.State());
		router.Show();


		forceFailResponse = false;


		// router = null;


		// // renew router for unresolved paid transaction.
		// // renewすると取得できない。
		// router = new PurchaseRouter(
		// 	iEnum => {
		// 		runner.StartCoroutine(iEnum);
		// 	},
		// 	productData => {
		// 		// dummy response.
		// 		return new ProductInfo[] {
		// 			new ProductInfo("100_gold_coins", "100_gold_coins_iOS", true, "one hundled of coins."),
		// 			new ProductInfo("1000_gold_coins", "1000_gold_coins_iOS", true, "one ton of coins.")
		// 		};
		// 	},
		// 	ticketData => Guid.NewGuid().ToString(),
		// 	() => {
		// 		Debug.Log("起動成功");
		// 	},
		// 	(err, reason, status) => {
		// 		Debug.Log("起動失敗 err:" + err + " reason:" + reason);
		// 	},
		// 	null,
		// 	DummyResponsehandlingDelegate
		// );

		// Debug.Log("適当な待ち開始");

		yield return new WaitForSeconds(100);
		router.Show();// 観測できないのはまあ良いとして、じゃあどのくらい待てば良いんだろう。
		
		
	}

	/*
		failしたpurchaseを受け取る機会ってどうなってるんだろう、待ったら来るのかな。
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
}
