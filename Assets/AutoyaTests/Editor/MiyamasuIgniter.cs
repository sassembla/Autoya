using Miyamasu;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad] public class MiyamasuIgniter {
	static MiyamasuIgniter () {
		#if CLOUDBUILD
		{
			Debug.LogError("hereComes!!");
		}
		#else
		// {
		// 	Debug.LogWarning("miyamasu start running.");
		// 	var testRunner = new MiyamasuTestRunner();
		// 	testRunner.RunTestsOnMainThread();
		// }
		#endif
	}
}