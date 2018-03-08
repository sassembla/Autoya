using System.Collections;
using System.Collections.Generic;
using AutoyaFramework;
using UnityEngine;

/**
	send http request to dummy server with authorized header.
 */
public class HttpConnections : MonoBehaviour
{

    // Use this for initialization
    IEnumerator Start()
    {
        Autoya.Auth_SetOnBootAuthFailed(
            (code, reason) =>
            {
                Debug.Log("code:" + code + " reason:" + reason);
            }
        );

        while (!Autoya.Auth_IsAuthenticated())
        {
            Debug.Log("waiting..");
            yield return null;
        }

        var connectionId1 = Autoya.Http_Get(
            "https://httpbin.org/get",      // url
            (conId, data) =>
            {// on succeeded
                Debug.Log("get data:" + data);
            },
            (conId, code, reason, autoyaStatus) =>
            {// on failed
                Debug.LogError("code:" + code + " reason:" + reason);
            },
            new Dictionary<string, string>(),// headers
            3.0                             // timeout
        );
        Debug.Log("start get with connectionId:" + connectionId1);

        var postData = "hello world.";
        var connectionId2 = Autoya.Http_Post(
            "https://httpbin.org/post",
            postData,
            (conId, resultData) =>
            {
                Debug.Log("post data:" + resultData);
            },
            (conId, code, reason, autoyaStatus) =>
            {
                Debug.LogError("code:" + code + " reason:" + reason);
            }
        );
        Debug.Log("start post with connectionId:" + connectionId2);
    }
}
