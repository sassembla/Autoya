using UnityEditor;
using UnityEngine;

namespace Miyamasu {
	/*
		Run tests when Initialize on load.
		this thread is Unity Editor's MainThread, is ≒ Unity Player's MainThread.
	*/
	[InitializeOnLoad] public class MiyamasuTestIgniter {
		static MiyamasuTestIgniter () {

			#if CLOUDBUILD
			{
				// do nothing.	
				/*
					そのうちUnityTestからMiyamasu起動したい。起動さえできれば勝てる気がする。
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
}