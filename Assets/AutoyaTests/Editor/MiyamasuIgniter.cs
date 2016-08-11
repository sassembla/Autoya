using Miyamasu;
using UnityEditor;

[InitializeOnLoad] public class MiyamasuIgniter {
	static MiyamasuIgniter () {
		Debug.LogWarning("miyamasu start running.");
		var testRunner = new MiyamasuTestRunner();
		testRunner.RunTestsOnMainThread();
	}
}