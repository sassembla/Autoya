using UnityEngine;
using AutoyaFramework.Connections.HTTP;
using AutoyaFramework.Persistence.Files;
using AutoyaFramework.Settings.Auth;
using AutoyaFramework.Purchase;
using System;
using AutoyaFramework.Settings.App;
using AutoyaFramework.Notification;

/**
    constructor implementation of Autoya.
*/
namespace AutoyaFramework
{
    public partial class Autoya
    {

        private ICoroutineUpdater mainthreadDispatcher;

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
                    this.mainthreadDispatcher = go.AddComponent<AutoyaMainthreadDispatcher>();
                    GameObject.DontDestroyOnLoad(go);
                }
                else
                {
                    this.mainthreadDispatcher = go.GetComponent<AutoyaMainthreadDispatcher>();
                }
            }
            else
            {
                // create editor runnner for Autoya.
                this.mainthreadDispatcher = new EditorUpdator();
            }

            _autoyaFilePersistence = new FilePersistence(basePath);
            _notification = new Notifications(AutoyaMainthreadDispatcher.AddNativeObserver);
            _autoyaHttp = new HTTPConnection();

            InitializeAppManifest();

            mainthreadDispatcher.Commit(
                InitializeAssetBundleFeature(),
                OnBootApplication(),
                IsFirstBoot(
                    isFirstBoot =>
                    {
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
