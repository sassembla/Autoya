using System;
using System.Collections;
using System.Linq;
using AutoyaFramework;
using AutoyaFramework.Settings.AssetBundles;
using AutoyaFramework.Settings.Auth;
using Miyamasu;

/**
    resVersionが変わったという情報をサーバが流してきた場合、
    AssetBundlesImpl側でその情報を取得、リストのロードを行う。
    この間、既存のAssetはすべて影響を受けそう。
    ・どんなイベントを提供するか
    
        ・状況判断を受け取って
            使っているAssetが更新される内容のリストが更新される予定。
            使っているAssetはないがリストが更新される予定。

        ・enumを返す
            yes リストの更新を行っていいよ
                リストの更新が行われる。
                既存のAssetについてどういう選択肢が取れる？
                    ・使っている部分がある場合、そのAssetをそのまま使い続けることができる。
                    ・現在使っているものを全部消して特定のstateに行くこともできる+そこでpreloadを使うのもあり。

                    再度ロードした時には新しいものがゲットできるようにする(これはできそう、条件付きのキャッシュ破棄予約)
                    まだロードしてない部分については単に取得し直しになるだけ、このへんはPreloadで対応してくれてれば文句ない。

                    ということで、preloadで対応できれば本当に平気そう。

            no  リストの更新を今は無視するよ(どうせあとですぐまた通知がくる。
                gameStateに関連して無視すればいいというスンポー。
                未切り替えリストがある場合はオンメモリに保持しておく。同じレスポンスが来た時にそのままイベント着火となる。
        
        その上で、画面遷移をこの中 or 後ろで継続して実行してもらう。
 */
public class AssetUpdateTests : MiyamasuTestRunner
{
    private string abListPath = "https://raw.githubusercontent.com/sassembla/Autoya/assetbundle_multi_list_support/AssetBundles/";

    private const string resversionDesc = AuthSettings.AUTH_RESPONSEHEADER_RESVERSION;

    [MSetup]
    public IEnumerator Setup()
    {
        var discarded = false;

        // delete assetBundleList anyway.
        Autoya.AssetBundle_DiscardAssetBundleList(
            () =>
            {
                discarded = true;
            },
            (code, reason) =>
            {
                switch (code)
                {
                    case Autoya.AssetBundlesError.NeedToDownloadAssetBundleList:
                        {
                            discarded = true;
                            break;
                        }
                    default:
                        {
                            Fail("code:" + code + " reason:" + reason);
                            break;
                        }
                }
            }
        );

        yield return WaitUntil(
            () => discarded,
            () => { throw new TimeoutException("too late."); }
        );

        var listExists = Autoya.AssetBundle_IsAssetBundleFeatureReady();
        True(!listExists, "exists, not intended.");
    }
    [MTeardown]
    public IEnumerator Teardown()
    {
        var discarded = false;

        // delete assetBundleList anyway.
        Autoya.AssetBundle_DiscardAssetBundleList(
            () =>
            {
                discarded = true;
            },
            (code, reason) =>
            {
                Fail("code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => discarded,
            () => { throw new TimeoutException("too late."); }
        );

        var listExists = Autoya.AssetBundle_IsAssetBundleFeatureReady();
        True(!listExists, "exists, not intended.");
    }



    private Autoya.ShouldRequestOrNot RequestYes(string identity, string newVersion)
    {
        var basePath = Autoya.Manifest_LoadRuntimeManifest().resourceInfos.Where(resInfo => resInfo.listIdentity == identity).FirstOrDefault().listDownloadUrl;
        var url = basePath + "/" + identity + "/" + AssetBundlesSettings.PLATFORM_STR + "/" + newVersion + "/" + identity + ".json";
        return Autoya.ShouldRequestOrNot.Yes(url);
    }

    private Autoya.ShouldRequestOrNot RequestNo(string newVersion)
    {
        return Autoya.ShouldRequestOrNot.No();
    }



    [MTest]
    public IEnumerator ReceiveFirstList()
    {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
            abListPath + "main_assets/" + AssetBundlesSettings.PLATFORM_STR + "/1.0.0/main_assets.json",
            status =>
            {
                done = true;
            },
            (code, reason, autoyaStatus) =>
            {
                // do nothing.
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        // リスト1.0.0が保持されている。
        True(Autoya.Debug_AssetBundle_FeatureState() == Autoya.AssetBundlesFeatureState.Ready);
        True(Autoya.AssetBundle_AssetBundleLists()[0].version == "1.0.0");
    }


    [MTest]
    public IEnumerator ReceiveListUpdated()
    {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
            abListPath + "main_assets/" + AssetBundlesSettings.PLATFORM_STR + "/1.0.0/main_assets.json",
            status =>
            {
                done = true;
            },
            (code, reason, autoyaStatus) =>
            {
                // do nothing.
                Fail("code:" + code + " reason:" + reason);
            }
        );


        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        // リスト1.0.0が保持されている。
        // 通信のレスポンスヘッダーに特定の値が含まれていることで、listの更新リクエストを送り出す機構を着火する。

        // 新しいリストの取得判断の関数をセット(レスポンスを捕まえられるはず)
        var listWillBeDownloaded = false;
        Autoya.Debug_SetOverridePoint_ShouldRequestNewAssetBundleList(
            (identity, newVersion) =>
            {
                listWillBeDownloaded = true;
                True(newVersion == "1.0.1");
                return RequestYes(identity, newVersion);
            }
        );

        // リストの更新判断の関数をセット
        var listWillBeUpdated = false;
        Autoya.Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList(
            (condition, proceed, cancel) =>
            {
                listWillBeUpdated = true;
                proceed();
            }
        );

        // この通信でresponseHeaderを指定してリストの更新機能を着火する。
        Autoya.Http_Get(
            "https://httpbin.org/response-headers?" + resversionDesc + "=main_assets:1.0.1",
            (conId, data) =>
            {
                // pass.
            },
            (conId, code, reason, status) =>
            {
                Fail();
            }
        );




        yield return WaitUntil(
            () => listWillBeDownloaded,
            () => { throw new TimeoutException("too late."); }
        );

        yield return WaitUntil(
            () => listWillBeUpdated,
            () => { throw new TimeoutException("too late."); }
        );
    }

    [MTest]
    public IEnumerator ReceiveUpdatedListThenListWillBeUpdated()
    {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
            abListPath + "main_assets/" + AssetBundlesSettings.PLATFORM_STR + "/1.0.0/main_assets.json",
            status =>
            {
                done = true;
            },
            (code, reason, autoyaStatus) =>
            {
                // do nothing.
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        // リスト1.0.0が保持されている。
        // 通信のレスポンスヘッダーに特定の値が含まれていることで、listの更新リクエストを送り出す機構を着火する。


        // 新しいリストの取得判断の関数をセット(レスポンスを捕まえられるはず)
        Autoya.Debug_SetOverridePoint_ShouldRequestNewAssetBundleList(
            (identity, newVersion) =>
            {
                True(newVersion == "1.0.1");
                return RequestYes(identity, newVersion);
            }
        );

        // リストの更新判断の関数をセット
        var listWillBeUpdated = false;
        Autoya.Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList(
            (condition, proceed, cancel) =>
            {
                listWillBeUpdated = true;
                proceed();
            }
        );


        Autoya.Http_Get(
            "https://httpbin.org/response-headers?" + resversionDesc + "=main_assets:1.0.1",
            (conId, data) =>
            {
                // pass.
            },
            (conId, code, reason, status) =>
            {
                Fail();
            }
        );

        yield return WaitUntil(
            () => listWillBeUpdated,
            () => { throw new TimeoutException("too late."); }
        );

        // list is updated.
        True(Autoya.AssetBundle_AssetBundleLists()[0].version == "1.0.1");

        True(Autoya.Debug_AssetBundle_FeatureState() == Autoya.AssetBundlesFeatureState.Ready);
    }

    [MTest]
    public IEnumerator ReceiveUpdatedListThenOnAssetBundleListUpdatedFired()
    {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
            abListPath + "main_assets/" + AssetBundlesSettings.PLATFORM_STR + "/1.0.0/main_assets.json",
            status =>
            {
                done = true;
            },
            (code, reason, autoyaStatus) =>
            {
                // do nothing.
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        // リスト1.0.0が保持されている。
        // 通信のレスポンスヘッダーに特定の値が含まれていることで、listの更新リクエストを送り出す機構を着火する。


        // 新しいリストの取得判断の関数をセット(レスポンスを捕まえられるはず)
        Autoya.Debug_SetOverridePoint_ShouldRequestNewAssetBundleList(
            (identity, newVersion) =>
            {
                True(newVersion == "1.0.1");
                return RequestYes(identity, newVersion);
            }
        );

        // リストの更新判断の関数をセット
        var isListUpdated = false;
        Autoya.Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList(
            (condition, proceed, cancel) =>
            {
                proceed();
            }
        );

        Autoya.Debug_SetOnOverridePoint_OnAssetBundleListUpdated(
            (newVersion, ready) =>
            {
                ready();
                isListUpdated = true;
            }
        );


        Autoya.Http_Get(
            "https://httpbin.org/response-headers?" + resversionDesc + "=main_assets:1.0.1",
            (conId, data) =>
            {
                // pass.
            },
            (conId, code, reason, status) =>
            {
                Fail();
            }
        );

        yield return WaitUntil(
            () => isListUpdated,
            () => { throw new TimeoutException("too late."); }
        );

        // list is updated.
        True(Autoya.AssetBundle_AssetBundleLists()[0].version == "1.0.1");

        True(Autoya.Debug_AssetBundle_FeatureState() == Autoya.AssetBundlesFeatureState.Ready);
    }

    [MTest]
    public IEnumerator ReceiveUpdatedListThenIgnore()
    {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
            abListPath + "main_assets/" + AssetBundlesSettings.PLATFORM_STR + "/1.0.0/main_assets.json",
            status =>
            {
                done = true;
            },
            (code, reason, autoyaStatus) =>
            {
                // do nothing.
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        // リスト1.0.0が保持されている。
        // 通信のレスポンスヘッダーに特定の値が含まれていることで、listの更新リクエストを送り出す機構を着火する。


        // 新しいリストの取得判断の関数をセット(レスポンスを捕まえられるはず)
        var listWillBeDownloaded = false;
        Autoya.Debug_SetOverridePoint_ShouldRequestNewAssetBundleList(
            (identity, newVersion) =>
            {
                listWillBeDownloaded = true;
                True(newVersion == "1.0.1");
                return RequestYes(identity, newVersion);
            }
        );

        // リストの更新判断の関数をセット、ここでは更新を無視する。
        var listWillBeIgnored = false;
        Autoya.Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList(
            (condition, proceed, cancel) =>
            {
                listWillBeIgnored = true;
                cancel();
            }
        );


        Autoya.Http_Get(
            "https://httpbin.org/response-headers?" + resversionDesc + "=main_assets:1.0.1",
            (conId, data) =>
            {
                // pass.
            },
            (conId, code, reason, status) =>
            {
                Fail();
            }
        );

        yield return WaitUntil(
            () => listWillBeDownloaded,
            () => { throw new TimeoutException("too late."); }
        );

        yield return WaitUntil(
            () => listWillBeIgnored,
            () => { throw new TimeoutException("too late."); }
        );

        // list is not updated yet.
        True(Autoya.AssetBundle_AssetBundleLists()[0].version == "1.0.0");

        True(Autoya.Debug_AssetBundle_FeatureState() == Autoya.AssetBundlesFeatureState.Ready);
    }

    [MTest]
    public IEnumerator ReceiveUpdatedListThenIgnoreAndIgnoredListIsCached()
    {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
            abListPath + "main_assets/" + AssetBundlesSettings.PLATFORM_STR + "/1.0.0/main_assets.json",
            status =>
            {
                done = true;
            },
            (code, reason, autoyaStatus) =>
            {
                // do nothing.
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        // リスト1.0.0が保持されている。
        // 通信のレスポンスヘッダーに特定の値が含まれていることで、listの更新リクエストを送り出す機構を着火する。


        // 新しいリストの取得判断の関数をセット(レスポンスを捕まえられるはず)
        Autoya.Debug_SetOverridePoint_ShouldRequestNewAssetBundleList(
            (identity, newVersion) =>
            {
                True(newVersion == "1.0.1");
                return RequestYes(identity, newVersion);
            }
        );

        // リストの更新判断の関数をセット、ここでは更新を無視する。
        // 無視されたリストはpostponedなリストとしてメモリ上に保持される。これによって無駄な取得リクエストを省く。
        var listWillBeIgnored = false;
        Autoya.Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList(
            (condition, proceed, cancel) =>
            {
                listWillBeIgnored = true;
                cancel();
            }
        );


        Autoya.Http_Get(
            "https://httpbin.org/response-headers?" + resversionDesc + "=main_assets:1.0.1",
            (conId, data) =>
            {
                // pass.
            },
            (conId, code, reason, status) =>
            {
                Fail();
            }
        );

        yield return WaitUntil(
            () => listWillBeIgnored,
            () => { throw new TimeoutException("too late."); }
        );

        // list is not updated yet.
        True(Autoya.AssetBundle_AssetBundleLists()[0].version == "1.0.0");

        True(Autoya.Debug_AssetBundle_FeatureState() == Autoya.AssetBundlesFeatureState.Ready);

        // set to the new list to be updated.
        var listWillBeUpdated = false;
        Autoya.Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList(
            (condition, proceed, cancel) =>
            {
                listWillBeUpdated = true;
                proceed();
            }
        );

        // get list again.
        Autoya.Http_Get(
            "https://httpbin.org/response-headers?" + resversionDesc + "=main_assets:1.0.1",
            (conId, data) =>
            {
                // pass.
            },
            (conId, code, reason, status) =>
            {
                Fail();
            }
        );

        yield return WaitUntil(
            () => listWillBeUpdated,
            () => { throw new TimeoutException("too late."); }
        );

        True(Autoya.AssetBundle_AssetBundleLists()[0].version == "1.0.1");
    }

    [MTest]
    public IEnumerator ReceiveUpdatedListThenListWillBeUpdatedThenRestore()
    {
        var defaultDesc = Autoya.Manifest_LoadRuntimeManifest().ToString();

        yield return ReceiveUpdatedListThenListWillBeUpdated();

        // list is updated. RuntieManifest too.
        var updatedDesc = Autoya.Manifest_LoadRuntimeManifest().ToString();
        True(defaultDesc != updatedDesc, "defaultDesc:" + defaultDesc + "\nupdatedDesc:" + updatedDesc);

        {
            var done = false;
            Autoya.AssetBundle_FactoryReset(
                () =>
                {
                    done = true;
                },
                (error, reason) =>
                {
                    Fail("err:" + error + " reason:" + reason);
                }
            );
            yield return WaitUntil(
                () => done,
                () => { throw new TimeoutException("timeout."); }
            );
        }

        var resettedDesc = Autoya.Manifest_LoadRuntimeManifest().ToString();
        True(resettedDesc == defaultDesc);

        {
            var done = false;
            Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
                abListPath + "main_assets/" + AssetBundlesSettings.PLATFORM_STR + "/1.0.0/main_assets.json",
                    status =>
                    {
                        done = true;
                    },
                    (code, reason, autoyaStatus) =>
                    {
                        // do nothing.
                    }
                );

            yield return WaitUntil(
                () => done,
                () => { throw new TimeoutException("faild to get assetBundleList."); }
            );
        }
    }
}