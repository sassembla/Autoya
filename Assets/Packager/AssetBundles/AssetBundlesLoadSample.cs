using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.AssetBundles;
using UnityEngine;

public class AssetBundlesLoadSample : MonoBehaviour
{
    IEnumerator Start()
    {
        var LIST_DOWNLOAD_URL = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles/main_assets/OSX/1.0.0/main_assets.json";
        var ASSET_NAME = "Assets/AutoyaTests/RuntimeData/AssetBundles/MainResources/textureName.png";

        // Download AssetBundle List from web.
        var listDownloader = new AssetBundleListDownloader();

        AssetBundleList list = null;
        var cor = listDownloader.DownloadAssetBundleList(
            LIST_DOWNLOAD_URL,
            (url, newList) =>
            {
                list = newList;
            },
            (code, reason, status) =>
            {
                Debug.LogError("list download error, code:" + code + " reason:" + reason);
            }
        );

        while (cor.MoveNext())
        {
            yield return null;
        }


        // Load Asset from your AssetBundle.
        var loader = new AssetBundleLoader(
            bundleName =>
            {
                var path = "https://raw.githubusercontent.com/sassembla/Autoya/master/AssetBundles/" +
                    list.identity + "/" +
                    list.target + "/" +
                    list.version + "/";
                return path;
            }
        );

        // set list to AssetBundleLoader.
        loader.UpdateAssetBundleList(list);

        var loadCor = loader.LoadAsset<Texture2D>(
            ASSET_NAME,
            (name, tex) =>
            {
                Debug.Log("asset loaded! tex:" + tex);
            },
            (name, error, reason, status) =>
            {
                Debug.LogError("asset load failed. error:" + error + " reason:" + reason);
            }
        );

        while (loadCor.MoveNext())
        {
            yield return null;
        }
    }
}
