using UnityEngine;
using AutoyaFramework.Connections.HTTP;
using AutoyaFramework.Persistence.Files;
using AutoyaFramework.Settings.Auth;
using AutoyaFramework.Purchase;
using System;

/**
    constructor implementation of Autoya.
*/
namespace AutoyaFramework {
    public partial class Autoya {

        private ICoroutineUpdater mainthreadDispatcher;
        
        /**
            all conditions which Autoya has.
        */
        private class AutoyaParameters {
            public string _app_version;
            public string _assets_version;
            
            public string _buildNumber;
        }

        private Autoya (string basePath="") {
            // Debug.LogWarning("autoya initialize start. basePath:" + basePath);
            
            var isPlayer = false;

            if (Application.isPlaying) {

                isPlayer = true;

                // create game object for Autoya.
                var go = GameObject.Find("AutoyaMainthreadDispatcher");
                if (go == null) {
                    go = new GameObject("AutoyaMainthreadDispatcher");
                    this.mainthreadDispatcher = go.AddComponent<AutoyaMainThreadDispatcher>();
                    GameObject.DontDestroyOnLoad(go);
                } else {
                    this.mainthreadDispatcher = go.GetComponent<AutoyaMainThreadDispatcher>();
                }
            } else {
                // create editor runnner for Autoya.
                this.mainthreadDispatcher = new EditorUpdator();
            }
            
            _autoyaFilePersistence = new FilePersistence(basePath);

            _autoyaHttp = new HTTPConnection();

            InitializeAssetBundleFeature();

            var isFirstBoot = IsFirstBoot();
            
            /*
                start authentication.
            */
            Authenticate(
                isFirstBoot, 
                () => {
                    /*
                        initialize purchase feature.
                    */
                    if (isPlayer) {
                        ReloadPurchasability();
                    }

                    // show version.
                    #if UNITY_CLOUD_BUILD
                    var manifest = Resources.Load<BuildManifestObject>("UnityCloudBuildManifest.scriptable");
                    // Autoya.Mainthread_Commit()
                    using (var sw = new System.IO.StreamWriter("applog", true)) {
                        sw.WriteLine(manifest.ToJson());
                    }
                    #endif
                }
            );
        }
        
        #if !UNITY_CLOUD_BUILD
        public class BuildManifestObject : ScriptableObject {
            public string ToJson () {
                return string.Empty;
            }
        }
        #endif
        /*
            get build parameter.
            cloudbuildの場合と同じにしちゃえばいいかな、どうだろ。
            ・クラウドビルドでビルドした場合はそれを使う
            ・それ以外のビルドをした場合には、特定のファイルを使う
            という感じにするか。

            クラウドビルドの全パラメータ出すのしんどいので、
            ・info
            と
            ・version
            に分けちゃおう。


         */
        public static int BuildNumber () {
            // 起動時にロードしとく
            // 起動後に表示できるようにしとく
            return -1;
        }

        public static void Shutdown () {
            autoya.mainthreadDispatcher.Destroy();
        }
    }
}