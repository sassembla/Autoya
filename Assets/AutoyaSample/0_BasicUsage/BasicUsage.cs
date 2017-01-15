using UnityEngine;
using System.Collections;
using AutoyaFramework;
using System;

/*
	basic usage of autoya.
	authentication sequence is running in background. nothing to do for login.
*/
public class BasicUsage : MonoBehaviour {
	
	IEnumerator Start () {
		/*
			authentication is running.
			wait finish of the authentication.
		*/
		var authenticated = false;

		Action done = () => {
			authenticated = true;
		};

		/*
			set the action to be called when authentication succeeds.
		*/
		Autoya.Auth_SetOnAuthenticated(done);

		while (!authenticated) {
			yield return null;
		}

		Debug.Log("login is done! welcome to Autoya.");
	}

	void Update () {
		// let's type "Autoya." ,
		// maybe autocompletion tells you something luckey.

		// Autoya.
	}
}
