using System;
using Unity.Services.Core;
using UnityEngine;

namespace AutoyaFramework.Purchase.UnityGameService
{
    public class UnityGameServiceRunner : MonoBehaviour
    {
        private UnityGameServiceRouter router;
        private Action onDone;
        public void Initialize(UnityGameServiceRouter router, Action onDone)
        {
            this.router = router;
            this.onDone = onDone;
        }

        async void Start()
        {
            // use default option contains "production" environment. 
            var ugsOption = new InitializationOptions();

            if (router.option != null)
            {
                foreach (var opt in router.option)
                {
                    // 値部分の型を元に分岐する
                    var valueType = opt.Value.GetType();
                    switch (valueType)
                    {
                        case Type t when t == typeof(bool):
                            ugsOption.SetOption(opt.Key, (bool)opt.Value);
                            break;
                        case Type t when t == typeof(int):
                            ugsOption.SetOption(opt.Key, (int)opt.Value);
                            break;
                        case Type t when t == typeof(float):
                            ugsOption.SetOption(opt.Key, (float)opt.Value);
                            break;
                        case Type t when t == typeof(string):
                            ugsOption.SetOption(opt.Key, (string)opt.Value);
                            break;
                        default:
                            Debug.LogError("can not set to InitializationOptions:" + valueType + " in key:" + opt.Key);
                            break;
                    }
                }
            }

        retry:
            try
            {
                // MEMO: I think this is only API which returns Task in Unity. very bad design.
                // I never want to use Task because Task can run properly from MonoBehaviour in Unity. it's hard to handle other cases like this.
                // this class is the proof why you should not use Task. if not Task, I could initialize this from the loop inside [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] without MB.
                // do not use Task which contains "should run in mainthread."
                await UnityServices.InitializeAsync(ugsOption);
            }
            catch (Exception e)
            {
                Debug.Log("e:" + e);
                if (router.onException != null)
                {
                    router.onException(e);
                }

                if (router.shouldRetry)
                {
                    goto retry;
                }
            }

            switch (UnityServices.State)
            {
                case ServicesInitializationState.Initialized:
                    onDone();
                    break;
                case ServicesInitializationState.Uninitialized:
                    if (router.shouldRetry)
                    {
                        goto retry;
                    }

                    Debug.LogWarning("it seems UnityServices.InitializeAsync is failed. if you want to retry, set shouldRetry to true.");
                    onDone();
                    break;
                default:
                    Debug.LogError("unhandled state, UnityServices.State:" + UnityServices.State);
                    break;
            }

            // disappears because it has finished its role of executing InitializeAsync in the main thread. 
            // I still don't think it's a good design to make it do useless things just for this. do not use Task which contains "should run in mainthread."
            Destroy(this.gameObject);
        }
    }
}
