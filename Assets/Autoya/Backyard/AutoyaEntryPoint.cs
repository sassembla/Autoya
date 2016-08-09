using UnityEngine;

/**
	Autoya framework entry point.
	This method will be called from Unity when Application started running.
	ignite before all "Awake" handler.
*/
namespace AutoyaFramework {
    public partial class Autoya {
		private static Autoya autoya;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] private static void EntryPoint () {
			Debug.LogError("framework start running.");
			autoya = new Autoya(Application.dataPath);
		}

		/*
			この関数はデバッグ時だけ存在すれば良い感じ。なので、なんらかビルド後の物体から見れないようにしないとな〜〜
		*/
		public static void TestEntryPoint (string basePath) {
			// Autoya.AutoLogin = false;
			autoya = new Autoya(basePath);
		}
	}
}