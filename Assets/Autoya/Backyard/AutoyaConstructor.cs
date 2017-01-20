using UnityEngine;
using AutoyaFramework.Connections.HTTP;
using AutoyaFramework.Persistence.Files;
using AutoyaFramework.Settings.Auth;
using AutoyaFramework.Purchase;

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
			Debug.LogWarning("autoya initialize start. basePath:" + basePath);
			
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

			Debug.LogWarning("調整中");
			// _purchaseRouter = new PurchaseRouter(
			// 	iEnum => {
			// 		mainthreadDispatcher.Commit(iEnum);
			// 	},
			// 	() => {
			// 		Debug.Log("ready purchase,");
			// 	}, 
			// 	(err, reason, autoyaStatus) => {
			// 		Debug.Log("failed to ready purchase, err:" + err + " reason:" + reason + " autoyaStatus/inMaintenance:" + autoyaStatus.inMaintenance + " isAuthFailed:" + autoyaStatus.isAuthFailed);
			// 	},
			// 	httpRequestHeaderDelegate, 
			// 	httpResponseHandlingDelegate
			// );

			var isFirstBoot = IsFirstBoot();

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