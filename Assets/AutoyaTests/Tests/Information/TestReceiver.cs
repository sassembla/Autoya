using System;
using AutoyaFramework.Information;
using UnityEngine;
using UnityEngine.EventSystems;

public class TestReceiver : MonoBehaviour, IUUebViewEventHandler {
    public Action OnLoadProgress;
    public Action OnContentLoaded;
    public Action OnContentLoadFailed;
    public Action OnElementTapped;
    public Action OnElementLongTapped;
    
    void IUUebViewEventHandler.OnLoadProgress(double prog) {
        Debug.Log("OnLoadProgress");
        OnLoadProgress();
    }

    void IUUebViewEventHandler.OnContentLoaded() {
        Debug.Log("OnContentLoaded");
        if (OnContentLoaded != null) {
            OnContentLoaded();
        }
    }

    void IUUebViewEventHandler.OnContentLoadFailed(ContentType type, int code, string reason) {
        Debug.Log("OnContentLoadFailed type:" + type + " code:" + code + " reason:" + reason);
        OnContentLoadFailed();
    }

    void IUUebViewEventHandler.OnElementTapped(ContentType type, string param, string id) {
        Debug.Log("OnElementTapped");
        OnElementTapped();
    }

    void IUUebViewEventHandler.OnElementLongTapped(ContentType type, string param, string id) {
        Debug.Log("OnElementLongTapped");
        OnElementLongTapped();
    }
}
