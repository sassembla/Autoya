using Miyamasu;
using UnityEditor;

[InitializeOnLoad] public class MiyamasuIgniter {
	static MiyamasuIgniter () {
		var testRunner = new MiyamasuTestRunner();
		testRunner.RunTestsOnMainThread();
	}
}