using AutoyaFramework;
using Miyamasu;
using UnityEngine;

using Connection;

/**
	tests for HTTP.
	Autoya strongly handle these server-related errors which comes from game-server.
*/
public class HTTPTests : MiyamasuTestRunner {

	[MTest] public bool HTTPGet () {
		Autoya.EntryPoint();

		var result = string.Empty;
		
		var connectionId = Autoya.HttpGet(
			"http://httpbin.org/get", 
			(string conId, string resultData) => {
				result = resultData;
			},
			(string conId, int code, string reason) => {
				result = reason;
			}
		);
		
		var wait = WaitUntil(
			() => !string.IsNullOrEmpty(result), 
			1
		);
		
		if (!wait) return false; 
		
		return true;
	}



	[MTest] public bool HTTPSGet () {
		Autoya.EntryPoint();

		var result = string.Empty;
		var connectionId = Autoya.HttpGet(
			"https://httpbin.org/get", 
			(string conId, string resultData) => {
				result = resultData;
			},
			(string conId, int code, string reason) => {
				result = reason;
			}
		);

		var wait = WaitUntil(
			() => !string.IsNullOrEmpty(result), 
			1
		);
		if (!wait) return false; 
		
		return true;
	}

	[MTest] public bool HTTPGetFailWith404 () {
		Autoya.EntryPoint();
		var resultCode = 0;
		
		var connectionId = Autoya.HttpGet(
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
			1
		);
		if (!wait) return false; 
		
		// result should be have reason,
		Assert(resultCode == 404, "code note match. resultCode:" + resultCode);

		return true;
	}

	[MTest] public bool HTTPGetFailWithUnauth () {
		Autoya.EntryPoint();
		
		var unauthReason = string.Empty;

		// set unauthorized method callback.
		Autoya.SetOnAuthFailed(
			(conId, reason) => {
				unauthReason = reason;
				
				// if want to start re-login, return true.
				return true;
			}
		);
		
		var connectionId = Autoya.HttpGet(
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
			1
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
