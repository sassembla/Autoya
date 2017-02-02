using System.Collections;
using System.Collections.Generic;
using AutoyaFramework;
using UnityEngine;
using AutoyaFramework.Connections.HTTP;

public class Information : MonoBehaviour {

	// Use this for initialization
	void Start () {
		var http = new HTTPConnection();
		var con = http.Get(
			"info",
			new Dictionary<string, string>(),
			"https://raw.githubusercontent.com/sassembla/Autoya/master/README.md",
			(conId, code, respHeader, data) => {
				Debug.Log("markdown:" + data);
				Autoya.Info_ConstructMarkdownView(data, id => {});
			},
			(conId, code, reason, respHeader) => {
				Debug.LogError("failed to dl, reason:" + reason);
			}
		);

		StartCoroutine(con);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
