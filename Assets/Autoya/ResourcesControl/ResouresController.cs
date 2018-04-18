using System;
using System.Collections;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using UnityEngine;

/*
    Resources feature support for Autoya.
    Resources_LoadAsset<T> method signature is equivalent to AssetBundle_LoadAsset<T> feature. easy to swap.
 */
namespace AutoyaFramework
{
    public partial class Autoya
    {
        public static void Resources_LoadAsset<T>(string assetPath, Action<string, T> succeeded, Action<string, AssetBundleLoadError, string, AutoyaStatus> failed) where T : UnityEngine.Object
        {
            var resRequest = Resources.LoadAsync<T>(assetPath);
            var cor = RequestCoroutine(assetPath, resRequest, succeeded, failed);
            Autoya.Mainthread_Commit(cor);
        }

        private static IEnumerator RequestCoroutine<T>(string assetName, ResourceRequest req, Action<string, T> succeeded, Action<string, AssetBundleLoadError, string, AutoyaStatus> loadFailed) where T : UnityEngine.Object
        {
            while (!req.isDone)
            {
                yield return null;
            }

            if (req.asset == null)
            {
                loadFailed(assetName, AssetBundleLoadError.NotContained, "searching asset name:" + assetName + " is not contained by any AssetBundles in all AssetBundleList.", new AutoyaStatus());
                yield break;
            }

            // req.asset is not null.

            var casted = req.asset as T;
            if (casted == null)
            {
                loadFailed(assetName, AssetBundleLoadError.NullAssetFound, "loaded assetName:" + assetName + " type:" + typeof(T) + " is null. maybe type does not matched. please check asset type and that bundle contains this asset.", new AutoyaStatus());
                yield break;
            }

            succeeded(assetName, casted);
        }

        public static void Resources_Unload(UnityEngine.Object obj)
        {
            Resources.UnloadAsset(obj);
        }
    }
}
