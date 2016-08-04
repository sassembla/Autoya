using AutoyaFramework;
using Miyamasu;


/**
	tests for Autoya Authorized HTTP.
	Autoya strongly handle these server-related errors which comes from game-server.
*/
public class AuthorizedHTTPTests : MiyamasuTestRunner {
	private void WaitAuthorized () {
		var authorized = false;

		Autoya.SetOnAuthSucceeded(
			() => {
				authorized = true;
			}
		);

		var wait = WaitUntil(() => authorized, 1);
	}

	[MTest] public bool AutoyaHTTPGet () {
		Autoya.EntryPoint();

		WaitAuthorized();

		var result = string.Empty;
		var connectionId = Autoya.AuthedHttpGet(
			"https://httpbin.org/get", 
			(string conId, string resultData) => {
				result = "done!:" + resultData;
			},
			(string conId, int code, string reason) => {
				// do nothing.
			}
		);

		var wait = WaitUntil(
			() => !string.IsNullOrEmpty(result), 
			5
		);
		if (!wait) return false; 
		
		return true;
	}

	[MTest] public bool AutoyaHTTPGetFailWith404 () {
		Autoya.EntryPoint();
		
		WaitAuthorized();

		var resultCode = 0;
		
		var connectionId = Autoya.AuthedHttpGet(
			"https://httpbin.org/status/404", 
			(string conId, string resultData) => {
				// do nothing.
			},
			(string conId, int code, string reason) => {
				resultCode = code;
			}
		);

		var wait = WaitUntil(
			() => (resultCode != 0), 
			5
		);
		if (!wait) return false; 
		
		// result should be have reason,
		Assert(resultCode == 404, "code note match. resultCode:" + resultCode);

		return true;
	}

	[MTest] public bool AutoyaHTTPGetFailWithUnauth () {
		Autoya.EntryPoint();

		WaitAuthorized();
		
		var unauthReason = string.Empty;

		// set unauthorized method callback.
		Autoya.SetOnAuthFailed(
			(conId, reason) => {
				unauthReason = reason;
				
				// if want to start re-login, return true.
				return true;
			}
		);
		
		var connectionId = Autoya.AuthedHttpGet(
			"https://httpbin.org/status/401", 
			(string conId, string resultData) => {
				// do nothing.
			},
			(string conId, int code, string reason) => {
				// do nothing.
			}
		);

		var wait = WaitUntil(
			() => !string.IsNullOrEmpty(unauthReason), 
			5
		);
		if (!wait) return false; 
		
		Assert(!string.IsNullOrEmpty(unauthReason), "code note match. unauthReason:" + unauthReason);

		return true;
	}
	

	// [MTest] public bool HTTPPost () {
	// 	Autoya.EntryPoint();

	// 	var result = string.Empty;
	// 	var connectionId = Autoya.HttpPost(
	// 		"http://google.com", 
	// 		"sampleDataString",
	// 		(string conId, string resultData) => {
	// 			result = resultData;
	// 		},
	// 		(string conId, int code, string reason) => {
	// 			result = reason;
	// 		}
	// 	);

	// 	var wait = WaitUntil(
	// 		() => !string.IsNullOrEmpty(result), 
	// 		1
	// 	);
	// 	if (!wait) return false; 
		
	// 	return true;
	// }

	// [MTest] public bool HTTPSPost () {
	// 	Autoya.EntryPoint();

	// 	var result = string.Empty;
	// 	var connectionId = Autoya.HttpPost(
	// 		"https://google.com", 
	// 		"sampleDataString",
	// 		(string conId, string resultData) => {
	// 			result = resultData;
	// 		},
	// 		(string conId, int code, string reason) => {
	// 			result = reason;
	// 		}
	// 	);

	// 	var wait = WaitUntil(
	// 		() => !string.IsNullOrEmpty(result), 
	// 		1
	// 	);
	// 	if (!wait) return false; 
		
	// 	return true;
	// }
}
