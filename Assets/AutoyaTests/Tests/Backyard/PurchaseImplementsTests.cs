using System;
using System.Collections;
using System.IO;
using AutoyaFramework;
using AutoyaFramework.Settings.Auth;
using Miyamasu;
using UnityEngine;

/**
	test for purchase via Autoya.
*/
public class PurchaseImplementationTests : MiyamasuTestRunner
{
    private void DeleteAllData(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
        Debug.Log("after delete, Directory.Exists(path):" + Directory.Exists(path));
    }

    [MSetup]
    public IEnumerator Setup()
    {
        Autoya.forceMaintenance = false;
        Autoya.forceFailAuthentication = false;
        Autoya.forceFailHttp = false;

        var dataPath = Application.persistentDataPath;

        var fwPath = Path.Combine(dataPath, AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN);
        DeleteAllData(fwPath);

        Autoya.TestEntryPoint(dataPath);

        yield return WaitUntil(
            () =>
            {
                return Autoya.Purchase_IsReady();
            },
            () => { throw new TimeoutException("failed to auth or failed to ready purchase."); },
            10
        );
    }

    [MTeardown]
    public void Teardown()
    {
        Autoya.forceMaintenance = false;
        Autoya.forceFailAuthentication = false;
        Autoya.forceFailHttp = false;
    }

    [MTest]
    public IEnumerator GetProductInfos()
    {
        var products = Autoya.Purchase_ProductInfos();
        True(products.Length == 3, "not match.");
        yield break;
    }

    [MTest]
    public IEnumerator PurchaseViaAutoya()
    {
        var succeeded = false;
        var done = false;

        var purchaseId = "myPurchaseId_" + Guid.NewGuid().ToString();

        Autoya.Purchase(
            purchaseId,
            "1000_gold_coins",
            pId =>
            {
                done = true;
                succeeded = true;
            },
            (pId, err, reason, autoyaStatus) =>
            {
                Fail("err:" + err + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("failed to purchase."); },
            30
        );
        True(succeeded, "not successed.");
    }

    // 課金中にリソースが切り替わったよエラーが来たら <- これは来ないようにしたい。

    [MTest]
    public IEnumerator RetrievePaidPurchase()
    {
        Debug.LogWarning("not yet.");
        // SendPaidTicketが発生する状態を作り出す。まずPurchaseを作り出す。そしてそのPurchaseをPendingした状態で、ブロックを解除する。
        // なんか難しいので簡単に書ける方法ないかな。そもそもサポートしないほうがいいのかな。どうなんだろ。
        /*
			機構を起動 -> 停止
		 */
        yield break;
    }

    [MTest]
    public IEnumerator MaintenainceInPurchase()
    {
        var failed = false;

        var purchaseId = "myPurchaseId_" + Guid.NewGuid().ToString();

        Autoya.forceMaintenance = true;

        Autoya.Purchase(
            purchaseId,
            "1000_gold_coins",
            pId =>
            {
                Fail();
            },
            (pId, err, reason, autoyaStatus) =>
            {
                True(autoyaStatus.inMaintenance);
                failed = true;
            }
        );

        yield return WaitUntil(
            () => failed,
            () => { throw new TimeoutException("failed to fail."); },
            10
        );
    }

    [MTest]
    public IEnumerator AuthFailedInPurchase()
    {
        var failed = false;

        var purchaseId = "myPurchaseId_" + Guid.NewGuid().ToString();

        Autoya.forceFailAuthentication = true;

        Autoya.Purchase(
            purchaseId,
            "1000_gold_coins",
            pId =>
            {
                Fail();
            },
            (pId, err, reason, autoyaStatus) =>
            {
                True(autoyaStatus.isAuthFailed);
                failed = true;
            }
        );

        yield return WaitUntil(
            () => failed,
            () => { throw new TimeoutException("failed to fail."); },
            10
        );

        Autoya.forceFailAuthentication = false;

        yield return WaitUntil(
            () => Autoya.Auth_IsAuthenticated(),
            () => { throw new TimeoutException("failed to fail."); },
            10
        );
    }

    [MTest]
    public IEnumerator PurchaseReadyGetProductsFail()
    {
        // まずシャットダウン
        Autoya.Purchase_DEBUG_Shutdown();

        // 通信を必ず失敗するようにセット
        Autoya.forceFailHttp = true;

        // routerを再度生成する。
        Autoya.Purchase_DEBUG_Reload();

        // attemptReadyPurchaseを着火する必要があるタイミングに切り替わるのを待つ
        yield return WaitUntil(
            () => Autoya.Purchase_NeedAttemptReadyPurchase(),
            () => { throw new TimeoutException("too late."); },
            10
        );
    }

    [MTest]
    public IEnumerator PurchaseReadyGetProductsFailThenReadyAgain()
    {
        // まずシャットダウン
        Autoya.Purchase_DEBUG_Shutdown();

        // 通信を必ず失敗するようにセット
        Autoya.forceFailHttp = true;

        // routerを再度生成する。
        Autoya.Purchase_DEBUG_Reload();

        // attemptReadyPurchaseを着火する必要があるタイミングに切り替わるのを待つ
        yield return WaitUntil(
            () => Autoya.Purchase_NeedAttemptReadyPurchase(),
            () => { throw new TimeoutException("too late."); },
            10
        );

        Autoya.forceFailHttp = false;

        Autoya.Purchase_AttemptReadyPurchase();

        yield return WaitUntil(
            () => Autoya.Purchase_IsReady(),
            () => { throw new TimeoutException("too late."); },
            10
        );
    }
}