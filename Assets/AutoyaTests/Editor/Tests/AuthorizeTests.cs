using AutoyaFramework;
using Miyamasu;

/**
	test for authorization controll.
*/
public class AuthorizeTests : MiyamasuTestRunner {
	[MTest] public bool WaitingAuthorize () {
		Autoya.EntryPoint();

		var authorized = false;
		Autoya.SetOnAuthSucceeded(
			() => {
				authorized = true;
			}
		);
		var wait = WaitUntil(() => authorized, 5);
		if (!wait) return false;

		return true;
	}
}