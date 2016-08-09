using UnityEngine;
using System.Collections;
using AutoyaFramework;
using System;

public class BasicUsage : MonoBehaviour {

	void Awake () {
		Debug.LogError("awakeしてる");
		var loggedIn = Autoya.Auth_IsLoggedIn();
		Debug.LogError("loggedIn:" + loggedIn);
		
		Action Login = () => {
			Debug.LogError("login is over.");
		};

		Autoya.Auth_SetOnAuthSucceeded(Login);
	}
	// Use this for initialization
	IEnumerator Start () {
		while (!Autoya.Auth_IsLoggedIn()) {
			yield return null;
		}
		Debug.LogError("login overed.");
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
