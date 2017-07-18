using System.Collections.Generic;
using AutoyaFramework;
using UnityEngine;
using AutoyaFramework.Connections.HTTP;
using UnityEngine.UI;

public class Information : MonoBehaviour {
	public InputField inputField;
	public string url = "https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/Information/MyView.html";

	// sample information view content instance.
	private GameObject informationView;

	// Use this for initialization
	void Start () {
		Input(url);
	}

	private void ShowInputWindow () {
		inputField.gameObject.SetActive(true);
	}

	public void Reload (string newUrl) {
		// destory view instance.
		Destroy(informationView);

		url = newUrl;
		Debug.LogError("url:" + url);
		Input(url);
	}
	
	private void Input (string result) {
		/*
			scroll view にInformationを入れるサンプル
		 */
		var scrollViewContent = GameObject.Find("Content");
		var scrollContentRect = scrollViewContent.GetComponent<RectTransform>();
		
		var viewSize = new Vector2(300,300);

		Autoya.Info_Show(
			url,// html or markdonw data url.
			"MyView",// name of view. related with uGUI created custom tag and depth name.
			viewSize.x,// view width.
			viewSize.y,// view height.
			0,// y anchor.
			viewObj => {
				this.informationView = viewObj;
				// add information obj to scroll view.
				informationView.transform.SetParent(scrollViewContent.transform, false);

				// get content height.
				var contentHeight = viewObj.GetComponent<RectTransform>().sizeDelta.y;

				// set height to scroll view's content.
				scrollContentRect.sizeDelta = new Vector2(scrollContentRect.sizeDelta.x, contentHeight);
			},
			progress => {
				Debug.Log("progress:" + progress);
			},
			() => {
				Debug.Log("view load done.");
				ShowInputWindow();
			},
			(code, reason) => {
				Debug.Log("view load failed, code:" + code + " reason:" + reason);
			}
		);
	}
}
