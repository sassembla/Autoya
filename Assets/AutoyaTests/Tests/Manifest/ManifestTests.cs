using System;
using System.Collections;
using System.IO;
using System.Linq;
using AutoyaFramework.AppManifest;
using Miyamasu;
using NUnit.Framework;
using UnityEngine;

public class ManifestTests : MiyamasuTestRunner
{
    private AppManifestStore<RuntimeManifestObject, BuildManifestObject> store;
    private readonly string filePath = Application.persistentDataPath + "/data";

    private bool Overwriter(string dataStr)
    {
        using (var sw = new StreamWriter(filePath))
        {
            sw.WriteLine(dataStr);
        }
        return true;
    }

    private string Loader()
    {
        using (var sr = new StreamReader(filePath))
        {
            return sr.ReadToEnd();
        }
    }

    [MSetup]
    public void Setup()
    {
        try
        {
            File.Delete(filePath);
        }
        catch
        {
            // do nothing.
        }

        try
        {
            var defaultData = new RuntimeManifestObject();
            var defaultDataStr = JsonUtility.ToJson(defaultData);
            Overwriter(defaultDataStr);
            store = new AppManifestStore<RuntimeManifestObject, BuildManifestObject>(Overwriter, Loader);
        }
        catch (Exception e)
        {
            Debug.Log("e:" + e);
        }
    }

    [MTeardown]
    public void Teardown()
    {
        try
        {
            File.Delete(filePath);
        }
        catch
        {
            // do nothing.
        }
    }

    // マニフェストを取得する
    [MTest]
    public IEnumerator GetManifest()
    {
        try
        {
            var manifestDict = store.GetParamDict();

            var any = false;
            foreach (var manifestParamItem in manifestDict)
            {
                any = true;
            }
            True(any);
        }
        catch (Exception e)
        {
            Debug.Log("e1:" + e);
        }

        yield break;
    }

    // runtimeマニフェストを上書きする。
    [MTest]
    public IEnumerator UpdateRuntimeManifest()
    {
        try
        {
            // load
            var oldOne = store.GetRuntimeManifest();

            foreach (var info in oldOne.resourceInfos)
            {
                info.listVersion = "1.1.0";
            }

            // update
            var succeeded = store.UpdateRuntimeManifest(oldOne);
            True(succeeded);

            var newOne = store.GetRuntimeManifest();
            foreach (var info in newOne.resourceInfos)
            {
                IsTrue(info.listVersion == "1.1.0");
            }

            // ここで、保存されているjsonファイルに対しても、変更が及んでいることを確認する。
            var savedData = Loader();
            var loadedOne = JsonUtility.FromJson<RuntimeManifestObject>(savedData);
            foreach (var info in loadedOne.resourceInfos)
            {
                IsTrue(info.listVersion == "1.1.0");
            }
        }
        catch (Exception e)
        {
            Debug.Log("e2:" + e);
        }

        yield break;
    }

    [MTest]
    public IEnumerator StoredHasOldABList()
    {
        var dummyListInfo = new AssetBundleListInfo
        {
            listIdentity = "dummy_list",
            listVersion = "1.0.0",
            listDownloadUrl = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles"
        };

        var sampleRuntimeManifest = new RuntimeManifestObject();
        sampleRuntimeManifest.resourceInfos = new AssetBundleListInfo[]{
            new AssetBundleListInfo
            {
                listIdentity = "main_assets",
                listVersion = "1.0.0",
                listDownloadUrl = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles"
            },
            new AssetBundleListInfo
            {
                listIdentity = "sub_assets",
                listVersion = "1.0.0",
                listDownloadUrl = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles"
            },
            new AssetBundleListInfo
            {
                listIdentity = "scenes",
                listVersion = "1.0.0",
                listDownloadUrl = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles"
            },
            dummyListInfo
        };

        var sampleJson = JsonUtility.ToJson(sampleRuntimeManifest);
        var compareBaseRuntimeManifest = new RuntimeManifestObject();
        compareBaseRuntimeManifest.UpdateFromStoredJson(sampleJson);

        var resourceInfosLists = compareBaseRuntimeManifest.resourceInfos;
        Assert.True(!resourceInfosLists.Contains(dummyListInfo), "not match.");

        foreach (var resourceInfosList in resourceInfosLists)
        {
            if (resourceInfosList.listIdentity == dummyListInfo.listIdentity)
            {
                Assert.Fail("should not contains stored and not contained in coded list.");
            }
        }

        yield break;
    }


    [MTest]
    public IEnumerator StoredDoesNotHaveRequiredABList()
    {
        var storedLatestVersion = "2.0.0";// stored has 2.x version.
        var sampleRuntimeManifest = new RuntimeManifestObject();
        sampleRuntimeManifest.resourceInfos = new AssetBundleListInfo[]{
            new AssetBundleListInfo
            {
                listIdentity = "main_assets",
                listVersion = storedLatestVersion,
                listDownloadUrl = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles"
            }
        };

        var sampleJson = JsonUtility.ToJson(sampleRuntimeManifest);
        var compareBaseRuntimeManifest = new RuntimeManifestObject();

        var defaultCodedContainedLists = compareBaseRuntimeManifest.resourceInfos;

        compareBaseRuntimeManifest.UpdateFromStoredJson(sampleJson);

        var mainAssetList = compareBaseRuntimeManifest.resourceInfos.Where(list => list.listIdentity == "main_assets").FirstOrDefault();
        Assert.True(mainAssetList.listVersion == storedLatestVersion, "not match.");
        yield break;
    }



    [MTest]
    public IEnumerator UpdatedHasStoredList()
    {
        var sampleRuntimeManifest = new RuntimeManifestObject();
        sampleRuntimeManifest.resourceInfos = new AssetBundleListInfo[]{
            new AssetBundleListInfo
            {
                listIdentity = "main_assets",
                listVersion = "1.0.0",
                listDownloadUrl = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles"
            }
        };

        var sampleJson = JsonUtility.ToJson(sampleRuntimeManifest);
        var compareBaseRuntimeManifest = new RuntimeManifestObject();

        var defaultCodedContainedLists = compareBaseRuntimeManifest.resourceInfos;

        compareBaseRuntimeManifest.UpdateFromStoredJson(sampleJson);

        var updatedIdentities = compareBaseRuntimeManifest.resourceInfos.Select(list => list.listIdentity).ToArray();
        var defaultCodedIdentities = defaultCodedContainedLists.Select(list => list.listIdentity).ToArray();
        foreach (var updatedIdentitiy in updatedIdentities)
        {
            Assert.True(defaultCodedIdentities.Contains(updatedIdentitiy), "not contained. updatedIdentitiy:" + updatedIdentitiy);
        }

        foreach (var defaultIdentitiy in defaultCodedIdentities)
        {
            Assert.True(updatedIdentities.Contains(defaultIdentitiy), "not contained. defaultIdentitiy:" + defaultIdentitiy);
        }
        yield break;
    }
}
