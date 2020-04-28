using System;
using System.Collections;
using System.IO;
using AutoyaFramework;
using AutoyaFramework.Settings.EndPoint;
using Miyamasu;
using NUnit.Framework;
using UnityEngine;

/**
    EndPointImplementation retry test.
 */
public class EndPointImplementationRetryTests : MiyamasuTestRunner
{
    private string baseUrl;

    [MSetup]
    public IEnumerator Setup()
    {
        baseUrl = EndPointSelectorSettings.ENDPOINT_INFO_URL;
        while (!Autoya.Auth_IsAuthenticated())
        {
            yield return null;
        }
    }

    [MTeardown]
    public void Teardown()
    {
        EndPointSelectorSettings.ENDPOINT_INFO_URL = baseUrl;
    }


    [MTest]
    public IEnumerator EndPointUpdateRetry()
    {
        // 必ず失敗するURLを実行
        EndPointSelectorSettings.ENDPOINT_INFO_URL = "https://127.0.0.1/" + Guid.NewGuid().ToString("N");

        // Autoyaの起動と、起動直後にEndPointの更新、
        // Retryするかどうかのポイントをリトライが完了したかどうかの確認ポイントとして、一度実行されたら更新が発生するようにセットする
        // こうすることで、リトライの直後にURLが書き換わり、認証まで終わるのを待てばEpの更新が完了する。
        var dataPath = Application.persistentDataPath;
        Autoya.TestEntryPoint(dataPath);

        Autoya.Debug_OnEndPointInstanceRequired(
            () =>
            {
                return new AutoyaFramework.EndPointSelect.IEndPoint[] { new main(), new sub() };
            }
        );

        var done = false;
        Autoya.Debug_SetShouldRetryEndPointGetRequest(
            () =>
            {
                done = true;
                return true;
            }
        );

        while (!done)
        {
            yield return null;
        }

        // change to valid url.
        EndPointSelectorSettings.ENDPOINT_INFO_URL = "https://raw.githubusercontent.com/sassembla/Autoya/master/Assets/AutoyaTests/RuntimeData/EndPoints/mainAndSub.json";
        while (!Autoya.Auth_IsAuthenticated())
        {
            yield return null;
        }

        // epUpdate succeeded and updated.
        var mainEp = Autoya.EndPoint_GetEndPoint<main>();
        Assert.True(mainEp.key0 == "val0", "not match. mainEp.key0:" + mainEp.key0);
        Assert.True(mainEp.key1 == "default_val1", "not match. mainEp.key1:" + mainEp.key1);

        var subEp = Autoya.EndPoint_GetEndPoint<sub>();
        Assert.True(subEp.key0 == "default_val0", "not match. subEp.key0:" + subEp.key0);
    }

}