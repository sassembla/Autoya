using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Settings.AssetBundles;
using Miyamasu;
using UnityEngine;



/**
	tests for Autoya Download list of whole AssetBundles.
*/
public class AssetBundleListDownloaderTests : MiyamasuTestRunner
{
    private string abListPath = "https://raw.githubusercontent.com/sassembla/Autoya/assetbundle_multi_list_support/AssetBundles/main_assets/" + "OSX/";

    [MTest]
    public IEnumerator GetAssetBundleList()
    {
        var listPath = abListPath + "1.0.0/main_assets.json";
        var listDownloader = new AssetBundleListDownloader();

        var done = false;
        yield return listDownloader.DownloadAssetBundleList(
            listPath,
            (downloadedUrl, list) =>
            {
                done = true;
            },
            (code, reason, autoyaStatus) =>
            {
                Fail("failed to get list." + code + " reason:" + reason);
                // do nothing.
            }
        );

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("not yet get assetBundleList."); }
        );
    }

    [MTest]
    public IEnumerator GetAssetBundleListFailed()
    {
        var listPath = abListPath + "FAKEPATH";
        var loader = new AssetBundleListDownloader();

        var done = false;
        yield return loader.DownloadAssetBundleList(
            listPath,
            (downloadedUrl, list) =>
            {
                True(false, "should not be succeeded.");
            },
            (code, reason, autoyaStatus) =>
            {
                True(code == 404, "error code does not match.");
                done = true;
            }
        );

        yield return WaitUntil(
            () => done, () => { throw new TimeoutException("not yet get assetBundleList."); }
        );
    }
}
