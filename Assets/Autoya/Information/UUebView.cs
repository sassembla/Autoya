using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AutoyaFramework.Information {

	/**
		UUebView instance.

		なんかこのインスタンスをnewできたら使えます的な感じになったほうがいい気がしてきたぞ。

		・MonoBehaviourなので、イベント送付先をuGUIのイベントで送付できる
			レシーバを登録する形になる

		・StartCoroutineが使える(executorを渡すことでOK)
			resLoaderのあり方次第か。
	 */
	public class UUebView : MonoBehaviour {
	

		/*
			preset parameters.
			you can use this UUebView with preset paramters for testing.
		 */
		public string presetUrl;
		public GameObject presetEventReceiver;


		public UUebViewCore Core {
			get; set;
		}

		void Start () {
			if (!string.IsNullOrEmpty(presetUrl) && presetEventReceiver != null) {
				Debug.Log("show preset view.");
				UUebViewCore.GenerateSingleViewFromUrl(presetEventReceiver, presetUrl, GetComponent<RectTransform>().sizeDelta);
			}
		}

		object lockObj = new object();
		Queue<IEnumerator> coroutines = new Queue<IEnumerator>();
		void Update () {
			lock (lockObj) {
				while (0 < coroutines.Count) {
					var cor = coroutines.Dequeue();
					StartCoroutine(cor);
				}
			}
		}
		
		public void CoroutineExecutor (IEnumerator iEnum) {
			lock (lockObj) {
				coroutines.Enqueue(iEnum);
			}
		}
    }

	public enum ContentType {
		HTML,
		IMAGE,
		LINK,
		CUSTOMTAGLIST
	}
}