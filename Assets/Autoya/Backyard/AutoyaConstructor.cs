using UnityEngine;
using AutoyaFramework.Connections.HTTP;
using AutoyaFramework.Persistence.Files;
using AutoyaFramework.Settings.Auth;
using AutoyaFramework.Purchase;
using System;
using AutoyaFramework.Settings.App;
using AutoyaFramework.Notification;
using System.Collections;

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
            InitializeEndPointImplementation();

            /*
                endPoint -> AB -> OnBootLoop -> authentication.
            */
            IEnumerator bootSequence()
            {
                {
                    var cor = UpdateEndPoints();
                    var cont = cor.MoveNext();
                    if (cont)
                    {
                        yield return null;
                        while (cor.MoveNext())
                        {
                            yield return null;
                        }
                    }
                }

                {
                    var cor = InitializeAssetBundleFeature();
                    var cont = cor.MoveNext();
                    if (cont)
                    {
                        yield return null;
                        while (cor.MoveNext())
                        {
                            yield return null;
                        }
                    }
                }

                {
                    var cor = OnBootApplication();
                    var cont = cor.MoveNext();
                    if (cont)
                    {
                        yield return null;
                        while (cor.MoveNext())
                        {
                            yield return null;
                        }
                    }
                }

                {
                    var cor = IsFirstBoot(
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
                    );

                    var cont = cor.MoveNext();
                    if (cont)
                    {
                        yield return null;
                        while (cor.MoveNext())
                        {
                            yield return null;
                        }
                    }
                }
            }

            mainthreadDispatcher.Commit(bootSequence());
        }

        public static void Shutdown()
        {
            autoya.mainthreadDispatcher.Destroy();
        }
    }
}
