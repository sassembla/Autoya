using Miyamasu;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad] public class MiyamasuIgniter {
	static MiyamasuIgniter () {
		#if CLOUDBUILD
		{
			// do nothing.
			/*
				そのうちテストシーンでの動作テストをUnityTestとかで組むのはアリな気がする。
			*/
			
		}
		#else
		{
			Debug.LogWarning("miyamasu start running.");
			var testRunner = new MiyamasuTestRunner();
			testRunner.RunTestsOnEditorMainThread();
		}
		#endif
	}
}