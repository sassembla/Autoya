using System;
using System.Collections.Generic;
using System.IO;
using AutoyaFramework;
using AutoyaFramework.Settings.Auth;
using Miyamasu;
using UnityEngine;

/**
	tests for Autoya Authenticated HTTP.
	Autoya strongly handle these server-related errors which comes from game-server.
	
	these test codes are depends on online env + "https://httpbin.org".
*/
public class AuthenticatedHTTPImplementationTests : MiyamasuTestRunner {
	private void DeleteAllData (string path) {
		if (Directory.Exists(path)) {
			Directory.Delete(path, true);
		}
	}
	
	[MSetup] public void Setup () {
		var authenticated = false;
		Action onMainThread = () => {
			var dataPath = Application.persistentDataPath;

			var fwPath = Path.Combine(dataPath, AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN);
			DeleteAllData(fwPath);
			
			Autoya.TestEntryPoint(dataPath);
			
			Autoya.Auth_SetOnAuthenticated(
				() => {
					authenticated = true;
				}
			);
		};
		RunOnMainThread(onMainThread);
		
		WaitUntil(
			() => {
				return authenticated;
			}, 
			5, 
			"failed to auth."
		);
		
		Assert(Autoya.Auth_IsAuthenticated(), "not logged in.");
	}
	
	[MTeardown] public void Teardown () {
		RunOnMainThread(Autoya.Shutdown);
	}

	[MTest] public void AutoyaHTTPGet () {
		var result = string.Empty;
		Autoya.Http_Get(
			"https://httpbin.org/get", 
			(string conId, string resultData) => {
				result = "done!:" + resultData;
			},
			(conId, code, reason, autoyaStatus) => {
				Assert(false, "failed. code:" + code + " reason:" + reason);
			}
		);

		WaitUntil(
			() => !string.IsNullOrEmpty(result), 
			5
		);
	}

	[MTest] public void AutoyaHTTPGetWithAdditionalHeader () {
		var result = string.Empty;
		Autoya.Http_Get(
			"https://httpbin.org/headers", 
			(conId, resultData) => {
				result = resultData;
			},
			(conId, code, reason, autoyaStatus) => {
				Assert(false, "failed. code:" + code + " reason:" + reason);
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
		
		Autoya.Http_Get(
			"https://httpbin.org/status/404", 
			(string conId, string resultData) => {
				Assert(false, "unexpected succeeded. resultData:" + resultData);
			},
			(conId, code, reason, autoyaStatus) => {
				resultCode = code;
			}
		);

		WaitUntil(
			() => (resultCode != 0), 
			5
		);
		
		// result should be have reason,
		Assert(resultCode == 404, "code unmatched. resultCode:" + resultCode);
	}

	[MTest] public void AutoyaHTTPGetFailWithUnauth () {
		var unauthorized = false;

		/*
			dummy server returns 401 forcibly.
		*/
		Autoya.Http_Get(
			"https://httpbin.org/status/401", 
			(string conId, string resultData) => {
				// do nothing.
				Debug.Log("Http_Get a resultData:" + resultData);
			},
			(conId, code, reason, autoyaStatus) => {
				unauthorized = autoyaStatus.isAuthFailed;
			}
		);

		WaitUntil(
			() => unauthorized,
			5
		);

		// token refresh feature is already running. wait end.
		WaitUntil(
			() => Autoya.Auth_IsAuthenticated(),
			5,
			"failed to refresh token."
		);
	}

	[MTest] public void AutoyaHTTPGetFailWithTimeout () {
		var failedCode = -1;
		var timeoutError = string.Empty;
		/*
			fake server should be response in 1msec. 
			server responses 1 sec later.
			it is impossible.
		*/
		Autoya.Http_Get(
			"https://httpbin.org/delay/1", 
			(string conId, string resultData) => {
				Assert(false, "got success result.");
			},
			(conId, code, reason, autoyaStatus) => {
				failedCode = code;
				timeoutError = reason;
			},
			null,
			0.0001
		);

		WaitUntil(
			() => {
				return !string.IsNullOrEmpty(timeoutError);
			}, 
			3
		);

		Assert(failedCode == BackyardSettings.HTTP_TIMEOUT_CODE, "unmatch. failedCode:" + failedCode + " message:" + timeoutError);
	}

	[MTest] public void AutoyaHTTPPost () {
		var result = string.Empty;
		Autoya.Http_Post(
			"https://httpbin.org/post", 
			"data",
			(string conId, string resultData) => {
				result = "done!:" + resultData;
			},
			(conId, code, reason, autoyaStatus) => {
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
	// 	Autoya.Http_Post(
	// 		"https://httpbin.org/headers", 
	// 		"data",
	// 		(string conId, string resultData) => {
	// 			TestLogger.Log("resultData:" + resultData);
	// 			result = resultData;
	// 		},
	// 		(conId, code, reason) => {
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
		
		Autoya.Http_Post(
			"https://httpbin.org/status/404",
			"data", 
			(string conId, string resultData) => {
				// do nothing.
			},
			(conId, code, reason, autoyaStatus) => {
				resultCode = code;
			}
		);

		WaitUntil(
			() => (resultCode == 404), 
			5,
			"failed to detect 404."
		);
		
		// result should be have reason,
		Assert(resultCode == 404, "code unmatched. resultCode:" + resultCode);
	}

	[MTest] public void AutoyaHTTPPostFailWithUnauth () {
		var unauthorized = false;

		/*
			dummy server returns 401 forcibly.
		*/
		Autoya.Http_Post(
			"https://httpbin.org/status/401", 
			"dummy_data",
			(string conId, string resultData) => {
				// do nothing.
			},
			(conId, code, reason, autoyaStatus) => {
				unauthorized = autoyaStatus.isAuthFailed;
			}
		);

		WaitUntil(
			() => unauthorized,
			5
		);


		// token refresh feature is already running. wait end.
		WaitUntil(
			() => Autoya.Auth_IsAuthenticated(),
			5,
			"failed to refresh token."
		);
	}

	[MTest] public void AutoyaHTTPPostFailWithTimeout () {
		var timeoutError = string.Empty;
		/*
			fake server should be response 1msec
		*/
		Autoya.Http_Post(
			"https://httpbin.org/delay/1",
			"data",
			(string conId, string resultData) => {
				Assert(false, "got success result.");
			},
			(conId, code, reason, autoyaStatus) => {
				Assert(code == BackyardSettings.HTTP_TIMEOUT_CODE, "not match. code:" + code + " reason:" + reason);
				timeoutError = reason;
			},
			null,
			0.0001// 1ms
		);

		WaitUntil(
			() => {
				return !string.IsNullOrEmpty(timeoutError);
			}, 
			3
		);
	}
	
}
