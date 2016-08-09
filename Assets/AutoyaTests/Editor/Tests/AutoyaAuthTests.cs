using AutoyaFramework;
using Miyamasu;

/**
	test for authorization controll.
*/
public class AutoyaAuthTests : MiyamasuTestRunner {
	[MTest] public bool WaitingAuthorize () {
		Autoya.TestEntryPoint(string.Empty);

		var authorized = false;
		Autoya.Auth_SetOnAuthSucceeded(
			() => {
				authorized = true;
			}
		);

		var wait = WaitUntil(() => authorized, 5);
		if (!wait) return false;

		return true;
	}

	/*
		適当に考え中。
		
		トークン取得
		・トークン取得に成功する
		・トークン取得に失敗する
		
		ログイン/アウト
		・マニュアルログインを試みる
		・マニュアルでログアウトする
		・ログインに失敗する
	*/
	[MTest] public bool HandleAccidentialLoginThenSucceeded () {
		WaitingAuthorize();

		var fakeReason = string.Empty;
		Autoya.Auth_SetOnAuthFailed(
			(conId, reason) => {
				fakeReason = reason;
				return true;
			}
		);
		
		// emit fake-accidential logout
		Autoya.Auth_Test_AccidentialLogout();

		if (!WaitUntil(() => !string.IsNullOrEmpty(fakeReason), 5)) return false;

		/*
			re-login feature is attempt. keep waiting.
		*/
		var authorized = false;
		Autoya.Auth_SetOnAuthSucceeded(
			() => {
				authorized = true;
			}
		);

		if (!WaitUntil(() => authorized, 5)) return false;

		return true;
	}

	// [MTest] public bool HandleAccidentialLoginThenFailedAgain () {
	// 	WaitingAuthorize();

	// 	var fakeReason = string.Empty;
	// 	Autoya.Auth_SetOnAuthFailed(
	// 		(conId, reason) => {
	// 			fakeReason = reason;
	// 			return true;
	// 		}
	// 	);
		
	// 	// emit fake-accidential logout
	// 	Autoya.Auth_Test_AccidentialLogout();

	// 	if (!WaitUntil(() => !string.IsNullOrEmpty(fakeReason), 5)) return false;

	// 	/*
	// 		re-login feature is attempt. this time, it should be fail. keep waiting.
	// 	*/

	// 	// intentionally goto fail again.
	// 	Autoya.Auth_Test_AccidentialLogout();

	// 	var unauthorized = false;
	// 	Autoya.Auth_SetOnAuthFailed(
	// 		(conId, reason) => {
	// 			unauthorized = true;
	// 			return false;
	// 		}
	// 	);

	// 	if (!WaitUntil(() => unauthorized, 5)) return false;

	// 	return true;
	// }

	// [MTest] public bool IntentionalLogout () {
	// 	return false;
	// }

	// [MTest] public bool IntentionalLogoutThenAutoRelogin () {
	// 	return false;
	// }

	// [MTest] public bool AccidentialLogoutThenManualReloginSucceeded () {
	// 	return false;
	// }

	// [MTest] public bool AccidentialLogoutThenManualReloginFailed () {
	// 	return false;
	// }



}