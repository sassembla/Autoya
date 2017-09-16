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
public class PurchaseImplementationTests : MiyamasuTestRunner {
	private void DeleteAllData (string path) {
		if (Directory.Exists(path)) {
			Directory.Delete(path, true);
		}
	}
	
	[MSetup] public IEnumerator Setup () {
		var dataPath = Application.persistentDataPath;

		var fwPath = Path.Combine(dataPath, AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN);
		DeleteAllData(fwPath);

		Autoya.TestEntryPoint(dataPath);
		
		yield return WaitUntil(
			() => {
				return Autoya.Purchase_IsReady();
			},
			() => {throw new TimeoutException("failed to auth or failed to ready purchase.");}
		);
	}

	[MTest] public IEnumerator GetProductInfos () {
		var products = Autoya.Purchase_ProductInfos();
		True(products.Length == 3, "not match.");
		yield break;
	}

	[MTest] public IEnumerator PurchaseViaAutoya () {
		var succeeded = false;
		var done = false;
		
		var purchaseId = "myPurchaseId_" + Guid.NewGuid().ToString();

		Autoya.Purchase(
			purchaseId, 
			"1000_gold_coins",
			pId => {
				done = true;
				succeeded = true;
			}, 
			(pId, err, reason, autoyaStatus) => {
				done = true;
				succeeded = false;
			}
		);

		yield return WaitUntil(
			() => done,
			() => {throw new TimeoutException("failed to purchase.");}
		);
		True(succeeded, "not successed.");
	}

	[MTest] public IEnumerator RetrievePaidPurchase () {
		Debug.LogWarning("not yet.");
		// SendPaidTicketが発生する状態を作り出す。まずPurchaseを作り出す。そしてそのPurchaseをPendingした状態で、ブロックを解除する。
		// なんか難しいので簡単に書ける方法ないかな。
		/*
			機構を起動 -> 停止
		
		 */
		 yield break;
	}
}