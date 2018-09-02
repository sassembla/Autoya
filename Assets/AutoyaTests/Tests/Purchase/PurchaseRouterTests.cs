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
public class PurchaseRouterTests : MiyamasuTestRunner
{
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

    [MSetup]
    public IEnumerator Setup()
    {
        var done = false;

        // overwrite Autoya instance for test purchase feature.
        yield return WaitPurchaseFeatureOfAutoya(
            () =>
            {
                done = true;
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("failed to ready."); }
        );

        if (!done)
        {
            Fail("Purchase feature test setup is failed to ready.");
            yield break;
        }

        // shutdown purchase feature for get valid result from Unity IAP.
        Autoya.Purchase_DEBUG_Shutdown();

        var purchaseRunner = new GameObject("PurchaseTestRunner");
        runner = purchaseRunner.AddComponent<TestMBRunner>();

        router = new PurchaseRouter(
            iEnum =>
            {
                runner.StartCoroutine(iEnum);
            },
            productData =>
            {
                // dummy response.
                return new ProductInfo[] {
                    new ProductInfo("100_gold_coins", "100_gold_coins_iOS", true, "one hundled of coins."),
                    new ProductInfo("1000_gold_coins", "1000_gold_coins_iOS", true, "one ton of coins.")
                };
            },
            givenProductId => givenProductId,
            ticketData => ticketData,
            () => { },
            (err, code, reason) => { }
        );

        yield return WaitUntil(() => router.IsPurchaseReady(), () => { throw new TimeoutException("failed to ready."); });
    }

    [MTeardown]
    public void Teardown()
    {
        GameObject.Destroy(runner.gameObject);
    }

    private IEnumerator WaitPurchaseFeatureOfAutoya(Action done)
    {
        Autoya.TestEntryPoint(Application.persistentDataPath);

        while (!Autoya.Purchase_IsReady())
        {
            yield return null;
        }
        done();
    }


    private bool forceFailResponse = false;
    private int forceFailCount = 0;
    private void DummyResponsehandlingDelegate(string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string> failed)
    {
        if (forceFailResponse)
        {
            forceFailCount++;
            failed(connectionId, httpCode, "expected failure in test.");
            return;
        }

        if (200 <= httpCode && httpCode < 299)
        {
            succeeded(connectionId, data);
            return;
        }

        failed(connectionId, httpCode, errorReason);
    }




    [MTest]
    public IEnumerator ShowProductInfos()
    {
        var products = router.ProductInfos();
        True(products.Length == 2, "not match.");
        yield break;
    }


    [MTest]
    public IEnumerator Purchase()
    {
        var purchaseId = "dummy purchase Id";
        var productId = "100_gold_coins";

        var purchaseDone = false;
        var purchaseSucceeded = false;
        var failedReason = string.Empty;

        yield return router.PurchaseAsync(
            purchaseId,
            productId,
            pId =>
            {
                purchaseDone = true;
                purchaseSucceeded = true;
            },
            (pId, err, code, reason) =>
            {
                purchaseDone = true;
                failedReason = reason;
            }
        );

        yield return WaitUntil(() => purchaseDone, () => { throw new TimeoutException("failed to purchase async."); });
        True(purchaseSucceeded, "purchase failed. reason:" + failedReason);
    }

    [MTest]
    public IEnumerator RetryPurchaseThenFail()
    {
        forceFailResponse = false;

        // routerをhandling付きで生成すればいい。

        router = new PurchaseRouter(
            iEnum =>
            {
                runner.StartCoroutine(iEnum);
            },
            productData =>
            {
                // dummy response.
                return new ProductInfo[] {
                    new ProductInfo("100_gold_coins", "100_gold_coins_iOS", true, "one hundled of coins."),
                    new ProductInfo("1000_gold_coins", "1000_gold_coins_iOS", true, "one ton of coins.")
                };
            },
            givenProductId => givenProductId,
            ticketData => ticketData,
            () => { },
            (err, code, reason) => { },
            backgroundPurchasedProductId => { },
            null,
            DummyResponsehandlingDelegate
        );

        yield return WaitUntil(() => router.IsPurchaseReady(), () => { throw new TimeoutException("failed to ready."); });

        var purchaseId = "dummy purchase Id";
        var productId = "100_gold_coins";

        var purchaseDone = false;

        var cor = router.PurchaseAsync(
            purchaseId,
            productId,
            pId =>
            {
                // never success.
            },
            (pId, err, code, reason) =>
            {
                purchaseDone = true;
            }
        );

        while (cor.MoveNext())
        {
            yield return null;
        }

        yield return WaitUntil(
            () =>
            {
                var state = router.State();
                if (state == PurchaseRouter.RouterState.Purchasing)
                {
                    // httpが強制的に失敗するようにする。
                    forceFailResponse = true;
                }
                return purchaseDone;
            },
            () => { throw new TimeoutException("timeout."); },
            15
        );

        True(router.State() == PurchaseRouter.RouterState.RetryFailed);

        /*
			・storeのリブートに際して失敗したtransactionが復活するかどうか
			が実機に依存するため、このテストはiOS/Androidの実機上でしか動作しない。
		 */
#if UNITY_EDITOR
        var a = true;
        if (a) yield break;
#elif UNITY_IOS || UNITY_ANDROID
		// pass.
#else
		yield break;
#endif

        // done, but transaction is remaining.

        var completed = false;

        // reload router will finish remaining transaction.
        router = new PurchaseRouter(
            iEnum =>
            {
                runner.StartCoroutine(iEnum);
            },
            productData =>
            {
                // dummy response.
                return new ProductInfo[] {
                    new ProductInfo("100_gold_coins", "100_gold_coins_iOS", true, "one hundled of coins."),
                    new ProductInfo("1000_gold_coins", "1000_gold_coins_iOS", true, "one ton of coins.")
                };
            },
            givenProductId => givenProductId,
            ticketData => ticketData,
            () => { },
            (err, code, reason) => { },
            backgroundPurchasedProductId =>
            {
                completed = true;
            }
        );

        yield return WaitUntil(
            () => completed,
            () => { throw new TimeoutException("failed to complete remaining transaction."); }
        );
    }

    [MTest]
    public IEnumerator RetryPurchaseThenFinallySuccess()
    {
        forceFailResponse = false;

        // routerをhandling付きで生成すればいい。

        router = new PurchaseRouter(
            iEnum =>
            {
                runner.StartCoroutine(iEnum);
            },
            productData =>
            {
                // dummy response.
                return new ProductInfo[] {
                    new ProductInfo("100_gold_coins", "100_gold_coins_iOS", true, "one hundled of coins."),
                    new ProductInfo("1000_gold_coins", "1000_gold_coins_iOS", true, "one ton of coins.")
                };
            },
            givenProductId => givenProductId,
            ticketData => ticketData,
            () => { },
            (err, code, reason) => { },
            backgroundPurchasedProductId => { },
            null,
            DummyResponsehandlingDelegate
        );

        yield return WaitUntil(() => router.IsPurchaseReady(), () => { throw new TimeoutException("failed to ready."); });


        var purchaseId = "dummy purchase Id";
        var productId = "100_gold_coins";

        var purchaseDone = false;

        var cor = router.PurchaseAsync(
            purchaseId,
            productId,
            pId =>
            {
                purchaseDone = true;
            },
            (pId, err, code, reason) =>
            {
                // never fail.
            }
        );

        while (cor.MoveNext())
        {
            yield return null;
        }

        yield return WaitUntil(
            () =>
            {
                var state = router.State();
                if (state == PurchaseRouter.RouterState.Purchasing)
                {
                    // httpが強制的に失敗するようにする。
                    forceFailResponse = true;
                }

                // リトライにN-1回失敗したあとに成功するようにフラグを変更する。
                if (forceFailCount == PurchaseSettings.PURCHASED_MAX_RETRY_COUNT - 1)
                {
                    forceFailResponse = false;
                }

                return purchaseDone;
            },
            () => { throw new TimeoutException("timeout."); },
            10
        );
    }

    [MTest]
    public IEnumerator RetryPurchaseThenFailThenComplete()
    {

        /*
			・storeのリブートに際して失敗したtransactionが復活するかどうか
			が実機に依存するため、このテストはiOS/Androidの実機上でしか動作しない。
		 */
#if UNITY_EDITOR
        var a = true;
        if (a) yield break;
#elif UNITY_IOS || UNITY_ANDROID
		// pass.
#else
		yield break;
#endif


        forceFailResponse = false;

        router = new PurchaseRouter(
            iEnum =>
            {
                runner.StartCoroutine(iEnum);
            },
            productData =>
            {
                // dummy response.
                return new ProductInfo[] {
                    new ProductInfo("100_gold_coins", "100_gold_coins_iOS", true, "one hundled of coins."),
                    new ProductInfo("1000_gold_coins", "1000_gold_coins_iOS", true, "one ton of coins.")
                };
            },
            givenProductId => givenProductId,
            ticketData => Guid.NewGuid().ToString(),
            () => { },
            (err, code, reason) => { },
            backgroundPurchasedProductId => { },
            null,
            DummyResponsehandlingDelegate
        );

        var storeId = router.StoreId();

        yield return WaitUntil(() => router.IsPurchaseReady(), () => { throw new TimeoutException("failed to ready."); });

        var purchaseId = "dummy purchase Id";
        var productId = "100_gold_coins";

        var purchaseDone = false;

        var cor = router.PurchaseAsync(
            purchaseId,
            productId,
            pId =>
            {
                // never success.
                Fail();
            },
            (pId, err, code, reason) =>
            {
                purchaseDone = true;
            }
        );

        while (cor.MoveNext())
        {
            yield return null;
        }

        yield return WaitUntil(
            () =>
            {
                var state = router.State();
                if (state == PurchaseRouter.RouterState.Purchasing)
                {
                    // httpが強制的に失敗するようにする。
                    forceFailResponse = true;
                }
                return purchaseDone;
            },
            () => { throw new TimeoutException("timeout."); },
            15
        );

        forceFailResponse = false;


        var rebooted = false;
        var completed = false;

        router = new PurchaseRouter(
            iEnum =>
            {
                runner.StartCoroutine(iEnum);
            },
            productData =>
            {
                // dummy response.
                return new ProductInfo[] {
                    new ProductInfo("100_gold_coins", "100_gold_coins_iOS", true, "one hundled of coins."),
                    new ProductInfo("1000_gold_coins", "1000_gold_coins_iOS", true, "one ton of coins.")
                };
            },
            givenProductId => givenProductId,
            ticketData => Guid.NewGuid().ToString(),
            () =>
            {
                rebooted = true;
            },
            (err, code, reason) =>
            {
                Fail("failed to boot store func. err:" + err + " reason:" + reason);
            },
            backgroundPurchasedProductId =>
            {
                completed = true;
            },
            null,
            DummyResponsehandlingDelegate
        );

        // store is renewed. not same id.
        AreNotEqual(storeId, router.StoreId());

        yield return WaitUntil(
            () => rebooted && completed,
            () => { throw new TimeoutException("timeout."); },
            10
        );
    }

    [Serializable]
    public class SampleTicletJsonData
    {
        [SerializeField] public string productId;
        [SerializeField] public string dateTime;

        public SampleTicletJsonData(string productId, string dateTime)
        {
            this.productId = productId;
            this.dateTime = dateTime;
        }
    }

    [MTest]
    public IEnumerator ChangePurhcaseSuceededRequest()
    {
        var dateTimeStr = DateTime.Now.Ticks.ToString();

        Func<string, string> onTicletRequestFunc = givenProductId =>
        {
            var data = new SampleTicletJsonData(givenProductId, dateTimeStr);
            var jsonStr = JsonUtility.ToJson(data);
            return jsonStr;
        };

        router = new PurchaseRouter(
           iEnum =>
           {
               runner.StartCoroutine(iEnum);
           },
           productData =>
           {
               // dummy response.
               return new ProductInfo[] {
                    new ProductInfo("100_gold_coins", "100_gold_coins_iOS", true, "one hundled of coins."),
                    new ProductInfo("1000_gold_coins", "1000_gold_coins_iOS", true, "one ton of coins.")
               };
           },
           onTicletRequestFunc,// ここがリクエストに乗っかるので、ticketDataの値でassertを書けばいい。
           ticketData =>
           {
               True(ticketData.Contains(dateTimeStr));
               return ticketData;
           },
           () => { },
           (err, code, reason) => { },
           backgroundPurchasedProductId => { },
           null,
           DummyResponsehandlingDelegate
       );

        yield return WaitUntil(() => router.IsPurchaseReady(), () => { throw new TimeoutException("failed to ready."); });

        var purchaseId = "dummy purchase Id";
        var productId = "100_gold_coins";

        var cor = router.PurchaseAsync(
            purchaseId,
            productId,
            pId =>
            {
                // do nothing.
            },
            (pId, err, code, reason) =>
            {
                // do nothing.
            }
        );

        while (cor.MoveNext())
        {
            yield return null;
        }
    }
}
