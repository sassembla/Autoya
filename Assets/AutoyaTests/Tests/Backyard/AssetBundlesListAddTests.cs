// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using AutoyaFramework;
// using AutoyaFramework.AssetBundles;
// using AutoyaFramework.Settings.AssetBundles;
// using AutoyaFramework.Settings.Auth;
// using Miyamasu;
// using UnityEngine;
// using UnityEngine.SceneManagement;

// public class AssetBundlesListAddTests : MiyamasuTestRunner
// {

//     private string abListDlPath = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles/";

//     [MSetup]
//     public IEnumerator Setup()
//     {
//         var discarded = false;

//         // delete assetBundleList anyway.
//         Autoya.AssetBundle_DiscardAssetBundleList(
//             () =>
//             {
//                 discarded = true;
//             },
//             (code, reason) =>
//             {
//                 switch (code)
//                 {
//                     case Autoya.AssetBundlesError.NeedToDownloadAssetBundleList:
//                         {
//                             discarded = true;
//                             break;
//                         }
//                     default:
//                         {
//                             Fail("code:" + code + " reason:" + reason);
//                             break;
//                         }
//                 }
//             }
//         );

//         yield return WaitUntil(
//             () => discarded,
//             () => { throw new TimeoutException("too late."); }
//         );

//         var listExists = Autoya.AssetBundle_IsAssetBundleFeatureReady();
//         True(!listExists, "exists, not intended.");

//         True(Caching.ClearCache(), "failed to clean cache.");

//         Autoya.Debug_Manifest_RenewRuntimeManifest();
//     }
//     [MTeardown]
//     public IEnumerator Teardown()
//     {
//         Autoya.AssetBundle_UnloadOnMemoryAssetBundles();


//         var discarded = false;

//         // delete assetBundleList anyway.
//         Autoya.AssetBundle_DiscardAssetBundleList(
//             () =>
//             {
//                 discarded = true;
//             },
//             (code, reason) =>
//             {
//                 switch (code)
//                 {
//                     case Autoya.AssetBundlesError.NeedToDownloadAssetBundleList:
//                         {
//                             discarded = true;
//                             break;
//                         }
//                     default:
//                         {
//                             Fail("code:" + code + " reason:" + reason);
//                             break;
//                         }
//                 }
//             }
//         );

//         yield return WaitUntil(
//             () => discarded,
//             () => { throw new TimeoutException("too late."); }
//         );

//         var listExists = Autoya.AssetBundle_IsAssetBundleFeatureReady();
//         True(!listExists, "exists, not intended.");

//         True(Caching.ClearCache());
//     }

//     [MTest]
//     public IEnumerator GetAssetBundleList()
//     {
//         var done = false;
//         Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
//             status =>
//             {
//                 done = true;
//             },
//             (code, reason, asutoyaStatus) =>
//             {
//                 Debug.Log("GetAssetBundleList failed, code:" + code + " reason:" + reason);
//                 // do nothing.
//             }
//         );

//         yield return WaitUntil(
//             () => done,
//             () => { throw new TimeoutException("faild to get assetBundleList."); }
//         );



//         /*
//             動的リスト更新について解決したい事象はどんなもんか。
//             ・サーバ側からトリガーが来ることで、updateを行う
//             ・サーバ側からトリガーが来ることで、新たなリストの取得を行う
//             これらのトリガーは別の方がいい気はする。理想のトリガーってどんなだろ
//             理想のトリガーは、やはり「サーバが適当なレスポンスを返してきた時」なんだけど、
//             このリストは別れてた方がいいんだ。これはこれで解決。ToDoとして実現できると思う。

//             テストしたい内容は、
//             ・起動した時classとpersistは内容が一致してる
//             ・その後、class情報が書き換わる
//             ・class情報が書き換わったあとにpersistとの差分にいつ気がつくか

//             というあたりで、これテストできるのかな、、起動時なので起動をやり直せばいいが、まーーやっぱテストできんな諦めよ。

//         */

//         var resversionDesc = AuthSettings.AUTH_RESPONSEHEADER_RESVERSION;
//         Autoya.Http_Get(
//                 "https://httpbin.org/response-headers?" + resversionDesc + "=unmanaged:1.0.0",
//                 (conId, data) =>
//                 {
//                     Debug.Log("レスポンスを得た data:" + data);

//                     // 成功するが、updateは発生しない。jsonに存在しないため。
//                 },
//                 (conId, code, reason, status) =>
//                 {
//                     Debug.Log("これは失敗するはず");
//                 }
//             );

//         while (true)
//         {
//             yield return null;
//         }
//     }



// }