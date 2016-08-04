using UnityEngine;

/**
	Autoya framework entry point.
	This method will be called from Unity when Application started running.
	ignite before all "Awake" handler.
*/
namespace AutoyaFramework {
    public partial class Autoya {
		private static Autoya autoya;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] public static void EntryPoint () {
			autoya = new Autoya();
		}
	}
}