using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.Information;
using UnityEngine;

public class ViewGen : MonoBehaviour {

	// Use this for initialization
	void Start () {
		var parent = GameObject.Find("Image") as GameObject;
		var parentRect = parent.GetComponent<RectTransform>();
		var parentWidth = parentRect.sizeDelta.x;
		var parentHeight = parentRect.sizeDelta.y;
		Debug.LogError("parentWidth:" + parentWidth);


		var res = Resources.Load("Views/ViewGenTest/DepthAssetList") as TextAsset;
		var jsonStr = res.text;
		Debug.LogError("jsonStr:" + jsonStr);
		
		var posList = JsonUtility.FromJson<CustomTagList>(jsonStr);
		var gameObj = new GameObject("test");

		var pos = posList.layerConstraints[0].constraints[0].rect;
		
		var trans = gameObj.AddComponent<RectTransform>();
		trans.SetParent(parent.transform);
		
		// これらの値を使わずに、親の値からこれらのパラメータを再現する。
		// 結果値であるoffsetとwidthが出せると良い。
		trans.anchoredPosition = pos.anchoredPosition;
		trans.sizeDelta = pos.sizeDelta;
		trans.anchorMin = pos.anchorMin;
		trans.anchorMax = pos.anchorMax;
		trans.pivot = pos.pivot;
		trans.offsetMin = pos.offsetMin;
		trans.offsetMax = pos.offsetMax;

		/*
			決定順番としては、
			親のサイズ -> アンカー位置 -> オフセット位置 で決まって行く感じか。
			
			で、
			アンカー：
				これは親のwidthを割合として保持する。
				min 0.1だったら、親の長さの10%の位置を保持する。
				max 0.9だったら、親の長さの90%の位置を保持する。

			オフセット：
				アンカーよりも内側にオブジェクトが入る場合、
				trans.offsetMin.xには+。
				trans.offsetMax.xには-の値が入る。


			pivotはあくまで回転中心。
		 */
		var childViewRect = ParsedTree.GetChildViewRectFromParentRectTrans(parentWidth, parentHeight, pos);
		var anchorWidth = (parentWidth * pos.anchorMin.x) + (parentWidth * (1 - pos.anchorMax.x));
		var anchorHeight = (parentHeight * pos.anchorMin.y) + (parentHeight * (1 - pos.anchorMax.y));

		var viewWidth = parentWidth - anchorWidth - pos.offsetMin.x + pos.offsetMax.x;
		var viewHeight = parentHeight - anchorHeight - pos.offsetMin.y + pos.offsetMax.y;
		
		Debug.LogError("pos.offsetMax.y:" + pos.offsetMax.y);
		
		var offsetX = (parentWidth * pos.anchorMin.x) + pos.offsetMin.x;
		var offsetY = (parentHeight * (1-pos.anchorMax.y)) - (pos.offsetMax.y);
		
		Debug.LogError("offsetX:" + offsetX + " offsetY:" + offsetY + " viewWidth:" + viewWidth + " viewHeight:" + viewHeight);
		Debug.LogError("childViewRect:" + childViewRect);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
