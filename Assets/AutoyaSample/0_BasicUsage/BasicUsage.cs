using UnityEngine;
using System.Collections;
using AutoyaFramework;
using System;

public class BasicUsage : MonoBehaviour {

	void Awake () {
		var loggedIn = Autoya.Auth_IsLoggedIn();
		
		Action Login = () => {
			Debug.Log("login is done.");
		};

		Autoya.Auth_SetOnLoginSucceeded(Login);
	}
	// Use this for initialization
	IEnumerator Start () {
		while (!Autoya.Auth_IsLoggedIn()) {
			yield return null;
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
