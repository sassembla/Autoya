using System;
using System.IO;
using AutoyaFramework;
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
		DeleteAllData(BackyardSettings.AUTH_STORED_FRAMEWORK_DOMAIN);
		
		var authorized = false;
		Action onMainThread = () => {
			var dataPath = string.Empty;
			Autoya.TestEntryPoint(dataPath);

			Autoya.Auth_SetOnLoginSucceeded(
				() => {
					authorized = true;
				}
			);
			Autoya.Auth_SetOnAuthFailed(
				(conId, reason) => {
					Debug.LogError("failed to setup.");
					return false;
				}
			);
		};
		RunOnMainThread(onMainThread);
		
		WaitUntil(
			() => authorized,
			3,
			"timeout in setup."
		);

		Assert(Autoya.Auth_IsLoggedIn(), "not logged in.");
	}

	
	[MTest] public void WaitDefaultAuthorize () {
		Assert(Autoya.Auth_IsLoggedIn(), "not yet logged in.");
	}
	
	[MTest] public void HandleAccidentialAuthErrorThenManualLoginSucceeded () {
		var fakeReason = string.Empty;
		Autoya.Auth_SetOnAuthFailed(
			(conId, reason) => {
				fakeReason = reason;
				return false;
			}
		);
		
		// emit fake-accidential logout
		Autoya.Auth_Test_CreateAuthError();

		WaitUntil(() => !string.IsNullOrEmpty(fakeReason), 5);

		var authorized = false;
		Autoya.Auth_SetOnLoginSucceeded(
			() => {
				authorized = true;
			}
		);

		Autoya.Auth_AttemptLogIn();
		
		WaitUntil(() => authorized, 5);
	}
	

	[MTest] public void HandleAccidentialLogoutThenAutoReloginSucceeded () {
		var fakeReason = string.Empty;
		Autoya.Auth_SetOnAuthFailed(
			(conId, reason) => {
				fakeReason = reason;
				return true;// auto relogin
			}
		);
		
		var authorized = false;
		Autoya.Auth_SetOnLoginSucceeded(
			() => {
				authorized = true;
			}
		);

		// emit fake-accidential logout
		Autoya.Auth_Test_CreateAuthError();

		/*
			loggedIn -> fake 401 request -> logout -> autoLogin -> loggedIn succeed.
		*/

		WaitUntil(
			() => {
				Debug.LogError("waiting..");
				return (!string.IsNullOrEmpty(fakeReason) && authorized);
			},
			3,
			"timeout."
		);
	}

	// [MTest] public void HandleAccidentialLoginThenFailedAgain () {
	// 	var fakeReason = string.Empty;
	// 	Autoya.Auth_SetOnAuthFailed(
	// 		(conId, reason) => {
	// 			fakeReason = reason;
	// 			return true;
	// 		}
	// 	);
		
	// 	// emit fake-accidential logout
	// 	Autoya.Auth_Test_AccidentialLogout();

	// 	/*
	// 		fake request -> received 401 response -> logout will be called -> OnAuthFailed called.
	// 	*/

	// 	if (!WaitUntil(() => !string.IsNullOrEmpty(fakeReason), 5)) return false;

	// 	/*
	// 		re-login feature is attempt. this time, it should be fail. keep waiting.
	// 	*/

	// 	var unauthorized = false;
	// 	Autoya.Auth_SetOnAuthFailed(
	// 		(conId, reason) => {
	// 			unauthorized = true;
	// 			return false;
	// 		}
	// 	);

		// ここんとこの方法が気にくわない。
	// 	// intentionally goto fail again.
	// 	Autoya.Auth_Test_AccidentialLogout();

		

	// 	if (!WaitUntil(() => unauthorized, 5)) return false;
	// }

	[MTest] public void IntentionalLogout () {
		Autoya.Auth_Logout();
		
		var loggedIn = Autoya.Auth_IsLoggedIn();
		Assert(!loggedIn, "state does not match.");
	}

	[MTest] public void IntentionalLogoutThenLoginWillBeSucceeded () {
		Autoya.Auth_Logout();
		var auth = false;
		Autoya.Auth_SetOnLoginSucceeded(
			() => {
				auth = true;
			}
		);

		Autoya.Auth_SetOnAuthFailed(
			(conId, reason) => {
				TestLogger.Log("Auth_SetOnAuthFailed:" + reason);
				return false;
			}
		);

		Autoya.Auth_AttemptLogIn();
		
		WaitUntil(
			() => {
				return auth;
			}, 
			5,
			"failed to relogin."
		);
	}
}