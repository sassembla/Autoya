using AutoyaFramework;
using Miyamasu;


/**
	tests for Autoya Authorized HTTP.
	Autoya strongly handle these server-related errors which comes from game-server.

	these test codes are depends on online env + "https://httpbin.org".
*/
public class AutoyaHTTPTests : MiyamasuTestRunner {
	private void WaitFakeAuthorized () {
		var authorized = false;

		Autoya.Auth_Test_AuthSuccess();

		Autoya.Auth_SetOnAuthSucceeded(
			() => {
				authorized = true;
			}
		);

		var wait = WaitUntil(() => authorized, 1);
	}

	[MTest] public bool AutoyaHTTPGet () {
		Autoya.EntryPoint();

		WaitFakeAuthorized();

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

	[MTest] public bool AutoyaHTTPGetFailWith404 () {
		Autoya.EntryPoint();
		
		WaitFakeAuthorized();

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
		Autoya.EntryPoint();

		WaitFakeAuthorized();
		
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
		Autoya.EntryPoint();

		WaitFakeAuthorized();

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
		Autoya.EntryPoint();

		WaitFakeAuthorized();

		var result = string.Empty;
		var connectionId = Autoya.Http_Post(
			"https://httpbin.org/post", 
			"data",
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

	[MTest] public bool AutoyaHTTPPostFailWith404 () {
		Autoya.EntryPoint();
		
		WaitFakeAuthorized();

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
		Autoya.EntryPoint();

		WaitFakeAuthorized();
		
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
		Autoya.EntryPoint();

		WaitFakeAuthorized();

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
