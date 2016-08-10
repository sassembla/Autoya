using AutoyaFramework;
using Miyamasu;

/**
	test for authorization flow control.
*/
public class AuthImplementationTests : MiyamasuTestRunner {
	[MTest] public bool WaitDefaultAuthorize () {
		if (Autoya.Auth_IsLoggedIn()) return true; 
		TestLogger.Log("Progress:" + Autoya.Auth_Progress());
		return false;
	}
	
	[MTest] public bool HandleAccidentialAuthErrorThenManualLoginSucceeded () {
		var fakeReason = string.Empty;
		Autoya.Auth_SetOnAuthFailed(
			(conId, reason) => {
				fakeReason = reason;
				return false;
			}
		);
		
		// emit fake-accidential logout
		Autoya.Auth_Test_CreateAuthError();

		if (!WaitUntil(() => !string.IsNullOrEmpty(fakeReason), 5)) return false;

		var authorized = false;
		Autoya.Auth_SetOnLoginSucceeded(
			() => {
				authorized = true;
			}
		);

		Autoya.Auth_AttemptLogIn();
		
		if (!WaitUntil(() => authorized, 5)) return false;

		return true;
	}
	
	[MTest] public bool HandleAccidentialLogoutThenAutoReloginSucceeded () {
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

		if (!WaitUntil(() => !string.IsNullOrEmpty(fakeReason), 5)) return false;
		if (!WaitUntil(() => authorized, 5)) return false;

		return true;
	}

	// [MTest] public bool HandleAccidentialLoginThenFailedAgain () {
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

	// 	return true;
	// }

	[MTest] public bool IntentionalLogout () {
		Autoya.Auth_Logout();

		var loggedIn = Autoya.Auth_IsLoggedIn();
		
		Assert(!loggedIn, "not match.");

		return true;
	}

	[MTest] public bool IntentionalLogoutThenLoginWillBeSucceeded () {
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
		
		if (!WaitUntil(() => auth, 5)) {
			var progress = Autoya.Auth_Progress();
			TestLogger.Log("progress:" + progress, true);
			return false;
		}

		return true;
	}
}