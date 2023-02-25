using UnityEngine;
using AutoyaFramework.Connections.HTTP;
using AutoyaFramework.Persistence.Files;
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

            // setup response handling.
            this.httpResponseHandlingDelegate = HttpResponseHandling;

            /*
                endPoint -> AB -> OnBootLoop -> authentication.
            */
            IEnumerator bootSequence()
            {
                // EndPointSelector initialize and update.
                {
                    InitializeEndPointImplementation();

                retryEndPointRequest:
                    var failed = false;
                    var cor = UpdateEndPoints(() => failed = true);
                    var cont = cor.MoveNext();
                    if (cont)
                    {
                        yield return null;
                        while (cor.MoveNext())
                        {
                            yield return null;
                        }
                    }

                    if (failed)
                    {
                        if (ShouldRetryEndPointGetRequestOrNot())
                        {
                            goto retryEndPointRequest;
                        }
                    }
                }

                // initialize AB feature.
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

                // boot app loop.
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

                // check if first boot or not.
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
                                        // wait the ready for booting purchase feature then start purchase feature.
                                        IEnumerator firstReloadPurchasabilityCor()
                                        {
                                            yield return OnBeforeBootingPurchasingFeature();

                                            // consider using DISABLE_RUNTIME_IAP_ANALYTICS if you want to use IAP but not want to use UnityGameService.
                                            var useAndOptions = OnInitializeUnityGameService();
                                            if (useAndOptions.use)
                                            {
                                                yield return InitializePurchasability(useAndOptions.shouldRetry, useAndOptions.option, useAndOptions.onException);
                                            }

                                            ReloadPurchasability();
                                        }

                                        mainthreadDispatcher.Commit(firstReloadPurchasabilityCor());
                                    }
                                }
                            );
                        }
                    );

                    // run 1st loop for getting autoya instance.
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
