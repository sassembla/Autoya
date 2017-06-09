using System.Collections.Generic;
using AutoyaFramework;
using UnityEngine;
using AutoyaFramework.Connections.HTTP;

public class Information : MonoBehaviour {

	// Use this for initialization
	void Start () {

		/*
			scroll view にInformationを入れるサンプル
		 */
		var scrollViewContent = GameObject.Find("Content");
		var scrollContentRect = scrollViewContent.GetComponent<RectTransform>();
		
		var viewSize = new Vector2(300,300);

		Autoya.Info_Show(
			"https://raw.githubusercontent.com/sassembla/Autoya/master/README.md",// html or markdonw data url.
			viewSize.x,// view width.
			viewSize.y,// view height.
			0,// y anchor.
			viewObj => {
				// add information obj to scroll view.
				viewObj.transform.SetParent(scrollViewContent.transform, false);

				// get content height.
				var contentHeight = viewObj.GetComponent<RectTransform>().sizeDelta.y;

				// set height to scroll view's content.
				scrollContentRect.sizeDelta = new Vector2(scrollContentRect.sizeDelta.x, contentHeight);
			},
			(a, b) => {

			}
		);
	}
}
