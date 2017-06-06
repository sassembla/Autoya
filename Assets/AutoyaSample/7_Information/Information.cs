using System.Collections.Generic;
using AutoyaFramework;
using UnityEngine;
using AutoyaFramework.Connections.HTTP;

public class Information : MonoBehaviour {

	// Use this for initialization
	void Start () {
		var scrollViewContent = GameObject.Find("Content");
		var scrollContentRect = scrollViewContent.GetComponent<RectTransform>();
		
		scrollContentRect.sizeDelta = new Vector2(300,300);
		
		Autoya.Info_Show(
			"https://raw.githubusercontent.com/sassembla/Autoya/master/README.md",
			scrollContentRect.sizeDelta.x,// view width.
			scrollContentRect.sizeDelta.y,// view height.
			0,// y anchor.
			viewObj => {
				// add information obj to scroll view.
				viewObj.transform.SetParent(scrollViewContent.transform, false);

				var contentHeight = viewObj.GetComponent<RectTransform>().sizeDelta.y;

				// set height.
				scrollContentRect.sizeDelta = new Vector2(scrollContentRect.sizeDelta.x, contentHeight);
			},
			(a, b) => {

			}
		);
	}
}
