using System;
using UnityEngine;

namespace AutoyaFramework.Information {
	public class InformationRootMonoBehaviour : MonoBehaviour {
		public void OnImageTapped (string tag, string key) {
			Debug.LogError("image. tag:" + tag + " key:" + key);
		}

		public void OnLinkTapped (string tag, string key) {
			Debug.LogError("link. tag:" + tag + " key:" + key);
		}
	}
}