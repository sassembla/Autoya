using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Settings.AssetBundles;
using AutoyaFramework.Settings.Auth;
using Miyamasu;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AssetBundlesImplementationTests : MiyamasuTestRunner
{

    private string abListDlPath = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles/";

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

        True(Caching.ClearCache(), "failed to clean cache.");

        Autoya.Debug_Manifest_RenewRuntimeManifest();
    }
    [MTeardown]
    public IEnumerator Teardown()
    {
        Autoya.AssetBundle_UnloadOnMemoryAssetBundles();


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

        True(Caching.ClearCache());
    }


    [MTest]
    public IEnumerator GetAssetBundleListFromDebugMethod()
    {
        var listIdentity = "main_assets";
        var fileName = "main_assets.json";
        var version = "1.0.0";

        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
            abListDlPath + listIdentity + "/" + AssetBundlesSettings.PLATFORM_STR + "/" + version + "/" + fileName,
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

    [MTest]
    public IEnumerator GetAssetBundleList()
    {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                done = true;
            },
            (code, reason, asutoyaStatus) =>
            {
                Debug.Log("GetAssetBundleList failed, code:" + code + " reason:" + reason);
                // do nothing.
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );
    }

    [MTest]
    public IEnumerator GetAssetBundleListFailThenTryAgain()
    {
        // fail once.
        {
            var listIdentity = "fake_main_assets";
            var notExistFileName = "fake_main_assets.json";
            var version = "1.0.0";
            var done = false;
            Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
                abListDlPath + listIdentity + "/" + AssetBundlesSettings.PLATFORM_STR + "/" + version + "/" + notExistFileName,
                status =>
                {
                    Fail("should not be succeeded.");
                },
                (err, reason, autoyaStatus) =>
                {
                    True(err.error == Autoya.ListDownloadError.FailedToDownload, "err does not match.");
                    done = true;
                }
            );

            yield return WaitUntil(
                () => done,
                () => { throw new TimeoutException("faild to fail getting assetBundleList."); }
            );
        }

        // try again with valid fileName.
        {
            var listIdentity = "main_assets";
            var fileName = "main_assets.json";
            var version = "1.0.0";

            var done = false;
            Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
                abListDlPath + listIdentity + "/" + AssetBundlesSettings.PLATFORM_STR + "/" + version + "/" + fileName,
                status =>
                {
                    done = true;
                },
                (code, reason, autoyaStatus) =>
                {
                    // do nothing.
                    Fail("reason:" + reason);
                }
            );

            yield return WaitUntil(
                () => done,
                () => { throw new TimeoutException("faild to get assetBundleList."); }
            );
        }
    }

    [MTest]
    public IEnumerator GetAssetBundleBeforeGetAssetBundleListBecomeFailed()
    {
        var loaderTest = new AssetBundleLoaderTests();
        var cor = loaderTest.LoadListFromWeb(abListDlPath + "main_assets" + "/" + AssetBundlesSettings.PLATFORM_STR + "/" + "1.0.0" + "/" + "main_assets.json");
        yield return cor;

        var list = cor.Current as AssetBundleList;

        var done = false;
        var assetName = list.assetBundles[0].assetNames[0];
        Autoya.AssetBundle_LoadAsset<GameObject>(
            assetName,
            (name, obj) =>
            {
                Fail("should not comes here.");
            },
            (name, err, reason, autoyaStatus) =>
            {
                True(err == AssetBundleLoadError.AssetBundleListIsNotReady, "not match.");
                done = true;
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("not yet failed."); }
        );
    }

    [MTest]
    public IEnumerator GetAssetBundle()
    {
        yield return GetAssetBundleList();

        var lists = Autoya.AssetBundle_AssetBundleLists();
        True(lists != null);

        var done = false;
        var assetName = lists.Where(list => list.identity == "main_assets").FirstOrDefault().assetBundles[0].assetNames[0];

        if (assetName.EndsWith(".png"))
        {
            Autoya.AssetBundle_LoadAsset<Texture2D>(
                assetName,
                (name, tex) =>
                {
                    done = true;
                },
                (name, err, reason, autoyaStatus) =>
                {
                    Fail("name:" + name + " err:" + err + " reason:" + reason);
                }
            );
        }
        else
        {
            Autoya.AssetBundle_LoadAsset<GameObject>(
                assetName,
                (name, obj) =>
                {
                    done = true;
                },
                (name, err, reason, autoyaStatus) =>
                {
                    Fail("name:" + name + " err:" + err + " reason:" + reason);
                }
            );
        }


        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("not yet done."); }
        );

        Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
    }

    [MTest]
    public IEnumerator PreloadAssetBundleBeforeGetAssetBundleListWillFail()
    {
        True(!Autoya.AssetBundle_IsAssetBundleFeatureReady(), "not match.");

        var done = false;

        Autoya.AssetBundle_Preload(
            "1.0.0/sample.preloadList.json",
            (willLoadBundleNames, proceed, cancel) =>
            {
                proceed();
            },
            progress =>
            {

            },
            () =>
            {
                Fail("should not be succeeded.");
            },
            (code, reason, autoyaStatus) =>
            {
                True(code == -(int)Autoya.AssetBundlesError.NeedToDownloadAssetBundleList, "not match. code:" + code + " reason:" + reason);
                done = true;
            },
            (failedAssetBundleName, code, reason, autoyaStatus) =>
            {

            },
            1
        );

        yield return WaitUntil(
           () => done,
           () => { throw new TimeoutException("not yet done."); }
       );
    }

    [MTest]
    public IEnumerator PreloadAssetBundle()
    {
        yield return GetAssetBundleList();

        var done = false;

        Autoya.AssetBundle_Preload(
            "sample.preloadList.json",
            (willLoadBundleNames, proceed, cancel) =>
            {
                proceed();
            },
            progress =>
            {

            },
            () =>
            {
                done = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Fail("should not be failed. code:" + code + " reason:" + reason);
            },
            (failedAssetBundleName, code, reason, autoyaStatus) =>
            {

            },
            1
        );

        yield return WaitUntil(
           () => done,
           () => { throw new TimeoutException("not yet done."); }
       );
    }
    [MTest]
    public IEnumerator PreloadAssetBundles()
    {
        yield return GetAssetBundleList();

        var done = false;

        Autoya.AssetBundle_Preload(
            "sample.preloadList2.json",
            (willLoadBundleNames, proceed, cancel) =>
            {
                proceed();
            },
            progress =>
            {

            },
            () =>
            {
                done = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Fail("should not be failed. code:" + code + " reason:" + reason);
            },
            (failedAssetBundleName, code, reason, autoyaStatus) =>
            {

            },
            2
        );

        yield return WaitUntil(
           () => done,
           () => { throw new TimeoutException("not yet done."); }
       );
    }

    [MTest]
    public IEnumerator PreloadAssetBundleWithGeneratedPreloadList()
    {
        yield return GetAssetBundleList();

        var done = false;

        var lists = Autoya.AssetBundle_AssetBundleLists();
        var mainAssetsList = lists.Where(list => list.identity == "main_assets").FirstOrDefault();
        NotNull(mainAssetsList);

        var preloadList = new PreloadList("test", mainAssetsList);

        // rewrite. set 1st content of bundleName.
        preloadList.bundleNames = new string[] { preloadList.bundleNames[0] };

        Autoya.AssetBundle_PreloadByList(
            preloadList,
            (willLoadBundleNames, proceed, cancel) =>
            {
                proceed();
            },
            progress =>
            {

            },
            () =>
            {
                done = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Fail("should not be failed. code:" + code + " reason:" + reason);
            },
            (failedAssetBundleName, code, reason, autoyaStatus) =>
            {

            },
            1
        );

        yield return WaitUntil(
           () => done,
           () => { throw new TimeoutException("not yet done."); }
       );
    }
    [MTest]
    public IEnumerator PreloadAssetBundlesWithGeneratedPreloadList()
    {
        yield return GetAssetBundleList();

        var done = false;

        var lists = Autoya.AssetBundle_AssetBundleLists();
        var preloadList = new PreloadList("test", lists.Where(list => list.identity == "main_assets").FirstOrDefault());

        Autoya.AssetBundle_PreloadByList(
            preloadList,
            (willLoadBundleNames, proceed, cancel) =>
            {
                proceed();
            },
            progress =>
            {

            },
            () =>
            {
                done = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Fail("should not be failed. code:" + code + " reason:" + reason);
            },
            (failedAssetBundleName, code, reason, autoyaStatus) =>
            {

            },
            4
        );

        yield return WaitUntil(
           () => done,
           () => { throw new TimeoutException("not yet done."); }
       );
    }

    [MTest]
    public IEnumerator IsAssetExistInAssetBundleList()
    {
        yield return GetAssetBundleList();
        var assetName = "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png";
        var exist = Autoya.AssetBundle_IsAssetExist(assetName);
        True(exist, "not exist:" + assetName);
    }

    [MTest]
    public IEnumerator IsAssetBundleExistInAssetBundleList()
    {
        yield return GetAssetBundleList();
        var bundleName = "texturename";
        var exist = Autoya.AssetBundle_IsAssetBundleExist(bundleName);
        True(exist, "not exist:" + bundleName);

    }

    [MTest]
    public IEnumerator AssetBundle_CachedBundleNames()
    {
        var listDownloaded = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                listDownloaded = true;
            },
            (error, reason, status) =>
            {

            }
        );
        yield return WaitUntil(
            () => listDownloaded,
            () => { throw new TimeoutException("failed to download list."); }
        );

        var done = false;
        Autoya.AssetBundle_CachedBundleNames(
            names =>
            {
                True(!names.Any());
                done = true;
            },
            (error, reason) =>
            {

            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("failed to get cached bundle names in time."); }
        );
    }

    [MTest]
    public IEnumerator AssetBundle_CachedBundleNamesWillBeUpdated()
    {
        var listDownloaded = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                listDownloaded = true;
            },
            (error, reason, status) =>
            {

            }
        );
        yield return WaitUntil(
            () => listDownloaded,
            () => { throw new TimeoutException("failed to download list."); }
        );

        // load 1 asset.
        var done = false;
        var assetName = string.Empty;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                assetName = Autoya.AssetBundle_AssetBundleLists().Where(list => list.identity == "main_assets").FirstOrDefault().assetBundles[0].assetNames[0];
                Autoya.AssetBundle_LoadAsset<GameObject>(
                    assetName,
                    (name, asset) =>
                    {
                        // succeeded to download AssetBundle and got asset from AB.
                        done = true;
                    },
                    (name, error, reason, autoyaStatus) =>
                    {

                    }
                );
            },
            (code, reason, asutoyaStatus) =>
            {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        var done2 = false;
        Autoya.AssetBundle_CachedBundleNames(
            names =>
            {
                True(names.Any());
                done2 = true;
            },
            (error, reason) =>
            {

            }
        );

        yield return WaitUntil(
            () => done2,
            () => { throw new TimeoutException("failed to get cached bundle names in time."); }
        );
    }

    [MTest]
    public IEnumerator AssetBundle_NotCachedBundleNames()
    {
        var listDownloaded = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                listDownloaded = true;
            },
            (error, reason, status) =>
            {

            }
        );
        yield return WaitUntil(
            () => listDownloaded,
            () => { throw new TimeoutException("failed to download list."); }
        );

        string[] names = null;
        Autoya.AssetBundle_NotCachedBundleNames(
            bundleNames =>
            {
                names = bundleNames;
            },
            (error, reason) =>
            {
                Debug.Log("error:" + error + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => names != null && 0 < names.Length,
            () => { throw new TimeoutException("failed to get Not chached bundle names."); }
        );

        // no asset cached.
        var wholeAssetBundleNames = Autoya.AssetBundle_AssetBundleLists().SelectMany(list => list.assetBundles).Select(bundleInfo => bundleInfo.bundleName).ToArray();
        True(names.Length == wholeAssetBundleNames.Length);
    }


    [MTest]
    public IEnumerator AssetBundle_NotCachedBundleNamesInSomeAssetCached()
    {

        // load 1 asset.
        var done = false;
        var assetName = string.Empty;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                assetName = Autoya.AssetBundle_AssetBundleLists().Where(list => list.identity == "main_assets").FirstOrDefault().assetBundles[0].assetNames[0];
                Autoya.AssetBundle_LoadAsset<GameObject>(
                    assetName,
                    (name, asset) =>
                    {
                        // succeeded to download AssetBundle and got asset from AB.
                        done = true;
                    },
                    (name, error, reason, autoyaStatus) =>
                    {
                        Fail("err:" + error + " reason:" + reason);
                    }
                );
            },
            (code, reason, asutoyaStatus) =>
            {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        // 1 or more assets are cached.(by dependencies.)


        string[] names = null;
        Autoya.AssetBundle_NotCachedBundleNames(
            bundleNames =>
            {
                names = bundleNames;
            },
            (error, reason) =>
            {
                Debug.Log("error:" + error + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => names != null && 0 < names.Length,
            () => { throw new TimeoutException("failed to get Not chached bundle names."); }
        );

        // 1 or more assets are cached.(by dependencies.)
        var wholeAssetBundleNames = Autoya.AssetBundle_AssetBundleLists().SelectMany(list => list.assetBundles).Select(bundleInfo => bundleInfo.bundleName).ToArray();
        True(names.Length < wholeAssetBundleNames.Length);
        True(!names.Contains(assetName), "cotntains.");
    }

    private IEnumerator LoadAllAssetBundlesOfMainAssets(Action<UnityEngine.Object[]> onLoaded)
    {
        var bundles = Autoya.AssetBundle_AssetBundleLists().Where(list => list.identity == "main_assets").FirstOrDefault().assetBundles;

        var loaded = 0;
        var allAssetCount = bundles.Sum(s => s.assetNames.Length);
        True(0 < allAssetCount, "allAssetCount:" + allAssetCount);

        var loadedAssets = new UnityEngine.Object[allAssetCount];

        foreach (var bundle in bundles)
        {
            foreach (var assetName in bundle.assetNames)
            {
                if (assetName.EndsWith(".png"))
                {
                    Autoya.AssetBundle_LoadAsset(
                        assetName,
                        (string name, Texture2D o) =>
                        {
                            loadedAssets[loaded] = o;
                            loaded++;
                        },
                        (name, error, reason, autoyaStatus) =>
                        {
                            Fail("failed to load asset:" + name + " reason:" + reason);
                        }
                    );
                }
                else if (assetName.EndsWith(".txt"))
                {
                    Autoya.AssetBundle_LoadAsset(
                        assetName,
                        (string name, TextAsset o) =>
                        {
                            loadedAssets[loaded] = o;
                            loaded++;
                        },
                        (name, error, reason, autoyaStatus) =>
                        {
                            Fail("failed to load asset:" + name + " reason:" + reason);
                        }
                    );
                }
                else
                {
                    Autoya.AssetBundle_LoadAsset(
                        assetName,
                        (string name, GameObject o) =>
                        {
                            loadedAssets[loaded] = o;
                            loaded++;
                        },
                        (name, error, reason, autoyaStatus) =>
                        {
                            Fail("failed to load asset:" + name + " reason:" + reason);
                        }
                    );
                }
            }
        }

        yield return WaitUntil(
            () => allAssetCount == loaded,
            () => { throw new TimeoutException("failed to load asset in time."); },
            10
        );

        onLoaded(loadedAssets);
    }

    [MTest]
    public IEnumerator UpdateListWithOnMemoryAssets()
    {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                done = true;
            },
            (code, reason, asutoyaStatus) =>
            {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        True(Autoya.AssetBundle_IsAssetBundleFeatureReady());


        UnityEngine.Object[] loadedAssets = null;

        // 全てのABをロード
        yield return LoadAllAssetBundlesOfMainAssets(objs => { loadedAssets = objs; });

        True(loadedAssets != null);

        // 1.0.1 リストの更新判断の関数をセット
        var listContainsUsingAssetsAndShouldBeUpdate = false;
        Autoya.Debug_SetOverridePoint_ShouldRequestNewAssetBundleList(
            (identity, ver) =>
            {
                var basePath = Autoya.Manifest_LoadRuntimeManifest().resourceInfos.Where(resInfo => resInfo.listIdentity == identity).FirstOrDefault().listDownloadUrl;
                var url = basePath + "/" + identity + "/" + AssetBundlesSettings.PLATFORM_STR + "/" + ver + "/" + identity + ".json";
                return Autoya.ShouldRequestOrNot.Yes(url);
            }
        );

        Autoya.Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList(
            (condition, proceed, cancel) =>
            {
                if (condition == Autoya.CurrentUsingBundleCondition.UsingAssetsAreChanged)
                {
                    listContainsUsingAssetsAndShouldBeUpdate = true;
                }
                proceed();
            }
        );



        // 1.0.1リストを取得
        Autoya.Http_Get(
            "https://httpbin.org/response-headers?" + AuthSettings.AUTH_RESPONSEHEADER_RESVERSION + "=main_assets:1.0.1",
            (conId, data) =>
            {
                // pass.
            },
            (conId, code, reason, status) =>
            {
                Fail("failed to get v1.0.1 List. code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => listContainsUsingAssetsAndShouldBeUpdate,
            () => { throw new TimeoutException("failed to get response."); },
            10
        );

        True(Autoya.AssetBundle_AssetBundleLists().Where(list => list.identity == "main_assets").FirstOrDefault().version == "1.0.1");

        // load状態のAssetはそのまま使用できる
        for (var i = 0; i < loadedAssets.Length; i++)
        {
            var loadedAsset = loadedAssets[i];
            True(loadedAsset != null);
        }
    }

    [MTest]
    public IEnumerator UpdateListWithOnMemoryAssetsThenReloadChangedAsset()
    {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                done = true;
            },
            (code, reason, asutoyaStatus) =>
            {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        True(Autoya.AssetBundle_IsAssetBundleFeatureReady());


        UnityEngine.Object[] loadedAssets = null;

        // 全てのABをロード
        yield return LoadAllAssetBundlesOfMainAssets(objs => { loadedAssets = objs; });

        True(loadedAssets != null);

        var guidsDict = loadedAssets.ToDictionary(
            a => a.name,
            a => a.GetInstanceID()
        );

        // 1.0.1 リストの更新判断の関数をセット
        var listContainsUsingAssetsAndShouldBeUpdate = false;
        Autoya.Debug_SetOverridePoint_ShouldRequestNewAssetBundleList(
            (identity, ver) =>
            {
                var basePath = Autoya.Manifest_LoadRuntimeManifest().resourceInfos.Where(resInfo => resInfo.listIdentity == identity).FirstOrDefault().listDownloadUrl;
                var url = basePath + "/" + identity + "/" + AssetBundlesSettings.PLATFORM_STR + "/" + ver + "/" + identity + ".json";
                return Autoya.ShouldRequestOrNot.Yes(url);
            }
        );

        Autoya.Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList(
            (condition, proceed, cancel) =>
            {
                if (condition == Autoya.CurrentUsingBundleCondition.UsingAssetsAreChanged)
                {
                    listContainsUsingAssetsAndShouldBeUpdate = true;
                }
                proceed();
            }
        );



        // 1.0.1リストを取得
        Autoya.Http_Get(
            "https://httpbin.org/response-headers?" + AuthSettings.AUTH_RESPONSEHEADER_RESVERSION + "=main_assets:1.0.1",
            (conId, data) =>
            {
                // pass.
            },
            (conId, code, reason, status) =>
            {
                Fail("code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => listContainsUsingAssetsAndShouldBeUpdate,
            () => { throw new TimeoutException("failed to get response."); },
            10
        );

        True(Autoya.AssetBundle_AssetBundleLists().Where(list => list.identity == "main_assets").FirstOrDefault().version == "1.0.1");

        // 再度ロード済みのAssetをLoadしようとすると、更新があったABについて最新を取得してくる。

        UnityEngine.Object[] loadedAssets2 = null;
        yield return LoadAllAssetBundlesOfMainAssets(objs => { loadedAssets2 = objs; });

        var newGuidsDict = loadedAssets2.ToDictionary(
            a => a.name,
            a => a.GetInstanceID()
        );

        var changedAssetCount = 0;
        foreach (var newGuidItem in newGuidsDict)
        {
            var name = newGuidItem.Key;
            var guid = newGuidItem.Value;
            if (guidsDict[name] != guid)
            {
                changedAssetCount++;
            }
        }
        True(changedAssetCount == 1);
    }

    [MTest]
    public IEnumerator UpdateListWithOnMemoryAssetsThenPreloadLoadedChangedAsset()
    {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                done = true;
            },
            (code, reason, asutoyaStatus) =>
            {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        True(Autoya.AssetBundle_IsAssetBundleFeatureReady());


        UnityEngine.Object[] loadedAssets = null;

        // 全てのABをロード
        yield return LoadAllAssetBundlesOfMainAssets(objs => { loadedAssets = objs; });

        True(loadedAssets != null);
        // var guids = loadedAssets.Select(a => a.GetInstanceID()).ToArray();

        var loadedAssetBundleNames = Autoya.AssetBundle_AssetBundleLists().Where(list => list.identity == "main_assets").FirstOrDefault().assetBundles.Select(a => a.bundleName).ToArray();

        // 1.0.1 リストの更新判断の関数をセット
        var listContainsUsingAssetsAndShouldBeUpdate = false;
        Autoya.Debug_SetOverridePoint_ShouldRequestNewAssetBundleList(
            (identity, ver) =>
            {
                var basePath = Autoya.Manifest_LoadRuntimeManifest().resourceInfos.Where(resInfo => resInfo.listIdentity == identity).FirstOrDefault().listDownloadUrl;
                var url = basePath + "/" + identity + "/" + AssetBundlesSettings.PLATFORM_STR + "/" + ver + "/" + identity + ".json";
                return Autoya.ShouldRequestOrNot.Yes(url);
            }
        );

        Autoya.Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList(
            (condition, proceed, cancel) =>
            {
                if (condition == Autoya.CurrentUsingBundleCondition.UsingAssetsAreChanged)
                {
                    listContainsUsingAssetsAndShouldBeUpdate = true;
                }
                proceed();
            }
        );



        // 1.0.1リストを取得
        Autoya.Http_Get(
            "https://httpbin.org/response-headers?" + AuthSettings.AUTH_RESPONSEHEADER_RESVERSION + "=main_assets:1.0.1",
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
            () => listContainsUsingAssetsAndShouldBeUpdate,
            () => { throw new TimeoutException("failed to get response."); },
            10
        );

        True(Autoya.AssetBundle_AssetBundleLists().Where(list => list.identity == "main_assets").FirstOrDefault().version == "1.0.1");


        // preload all.
        var preloadDone = false;

        var preloadList = new PreloadList("dummy", loadedAssetBundleNames);
        Autoya.AssetBundle_PreloadByList(
            preloadList,
            (preloadCandidateBundleNames, go, stop) =>
            {
                // all assetBundles should not be download. on memory loaded ABs are not updatable.
                True(preloadCandidateBundleNames.Length == 0);
                go();
            },
            progress => { },
            () =>
            {
                preloadDone = true;
            },
            (code, reason, status) =>
            {
                Fail("code:" + code + " reason:" + reason);
            },
            (failedAssetBundleName, code, reason, status) =>
            {
                Fail("failedAssetBundleName:" + failedAssetBundleName + " code:" + code + " reason:" + reason);
            },
            5
        );

        yield return WaitUntil(
            () => preloadDone,
            () => { throw new TimeoutException("failed to preload."); },
            10
        );
    }

    [MTest]
    public IEnumerator UpdateListWithOnMemoryAssetsThenPreloadUnloadedChangedAsset()
    {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                done = true;
            },
            (code, reason, asutoyaStatus) =>
            {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        True(Autoya.AssetBundle_IsAssetBundleFeatureReady());


        UnityEngine.Object[] loadedAssets = null;

        // 全てのABをロード
        yield return LoadAllAssetBundlesOfMainAssets(objs => { loadedAssets = objs; });

        True(loadedAssets != null);

        var loadedAssetBundleNames = Autoya.AssetBundle_AssetBundleLists().Where(list => list.identity == "main_assets").FirstOrDefault().assetBundles.Select(a => a.bundleName).ToArray();

        // 1.0.1 リストの更新判断の関数をセット
        var listContainsUsingAssetsAndShouldBeUpdate = false;
        Autoya.Debug_SetOverridePoint_ShouldRequestNewAssetBundleList(
            (identity, ver) =>
            {
                var basePath = Autoya.Manifest_LoadRuntimeManifest().resourceInfos.Where(resInfo => resInfo.listIdentity == identity).FirstOrDefault().listDownloadUrl;
                var url = basePath + "/" + identity + "/" + AssetBundlesSettings.PLATFORM_STR + "/" + ver + "/" + identity + ".json";
                return Autoya.ShouldRequestOrNot.Yes(url);
            }
        );

        Autoya.Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList(
            (condition, proceed, cancel) =>
            {
                if (condition == Autoya.CurrentUsingBundleCondition.UsingAssetsAreChanged)
                {
                    listContainsUsingAssetsAndShouldBeUpdate = true;
                }
                proceed();
            }
        );



        // 1.0.1リストを取得
        Autoya.Http_Get(
            "https://httpbin.org/response-headers?" + AuthSettings.AUTH_RESPONSEHEADER_RESVERSION + "=main_assets:1.0.1",
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
            () => listContainsUsingAssetsAndShouldBeUpdate,
            () => { throw new TimeoutException("failed to get response."); },
            10
        );

        True(Autoya.AssetBundle_AssetBundleLists().Where(list => list.identity == "main_assets").FirstOrDefault().version == "1.0.1");


        // unload all assets on memory.
        Autoya.AssetBundle_UnloadOnMemoryAssetBundles();

        // preload all.
        var preloadDone = false;


        // 更新がかかっているABを取得する。
        var preloadList = new PreloadList("dummy", loadedAssetBundleNames);
        Autoya.AssetBundle_PreloadByList(
            preloadList,
            (preloadCandidateBundleNames, go, stop) =>
            {
                // all assetBundles should not be download. on memory loaded ABs are not updatable.
                True(preloadCandidateBundleNames.Length == 1);
                go();
            },
            progress => { },
            () =>
            {
                preloadDone = true;
            },
            (code, reason, status) =>
            {
                Fail("code:" + code + " reason:" + reason);
            },
            (failedAssetBundleName, code, reason, status) =>
            {
                Fail("failedAssetBundleName:" + failedAssetBundleName + " code:" + code + " reason:" + reason);
            },
            5
        );

        yield return WaitUntil(
            () => preloadDone,
            () => { throw new TimeoutException("failed to preload."); },
            10
        );
    }

    [MTest]
    public IEnumerator DownloadSameBundleListAtOnce()
    {
        var listIdentity = "main_assets";
        var fileName = "main_assets.json";
        var version = "1.0.0";

        var done1 = false;
        var done2 = false;
        Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
            abListDlPath + listIdentity + "/" + AssetBundlesSettings.PLATFORM_STR + "/" + version + "/" + fileName,
            status =>
            {
                done1 = true;
                Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
                    abListDlPath + listIdentity + "/" + AssetBundlesSettings.PLATFORM_STR + "/" + version + "/" + fileName,
                    status2 =>
                    {
                        done2 = true;
                    },
                    (code, reason, autoyaStatus) =>
                    {
                        Fail("code:" + code + " reason:" + reason);
                    }
                );
            },
            (code, reason, autoyaStatus) =>
            {
                Fail("code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done1 && done2,
            () =>
            {
                throw new TimeoutException("failed to download multiple list in time.");
            },
            5
        );
    }



    [MTest]
    public IEnumerator DownloadMultipleBundleListAtOnce()
    {
        var done1 = false;
        var done2 = false;
        Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
            abListDlPath + "main_assets/" + AssetBundlesSettings.PLATFORM_STR + "/1.0.0/main_assets.json",
            status =>
            {
                done1 = true;
                Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
                    abListDlPath + "sub_assets/" + AssetBundlesSettings.PLATFORM_STR + "/1.0.0/sub_assets.json",
                    status2 =>
                    {
                        done2 = true;
                    },
                    (code, reason, autoyaStatus) =>
                    {
                        Fail("code:" + code + " reason:" + reason);
                    }
                );
            },
            (code, reason, autoyaStatus) =>
            {
                // do nothing.
            }
        );

        yield return WaitUntil(
            () => done1 && done2,
            () =>
            {
                throw new TimeoutException("failed to download multiple list in time.");
            },
            5
        );
    }

    [MTest]
    public IEnumerator DownloadedMultipleListsAreEnabled()
    {
        yield return DownloadMultipleBundleListAtOnce();
        // それぞれのリストの要素を使って、動作していることを確認する。

        var mainAssetsAssetName = Autoya.AssetBundle_AssetBundleLists().Where(list => list.identity == "main_assets").FirstOrDefault().assetBundles[0].assetNames[0];
        var subAssetsAssetName = Autoya.AssetBundle_AssetBundleLists().Where(list => list.identity == "sub_assets").FirstOrDefault().assetBundles[0].assetNames[0];


        GameObject mainAsset = null;
        Autoya.AssetBundle_LoadAsset<GameObject>(
            mainAssetsAssetName,
            (name, asset) =>
            {
                mainAsset = asset;
            },
            (name, error, reason, status) =>
            {

            }
        );

        TextAsset subAsset = null;
        Autoya.AssetBundle_LoadAsset<TextAsset>(
            subAssetsAssetName,
            (name, asset) =>
            {
                subAsset = asset;
            },
            (name, error, reason, status) =>
            {

            }
        );

        yield return WaitUntil(
            () => mainAsset != null && subAsset != null,
            () => { throw new TimeoutException("failed to load."); }
        );
    }

    [MTest]
    public IEnumerator UpdateMultipleListAtOnce()
    {
        yield return DownloadMultipleBundleListAtOnce();

        // main_assetsは1.1、sub_assetsは2.0がサーバ上にある。
        // 1.0.1 リストの更新判断の関数をセット
        var listContainsUsingAssetsAndShouldBeUpdateCount = 0;
        Autoya.Debug_SetOverridePoint_ShouldRequestNewAssetBundleList(
            (identity, ver) =>
            {
                var basePath = Autoya.Manifest_LoadRuntimeManifest().resourceInfos.Where(resInfo => resInfo.listIdentity == identity).FirstOrDefault().listDownloadUrl;
                var url = basePath + "/" + identity + "/" + AssetBundlesSettings.PLATFORM_STR + "/" + ver + "/" + identity + ".json";
                return Autoya.ShouldRequestOrNot.Yes(url);
            }
        );

        Autoya.Debug_SetOverridePoint_ShouldUpdateToNewAssetBundleList(
            (condition, proceed, cancel) =>
            {
                if (condition == Autoya.CurrentUsingBundleCondition.NoUsingAssetsChanged)
                {
                    listContainsUsingAssetsAndShouldBeUpdateCount++;
                }
                proceed();
            }
        );



        // 1.0.1、2.0.0 リストを取得
        Autoya.Http_Get(
            "https://httpbin.org/response-headers?" + AuthSettings.AUTH_RESPONSEHEADER_RESVERSION + "=main_assets:1.0.1,sub_assets:2.0.0",
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
            () => listContainsUsingAssetsAndShouldBeUpdateCount == 2,
            () => { throw new TimeoutException("failed to get response."); },
            10
        );

        True(Autoya.AssetBundle_AssetBundleLists().Where(list => list.identity == "main_assets").FirstOrDefault().version == "1.0.1");
        True(Autoya.AssetBundle_AssetBundleLists().Where(list => list.identity == "sub_assets").FirstOrDefault().version == "2.0.0");
    }

    [MTest]
    public IEnumerator DownloadAssetBundleListManually()
    {
        var url = abListDlPath + "main_assets/" + AssetBundlesSettings.PLATFORM_STR + "/1.0.0/main_assets.json";
        var done1 = false;
        Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
            url,
            status =>
            {
                done1 = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Fail("code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done1,
            () => { throw new TimeoutException("timeout."); }
        );

        // この時点で、Readyになっている + RuntimeManifestにいろいろ入っているはず。
        var runtimeManifest = Autoya.Manifest_LoadRuntimeManifest();
        True(runtimeManifest.resourceInfos.Where(rInfo => rInfo.listIdentity == "main_assets").Any());
        True(runtimeManifest.resourceInfos.Where(rInfo => rInfo.listIdentity == "main_assets").Where(rInfo => rInfo.listVersion == "1.0.0").Any());
    }

    /*
        あらかじめRuntimeManifestにAssetBundleListのidentityやbasePath(url)が記載されていない状態で
        AssetBundleListをダウンロードしようとすると、特にurlに関して解決できない問題を持った状態になるため、前提としてDLに失敗する。
     */
    [MTest]
    public IEnumerator DownloadAssetBundleListManuallyWithoutPrepareWillFail()
    {
        // runtimeManifestのリソースリストを空にする。これで、取得したAssetBundleListのidentityが記録上存在しないという状態を作り出せる。
        {
            var defaultRuntimeManifest = Autoya.Manifest_LoadRuntimeManifest();
            defaultRuntimeManifest.resourceInfos = new AutoyaFramework.AppManifest.AssetBundleListInfo[0];
            Autoya.Manifest_UpdateRuntimeManifest(defaultRuntimeManifest);
        }

        var url = abListDlPath + "main_assets/" + AssetBundlesSettings.PLATFORM_STR + "/1.0.0/main_assets.json";
        var done1 = false;
        Autoya.AssetBundle_DownloadAssetBundleListFromUrlManually(
            url,
            status =>
            {
                Fail();
            },
            (code, reason, autoyaStatus) =>
            {
                done1 = true;
            }
        );

        yield return WaitUntil(
            () => done1,
            () => { throw new TimeoutException("timeout."); }
        );

        // リソースリストの復帰。
        Autoya.Debug_Manifest_RenewRuntimeManifest();
    }

    [MTest]
    public IEnumerator PreloadAndLoadSameAssetBundle()
    {
        /*
            同じABを同時にLoadAssetとPreloadで読む。
         */
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                done = true;
            },
            (code, reason, asutoyaStatus) =>
            {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        var preloaded = false;
        var loadAssetSucceeded = false;

        /*
            start loadAsset and preload against same assetBundle.
         */
        Autoya.AssetBundle_LoadAsset<GameObject>(
            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName1.prefab",
            (name, prefab) =>
            {
                loadAssetSucceeded = true;
            },
            (name, error, reason, status) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("failed to load Asset. error:" + error + " reason:" + reason);
            }
        );

        var preloadList = new PreloadList("test", new string[] { "texturename1" });

        // download preloadList from web then preload described assetBundles.
        Autoya.AssetBundle_PreloadByList(
            preloadList,
            (willLoadBundleNames, proceed, cancel) =>
            {
                proceed();
            },
            progress =>
            {
                // Debug.Log("progress:" + progress);
            },
            () =>
            {
                preloaded = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("preload failed. code:" + code + " reason:" + reason);
            },
            (downloadFailedAssetBundleName, code, reason, autoyaStatus) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("failed to preload assetBundle:" + downloadFailedAssetBundleName + ". code:" + code + " reason:" + reason);
            },
            10
        );

        yield return WaitUntil(
            () => preloaded && loadAssetSucceeded,
            () =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                throw new TimeoutException("timeout.");
            }
        );
    }


    [MTest]
    public IEnumerator PreloadAndLoadDependentAssetBundle()
    {
        /*
            あるAssetBundleをロード開始し、そのBundleが依存しているBundleを同時にPreloadで得る。
            a -> b
            +
            preload b
         */
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                done = true;
            },
            (code, reason, asutoyaStatus) =>
            {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        var preloaded = false;
        var loadAssetSucceeded = false;


        Autoya.AssetBundle_LoadAsset<GameObject>(
            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/nestedPrefab.prefab",
            (name, prefab) =>
            {
                loadAssetSucceeded = true;
            },
            (name, error, reason, status) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("failed to load Asset. error:" + error + " reason:" + reason);
            }
        );

        var preloadList = new PreloadList("test", new string[] { "texturename" });

        // download preloadList from web then preload described assetBundles.
        Autoya.AssetBundle_PreloadByList(
            preloadList,
            (willLoadBundleNames, proceed, cancel) =>
            {
                True(willLoadBundleNames.Length == 1);
                proceed();
            },
            progress =>
            {
                // Debug.Log("progress:" + progress);
            },
            () =>
            {
                preloaded = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("preload failed. code:" + code + " reason:" + reason);
            },
            (downloadFailedAssetBundleName, code, reason, autoyaStatus) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("failed to preload assetBundle:" + downloadFailedAssetBundleName + ". code:" + code + " reason:" + reason);
            },
            10
        );

        yield return WaitUntil(
            () => preloaded && loadAssetSucceeded,
            () =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                throw new TimeoutException("timeout.");
            }
        );
    }

    [MTest]
    public IEnumerator PreloadAndLoadDependentAssetBundle_Rev()
    {
        /*
            あるAssetBundleをロード開始し、そのBundleが依存しているBundleを同時にPreloadで得る。
            a -> b
            +
            preload b
         */
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                done = true;
            },
            (code, reason, asutoyaStatus) =>
            {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        var preloaded = false;
        var loadAssetSucceeded = false;

        var preloadList = new PreloadList("test", new string[] { "texturename" });

        // download preloadList from web then preload described assetBundles.
        Autoya.AssetBundle_PreloadByList(
            preloadList,
            (willLoadBundleNames, proceed, cancel) =>
            {
                True(willLoadBundleNames.Length == 1);
                proceed();
            },
            progress =>
            {
                // Debug.Log("progress:" + progress);
            },
            () =>
            {
                preloaded = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("preload failed. code:" + code + " reason:" + reason);
            },
            (downloadFailedAssetBundleName, code, reason, autoyaStatus) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("failed to preload assetBundle:" + downloadFailedAssetBundleName + ". code:" + code + " reason:" + reason);
            },
            10
        );


        Autoya.AssetBundle_LoadAsset<GameObject>(
            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/nestedPrefab.prefab",
            (name, prefab) =>
            {
                loadAssetSucceeded = true;
            },
            (name, error, reason, status) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("failed to load Asset. error:" + error + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => preloaded && loadAssetSucceeded,
            () =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                throw new TimeoutException("timeout.");
            }
        );
    }

    [MTest]
    public IEnumerator LoadAndPreloadDependentAssetBundle()
    {
        /*
            あるAssetBundleをPreload開始し、そのBundleが依存しているBundleを同時にLoadAssetで得る。
            preload a
            +
            a -> b
         */
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                done = true;
            },
            (code, reason, asutoyaStatus) =>
            {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        var preloaded = false;
        var loadAssetSucceeded = false;

        Autoya.AssetBundle_LoadAsset<Texture2D>(
            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png",
            (name, tex) =>
            {
                loadAssetSucceeded = true;
            },
            (name, error, reason, status) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("failed to load Asset. error:" + error + " reason:" + reason);
            }
        );

        var preloadList = new PreloadList("testDependentAssetBundle", new string[] { "nestedprefab" });

        // download preloadList from web then preload described assetBundles.
        Autoya.AssetBundle_PreloadByList(
            preloadList,
            (willLoadBundleNames, proceed, cancel) =>
            {
                True(willLoadBundleNames.Length == 2);
                proceed();
            },
            progress =>
            {
                // Debug.Log("progress:" + progress);
            },
            () =>
            {
                preloaded = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("preload failed. code:" + code + " reason:" + reason);
            },
            (downloadFailedAssetBundleName, code, reason, autoyaStatus) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("failed to preload assetBundle:" + downloadFailedAssetBundleName + ". code:" + code + " reason:" + reason);
            },
            10
        );

        yield return WaitUntil(
            () => preloaded && loadAssetSucceeded,
            () =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                throw new TimeoutException("timeout.");
            }
        );
    }

    [MTest]
    public IEnumerator LoadAndPreloadDependentAssetBundle_Rev()
    {
        /*
            あるAssetBundleをPreload開始し、そのBundleが依存しているBundleを同時にLoadAssetで得る。
            preload a
            +
            a -> b
         */
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                done = true;
            },
            (code, reason, asutoyaStatus) =>
            {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        var preloaded = false;
        var loadAssetSucceeded = false;

        var preloadList = new PreloadList("testDependentAssetBundle", new string[] { "nestedprefab" });

        // download preloadList from web then preload described assetBundles.
        Autoya.AssetBundle_PreloadByList(
            preloadList,
            (willLoadBundleNames, proceed, cancel) =>
            {
                True(willLoadBundleNames.Length == 2);
                proceed();
            },
            progress =>
            {
                // Debug.Log("progress:" + progress);
            },
            () =>
            {
                preloaded = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("preload failed. code:" + code + " reason:" + reason);
            },
            (downloadFailedAssetBundleName, code, reason, autoyaStatus) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("failed to preload assetBundle:" + downloadFailedAssetBundleName + ". code:" + code + " reason:" + reason);
            },
            10
        );

        Autoya.AssetBundle_LoadAsset<Texture2D>(
            "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png",
            (name, tex) =>
            {
                loadAssetSucceeded = true;
            },
            (name, error, reason, status) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("failed to load Asset. error:" + error + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => preloaded && loadAssetSucceeded,
            () =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                throw new TimeoutException("timeout.");
            }
        );
    }

    [MTest]
    public IEnumerator PreloadAndLoadAllAssetBundle()
    {
        /*
            全てのAssetBundleのロード開始し、同時にPreloadも開始する。            
         */
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                done = true;
            },
            (code, reason, asutoyaStatus) =>
            {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        var preloaded = false;

        var assetBundleLists = Autoya.AssetBundle_AssetBundleLists();
        var assetNames = assetBundleLists.SelectMany(list => list.assetBundles).SelectMany(bundleInfo => bundleInfo.assetNames).ToArray();
        var loadAssetSucceeded = new Dictionary<string, bool>();

        var sceneReleaseCors = new List<AsyncOperation>();

        foreach (var assetName in assetNames)
        {
            loadAssetSucceeded[assetName] = false;

            // ignore unity scene.
            if (assetName.EndsWith(".unity"))
            {
                Autoya.AssetBundle_LoadScene(
                    assetName,
                    LoadSceneMode.Additive,
                    name =>
                    {
                        loadAssetSucceeded[assetName] = true;
                        sceneReleaseCors.Add(SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(name)));
                    },
                    (name, error, reason, status) =>
                    {
                        Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                        Autoya.AssetBundle_DeleteAllStorageCache();
                        Fail("failed to load scene. error:" + error + " reason:" + reason);
                    }
                );
                continue;
            }

            Autoya.AssetBundle_LoadAsset<UnityEngine.Object>(
                assetName,
                (name, obj) =>
                {
                    // Debug.Log("name:" + name);
                    loadAssetSucceeded[assetName] = true;
                },
                (name, error, reason, status) =>
                {
                    Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                    Autoya.AssetBundle_DeleteAllStorageCache();
                    Fail("failed to load Asset. error:" + error + " reason:" + reason);
                }
            );
        }

        var preloadListTargetBundleNames = assetBundleLists.SelectMany(list => list.assetBundles).Select(bundleInfo => bundleInfo.bundleName).ToArray();
        var preloadList = new PreloadList("allAssetBundles", preloadListTargetBundleNames);

        // download preloadList from web then preload described assetBundles.
        Autoya.AssetBundle_PreloadByList(
            preloadList,
            (willLoadBundleNames, proceed, cancel) =>
            {
                // Debug.Log("willLoadBundleNames:" + string.Join(", ", willLoadBundleNames));
                proceed();
            },
            progress =>
            {
                // Debug.Log("progress:" + progress);
            },
            () =>
            {
                preloaded = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("preload failed. code:" + code + " reason:" + reason);
            },
            (downloadFailedAssetBundleName, code, reason, autoyaStatus) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("failed to preload assetBundle:" + downloadFailedAssetBundleName + ". code:" + code + " reason:" + reason);
            },
            100
        );

        yield return WaitUntil(
            () =>
            {
                var notDone = loadAssetSucceeded.Where((i, c) => !i.Value).Any();
                if (notDone)
                {
                    return false;
                }

                // loadAssetSucceeded is done.

                return preloaded;
            },
            () =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                throw new TimeoutException("timeout.");
            }
        );

        yield return WaitUntil(
            () => !sceneReleaseCors.Where(op => !op.isDone).Any(),
            () => { throw new TimeoutException("failed to unload all loaded scene."); }
        );
    }

    [MTest]
    public IEnumerator PreloadAndLoadAllAssetBundle_Rev()
    {
        /*
            全てのABのPreloadを開始し、全てのAssetBundleのロードも開始する。
         */
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                done = true;
            },
            (code, reason, asutoyaStatus) =>
            {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        var preloaded = false;

        var assetBundleLists = Autoya.AssetBundle_AssetBundleLists();
        var assetNames = assetBundleLists.SelectMany(list => list.assetBundles).SelectMany(bundleInfo => bundleInfo.assetNames).ToArray();

        var loadAssetSucceeded = new Dictionary<string, bool>();

        var preloadListTargetBundleNames = assetBundleLists.SelectMany(list => list.assetBundles).Select(bundleInfo => bundleInfo.bundleName).ToArray();
        var preloadList = new PreloadList("allAssetBundles", preloadListTargetBundleNames);

        // download preloadList from web then preload described assetBundles.
        Autoya.AssetBundle_PreloadByList(
            preloadList,
            (willLoadBundleNames, proceed, cancel) =>
            {
                // Debug.Log("willLoadBundleNames:" + string.Join(", ", willLoadBundleNames));
                proceed();
            },
            progress =>
            {
                // Debug.Log("progress:" + progress);
            },
            () =>
            {
                preloaded = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("preload failed. code:" + code + " reason:" + reason);
            },
            (downloadFailedAssetBundleName, code, reason, autoyaStatus) =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                Fail("failed to preload assetBundle:" + downloadFailedAssetBundleName + ". code:" + code + " reason:" + reason);
            },
            100
        );

        var sceneReleaseCors = new List<AsyncOperation>();

        foreach (var assetName in assetNames)
        {
            loadAssetSucceeded[assetName] = false;

            // ignore unity scene.
            if (assetName.EndsWith(".unity"))
            {
                Autoya.AssetBundle_LoadScene(
                    assetName,
                    LoadSceneMode.Additive,
                    name =>
                    {
                        loadAssetSucceeded[assetName] = true;
                        sceneReleaseCors.Add(SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(name)));
                    },
                    (name, error, reason, status) =>
                    {
                        Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                        Autoya.AssetBundle_DeleteAllStorageCache();
                        Fail("failed to load scene. error:" + error + " reason:" + reason);
                    }
                );
                continue;
            }

            Autoya.AssetBundle_LoadAsset<UnityEngine.Object>(
                assetName,
                (name, obj) =>
                {
                    loadAssetSucceeded[assetName] = true;
                },
                (name, error, reason, status) =>
                {
                    Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                    Autoya.AssetBundle_DeleteAllStorageCache();
                    Fail("failed to load Asset. error:" + error + " reason:" + reason);
                }
            );
        }

        yield return WaitUntil(
            () =>
            {
                var notDone = loadAssetSucceeded.Where((i, c) => !i.Value).Any();
                if (notDone)
                {
                    return false;
                }

                // loadAssetSucceeded is done.

                return preloaded;
            },
            () =>
            {
                Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                Autoya.AssetBundle_DeleteAllStorageCache();
                throw new TimeoutException("timeout.");
            }
        );

        yield return WaitUntil(
            () => !sceneReleaseCors.Where(op => !op.isDone).Any(),
            () => { throw new TimeoutException("failed to unload all loaded scene."); }
        );
    }

    [MTest]
    public IEnumerator LoadSceneAdditive()
    {
        var done = false;
        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            status =>
            {
                done = true;
            },
            (code, reason, asutoyaStatus) =>
            {
                Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("faild to get assetBundleList."); }
        );

        var loadSceneDone = false;
        var sceneName = string.Empty;

        Autoya.AssetBundle_LoadScene(
            "Assets/AutoyaTests/RuntimeData/bundledScene.unity",
            LoadSceneMode.Additive,
            loadedSceneName =>
            {
                loadSceneDone = true;
                sceneName = loadedSceneName;
            },
            (loadFailedSceneName, error, reason, status) =>
            {
                Fail("failed to load scene:" + loadFailedSceneName + " from AB, error:" + error + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => loadSceneDone,
            () => { throw new TimeoutException("failed to load scene."); }
        );

        var cor = SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(sceneName));
        while (!cor.isDone)
        {
            yield return null;
        }
    }

    [MTest]
    public IEnumerator LoadSceneAdditiveSync()
    {
        Debug.LogWarning("unable to test by UnityTest's bug.");
        yield break;
        // var done = false;
        // Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
        //     status =>
        //     {
        //         done = true;
        //     },
        //     (code, reason, asutoyaStatus) =>
        //     {
        //         Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
        //     }
        // );

        // yield return WaitUntil(
        //     () => done,
        //     () => { throw new TimeoutException("faild to get assetBundleList."); }
        // );

        // var loadSceneDone = false;
        // var sceneName = string.Empty;

        // Autoya.AssetBundle_LoadScene(
        //     "Assets/AutoyaTests/RuntimeData/bundledScene.unity",
        //     LoadSceneMode.Additive,
        //     loadedSceneName =>
        //     {
        //         loadSceneDone = true;
        //         sceneName = loadedSceneName;
        //     },
        //     (loadFailedSceneName, error, reason, status) =>
        //     {
        //         Fail("failed to load scene:" + loadFailedSceneName + " from AB, error:" + error + " reason:" + reason);
        //     },
        //     false
        // );

        // yield return WaitUntil(
        //     () => loadSceneDone,
        //     () => { throw new TimeoutException("failed to load scene."); }
        // );
        // var scene = SceneManager.GetSceneByPath(sceneName);
        // Debug.Log("scene:" + scene.name + " path:" + scene.path);

        // var cor = SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(sceneName));
        // Debug.Log("cor:" + cor);
        // while (!cor.isDone)
        // {
        //     yield return null;
        // }
    }

    [MTest]
    public IEnumerator LoadSceneSingle()
    {
        Debug.LogWarning("unable to test by UnityTest's spec.");
        yield break;
        // var done = false;
        // Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
        //     status =>
        //     {
        //         done = true;
        //     },
        //     (code, reason, asutoyaStatus) =>
        //     {
        //         Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
        //     }
        // );

        // yield return WaitUntil(
        //     () => done,
        //     () => { throw new TimeoutException("faild to get assetBundleList."); }
        // );

        // var loadSceneDone = false;
        // var sceneName = string.Empty;

        // Autoya.AssetBundle_LoadScene(
        //     "Assets/AutoyaTests/RuntimeData/bundledScene.unity",
        //     LoadSceneMode.Single,
        //     loadedSceneName =>
        //     {
        //         loadSceneDone = true;
        //         sceneName = loadedSceneName;
        //     },
        //     (loadFailedSceneName, error, reason, status) =>
        //     {
        //         Fail("failed to load scene:" + loadFailedSceneName + " from AB, error:" + error + " reason:" + reason);
        //     }
        // );

        // yield return WaitUntil(
        //     () => loadSceneDone,
        //     () => { throw new TimeoutException("failed to load scene."); }
        // );

        // var cor = SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(sceneName));
        // while (!cor.isDone)
        // {
        //     yield return null;
        // }
    }

    [MTest]

    public IEnumerator LoadSceneSingleSync()
    {
        Debug.LogWarning("unable to test by UnityTest's bug.");
        yield break;
        // var done = false;
        // Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
        //     status =>
        //     {
        //         done = true;
        //     },
        //     (code, reason, asutoyaStatus) =>
        //     {
        //         Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
        //     }
        // );

        // yield return WaitUntil(
        //     () => done,
        //     () => { throw new TimeoutException("faild to get assetBundleList."); }
        // );

        // var loadSceneDone = false;
        // var sceneName = string.Empty;

        // Autoya.AssetBundle_LoadScene(
        //     "Assets/AutoyaTests/RuntimeData/bundledScene.unity",
        //     LoadSceneMode.Single,
        //     loadedSceneName =>
        //     {
        //         loadSceneDone = true;
        //         sceneName = loadedSceneName;
        //     },
        //     (loadFailedSceneName, error, reason, status) =>
        //     {
        //         Fail("failed to load scene:" + loadFailedSceneName + " from AB, error:" + error + " reason:" + reason);
        //     },
        //     false
        // );

        // yield return WaitUntil(
        //     () => loadSceneDone,
        //     () => { throw new TimeoutException("failed to load scene."); }
        // );

        // var cor = SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(sceneName));
        // while (!cor.isDone)
        // {
        //     yield return null;
        // }
    }

    [MTest]
    public IEnumerator PreloadScene()
    {
        Debug.LogWarning("not yet implemented: PreloadScene");
        yield break;
    }

    [MTest]
    public IEnumerator FactoryReset()
    {
        var beforeRestoreLoadBundleNames = new string[0];
        {
            /*
                全てのABのPreloadを開始し、全てのAssetBundleのロードも開始する。
             */
            var done = false;
            Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
                status =>
                {
                    done = true;
                },
                (code, reason, asutoyaStatus) =>
                {
                    Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
                }
            );

            yield return WaitUntil(
                () => done,
                () => { throw new TimeoutException("faild to get assetBundleList."); }
            );

            var preloaded = false;

            var assetBundleLists = Autoya.AssetBundle_AssetBundleLists();
            var assetNames = assetBundleLists.SelectMany(list => list.assetBundles).SelectMany(bundleInfo => bundleInfo.assetNames).ToArray();

            var loadAssetSucceeded = new Dictionary<string, bool>();

            var preloadListTargetBundleNames = assetBundleLists.SelectMany(list => list.assetBundles).Select(bundleInfo => bundleInfo.bundleName).ToArray();
            var preloadList = new PreloadList("allAssetBundles", preloadListTargetBundleNames);

            // download preloadList from web then preload described assetBundles.
            Autoya.AssetBundle_PreloadByList(
                preloadList,
                (willLoadBundleNames, proceed, cancel) =>
                {
                    beforeRestoreLoadBundleNames = willLoadBundleNames;
                    // Debug.Log("willLoadBundleNames:" + string.Join(", ", willLoadBundleNames));
                    proceed();
                },
                progress =>
                {
                    // Debug.Log("progress:" + progress);
                },
                () =>
                {
                    preloaded = true;
                },
                (code, reason, autoyaStatus) =>
                {
                    Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                    Autoya.AssetBundle_DeleteAllStorageCache();
                    Fail("preload failed. code:" + code + " reason:" + reason);
                },
                (downloadFailedAssetBundleName, code, reason, autoyaStatus) =>
                {
                    Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                    Autoya.AssetBundle_DeleteAllStorageCache();
                    Fail("failed to preload assetBundle:" + downloadFailedAssetBundleName + ". code:" + code + " reason:" + reason);
                },
                100
            );

            yield return WaitUntil(
                () => preloaded,
                () => { throw new TimeoutException("timeout."); }
            );
        }


        var resetted = false;

        Autoya.AssetBundle_FactoryReset(
            () =>
                {
                    // pass.
                    resetted = true;
                },
                (err, reason) =>
                {
                    Fail("err:" + err + " reason:" + reason);
                }
            );

        yield return WaitUntil(
            () => resetted,
            () => { throw new TimeoutException("timeout to reset."); }
        );

        // この時点で、リストがなくてstateが変わっているはず。
        True(!Autoya.AssetBundle_IsAssetBundleFeatureReady());

        {
            /*
                全てのABのPreloadを開始し、全てのAssetBundleのロードも開始する。
            */
            var done = false;
            Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
                status =>
                {
                    done = true;
                },
                (code, reason, asutoyaStatus) =>
                {
                    Fail("UpdateListWithOnMemoryAssets failed, code:" + code + " reason:" + reason);
                }
            );

            yield return WaitUntil(
                () => done,
                () => { throw new TimeoutException("faild to get assetBundleList."); }
            );

            var preloaded = false;

            var assetBundleLists = Autoya.AssetBundle_AssetBundleLists();
            var assetNames = assetBundleLists.SelectMany(list => list.assetBundles).SelectMany(bundleInfo => bundleInfo.assetNames).ToArray();

            var loadAssetSucceeded = new Dictionary<string, bool>();

            var preloadListTargetBundleNames = assetBundleLists.SelectMany(list => list.assetBundles).Select(bundleInfo => bundleInfo.bundleName).ToArray();
            var preloadList = new PreloadList("allAssetBundles", preloadListTargetBundleNames);

            // download preloadList from web then preload described assetBundles.
            Autoya.AssetBundle_PreloadByList(
                preloadList,
                (willLoadBundleNames, proceed, cancel) =>
                {
                    True(willLoadBundleNames.Length == beforeRestoreLoadBundleNames.Length);
                    // Debug.Log("willLoadBundleNames:" + string.Join(", ", willLoadBundleNames));
                    proceed();
                },
                progress =>
                {
                    // Debug.Log("progress:" + progress);
                },
                () =>
                {
                    preloaded = true;
                },
                (code, reason, autoyaStatus) =>
                {
                    Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                    Autoya.AssetBundle_DeleteAllStorageCache();
                    Fail("preload failed. code:" + code + " reason:" + reason);
                },
                (downloadFailedAssetBundleName, code, reason, autoyaStatus) =>
                {
                    Autoya.AssetBundle_UnloadOnMemoryAssetBundles();
                    Autoya.AssetBundle_DeleteAllStorageCache();
                    Fail("failed to preload assetBundle:" + downloadFailedAssetBundleName + ". code:" + code + " reason:" + reason);
                },
                100
            );

            yield return WaitUntil(
                () => preloaded,
                () => { throw new TimeoutException("timeout."); }
            );
        }
    }


    // ABFeature自体のテストとして、Autoyaの起動時にダミーのManifestとStoredを生成し、食い違うことを確認する
    public IEnumerator CompareAssetBundleListOnBoot()
    {

        yield break;
    }
}