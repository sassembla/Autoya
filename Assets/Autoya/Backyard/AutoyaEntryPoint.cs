using UnityEngine;

/**
	Autoya framework entry point.
	This method will be called from Unity when the App starts running.
*/
namespace AutoyaFramework {
    public partial class Autoya {
		private static Autoya autoya;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] private static void EntryPoint () {
			autoya = new Autoya(Application.persistentDataPath);
		}
		
		#if UNITY_EDITOR
		/**
			this method can be called from Editor for testing.
		*/
		public static void TestEntryPoint (string basePath) {
			autoya = new Autoya(basePath);
		}

		public static bool forceFailFirstBoot = false;
		public static bool forceMaintenance = false;
		
		#endif
	}
}