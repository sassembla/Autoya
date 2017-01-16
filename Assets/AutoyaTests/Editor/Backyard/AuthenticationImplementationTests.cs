using System;
using System.IO;
using AutoyaFramework;
using AutoyaFramework.Settings.Auth;
using Miyamasu;
using UnityEngine;

/**
	test for authorization flow control.
*/
public class AuthImplementationTests : MiyamasuTestRunner {
	private void DeleteAllData (string path) {
		if (Directory.Exists(path)) {
			Directory.Delete(path, true);
		}
	}
	
	[MSetup] public void Setup () {
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
			"timeout in setup."
		);

		Assert(Autoya.Auth_IsAuthenticated(), "not logged in.");
	}

    [MTeardown] public void Teardown () {
        RunOnMainThread(Autoya.Shutdown);
    }

	
	[MTest] public void WaitDefaultAuthorize () {
		Assert(Autoya.Auth_IsAuthenticated(), "not yet logged in.");
	}

	[MTest] public void HandleBootAuthFailed () {
		DeleteAllData(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN);
		
		var authorized = false;
		Action onMainThread = () => {

			Autoya.forceFailFirstBoot = true;

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
			"timeout in setup."
		);

		Assert(Autoya.Auth_IsAuthenticated(), "not logged in.");
	}
	
	[MTest] public void HandleAccidentialAuthErrorThenManualLoginSucceeded () {
		Debug.LogError("まだ書いてない");
	// 	var fakeReason = string.Empty;
	// 	Autoya.Auth_SetOnRefreshAuthFailed(
	// 		(code, reason) => {
	// 			fakeReason = reason;
	// 		}
	// 	);
		
	// 	// emit fake 401 response.
	// 	Autoya.Auth_Test_CreateAuthError();

	// 	var authorized = false;
	// 	Autoya.Auth_SetOnAuthenticated(
	// 		() => {
	// 			authorized = true;
	// 		}
	// 	);

	// 	WaitUntil(() => !string.IsNullOrEmpty(fakeReason) && authorized, 5);

	// 	/*
	// 		明示的にrefreshAuthのAttemptを行うシーンをどう用意しようかな、、
	// 		・authErrorだす
	// 		・retryに失敗する
	// 		という条件が必要で、これはなかなか、、そこまでいかないと、
	// 		「リトライしますか？」という画面が出てこない。

	// 		他の経路として、「bootAuthが失敗する」「refreshAuthが失敗する」という経路も必要な気がする。
	// 		Attemptはbootにもrefreshにも対応しているので、
	// 	*/
	}

	
	[MTest] public void IntentionalLogout () {
		Debug.LogError("someone needs logout? part1");
		// Autoya.Auth_Logout();
		
		// var loggedIn = Autoya.Auth_IsAuthenticated();
		// Assert(!loggedIn, "state does not match.");
	}

	[MTest] public void IntentionalLogoutThenRefreshAuthWillBeSucceeded () {
		Debug.LogError("someone needs logout? part2");
		// Autoya.Auth_Logout();
		// var auth = false;
		// Autoya.Auth_SetOnLoginSucceeded(
		// 	() => {
		// 		auth = true;
		// 	}
		// );

		// Autoya.Auth_SetOnAuthFailed(
		// 	(conId, reason) => {
		// 		TestLogger.Log("Auth_SetOnAuthFailed:" + reason);
		// 		return false;
		// 	}
		// );

		// Autoya.Auth_AttemptAuthentication();
		
		// WaitUntil(
		// 	() => {
		// 		return auth;
		// 	}, 
		// 	5,
		// 	"failed to relogin."
		// );
	}
}