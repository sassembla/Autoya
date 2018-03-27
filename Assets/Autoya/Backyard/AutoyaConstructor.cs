using UnityEngine;
using AutoyaFramework.Connections.HTTP;
using AutoyaFramework.Persistence.Files;
using AutoyaFramework.Settings.Auth;
using AutoyaFramework.Purchase;
using System;
using AutoyaFramework.Settings.App;

/**
    constructor implementation of Autoya.
*/
namespace AutoyaFramework
{
    public partial class Autoya
    {

        private ICoroutineUpdater mainthreadDispatcher;

        /**
            all conditions which Autoya has.
        */
        private class AutoyaParameters
        {
            // public string _app_version;
            // public string _assets_version;

            // public string _buildNumber;
        }

        private Autoya(string basePath = "")
        {
            // Debug.LogWarning("autoya initialize start. basePath:" + basePath);

            var isPlayer = false;

            if (Application.isPlaying)
            {

                isPlayer = true;

                // create game object for Autoya.
                var go = GameObject.Find("AutoyaMainthreadDispatcher");
                if (go == null)
                {
                    go = new GameObject("AutoyaMainthreadDispatcher");
                    this.mainthreadDispatcher = go.AddComponent<AutoyaMainThreadDispatcher>();
                    GameObject.DontDestroyOnLoad(go);
                }
                else
                {
                    this.mainthreadDispatcher = go.GetComponent<AutoyaMainThreadDispatcher>();
                }
            }
            else
            {
                // create editor runnner for Autoya.
                this.mainthreadDispatcher = new EditorUpdator();
            }

            _autoyaFilePersistence = new FilePersistence(basePath);

            _autoyaHttp = new HTTPConnection();

            mainthreadDispatcher.Commit(
                OnBootApplication(),
                IsFirstBoot(
                    isFirstBoot =>
                    {
                        InitializeAppManifest();

                        InitializeAssetBundleFeature();

                        /*
                            start authentication.
                        */
                        Authenticate(
                            isFirstBoot,
                            () =>
                            {
                                /*
                                    initialize purchase feature.
                                */
                                if (isPlayer)
                                {
                                    ReloadPurchasability();
                                }
                            }
                        );
                    }
                )
            );


        }

        public static void Shutdown()
        {
            autoya.mainthreadDispatcher.Destroy();
        }
    }
}
