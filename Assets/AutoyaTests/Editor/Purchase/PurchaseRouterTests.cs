using System;
using System.Collections;
using System.Threading;
using AutoyaFramework;
using AutoyaFramework.Connections.HTTP;
using AutoyaFramework.Purchase;
using Miyamasu;
using UnityEngine;
using UnityEngine.Purchasing;

/**
	tests for Autoya Purchase
*/
public class PurchaseRouterTests : MiyamasuTestRunner {
    /**
        Unity 5.5対応のpurchaseのテスト。

        通信機構はAutoyaの認証ありのものをそのまま使うので、外部へと通信処理を出せる必要がある。というか内部でAutoya使えばいいのか。
        AssetBundleもそれができればいいな。できるルートを作ればいい。
        -> 通信の結果判別Actionを受け入れさせればいいことに気づいた。ので、その部分を更新しよう。

        {アイテム一覧取得処理}
            ・アイテム一覧を取得する。

        {アップデート処理} (FW全体でいろいろペンディング？)
            ・起動時処理(勝手に購買処理が完了したりするはず)
            ・storedチケットがない場合の購入完了処理
            ・storedチケットがある場合の購入完了処理

        {購入処理} (FW全体でいろいろペンディング？)
            ・事前通信
            ・購買処理
            ・チケットの保存
            ・購買完了通信
            
            ・購入成功チケットの削除
            ・購入キャンセルチケットの削除
            ・購入失敗チケットの処理
        
        レストアとかは対応しないぞ。
    */
    private PurchaseRouter router;
    
    [MSetup] public void Setup () {
        if (!IsTestRunningInPlayingMode()) return;

        Action<string, Action<string, string>, Action<string, int, string>> httpGet = (url, successed, failed) => {
            Autoya.Http_Get(url, successed, failed);
        };

        Action<string, string, Action<string, string>, Action<string, int, string>> httpPost = (url, data, successed, failed) => {
            Autoya.Http_Post(url, data, successed, failed);
        };

        RunOnMainThread(
            () => {
                router = new PurchaseRouter(httpGet, httpPost);
            }
        );
        
        WaitUntil(() => router.IsPurchaseReady(), 2, "failed to ready.");
    }

    [MTest] public void ReadyPurchase () {
        if (router == null) {
            MarkSkipped();
            return;
        }
        Assert(router.IsPurchaseReady(), "not ready.");
    }

    [MTest] public void StartPurchaseDummy () {
        if (router == null) {
            MarkSkipped();
            return;
        }
        
        WaitUntil(() => router.IsPurchaseReady(), 2, "failed to ready.");
        var purchaseId = "dummy purchase Id";
        var productId = "100_gold_coins";

        var purchaseDone = false;
        var purchaseSucceeded = false;
        var failedReason = string.Empty;

        RunOnMainThread(
            () => {
                router.PurchaseAsync(
                    purchaseId,
                    productId,
                    pId => {
                        purchaseDone = true;
                        purchaseSucceeded = true;
                    },
                    (pId, err, reason) => {
                        Debug.LogError("おや？");
                        purchaseDone = true;
                        failedReason = reason;
                    }
                );
            }
        );

        WaitUntil(() => purchaseDone, 10, "failed to purchase async.");
        Assert(purchaseSucceeded, "purchase failed. reason:" + failedReason);
    }

    [MTest] public void ReloadStore () {
        if (router == null) {
            MarkSkipped();
            return;
        }
        
        WaitUntil(() => router.IsPurchaseReady(), 2, "failed to ready.");
        
        /*
            router is already ready. nothing to do.
        */
        RunOnMainThread(
            () => {
                router.Reload(
                    () => {},
                    (err, reason) => {}
                );
            }
        );
        
        WaitUntil(() => router.IsPurchaseReady(), 2, "failed to reload.");
    }

    /**
        force fail initialize of router.
    */
    [MTest] public void ReloadUnreadyStore () {
        if (router == null) {
            MarkSkipped();
            return;
        }

        Action<string, Action<string, string>, Action<string, int, string>> httpGet = (url, successed, failed) => {
            // empty http get. will be timeout.
        };

        Action<string, string, Action<string, string>, Action<string, int, string>> httpPost = (url, data, successed, failed) => {
            // empty http post. will be timeout.
        };

        // renew router.
        RunOnMainThread(
            () => {
                router = new PurchaseRouter(httpGet, httpPost);
            }
        );
        
        try {
            WaitUntil(() => router.IsPurchaseReady(), 2, "failed to ready.");
        } catch {
            // catch timeout. do nothing.
        }

        Assert(!router.IsPurchaseReady(), "not intended.");
        
        // すでにnewされているrouterのハンドラを更新しないとダメか、、
        router.httpGet = (url, successed, failed) => {
            Autoya.Http_Get(url, successed, failed);
        };

        router.httpPost = (url, data, successed, failed) => {
            Autoya.Http_Post(url, data, successed, failed);
        };

        var ready = false;
        router.Reload(
            () => {
                ready = true;
            },
            (err, reason) => {}
        );

        WaitUntil(() => ready, 5, "not get ready.");
        Assert(router.IsPurchaseReady(), "not ready.");
    }
}
