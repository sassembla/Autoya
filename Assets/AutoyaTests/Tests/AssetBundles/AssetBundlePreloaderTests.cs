using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using Miyamasu;
using UnityEngine;



/**
	tests for Autoya Download prloadList of partial AssetBundles.


*/
public class AssetBundlePreloaderTests : MiyamasuTestRunner {
    private const string preloadListBaseURLPath = "https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/PreloadLists/";

    private AssetBundlePreloader assetBundlePreloader;
    [MSetup] public void Setup () {
        assetBundlePreloader = new AssetBundlePreloader(preloadListBaseURLPath);
    }

    [MTeardownAttribute] public void Teardown () {

    }

	[MTest] public void GetPreloadList () {
        // Preloadが終わった時に着火されるハンドラがほしいね。そしたらほっておける。
        var done = false;
        
        RunEnumeratorOnMainThread(
            assetBundlePreloader.Preload(
                "sample.preloadList.json", 
                preloadedKey => {
                    done = true;
                }
            )
        );

        WaitUntil(() => done, 2, "not yet done.");
    }
    
    // [MTest] public void GetPreloadListButWholeABListIsChanged () {
        // なにかしらエラーが出せればいいと思うんだけど、エラーを出すコンテキストを集めておこう。まだ実装できない。
    //     Debug.LogError("特定のpreloadListを取得、書かれているAssetBundleを全て取得する、、、の開始時処理中で、リストそのものが書き換わったことを検知したので、停止する?");
    // }

    [MTest] public void GetPreloadListThenLoadByListName () {
        Debug.LogWarning("特定のpreloadListを取得、書かれているAssetBundleを全て取得する。キャッシュが終わったことを確認");
    }

}
