using AutoyaFramework;
using Miyamasu;

/**
	tests for HTTP.
*/
public class HTTPTests : MiyamasuTestRunner {

	[MTest] public bool HTTPGet () {
		Autoya.EntryPoint();

		var result = string.Empty;
		var connectionId = Autoya.HttpGet(
			"https://google.com", 
			(string conId, string resultData) => {
				result = resultData;
			},
			(string conId, int code, string reason) => {
				
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
	
	// [MTest] public bool HTTPPost () {
	// 	Autoya.EntryPoint();

	// 	var result = string.Empty;
	// 	var connectionId = Autoya.HttpPost(
	// 		"https://google.com", 
	// 		"sampleDataString",
	// 		(string conId, string resultData) => {
	// 			result = resultData;
	// 		},
	// 		(string conId, int code, string reason) => {
				
	// 		}
	// 	);

	// 	var wait = WaitUntil(
	// 		"HTTPPost", 
	// 		() => !string.IsNullOrEmpty(result), 
	// 		1
	// 	);
	// 	if (!wait) return false; 
		
	// 	return true;
	// }
}
