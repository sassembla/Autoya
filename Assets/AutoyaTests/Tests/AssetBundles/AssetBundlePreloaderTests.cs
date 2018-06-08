using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Settings.AssetBundles;
using Miyamasu;
using UnityEngine;



/**
	tests for download preloadList of the AssetBundles.
*/
public class AssetBundlePreloaderTests : MiyamasuTestRunner
{
    private string abListPath = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles/main_assets/" + AssetBundlesSettings.PLATFORM_STR + "/1.0.0/main_assets.json";

    private string abDlPath = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles/main_assets/" + AssetBundlesSettings.PLATFORM_STR + "/";

    private string preloadListDlPath = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles/preload/";

    private AssetBundlePreloader assetBundlePreloader;

    private AssetBundleLoader loader;

    [MSetup]
    public IEnumerator Setup()
    {
        assetBundlePreloader = new AssetBundlePreloader();

        var loaderTestObj = new AssetBundleLoaderTests();
        var listCor = loaderTestObj.LoadListFromWeb(abListPath);
        yield return listCor;

        var assetBundleList = listCor.Current as AssetBundleList;
        loader = new AssetBundleLoader(identity => abDlPath + assetBundleList.version + "/");
        loader.UpdateAssetBundleList(assetBundleList);


        var cleaned = loader.CleanCachedAssetBundles();

        if (!cleaned)
        {
            Fail("clean cache failed 1.");
        }
    }

    [MTeardownAttribute]
    public void Teardown()
    {
        var cleaned = loader.CleanCachedAssetBundles();

        if (!cleaned)
        {
            Fail("clean cache failed 2.");
        }
    }

    [MTest]
    public IEnumerator GetPreloadList()
    {
        var done = false;

        yield return assetBundlePreloader.Preload(
            loader,
            preloadListDlPath + "sample.preloadList.json",
            (willLoadBundleNames, proceed, cancel) =>
            {
                proceed();
            },
            progress =>
            {
                True(progress == 1.0, "not match. progress:" + progress);
            },
            () =>
            {
                True(!loader.IsAssetBundleCachedOnMemory("texturename"), "cached on memory.");
                True(loader.IsAssetBundleCachedOnStorage("texturename"), "not cached on storage.");
                done = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Debug.LogError("failed to download, code:" + code + " reason:" + reason);
            },
            (preloadFailedAssetBundleName, code, reason, autoyaStatus) =>
            {
                Debug.LogError("failed to download, name:" + preloadFailedAssetBundleName + " code:" + code);
            }
        );

        yield return WaitUntil(() => done, () => { throw new TimeoutException("not yet done."); });
    }

    [MTest]
    public IEnumerator PreloadWithCached_NoAdditionalDownload()
    {
        // preload once.
        yield return GetPreloadList();

        var done = false;
        yield return assetBundlePreloader.Preload(
            loader,
            preloadListDlPath + "sample.preloadList.json",
            (willLoadBundleNames, proceed, cancel) =>
            {
                proceed();
            },
            progress =>
            {
                Fail("should not be progress.");
            },
            () =>
            {
                done = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Debug.LogError("failed to download, code:" + code + " reason:" + reason);
            },
            (preloadFailedAssetBundleName, code, reason, autoyaStatus) =>
            {
                Debug.LogError("failed to download, name:" + preloadFailedAssetBundleName + " code:" + code);
            }
        );

        yield return WaitUntil(() => done, () => { throw new TimeoutException("not yet done."); });
    }

    [MTest]
    public IEnumerator PreloadWithCachedAndNotCached()
    {
        // preload once.
        yield return GetPreloadList();

        var doneCount = 0;
        yield return assetBundlePreloader.Preload(
            loader,
            preloadListDlPath + "sample.preloadList2.json",
            (willLoadBundleNames, proceed, cancel) =>
            {
                proceed();
            },
            progress =>
            {
                // 1.0
                True(progress == 1.0, "not match. progress:" + progress);
                doneCount++;
            },
            () =>
            {
                // do nothng.
            },
            (code, reason, autoyaStatus) =>
            {
                Debug.LogError("failed to download, code:" + code + " reason:" + reason);
            },
            (preloadFailedAssetBundleName, code, reason, autoyaStatus) =>
            {
                Debug.LogError("failed to download, name:" + preloadFailedAssetBundleName + " code:" + code);
            }
        );

        yield return WaitUntil(() => doneCount == 1, () => { throw new TimeoutException("not yet done."); });
    }

    [MTest]
    public IEnumerator Preload2AssetBundles()
    {
        var doneCount = 0;
        yield return assetBundlePreloader.Preload(
            loader,
            preloadListDlPath + "sample.preloadList2.json",
            (willLoadBundleNames, proceed, cancel) =>
            {
                proceed();
            },
            progress =>
            {
                // 0.5, 1 の2つが来るはず
                True(
                    progress == 0.5 ||
                    progress == 1.0,
                    "not match. progress:" + progress
                );
                doneCount++;
            },
            () =>
            {
                // do nothng.
            },
            (code, reason, autoyaStatus) =>
            {
                Debug.LogError("failed to download, code:" + code + " reason:" + reason);
            },
            (preloadFailedAssetBundleName, code, reason, autoyaStatus) =>
            {
                Debug.LogError("failed to download, name:" + preloadFailedAssetBundleName + " code:" + code);
            }
        );

        WaitUntil(() => doneCount == 2, () => { throw new TimeoutException("not yet done. doneCount:" + doneCount); });
    }

    [MTest]
    public IEnumerator PreloadWithPreloadList()
    {
        var preloadBundleNames = loader.GetWholeBundleNames();
        var preloadList = new PreloadList("PreloadWithPreloadList", preloadBundleNames);

        var doneCount = 0;

        yield return assetBundlePreloader.Preload(
            loader,
            preloadList,
            (willLoadBundleNames, proceed, cancel) =>
            {
                proceed();
            },
            progress =>
            {
                doneCount++;
            },
            () =>
            {
                // do nothng.
            },
            (code, reason, autoyaStatus) =>
            {
                Debug.LogError("failed to download, code:" + code + " reason:" + reason);
            },
            (preloadFailedAssetBundleName, code, reason, autoyaStatus) =>
            {
                Debug.LogError("failed to download, name:" + preloadFailedAssetBundleName + " code:" + code);
            }
        );

        WaitUntil(() => doneCount == preloadBundleNames.Length, () => { throw new TimeoutException("not yet done. doneCount:" + doneCount); });
    }

    [MTest]
    public IEnumerator ContinueGetPreloading()
    {
        var doneCount = 0;
        yield return assetBundlePreloader.Preload(
            loader,
            preloadListDlPath + "sample.preloadList2.json",
            (willLoadBundleNames, proceed, cancel) =>
            {
                proceed();
            },
            progress =>
            {
                // 0.5, 1 の2つが来るはず
                True(
                    progress == 0.5 ||
                    progress == 1.0,
                    "not match. progress:" + progress
                );
                doneCount++;
            },
            () =>
            {
                // do nothng.
            },
            (code, reason, autoyaStatus) =>
            {
                Debug.LogError("failed to download, code:" + code + " reason:" + reason);
            },
            (preloadFailedAssetBundleName, code, reason, autoyaStatus) =>
            {
                Debug.LogError("failed to download, name:" + preloadFailedAssetBundleName + " code:" + code + " reason:" + reason);
            }
        );

        WaitUntil(() => doneCount == 2, () => { throw new TimeoutException("not yet done. doneCount:" + doneCount); });
    }

    [MTest]
    public IEnumerator DiscontinueGetPreloading()
    {
        var done = false;
        yield return assetBundlePreloader.Preload(
            loader,
            preloadListDlPath + "sample.preloadList2.json",
            (willLoadBundleNames, proceed, cancel) =>
            {
                cancel();
            },
            progress =>
            {
                Fail("should not come here.");
            },
            () =>
            {
                done = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Fail("should not come here.");
            },
            (preloadFailedAssetBundleName, code, reason, autoyaStatus) =>
            {
                Fail("should not come here.");
            }
        );

        yield return WaitUntil(() => done, () => { throw new TimeoutException("not yet done."); });
    }

    [MTest]
    public IEnumerator GetPreloadingAssetBundleWeight()
    {
        var doneCount = 0;
        yield return assetBundlePreloader.Preload(
            loader,
            preloadListDlPath + "sample.preloadList2.json",
            (willLoadBundleNames, proceed, cancel) =>
            {
                True(0 < willLoadBundleNames.Length);
                var totalWeight = loader.GetAssetBundlesWeight(willLoadBundleNames);
                True(0 < totalWeight);
                proceed();
            },
            progress =>
            {
                doneCount++;
            },
            () =>
            {
                // do nothng.
            },
            (code, reason, autoyaStatus) =>
            {
                Debug.LogError("failed to download, code:" + code + " reason:" + reason);
            },
            (preloadFailedAssetBundleName, code, reason, autoyaStatus) =>
            {
                Debug.LogError("failed to download, name:" + preloadFailedAssetBundleName + " code:" + code);
            }
        );

        WaitUntil(() => doneCount == 2, () => { throw new TimeoutException("not yet done. doneCount:" + doneCount); });
    }

    [MTest]
    public IEnumerator GetPreloadingAssetBundleNames()
    {
        // Preloaderのテストで、途中で得られるbundleNameがキャッシュ状況によって変わるやつ
        Debug.LogWarning("not yet tested.");
        yield break;
    }

    [MTest]
    public IEnumerator GetPreloadingAssetBundleNamesWithCache()
    {
        // Preloaderのテストで、途中で得られるbundleNameがキャッシュ状況によって変わるやつ
        Debug.LogWarning("not yet tested.");
        yield break;
    }

}
