using System;
using System.Collections;
using System.IO;
using AutoyaFramework.AppManifest;
using Miyamasu;
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
}
