using System;
using System.Collections.Generic;
using AutoyaFramework;
using Miyamasu;
using UnityEngine;



/**
	tests for Autoya Authorized HTTP.
	Autoya strongly handle these server-related errors which comes from game-server.
	
	these test codes are depends on online env + "https://httpbin.org".
*/
public class AuthorizedHTTPImplementationTests : MiyamasuTestRunner {
	[MSetup] public void Setup () {
		
		var authorized = false;
		Action onMainThread = () => {
			var dataPath = string.Empty;
			Autoya.TestEntryPoint(dataPath);
			
			Autoya.Auth_SetOnLoginSucceeded(
				() => {
					authorized = true;
				}
			);
		};
		RunOnMainThread(onMainThread);
		
		WaitUntil(() => authorized, 10); 
	}

	[MTeardown] public void Teardown () {
		RunOnMainThread(
			() => {
				var obj = GameObject.Find("MainThreadDispatcher");
				if (obj != null) GameObject.DestroyImmediate(obj); 
			}
		);
	}
	
	[MTest] public void AutoyaHTTPGet () {
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

		WaitUntil(
			() => !string.IsNullOrEmpty(result), 
			5
		);
	}

	[MTest] public void AutoyaHTTPGetWithAdditionalHeader () {
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
				{"Hello", "World"}
			}
		);

		WaitUntil(
			() => (result.Contains("Hello") && result.Contains("World")), 
			5
		);
	}

	[MTest] public void AutoyaHTTPGetFailWith404 () {
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

		WaitUntil(
			() => (resultCode != 0), 
			5
		);
		
		// result should be have reason,
		Assert(resultCode == 404, "code note match. resultCode:" + resultCode);
	}

	[MTest] public void AutoyaHTTPGetFailWithUnauth () {
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

		WaitUntil(
			() => !string.IsNullOrEmpty(unauthReason), 
			5
		);
		
		Assert(!string.IsNullOrEmpty(unauthReason), "code note match. unauthReason:" + unauthReason);
	}

	[MTest] public void AutoyaHTTPGetFailWithTimeout () {
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
				Assert(code == BackyardSettings.HTTP_TIMEOUT_CODE, "not match.");
				timeoutError = reason;
			},
			null,
			0.01
		);

		WaitUntil(
			() => !string.IsNullOrEmpty(timeoutError), 
			5
		);
	}

	[MTest] public void AutoyaHTTPPost () {
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

		WaitUntil(
			() => !string.IsNullOrEmpty(result), 
			5
		);
	}

	/*
		target test site does not support show post request. hmmm,,,
	*/
	// [MTest] public void AutoyaHTTPPostWithAdditionalHeader () {
	// 	var result = string.Empty;
	// 	var connectionId = Autoya.Http_Post(
	// 		"https://httpbin.org/headers", 
	// 		"data",
	// 		(string conId, string resultData) => {
	// 			TestLogger.Log("resultData:" + resultData);
	// 			result = resultData;
	// 		},
	// 		(string conId, int code, string reason) => {
	// 			TestLogger.Log("fmmmm,,,,, AutoyaHTTPPostWithAdditionalHeader failed conId:" + conId + " reason:" + reason);
	// 			// do nothing.
	// 		},
	// 		new Dictionary<string, string>{
	// 			{"Hello", "World"}
	// 		}
	// 	);

	// 	var wait = WaitUntil(
	// 		() => (result.Contains("Hello") && result.Contains("World")), 
	// 		5
	// 	);
	// 	if (!wait) return false; 
		
	// 	return true;
	// }

	[MTest] public void AutoyaHTTPPostFailWith404 () {
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

		WaitUntil(
			() => (resultCode != 0), 
			5
		);
		
		// result should be have reason,
		Assert(resultCode == 404, "code note match. resultCode:" + resultCode);
	}

	[MTest] public void AutoyaHTTPPostFailWithUnauth () {
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

		WaitUntil(
			() => !string.IsNullOrEmpty(unauthReason), 
			5
		);
		
		Assert(!string.IsNullOrEmpty(unauthReason), "code note match. unauthReason:" + unauthReason);
	}

	[MTest] public void AutoyaHTTPPostFailWithTimeout () {
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
				Assert(code == BackyardSettings.HTTP_TIMEOUT_CODE, "not match. code:" + code + " reason:" + reason);
				timeoutError = reason;
			},
			null,
			0.0001// 1ms
		);

		WaitUntil(
			() => !string.IsNullOrEmpty(timeoutError), 
			5
		);
	}
	
}
