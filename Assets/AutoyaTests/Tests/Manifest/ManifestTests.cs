using System.Collections;
using AutoyaFramework.AppManifest;
using Miyamasu;
using UnityEngine;

public class ManifestTests : MiyamasuTestRunner {
    AppManifestStore<RuntimeManifestObject> store;

    private bool Overwriter (string dataStr) {
        return true;
    }

    private string Loader () {
        return string.Empty;
    }

    [MSetup] public void Setup () {
        store = new AppManifestStore<RuntimeManifestObject>(Overwriter, Loader);
    }
    
    // マニフェストを取得する
	[MTest] public IEnumerator GetManifest () {
        var manifestDict = store.GetParamDict();
        
        var any = false;
        foreach (var manifestParamItem in manifestDict) {
            any = true;
        }
        True(any);
        
        yield break;
	}

    [MTest] public IEnumerator UpdateRuntimeManifest () {
        // load
        var oldOne = store.GetRuntimeManifest();

        oldOne.resVersion = "1.1.0";

        // update
        var succeeded = store.UpdateRuntimeManifest(oldOne);
        True(succeeded);
        
        var newOne = store.GetRuntimeManifest();
        True(newOne.resVersion == "1.1.0");

        yield break;
	}
}
