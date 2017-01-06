using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AutoyaFramework {
    public class AutoyaMainthreadDispatcher : MonoBehaviour {
        private List<IEnumerator> coroutines = new List<IEnumerator>();

        private void Awake () {
            DontDestroyOnLoad(gameObject);
        }

        public void Commit (IEnumerator iEnum) {
            coroutines.Add(iEnum);
        }

        private void Update () {
            if (0 < coroutines.Count) {
                foreach (var coroutine in coroutines) {
                    // Debug.Log("commiting:" + coroutine);
                    StartCoroutine(coroutine);
                }
                coroutines.Clear();
            } 
        }

        private void OnApplicationQuit () {
            Destroy(gameObject);
        }
    }
}