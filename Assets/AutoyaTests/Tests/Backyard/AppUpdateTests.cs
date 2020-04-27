using System;
using System.Collections;
using AutoyaFramework;
using Miyamasu;

/**
    appVersionが変わったという情報をサーバが流してきた場合、
    特定のハンドラでそれを受けて、メソッドを実行する。
 */
public class AppUpdateTests : MiyamasuTestRunner
{

    [MSetup]
    public IEnumerator Setup()
    {
        while (!Autoya.Auth_IsAuthenticated())
        {
            yield return null;
        }
    }

    [MTest]
    public IEnumerator ReceiveAppUpdate()
    {
        var done = false;

        Autoya.Debug_SetOverridePoint_OnNewAppRequested(
            newAppVer =>
            {
                done = true;
            }
        );

        Autoya.Http_Get(
            "https://httpbin.org/response-headers?appversion=1.0.1",
            (conId, data) =>
            {
                done = true;
            },
            (conid, code, reason, autoyaStatus) =>
            {

            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("too late"); }
        );
    }
}