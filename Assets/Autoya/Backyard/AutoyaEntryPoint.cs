using UnityEngine;
using System.Collections;

/**
	
*/
namespace AutoyaFramework {
	public partial class Autoya {
		private static Autoya autoya;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] private static void EntryPoint () {
			autoya = new Autoya();
		}
	}
}