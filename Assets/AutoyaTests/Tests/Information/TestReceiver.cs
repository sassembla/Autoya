using System;
using System.Collections;
using System.Collections.Generic;
using UUebView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TestReceiver : MonoBehaviour, IUUebViewEventHandler {
    public Action OnLoadStarted;
    public Action OnProgress;
    public Action OnLoaded;
    public Action OnUpdated;
    public Action OnContentLoadFailed;
    public Action OnElementTapped;
    public Action OnElementLongTapped;
    
    void IUUebViewEventHandler.OnLoadStarted() {
        Debug.Log("OnLoadStarted");
        if (OnLoadStarted != null) {
            OnLoadStarted();
        }
    }

    void IUUebViewEventHandler.OnProgress(double progress) {
        Debug.Log("OnProgress progress:" + progress);
        if (OnProgress != null) {
            OnProgress();
        }
    }

    void IUUebViewEventHandler.OnLoaded(string[] treeIds) {
        Debug.Log("OnLoaded");
        if (OnLoaded != null) {
            OnLoaded();
        }
    }

    void IUUebViewEventHandler.OnUpdated(string[] newTreeIds) {
        Debug.Log("OnUpdated");
        if (OnUpdated != null) {
            OnUpdated();
        }
    }

    void IUUebViewEventHandler.OnLoadFailed(ContentType type, int code, string reason) {
        Debug.Log("OnContentLoadFailed type:" + type + " code:" + code + " reason:" + reason);
        if (OnContentLoadFailed != null) {
            OnContentLoadFailed();
        }
    }

    void IUUebViewEventHandler.OnElementTapped(ContentType type, GameObject element, string param, string id) {
        Debug.Log("OnElementTapped type:" + type + " param:" + param + " id:" + id);
        if (OnElementTapped != null) {
            OnElementTapped();
        }
        
        switch (type) {
            case ContentType.IMAGE: {
                if (element != null) {
                    var img = element.GetComponent<Image>();
                    StartCoroutine(Rotate(img));
                }
                break;
            }
        }
    }

    private IEnumerator Rotate (Image img) {
        var rectTrans = img.GetComponent<RectTransform>();
        var count = 0;
        var max = 18;
        var diff = new Vector3(0, 0, -(180f / max));

        var start = rectTrans.rotation;

        while (true)
        {
            rectTrans.Rotate(diff);
            count++;

            if (count == max)
            {
                break;
            }
            yield return null;
        }

        rectTrans.rotation = Quaternion.Euler(0, 0, start.eulerAngles.z - 180f);
    }

    void IUUebViewEventHandler.OnElementLongTapped(ContentType type, string param, string id) {
        Debug.Log("OnElementLongTapped");
        if (OnElementLongTapped != null) {
            OnElementLongTapped();
        }
    }

    
}
