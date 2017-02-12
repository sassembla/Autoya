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
	
    [MSetup] public void Setup () {
		if (!IsTestRunningInPlayingMode()) {
			SkipCurrentTest("Purchase feature should run on MainThread.");
            return;
		};

        var authorized = false;
		Action onMainThread = () => {
            var dataPath = Application.persistentDataPath;

            var fwPath = Path.Combine(dataPath, AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN);
            DeleteAllData(fwPath);

			Autoya.TestEntryPoint(dataPath);
			
			Autoya.Auth_SetOnAuthenticated(
				() => {
					authorized = true;
				}
			);
		};
		RunOnMainThread(onMainThread);
		
		WaitUntil(
			() => {
				return authorized && Autoya.Purchase_IsReady();
			}, 
			5, 
			"failed to auth or failed to ready purchase."
		);
    }

    [MTest] public void GetProductInfos () {
        var products = Autoya.Purchase_ProductInfos();
        Assert(products.Length == 3, "not match.");
    }

    [MTest] public void PurchaseViaAutoya () {
        var succeeded = false;
        var done = false;
        RunOnMainThread(
            () => {
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
            }
        );

        WaitUntil(
            () => done, 
            10,
            "failed to purchase."
        );
        Assert(succeeded, "not successed.");
    }
}