using System.Collections.Generic;
using AutoyaFramework;
using UnityEngine;

public class Connections : MonoBehaviour {

	// Use this for initialization
	void Start () {
		var connectionId1 = Autoya.Http_Get(
			"https://httpbin.org/get",		// url
			(conId, data) => {				// on succeeded
				Debug.Log("get data:" + data);
			},
			(conId, code, reason) => {		// on failed
				Debug.LogError("code:" + code + " reason:" + reason);
			},
			new Dictionary<string, string>(),// headers
			3.0 							// timeout
		);

		var postData = "hello world.";
		var connectionId2 = Autoya.Http_Post(
			"https://httpbin.org/post",
			postData,
			(conId, resultData) => {
				Debug.Log("post data:" + resultData);
			},
			(conId, code, reason) => {
				Debug.LogError("code:" + code + " reason:" + reason);
			}
		);
	}
}
