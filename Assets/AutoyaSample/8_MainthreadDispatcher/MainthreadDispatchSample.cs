using System.Collections.Generic;
using AutoyaFramework;
using UnityEngine;


public class MainthreadDispatchSample : MonoBehaviour {
    void Awake () {
        var http = new AutoyaFramework.Connections.HTTP.HTTPConnection();
        var httpCoroutine = http.Get(
            "newConnectionId",
            new Dictionary<string, string>(),
            "https://google.com",
            (conId, code, responseHeaders, data) => {
                // succeeded.
                Debug.Log("data:" + data);
            },
            (conId, code, reason, responseHeaders) => {
                // failed.
                Debug.Log("code:" + code + " reason:" + reason);
            }
        );

        Autoya.Mainthread_Commit(httpCoroutine);
    }
}