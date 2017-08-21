using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoyaFramework.Information {
	public class LoadingCoroutineObj {
		public bool isDone = false;
	}
	
	/**
		UUebView component.

		testing usage:
			attach this component to gameobject and set preset urls and event receiver.

		actual usage:
			let's use UUebViewCore.GenerateSingleViewFromHTML or UUebViewCore.GenerateSingleViewFromUrl.
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
				var view = UUebViewCore.GenerateSingleViewFromUrl(presetEventReceiver, presetUrl, GetComponent<RectTransform>().sizeDelta);
				view.transform.SetParent(this.transform, false);
			}
		}

		object lockObj = new object();
		private Queue<IEnumerator> queuedCoroutines = new Queue<IEnumerator>();
		private Queue<IEnumerator> unmanagedCoroutines = new Queue<IEnumerator>();
        private List<LoadingCoroutineObj> loadingCoroutines = new List<LoadingCoroutineObj>();
		


		void Update () {
			lock (lockObj) {
				while (0 < queuedCoroutines.Count) {
					var cor = queuedCoroutines.Dequeue();
					var loadCorObj = new LoadingCoroutineObj();
					var loadingCor = CreateLoadingCoroutine(cor, loadCorObj);
					StartCoroutine(loadingCor);

					// collect loading coroutines.
					AddLoading(loadCorObj);
				}

				while (0 < unmanagedCoroutines.Count) {
					var cor = unmanagedCoroutines.Dequeue();
					StartCoroutine(cor);
				}
			}
		}

		private IEnumerator CreateLoadingCoroutine (IEnumerator cor, LoadingCoroutineObj loadCor) {
			while (cor.MoveNext()) {
				yield return null;
			}
			loadCor.isDone = true;
		}

		private void AddLoading (LoadingCoroutineObj runObj) {
            loadingCoroutines.Add(runObj);
        }

		public void Internal_CoroutineExecutor (IEnumerator iEnum) {
			lock (lockObj) {
				unmanagedCoroutines.Enqueue(iEnum);
			}
		}
		
		public void CoroutineExecutor (IEnumerator iEnum) {
			lock (lockObj) {
				queuedCoroutines.Enqueue(iEnum);
			}
		}

		public bool IsWaitStartLoading () {
			lock (lockObj) {
				if (queuedCoroutines.Any()) {
					return true;
				}
			}
			return false;
		}

		public bool IsLoading () {
			lock (lockObj) {
				if (queuedCoroutines.Any()) {
					return true;
				}

				if (loadingCoroutines.Where(cor => !cor.isDone).Any()) {
					// Debug.LogError("loading:" + loadingCoroutines.Count);
					return true;
				}
			}

			return false;
		}

        public void EmitButtonEventById (string elementId) {
            Core.OnImageTapped(elementId);
        }

		public void EmitLinkEventById (string elementId) {
            Core.OnLinkTapped(elementId);
        }

        public LoadingCoroutineObj[] LoadingActs () {
            return loadingCoroutines.Where(r => !r.isDone).ToArray();
        }
    }

	public enum ContentType {
		HTML,
		IMAGE,
		LINK,
		CUSTOMTAGLIST
	}
}