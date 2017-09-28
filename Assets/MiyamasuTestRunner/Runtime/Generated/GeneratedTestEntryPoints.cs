
using UnityEngine.TestTools;
using System;
using System.Collections;
public class PurchaseRouterTests_Miyamasu {
    [UnityTest] public IEnumerator ShowProductInfos() {
        var rec = new Miyamasu.Recorder("PurchaseRouterTests", "ShowProductInfos");
        var instance = new PurchaseRouterTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.ShowProductInfos();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator Purchase() {
        var rec = new Miyamasu.Recorder("PurchaseRouterTests", "Purchase");
        var instance = new PurchaseRouterTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.Purchase();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator RetryPurchaseThenFail() {
        var rec = new Miyamasu.Recorder("PurchaseRouterTests", "RetryPurchaseThenFail");
        var instance = new PurchaseRouterTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.RetryPurchaseThenFail();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator RetryPurchaseThenFinallySuccess() {
        var rec = new Miyamasu.Recorder("PurchaseRouterTests", "RetryPurchaseThenFinallySuccess");
        var instance = new PurchaseRouterTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.RetryPurchaseThenFinallySuccess();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator RetryPurchaseThenFailThenWait() {
        var rec = new Miyamasu.Recorder("PurchaseRouterTests", "RetryPurchaseThenFailThenWait");
        var instance = new PurchaseRouterTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.RetryPurchaseThenFailThenWait();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
}