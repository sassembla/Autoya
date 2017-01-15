using UnityEngine;
using AutoyaFramework.Connections.HTTP;
using AutoyaFramework.Persistence.Files;
using AutoyaFramework.Settings.Auth;

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
			
			if (Application.isPlaying) {// create game object for Autoya.
				var go = GameObject.Find("AutoyaMainthreadDispatcher");
				if (go == null) {
					go = new GameObject("AutoyaMainthreadDispatcher");
					this.mainthreadDispatcher = go.AddComponent<AutoyaMainThreadDispatcher>();
					GameObject.DontDestroyOnLoad(go);
				} else {
					this.mainthreadDispatcher = go.GetComponent<AutoyaMainThreadDispatcher>();
				}
			} else {// create editor runnner for Autoya.
				this.mainthreadDispatcher = new EditorUpdator();
			}
			
			_autoyaFilePersistence = new FilePersistence(basePath);

			_autoyaHttp = new HTTPConnection();

			/* 
				セッティングよみ出ししちゃおう。なんか、、LocalStorageからapp_versionとかだな。Unityで起動時に上書きとかしとけば良い気がする。
				asset_versionはAssetsListに組み込まれてるんで、それを読みだして云々、っていう感じにできる。
			*/
			
			var tokenCandidatePaths = _autoyaFilePersistence.FileNamesInDomain(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN);
			var isFirstBoot = tokenCandidatePaths.Length == 0;

			/*
				start authentication.
			*/
			Authenticate(isFirstBoot);
		}
        

		public static int BuildNumber () {
			return -1;
		}

		public static void Shutdown () {
			autoya.mainthreadDispatcher.Destroy();
		}
    }
}