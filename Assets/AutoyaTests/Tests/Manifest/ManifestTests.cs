using System.Collections;
using System.IO;
using AutoyaFramework.AppManifest;
using Miyamasu;
using UnityEngine;

public class ManifestTests : MiyamasuTestRunner
{
    private AppManifestStore<RuntimeManifestObject, BuildManifestObject> store;

    const string tempSavePath = "Temp/Autoya.test.manifest";

    private bool Overwriter(string dataStr)
    {
        using (var sw = new StreamWriter(tempSavePath))
        {
            sw.WriteLine(dataStr);
        }
        return true;
    }

    private string Loader()
    {
        using (var sr = new StreamReader(tempSavePath))
        {
            return sr.ReadToEnd();
        }
    }

    [MSetup]
    public void Setup()
    {
        var defaultData = new RuntimeManifestObject();
        var defaultDataStr = JsonUtility.ToJson(defaultData);
        Overwriter(defaultDataStr);
        store = new AppManifestStore<RuntimeManifestObject, BuildManifestObject>(Overwriter, Loader);
    }

    // マニフェストを取得する
    [MTest]
    public IEnumerator GetManifest()
    {
        var manifestDict = store.GetParamDict();

        var any = false;
        foreach (var manifestParamItem in manifestDict)
        {
            any = true;
        }
        True(any);

        yield break;
    }

    // runtimeマニフェストを上書きする。
    [MTest]
    public IEnumerator UpdateRuntimeManifest()
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

        yield break;
    }
}
