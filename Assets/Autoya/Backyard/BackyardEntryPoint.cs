using UnityEngine;
using System.Collections;

public class BackyardEntryPoint {
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void EntryPoint () {
		Debug.Log("autoya initialize start.");
	}
}
