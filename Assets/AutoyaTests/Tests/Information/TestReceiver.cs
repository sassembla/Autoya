using System;
using AutoyaFramework.Information;
using UnityEngine;
using UnityEngine.EventSystems;

public class TestReceiver : MonoBehaviour, IUUebViewEventHandler {
    void IUUebViewEventHandler.OnLoadProgress(double progress)
    {
        throw new NotImplementedException();
    }

    void IUUebViewEventHandler.OnContentLoaded()
    {
        GameObject.Destroy(this.gameObject);
    }

    void IUUebViewEventHandler.OnContentLoadFailed(ContentType type, int code, string reason)
    {
        throw new NotImplementedException();
    }

    void IUUebViewEventHandler.OnElementTapped(ContentType type, string param, string id)
    {
        throw new NotImplementedException();
    }

    void IUUebViewEventHandler.OnElementLongTapped(ContentType type, string param, string id)
    {
        throw new NotImplementedException();
    }
}
