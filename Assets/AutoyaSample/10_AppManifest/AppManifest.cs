using System.Collections;
using System.Collections.Generic;
using AutoyaFramework;
using UnityEngine;

public class AppManifest : MonoBehaviour {

	private Dictionary<string, string> appManifestParams = new Dictionary<string, string>();

	/*
		get app manifest.
	 */
	void Start () {
		appManifestParams = Autoya.Manifest_GetAppManifest();
		foreach (var man in appManifestParams) {
			Debug.Log("key:" + man.Key + " val:" + man.Value);
		}
	}

	/*
		show manifest parameters.
	 */
	void OnGUI () {
		foreach (var appManifestParam in appManifestParams) {
			GUILayout.Label(appManifestParam.Key + " : " + appManifestParam.Value);
		}
	}
	
}
