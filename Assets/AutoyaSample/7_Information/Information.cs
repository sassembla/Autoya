using System.Collections.Generic;
using AutoyaFramework;
using UnityEngine;
using AutoyaFramework.Connections.HTTP;
using UnityEngine.UI;

public class Information : MonoBehaviour {
	public InputField inputField;
	public string url;

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
			Scroll view のcontentを雑に操作している。そのうち綺麗にしたい。
		 */
		var scrollViewContent = GameObject.Find("Content");
		var scrollContentRect = scrollViewContent.GetComponent<RectTransform>();
		
		var viewSize = new Vector2(400 ,300);
		
		Autoya.Info_Show(
			url,// html or markdonw data url.
			"MyInfoView",// name of view. related with uGUI created custom tag and depth name.
			viewSize.x,// view width.
			viewSize.y,// view height.
			0,// y anchor.
			viewObj => {
				this.informationView = viewObj;
				// add information obj to scroll view.
				this.informationView.transform.SetParent(scrollViewContent.transform, false);
			},
			layoutDone => {
				// fire when layout changed.
				// set height to scroll view's content.
				scrollContentRect.sizeDelta = new Vector2(scrollContentRect.sizeDelta.x, layoutDone.height);
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

/*	sample html:

<!--depth asset list url(resources://Views/MyInfoView/DepthAssetList)-->

<div>
	<p>Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Aenean commodo ligula eget dolor. Aenean massa. Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Donec quam felis, ultricies nec, pellentesque eu, pretium quis, sem. Nulla consequat massa quis enim. Donec pede justo, fringilla vel, aliquet nec, vulputate eget, arcu. In enim justo, rhoncus ut, imperdiet a, venenatis vitae, justo. Nullam dictum felis eu pede mollis pretium. Integer tincidunt. Cras dapibus. Vivamus elementum semper nisi. Aenean vulputate eleifend tellus. Aenean leo ligula, porttitor eu, consequat vitae, eleifend ac, enim. Aliquam lorem ante, dapibus in, viverra quis, feugiat a, tellus. Phasellus viverra nulla ut metus varius laoreet. Quisque rutrum. Aenean imperdiet. Etiam ultricies nisi vel augue. Curabitur ullamcorper ultricies nisi. Nam eget dui. Etiam rhoncus. Maecenas tempus, tellus eget condimentum rhoncus, sem quam semper libero, sit amet adipiscing sem neque sed ipsum. Nam quam nunc, blandit vel, luctus pulvinar, hendrerit id, lorem. Maecenas nec odio et ante tincidunt tempus. Donec vitae sapien ut libero venenatis faucibus. Nullam quis ante. Etiam sit amet orci eget eros faucibus tincidunt. Duis leo. Sed fringilla mauris sit amet nibh. Donec sodales sagittis magna. Sed consequat, leo eget bibendum sodales, augue velit cursus nunc,</p>
</div>
<p> </p>
<div>
	<p>something2</p>
</div>
<p> </p>
<div>
	<p>something3</p>
</div>
<p> </p>
<div>
	<p>something4</p>
</div>
<p> </p>
<p> </p>

*/		
	}
}
