using AutoyaFramework;
using Miyamasu;
using UnityEngine;


/**
	tests for HTTP.
*/
public class HTTPTests : MiyamasuTestRunner {

	[MTest] public bool HTTPGet () {
		Autoya.EntryPoint();

		var result = string.Empty;
		Debug.LogError("send");
		var connectionId = Autoya.HttpGet(
			"http://google.com", 
			(string conId, string resultData) => {
				result = resultData;
			},
			(string conId, int code, string reason) => {
				result = reason;
			}
		);
		
		var wait = WaitUntil(
			"HTTPGet", 
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
			"https://google.com", 
			(string conId, string resultData) => {
				result = resultData;
			},
			(string conId, int code, string reason) => {
				result = reason;
			}
		);

		var wait = WaitUntil(
			"HTTPSGet", 
			() => !string.IsNullOrEmpty(result), 
			1
		);
		if (!wait) return false; 
		
		return true;
	}
	
	[MTest] public bool HTTPPost () {
		Autoya.EntryPoint();

		var result = string.Empty;
		var connectionId = Autoya.HttpPost(
			"http://google.com", 
			"sampleDataString",
			(string conId, string resultData) => {
				result = resultData;
			},
			(string conId, int code, string reason) => {
				result = reason;
			}
		);

		var wait = WaitUntil(
			"HTTPPost", 
			() => !string.IsNullOrEmpty(result), 
			1
		);
		if (!wait) return false; 
		
		return true;
	}

	[MTest] public bool HTTPSPost () {
		Autoya.EntryPoint();

		var result = string.Empty;
		var connectionId = Autoya.HttpPost(
			"https://google.com", 
			"sampleDataString",
			(string conId, string resultData) => {
				result = resultData;
			},
			(string conId, int code, string reason) => {
				result = reason;
			}
		);

		var wait = WaitUntil(
			"HTTPSPost", 
			() => !string.IsNullOrEmpty(result), 
			1
		);
		if (!wait) return false; 
		
		return true;
	}
}
