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

        DeleteAllData(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN);

		var authorized = false;
		Action onMainThread = () => {
			var dataPath = string.Empty;
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
				return authorized;
			}, 
			5, 
			"failed to auth."
		);
		
		Assert(Autoya.Auth_IsAuthenticated(), "not logged in.");
    }

    [MTest] public void PurchaseViaAutoya () {
        // var succeeded = false;
        // var done = false;
        // RunEnumeratorOnMainThread(
        //     Purchase(isSucceeded => {
        //         done = true;
        //         succeeded = isSucceeded;
        //     }),
        //     false
        // );

        // WaitUntil(
        //     () => done, 
        //     10,
        //     "failed to purchase."
        // );
        // Assert(succeeded, "not successed.");
    }

    private IEnumerator Purchase (Action<bool> done) {
        while (!Autoya.Purchase_IsReady()) {
            yield return null;
        }
        
        var purchaseId = "myPurchaseId_" + Guid.NewGuid().ToString();
        
        Autoya.Purchase(
            purchaseId, 
            "100_gold_coins", 
            pId => {
                done(true);
            }, 
            (pId, err, reason, autoyaStatus) => {
                done(false);
            }
        );
    }
}