using Miyamasu;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad] public class MiyamasuIgniter {
	static MiyamasuIgniter () {
		Debug.LogWarning("miyamasu start running.");
		var testRunner = new MiyamasuTestRunner();
		testRunner.RunTestsOnMainThread();
	}
}