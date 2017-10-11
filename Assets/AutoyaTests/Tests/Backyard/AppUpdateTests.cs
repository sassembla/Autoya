using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework;
using AutoyaFramework.Settings.AssetBundles;
using AutoyaFramework.Settings.Auth;
using Miyamasu;
using UnityEngine;

/**
    appVersionが変わったという情報をサーバが流してきた場合、
    特定のハンドラでそれを受けて、メソッドを実行する。

    そんだけだね。buildManifestと比較して確認するだけでよさげ。
    OverridePointsに一個追加。
 */
public class AppUpdateTests : MiyamasuTestRunner {
    
    [MSetup] public IEnumerator Setup () {
        while (!Autoya.Auth_IsAuthenticated()) {
            yield return null;
        }
    }
    
    [MTest] public IEnumerator ReceiveAppUpdate () {
        var done = false;

        Autoya.Debug_SetOverridePoint_OnNewAppRequested(
            newAppVer => {
                done = true;
            }
        );

        Autoya.Http_Get(
            "https://httpbin.org/response-headers?appversion=1.0.1", 
            (conId, data) => {
                done = true;
            },
            (conid, code, reason, autoyaStatus) => {

            }
        );

        yield return WaitUntil(
            () => done,
            () => {throw new TimeoutException("too late");}
        );
    }
}