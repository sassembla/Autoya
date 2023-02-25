
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoyaFramework.Purchase.UnityGameService
{

    public class UnityGameServiceRouter
    {
        public readonly bool shouldRetry;
        public readonly Dictionary<string, object> option;
        public readonly Action<Exception> onException;



        private UnityGameServiceRunner runner;
        public bool isReady
        {
            private set;
            get;
        }


        // コンストラクタで各種値を束縛し、値集としてrunnerに渡す。
        public UnityGameServiceRouter(bool shouldRetry, System.Collections.Generic.Dictionary<string, object> option, Action<Exception> onException)
        {
            this.shouldRetry = shouldRetry;
            this.option = option;
            this.onException = onException;

            runner = new GameObject("UnityGameServiceRunner").AddComponent<UnityGameServiceRunner>();
            runner.Initialize(
                this,
                () =>
                {
                    isReady = true;
                }
            );
        }
    }
}