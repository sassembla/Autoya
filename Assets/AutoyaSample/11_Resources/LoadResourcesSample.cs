using System;
using System.Collections;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Settings.AssetBundles;
using UnityEngine;
using UnityEngine.UI;

public class LoadResourcesSample : MonoBehaviour
{
    public Image image;
    private Sprite sprite;

    void Start()
    {
        Autoya.Resources_LoadAsset<Sprite>(
            "SampleResource/shisyamo",
            (assetName, sprite) =>
            {
                Debug.Log("asset:" + assetName + " is successfully loaded as:" + sprite);

                image.sprite = sprite;
            },
            (assetName, err, reason, autoyaStatus) =>
            {
                Debug.LogError("failed to load assetName:" + assetName + " err:" + err + " reason:" + reason + " autoyaStatus:" + autoyaStatus);
            }
        );
    }

    void OnApplicationQuit()
    {
        Autoya.Resources_Unload(sprite);
    }

}

