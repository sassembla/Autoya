
using UnityEngine.TestTools;
using System;
using System.Collections;
public class AssetBundleListDownloaderTests_Miyamasu {
    [UnityTest] public IEnumerator GetAssetBundleList() {
        var rec = new Miyamasu.Recorder("AssetBundleListDownloaderTests", "GetAssetBundleList");
        var instance = new AssetBundleListDownloaderTests();
        instance.rec = rec;

        
        
        yield return instance.GetAssetBundleList();
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator GetAssetBundleListFailed() {
        var rec = new Miyamasu.Recorder("AssetBundleListDownloaderTests", "GetAssetBundleListFailed");
        var instance = new AssetBundleListDownloaderTests();
        instance.rec = rec;

        
        
        yield return instance.GetAssetBundleListFailed();
        rec.MarkAsPassed();

        
    }
}
public class AssetBundleLoaderTests_Miyamasu {
    [UnityTest] public IEnumerator LoadAssetByAssetName() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "LoadAssetByAssetName");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.LoadAssetByAssetName();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator LoadSameAssetByAssetName() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "LoadSameAssetByAssetName");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.LoadSameAssetByAssetName();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator LoadAssetWithDependency() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "LoadAssetWithDependency");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.LoadAssetWithDependency();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator LoadSameAssetWithDependsOnOneAssetBundle() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "LoadSameAssetWithDependsOnOneAssetBundle");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.LoadSameAssetWithDependsOnOneAssetBundle();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator Load2Assets_1isDependsOnAnother_DependedFirst() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "Load2Assets_1isDependsOnAnother_DependedFirst");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.Load2Assets_1isDependsOnAnother_DependedFirst();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator Load2Assets_1isDependsOnAnother_DependingFirst() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "Load2Assets_1isDependsOnAnother_DependingFirst");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.Load2Assets_1isDependsOnAnother_DependingFirst();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator Load2AssetsWhichDependsOnSameAssetBundle() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "Load2AssetsWhichDependsOnSameAssetBundle");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.Load2AssetsWhichDependsOnSameAssetBundle();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator NestedDependency() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "NestedDependency");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.NestedDependency();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator LoadCrcMismatchedBundle() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "LoadCrcMismatchedBundle");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.LoadCrcMismatchedBundle();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator LoadMissingBundle() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "LoadMissingBundle");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.LoadMissingBundle();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator LoadMissingDependentBundle() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "LoadMissingDependentBundle");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.LoadMissingDependentBundle();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator LoadBundleWithTimeout() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "LoadBundleWithTimeout");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.LoadBundleWithTimeout();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator LoadAllAssetsOnce() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "LoadAllAssetsOnce");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.LoadAllAssetsOnce();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator OnMemoryBundleNames() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "OnMemoryBundleNames");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.OnMemoryBundleNames();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator OnMemoryAssetNames() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "OnMemoryAssetNames");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.OnMemoryAssetNames();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator UnloadAllAssetBundles() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "UnloadAllAssetBundles");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.UnloadAllAssetBundles();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator GetContainedAssetBundleName() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "GetContainedAssetBundleName");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.GetContainedAssetBundleName();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator UnloadAssetBundle() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "UnloadAssetBundle");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.UnloadAssetBundle();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator IsBundleCachedOnStorage() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "IsBundleCachedOnStorage");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.IsBundleCachedOnStorage();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator IsBundleCachedOnMemory() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "IsBundleCachedOnMemory");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.IsBundleCachedOnMemory();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator AssetBundleInfoFromAssetName() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "AssetBundleInfoFromAssetName");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.AssetBundleInfoFromAssetName();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator GetAssetBundleSize() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "GetAssetBundleSize");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.GetAssetBundleSize();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator GetSameAssetBundleOnceThenFailToDownload() {
        var rec = new Miyamasu.Recorder("AssetBundleLoaderTests", "GetSameAssetBundleOnceThenFailToDownload");
        var instance = new AssetBundleLoaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.GetSameAssetBundleOnceThenFailToDownload();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
}
public class AssetBundlePreloaderTests_Miyamasu {
    [UnityTest] public IEnumerator GetPreloadList() {
        var rec = new Miyamasu.Recorder("AssetBundlePreloaderTests", "GetPreloadList");
        var instance = new AssetBundlePreloaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.GetPreloadList();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator PreloadWithCached_NoAdditionalDownload() {
        var rec = new Miyamasu.Recorder("AssetBundlePreloaderTests", "PreloadWithCached_NoAdditionalDownload");
        var instance = new AssetBundlePreloaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.PreloadWithCached_NoAdditionalDownload();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator PreloadWithCachedAndNotCached() {
        var rec = new Miyamasu.Recorder("AssetBundlePreloaderTests", "PreloadWithCachedAndNotCached");
        var instance = new AssetBundlePreloaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.PreloadWithCachedAndNotCached();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator Preload2AssetBundles() {
        var rec = new Miyamasu.Recorder("AssetBundlePreloaderTests", "Preload2AssetBundles");
        var instance = new AssetBundlePreloaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.Preload2AssetBundles();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator PreloadWithPreloadList() {
        var rec = new Miyamasu.Recorder("AssetBundlePreloaderTests", "PreloadWithPreloadList");
        var instance = new AssetBundlePreloaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.PreloadWithPreloadList();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator ContinueGetPreloading() {
        var rec = new Miyamasu.Recorder("AssetBundlePreloaderTests", "ContinueGetPreloading");
        var instance = new AssetBundlePreloaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.ContinueGetPreloading();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator DiscontinueGetPreloading() {
        var rec = new Miyamasu.Recorder("AssetBundlePreloaderTests", "DiscontinueGetPreloading");
        var instance = new AssetBundlePreloaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.DiscontinueGetPreloading();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator GetPreloadingAssetBundleWeight() {
        var rec = new Miyamasu.Recorder("AssetBundlePreloaderTests", "GetPreloadingAssetBundleWeight");
        var instance = new AssetBundlePreloaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.GetPreloadingAssetBundleWeight();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator GetPreloadingAssetBundleNames() {
        var rec = new Miyamasu.Recorder("AssetBundlePreloaderTests", "GetPreloadingAssetBundleNames");
        var instance = new AssetBundlePreloaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.GetPreloadingAssetBundleNames();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator GetPreloadingAssetBundleNamesWithCache() {
        var rec = new Miyamasu.Recorder("AssetBundlePreloaderTests", "GetPreloadingAssetBundleNamesWithCache");
        var instance = new AssetBundlePreloaderTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.GetPreloadingAssetBundleNamesWithCache();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
}
public class AppUpdateTests_Miyamasu {
    [UnityTest] public IEnumerator ReceiveAppUpdate() {
        var rec = new Miyamasu.Recorder("AppUpdateTests", "ReceiveAppUpdate");
        var instance = new AppUpdateTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.ReceiveAppUpdate();
        rec.MarkAsPassed();

        
    }
}
public class AssetBundlesImplementationTests_Miyamasu {
    [UnityTest] public IEnumerator GetAssetBundleListFromDebugMethod() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "GetAssetBundleListFromDebugMethod");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.GetAssetBundleListFromDebugMethod();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator GetAssetBundleList() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "GetAssetBundleList");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.GetAssetBundleList();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator GetAssetBundleListUrl() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "GetAssetBundleListUrl");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.GetAssetBundleListUrl();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator GetAssetBundleListFailThenTryAgain() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "GetAssetBundleListFailThenTryAgain");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.GetAssetBundleListFailThenTryAgain();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator GetAssetBundleBeforeGetAssetBundleListBecomeFailed() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "GetAssetBundleBeforeGetAssetBundleListBecomeFailed");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.GetAssetBundleBeforeGetAssetBundleListBecomeFailed();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator GetAssetBundle() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "GetAssetBundle");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.GetAssetBundle();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator PreloadAssetBundleBeforeGetAssetBundleListWillFail() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "PreloadAssetBundleBeforeGetAssetBundleListWillFail");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.PreloadAssetBundleBeforeGetAssetBundleListWillFail();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator PreloadAssetBundle() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "PreloadAssetBundle");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.PreloadAssetBundle();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator PreloadAssetBundles() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "PreloadAssetBundles");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.PreloadAssetBundles();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator PreloadAssetBundleWithGeneratedPreloadList() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "PreloadAssetBundleWithGeneratedPreloadList");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.PreloadAssetBundleWithGeneratedPreloadList();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator PreloadAssetBundlesWithGeneratedPreloadList() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "PreloadAssetBundlesWithGeneratedPreloadList");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.PreloadAssetBundlesWithGeneratedPreloadList();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator IsAssetExistInAssetBundleList() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "IsAssetExistInAssetBundleList");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.IsAssetExistInAssetBundleList();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator IsAssetBundleExistInAssetBundleList() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "IsAssetBundleExistInAssetBundleList");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.IsAssetBundleExistInAssetBundleList();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator AssetBundle_NotCachedBundleNames() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "AssetBundle_NotCachedBundleNames");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.AssetBundle_NotCachedBundleNames();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator UpdateListWithOnMemoryAssets() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "UpdateListWithOnMemoryAssets");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.UpdateListWithOnMemoryAssets();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator UpdateListWithOnMemoryAssetsThenReloadChangedAsset() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "UpdateListWithOnMemoryAssetsThenReloadChangedAsset");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.UpdateListWithOnMemoryAssetsThenReloadChangedAsset();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator UpdateListWithOnMemoryAssetsThenPRreloadChangedAsset() {
        var rec = new Miyamasu.Recorder("AssetBundlesImplementationTests", "UpdateListWithOnMemoryAssetsThenPRreloadChangedAsset");
        var instance = new AssetBundlesImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.UpdateListWithOnMemoryAssetsThenPRreloadChangedAsset();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
}
public class AssetUpdateTests_Miyamasu {
    [UnityTest] public IEnumerator ReceiveFirstList() {
        var rec = new Miyamasu.Recorder("AssetUpdateTests", "ReceiveFirstList");
        var instance = new AssetUpdateTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.ReceiveFirstList();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator ReceiveListUpdated() {
        var rec = new Miyamasu.Recorder("AssetUpdateTests", "ReceiveListUpdated");
        var instance = new AssetUpdateTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.ReceiveListUpdated();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator ReceiveUpdatedListThenListWillBeUpdated() {
        var rec = new Miyamasu.Recorder("AssetUpdateTests", "ReceiveUpdatedListThenListWillBeUpdated");
        var instance = new AssetUpdateTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.ReceiveUpdatedListThenListWillBeUpdated();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator ReceiveUpdatedListThenIgnore() {
        var rec = new Miyamasu.Recorder("AssetUpdateTests", "ReceiveUpdatedListThenIgnore");
        var instance = new AssetUpdateTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.ReceiveUpdatedListThenIgnore();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator ReceiveUpdatedListThenIgnoreAndIgnoredListIsCached() {
        var rec = new Miyamasu.Recorder("AssetUpdateTests", "ReceiveUpdatedListThenIgnoreAndIgnoredListIsCached");
        var instance = new AssetUpdateTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.ReceiveUpdatedListThenIgnoreAndIgnoredListIsCached();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
}
public class AuthenticatedHTTPImplementationTests_Miyamasu {
    [UnityTest] public IEnumerator AutoyaHTTPGet() {
        var rec = new Miyamasu.Recorder("AuthenticatedHTTPImplementationTests", "AutoyaHTTPGet");
        var instance = new AuthenticatedHTTPImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.AutoyaHTTPGet();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator AutoyaHTTPGetWithAdditionalHeader() {
        var rec = new Miyamasu.Recorder("AuthenticatedHTTPImplementationTests", "AutoyaHTTPGetWithAdditionalHeader");
        var instance = new AuthenticatedHTTPImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.AutoyaHTTPGetWithAdditionalHeader();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator AutoyaHTTPGetFailWith404() {
        var rec = new Miyamasu.Recorder("AuthenticatedHTTPImplementationTests", "AutoyaHTTPGetFailWith404");
        var instance = new AuthenticatedHTTPImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.AutoyaHTTPGetFailWith404();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator AutoyaHTTPGetFailWithUnauth() {
        var rec = new Miyamasu.Recorder("AuthenticatedHTTPImplementationTests", "AutoyaHTTPGetFailWithUnauth");
        var instance = new AuthenticatedHTTPImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.AutoyaHTTPGetFailWithUnauth();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator AutoyaHTTPGetFailWithTimeout() {
        var rec = new Miyamasu.Recorder("AuthenticatedHTTPImplementationTests", "AutoyaHTTPGetFailWithTimeout");
        var instance = new AuthenticatedHTTPImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.AutoyaHTTPGetFailWithTimeout();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator AutoyaHTTPPost() {
        var rec = new Miyamasu.Recorder("AuthenticatedHTTPImplementationTests", "AutoyaHTTPPost");
        var instance = new AuthenticatedHTTPImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.AutoyaHTTPPost();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator AutoyaHTTPPostFailWith404() {
        var rec = new Miyamasu.Recorder("AuthenticatedHTTPImplementationTests", "AutoyaHTTPPostFailWith404");
        var instance = new AuthenticatedHTTPImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.AutoyaHTTPPostFailWith404();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator AutoyaHTTPPostFailWithUnauth() {
        var rec = new Miyamasu.Recorder("AuthenticatedHTTPImplementationTests", "AutoyaHTTPPostFailWithUnauth");
        var instance = new AuthenticatedHTTPImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.AutoyaHTTPPostFailWithUnauth();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator AutoyaHTTPPostFailWithTimeout() {
        var rec = new Miyamasu.Recorder("AuthenticatedHTTPImplementationTests", "AutoyaHTTPPostFailWithTimeout");
        var instance = new AuthenticatedHTTPImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.AutoyaHTTPPostFailWithTimeout();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
}
public class AuthImplementationTests_Miyamasu {
    [UnityTest] public IEnumerator WaitDefaultAuthenticate() {
        var rec = new Miyamasu.Recorder("AuthImplementationTests", "WaitDefaultAuthenticate");
        var instance = new AuthImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.WaitDefaultAuthenticate();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator DeleteAllUserData() {
        var rec = new Miyamasu.Recorder("AuthImplementationTests", "DeleteAllUserData");
        var instance = new AuthImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.DeleteAllUserData();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator HandleBootAuthFailed() {
        var rec = new Miyamasu.Recorder("AuthImplementationTests", "HandleBootAuthFailed");
        var instance = new AuthImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.HandleBootAuthFailed();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator HandleBootAuthFailedThenAttemptAuthentication() {
        var rec = new Miyamasu.Recorder("AuthImplementationTests", "HandleBootAuthFailedThenAttemptAuthentication");
        var instance = new AuthImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.HandleBootAuthFailedThenAttemptAuthentication();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator HandleLogoutThenAuthenticationAttemptSucceeded() {
        var rec = new Miyamasu.Recorder("AuthImplementationTests", "HandleLogoutThenAuthenticationAttemptSucceeded");
        var instance = new AuthImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.HandleLogoutThenAuthenticationAttemptSucceeded();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator IntentionalLogout() {
        var rec = new Miyamasu.Recorder("AuthImplementationTests", "IntentionalLogout");
        var instance = new AuthImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.IntentionalLogout();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator HandleTokenRefreshFailed() {
        var rec = new Miyamasu.Recorder("AuthImplementationTests", "HandleTokenRefreshFailed");
        var instance = new AuthImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.HandleTokenRefreshFailed();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator HandleTokenRefreshFailedThenAttemptAuthentication() {
        var rec = new Miyamasu.Recorder("AuthImplementationTests", "HandleTokenRefreshFailedThenAttemptAuthentication");
        var instance = new AuthImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.HandleTokenRefreshFailedThenAttemptAuthentication();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator UnauthorizedThenHttpGet() {
        var rec = new Miyamasu.Recorder("AuthImplementationTests", "UnauthorizedThenHttpGet");
        var instance = new AuthImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.UnauthorizedThenHttpGet();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
    [UnityTest] public IEnumerator AvoidHttpAuthFailCascade() {
        var rec = new Miyamasu.Recorder("AuthImplementationTests", "AvoidHttpAuthFailCascade");
        var instance = new AuthImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.AvoidHttpAuthFailCascade();
        rec.MarkAsPassed();

        
        yield return instance.Teardown();
    }
}
public class MaintenanceTests_Miyamasu {
    [UnityTest] public IEnumerator Maintenance() {
        var rec = new Miyamasu.Recorder("MaintenanceTests", "Maintenance");
        var instance = new MaintenanceTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.Maintenance();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator SetOnMaintenance() {
        var rec = new Miyamasu.Recorder("MaintenanceTests", "SetOnMaintenance");
        var instance = new MaintenanceTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.SetOnMaintenance();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
}
public class PurchaseImplementationTests_Miyamasu {
    [UnityTest] public IEnumerator GetProductInfos() {
        var rec = new Miyamasu.Recorder("PurchaseImplementationTests", "GetProductInfos");
        var instance = new PurchaseImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.GetProductInfos();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator PurchaseViaAutoya() {
        var rec = new Miyamasu.Recorder("PurchaseImplementationTests", "PurchaseViaAutoya");
        var instance = new PurchaseImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.PurchaseViaAutoya();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator RetrievePaidPurchase() {
        var rec = new Miyamasu.Recorder("PurchaseImplementationTests", "RetrievePaidPurchase");
        var instance = new PurchaseImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.RetrievePaidPurchase();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator MaintenainceInPurchase() {
        var rec = new Miyamasu.Recorder("PurchaseImplementationTests", "MaintenainceInPurchase");
        var instance = new PurchaseImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.MaintenainceInPurchase();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator AuthFailedInPurchase() {
        var rec = new Miyamasu.Recorder("PurchaseImplementationTests", "AuthFailedInPurchase");
        var instance = new PurchaseImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.AuthFailedInPurchase();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator PurchaseReadyGetProductsFail() {
        var rec = new Miyamasu.Recorder("PurchaseImplementationTests", "PurchaseReadyGetProductsFail");
        var instance = new PurchaseImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.PurchaseReadyGetProductsFail();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator PurchaseReadyGetProductsFailThenReadyAgain() {
        var rec = new Miyamasu.Recorder("PurchaseImplementationTests", "PurchaseReadyGetProductsFailThenReadyAgain");
        var instance = new PurchaseImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.PurchaseReadyGetProductsFailThenReadyAgain();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
}
public class EncryptTests_Miyamasu {
    [UnityTest] public IEnumerator AESEncrypt() {
        var rec = new Miyamasu.Recorder("EncryptTests", "AESEncrypt");
        var instance = new EncryptTests();
        instance.rec = rec;

        
        
        instance.AESEncrypt(); yield return null;
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator AESEncryptLong() {
        var rec = new Miyamasu.Recorder("EncryptTests", "AESEncryptLong");
        var instance = new EncryptTests();
        instance.rec = rec;

        
        
        instance.AESEncryptLong(); yield return null;
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator Sha256Hash() {
        var rec = new Miyamasu.Recorder("EncryptTests", "Sha256Hash");
        var instance = new EncryptTests();
        instance.rec = rec;

        
        
        instance.Sha256Hash(); yield return null;
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator Sha512Hash() {
        var rec = new Miyamasu.Recorder("EncryptTests", "Sha512Hash");
        var instance = new EncryptTests();
        instance.rec = rec;

        
        
        instance.Sha512Hash(); yield return null;
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator RIPEMD160Hash() {
        var rec = new Miyamasu.Recorder("EncryptTests", "RIPEMD160Hash");
        var instance = new EncryptTests();
        instance.rec = rec;

        
        
        instance.RIPEMD160Hash(); yield return null;
        rec.MarkAsPassed();

        
    }
}
public class ManifestTests_Miyamasu {
    [UnityTest] public IEnumerator GetManifest() {
        var rec = new Miyamasu.Recorder("ManifestTests", "GetManifest");
        var instance = new ManifestTests();
        instance.rec = rec;

        
        try {
            instance.Setup();
        } catch (Exception e) {
            rec.SetupFailed(e);
            throw;
        }
        
        yield return instance.GetManifest();
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator UpdateRuntimeManifest() {
        var rec = new Miyamasu.Recorder("ManifestTests", "UpdateRuntimeManifest");
        var instance = new ManifestTests();
        instance.rec = rec;

        
        try {
            instance.Setup();
        } catch (Exception e) {
            rec.SetupFailed(e);
            throw;
        }
        
        yield return instance.UpdateRuntimeManifest();
        rec.MarkAsPassed();

        
    }
}
public class FilePersistImplementationTests_Miyamasu {
    [UnityTest] public IEnumerator Update() {
        var rec = new Miyamasu.Recorder("FilePersistImplementationTests", "Update");
        var instance = new FilePersistImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.Update();
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator Append() {
        var rec = new Miyamasu.Recorder("FilePersistImplementationTests", "Append");
        var instance = new FilePersistImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.Append();
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator Load() {
        var rec = new Miyamasu.Recorder("FilePersistImplementationTests", "Load");
        var instance = new FilePersistImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.Load();
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator LoadFail() {
        var rec = new Miyamasu.Recorder("FilePersistImplementationTests", "LoadFail");
        var instance = new FilePersistImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.LoadFail();
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator Delete() {
        var rec = new Miyamasu.Recorder("FilePersistImplementationTests", "Delete");
        var instance = new FilePersistImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.Delete();
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator DeleteByDomain() {
        var rec = new Miyamasu.Recorder("FilePersistImplementationTests", "DeleteByDomain");
        var instance = new FilePersistImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.DeleteByDomain();
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator DeleteNonExist() {
        var rec = new Miyamasu.Recorder("FilePersistImplementationTests", "DeleteNonExist");
        var instance = new FilePersistImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.DeleteNonExist();
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator FileNamesInDomain() {
        var rec = new Miyamasu.Recorder("FilePersistImplementationTests", "FileNamesInDomain");
        var instance = new FilePersistImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.FileNamesInDomain();
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator EmptyFileNamesInDomain() {
        var rec = new Miyamasu.Recorder("FilePersistImplementationTests", "EmptyFileNamesInDomain");
        var instance = new FilePersistImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.EmptyFileNamesInDomain();
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator CreateFileThenDeleteFileThenFileNamesInDomain() {
        var rec = new Miyamasu.Recorder("FilePersistImplementationTests", "CreateFileThenDeleteFileThenFileNamesInDomain");
        var instance = new FilePersistImplementationTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.CreateFileThenDeleteFileThenFileNamesInDomain();
        rec.MarkAsPassed();

        
    }
}
public class PurchaseRouterTests_Miyamasu {
    [UnityTest] public IEnumerator ShowProductInfos() {
        var rec = new Miyamasu.Recorder("PurchaseRouterTests", "ShowProductInfos");
        var instance = new PurchaseRouterTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.ShowProductInfos();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator Purchase() {
        var rec = new Miyamasu.Recorder("PurchaseRouterTests", "Purchase");
        var instance = new PurchaseRouterTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.Purchase();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator RetryPurchaseThenFail() {
        var rec = new Miyamasu.Recorder("PurchaseRouterTests", "RetryPurchaseThenFail");
        var instance = new PurchaseRouterTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.RetryPurchaseThenFail();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator RetryPurchaseThenFinallySuccess() {
        var rec = new Miyamasu.Recorder("PurchaseRouterTests", "RetryPurchaseThenFinallySuccess");
        var instance = new PurchaseRouterTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.RetryPurchaseThenFinallySuccess();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
    [UnityTest] public IEnumerator RetryPurchaseThenFailThenComplete() {
        var rec = new Miyamasu.Recorder("PurchaseRouterTests", "RetryPurchaseThenFailThenComplete");
        var instance = new PurchaseRouterTests();
        instance.rec = rec;

        
        yield return instance.Setup();
        
        yield return instance.RetryPurchaseThenFailThenComplete();
        rec.MarkAsPassed();

        
        try {
            instance.Teardown();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }
    }
}
public class JWTTests_Miyamasu {
    [UnityTest] public IEnumerator Read() {
        var rec = new Miyamasu.Recorder("JWTTests", "Read");
        var instance = new JWTTests();
        instance.rec = rec;

        
        
        instance.Read(); yield return null;
        rec.MarkAsPassed();

        
    }
    [UnityTest] public IEnumerator Create() {
        var rec = new Miyamasu.Recorder("JWTTests", "Create");
        var instance = new JWTTests();
        instance.rec = rec;

        
        
        instance.Create(); yield return null;
        rec.MarkAsPassed();

        
    }
}