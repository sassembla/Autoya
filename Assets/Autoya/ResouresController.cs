// using System;
// using System.Collections;
// using AutoyaFramework;
// using UnityEngine;

// public class ResourcesController
// {
//     public static void LoadAsset<T>(string assetPath, Action<string, T> succeeded, Action<string, int, string, object> failed) where T : UnityEngine.Object
//     {
//         var resRequest = Resources.LoadAsync(assetPath);
//         var cor = RequestCoroutine(assetPath, resRequest, succeeded, failed);
//         Autoya.Mainthread_Commit(cor);
//     }

//     private static IEnumerator RequestCoroutine<T>(string assetPath, ResourceRequest req, Action<string, T> succeeded, Action<string, int, string, object> failed) where T : UnityEngine.Object
//     {
//         while (!req.isDone)
//         {
//             yield return null;
//         }

//         if (req.asset == null)
//         {
//             failed(assetPath, -1, "failed to find asset.", new object());
//             yield break;
//         }

//         // req.asset is not null.
//         Debug.Log("req.asset:" + req.asset);

//         var casted = req.asset as T;
//         if (casted == null)
//         {
//             failed(assetPath, -2, "failed to cast asset to required type.", new object());
//             yield break;
//         }

//         succeeded(assetPath, casted);
//     }
// }