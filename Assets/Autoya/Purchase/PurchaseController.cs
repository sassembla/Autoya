using System;
using UnityEngine;

namespace AutoyaFramework.Purchase {
    public class PurchaseController {
        /*
            Transaction生成と、そのTransactionを使って購入開始、購入完了時に通知、というのを行う。
        */
        public void Purchase (string itemId, Action<string> purchased, Action<string, int> failed) {
            Debug.LogError("購入開始から完了までをまるっと包む");
        } 
    }
}