using System.Collections.Generic;
using AutoyaFramework;
using Miyamasu;


/**
	tests for Autoya Authorized HTTP.
	Autoya strongly handle these server-related errors which comes from game-server.

	these test codes are depends on online env + "https://httpbin.org".
*/
public class AutoyaHTTPTests : MiyamasuTestRunner {
	
	private void WaitAuthorized () {
		
		var authorized = false;

		Autoya.Auth_SetOnAuthSucceeded(
			() => {
				authorized = true;
			}
		);

		var wait = WaitUntil(() => authorized, 2);
	}

	[MTest] public bool AutoyaHTTPGet () {
		Autoya.TestEntryPoint(string.Empty);

		WaitAuthorized();

		var result = string.Empty;
		var connectionId = Autoya.Http_Get(
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

	[MTest] public bool AutoyaHTTPGetWithAdditionalHeader () {
		Autoya.TestEntryPoint(string.Empty);

		WaitAuthorized();

		var result = string.Empty;
		var connectionId = Autoya.Http_Get(
			"https://httpbin.org/headers", 
			(string conId, string resultData) => {
				result = resultData;
			},
			(string conId, int code, string reason) => {
				// do nothing.
			},
			new Dictionary<string, string>{
				{"hello", "world"}
			}
		);

		var wait = WaitUntil(
			() => (result.Contains("hello") && result.Contains("world")), 
			5
		);
		if (!wait) return false; 
		
		return true;
	}

	[MTest] public bool AutoyaHTTPGetFailWith404 () {
		Autoya.TestEntryPoint(string.Empty);
		
		WaitAuthorized();

		var resultCode = 0;
		
		var connectionId = Autoya.Http_Get(
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
		Autoya.TestEntryPoint(string.Empty);

		WaitAuthorized();
		
		var unauthReason = string.Empty;

		// set unauthorized method callback.
		Autoya.Auth_SetOnAuthFailed(
			(conId, reason) => {
				unauthReason = reason;
				
				// if want to start re-login, return true.
				return true;
			}
		);

		/*
			dummy server returns 401 forcely.
		*/
		var connectionId = Autoya.Http_Get(
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

	[MTest] public bool AutoyaHTTPGetFailWithTimeout () {
		Autoya.TestEntryPoint(string.Empty);

		WaitAuthorized();

		var timeoutError = string.Empty;
		/*
			fake server should be response 0.01 sec
		*/
		var connectionId = Autoya.Http_Get(
			"https://httpbin.org/delay/10", 
			(string conId, string resultData) => {
				// do nothing.
			},
			(string conId, int code, string reason) => {
				timeoutError = reason;
			},
			null,
			0.01
		);

		var wait = WaitUntil(
			() => !string.IsNullOrEmpty(timeoutError), 
			5
		);
		if (!wait) return false;
		// TestLogger.Log("timeoutError:" + timeoutError);
		return true;
	}

	[MTest] public bool AutoyaHTTPPost () {
		Autoya.TestEntryPoint(string.Empty);

		WaitAuthorized();

		var result = string.Empty;
		var connectionId = Autoya.Http_Post(
			"https://httpbin.org/post", 
			"data",
			(string conId, string resultData) => {
				TestLogger.Log("resultData:" + resultData);
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

	[MTest] public bool AutoyaHTTPPostWithAdditionalHeader () {
		Autoya.TestEntryPoint(string.Empty);

		WaitAuthorized();

		var result = string.Empty;
		var connectionId = Autoya.Http_Post(
			"https://httpbin.org/headers", 
			"data",
			(string conId, string resultData) => {
				result = resultData;
			},
			(string conId, int code, string reason) => {
				// do nothing.
			},
			new Dictionary<string, string>{
				{"hello", "world"}
			}
		);

		var wait = WaitUntil(
			() => (result.Contains("hello") && result.Contains("world")), 
			5
		);
		if (!wait) return false; 
		
		return true;
	}

	[MTest] public bool AutoyaHTTPPostFailWith404 () {
		Autoya.TestEntryPoint(string.Empty);
		
		WaitAuthorized();

		var resultCode = 0;
		
		var connectionId = Autoya.Http_Post(
			"https://httpbin.org/status/404",
			"data", 
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

	[MTest] public bool AutoyaHTTPPostFailWithUnauth () {
		Autoya.TestEntryPoint(string.Empty);

		WaitAuthorized();
		
		var unauthReason = string.Empty;

		// set unauthorized method callback.
		Autoya.Auth_SetOnAuthFailed(
			(conId, reason) => {
				unauthReason = reason;
				
				// if want to start re-login, return true.
				return true;
			}
		);

		/*
			dummy server returns 401 forcely.
		*/
		var connectionId = Autoya.Http_Post(
			"https://httpbin.org/status/401",
			"data", 
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

	[MTest] public bool AutoyaHTTPPostFailWithTimeout () {
		Autoya.TestEntryPoint(string.Empty);

		WaitAuthorized();

		var timeoutError = string.Empty;
		/*
			fake server should be response 0.01 sec
		*/
		var connectionId = Autoya.Http_Post(
			"https://httpbin.org/delay/10", 
			"data",
			(string conId, string resultData) => {
				// do nothing.
			},
			(string conId, int code, string reason) => {
				timeoutError = reason;
			},
			null,
			0.01
		);

		var wait = WaitUntil(
			() => !string.IsNullOrEmpty(timeoutError), 
			5
		);
		if (!wait) return false;
		// TestLogger.Log("timeoutError:" + timeoutError);
		return true;
	}
}
