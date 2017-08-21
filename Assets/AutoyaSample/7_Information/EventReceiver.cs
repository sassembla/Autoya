using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.Information;
using UnityEngine;

public class EventReceiver : MonoBehaviour, IUUebViewEventHandler {

     // Use this for initialization
    void Start () {
        var scrollView = GameObject.Find("Content");
        
        var url = "resources://InformationSampleRes/test.html";
        var view = UUebViewCore.GenerateSingleViewFromUrl(this.gameObject, url, scrollView.GetComponent<RectTransform>().sizeDelta);
        view.transform.SetParent(scrollView.transform, false);
    }

    void IUUebViewEventHandler.OnElementLongTapped (ContentType type, string param, string id) {
        throw new System.NotImplementedException();
        // まだ着火されない。
    }

    void IUUebViewEventHandler.OnElementTapped(ContentType type, GameObject element, string param, string id) {
        Debug.Log("element tapped:" + type + " id:" + id);
    }

    void IUUebViewEventHandler.OnLoaded () {
        // ロードが終わったタイミングでビュー高さが取得できるんだけど、ここをスマートに反映させる方法を考え中。
        var content = GameObject.Find("Content");
        var scrollViewContentRectTrans = content.GetComponent<RectTransform>();
        
        foreach (Transform trans in content.transform) {
            var childHeight = trans.GetComponent<RectTransform>().sizeDelta.y;
            scrollViewContentRectTrans.sizeDelta = new Vector2(scrollViewContentRectTrans.sizeDelta.x, childHeight);
            break;
        }

        Debug.Log("loaded.");
    }

    void IUUebViewEventHandler.OnLoadFailed (ContentType type, int code, string reason) {
        Debug.LogError("load failed:" + type + " code:" + code + " reason:" + reason);
    }

    void IUUebViewEventHandler.OnLoadStarted () {
        Debug.Log("load started.");
    }

    void IUUebViewEventHandler.OnProgress (double progress) {
        Debug.Log("loading.. progress:" + progress);
    }

    void IUUebViewEventHandler.OnUpdated () {
        Debug.Log("updated.");
    }

   
}
