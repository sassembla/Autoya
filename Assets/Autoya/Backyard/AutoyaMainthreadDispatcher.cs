using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
	micro main thread dispatcher for Autoya.
*/
namespace AutoyaFramework
{
    public class AutoyaMainthreadDispatcher : MonoBehaviour, ICoroutineUpdater
    {
        private List<IEnumerator> coroutines = new List<IEnumerator>();
        private object lockObj = new object();

        public void Commit(IEnumerator iEnum)
        {
            lock (lockObj)
            {
                coroutines.Add(iEnum);
            }
        }

        public void Commit(params IEnumerator[] iEnums)
        {
            var cor = CombineCoroutines(iEnums);
            lock (lockObj)
            {
                coroutines.Add(cor);
            }
        }

        private IEnumerator CombineCoroutines(IEnumerator[] iEnums)
        {
            var index = 0;
            while (index < iEnums.Length)
            {
                yield return iEnums[index];
                index++;
            }
        }

        private static Dictionary<string, Action<string>> nativeObserver = new Dictionary<string, Action<string>>();

        public static void AddNativeObserver(string key, Action<string> act)
        {
            // set event action.
            nativeObserver[key] = act;
        }

        // call from native plugin with special header string. e,g, URLScheme:<parameters>
        public void OnNativeEvent(string param)
        {
            var firstCoronIndex = param.IndexOf(":");
            if (firstCoronIndex == -1)
            {
                return;
            }

            var key = param.Substring(0, firstCoronIndex);
            var val = param.Substring(firstCoronIndex + 1);
            if (nativeObserver.ContainsKey(key))
            {
                nativeObserver[key](val);
            }
        }

        private void Update()
        {
            if (0 < coroutines.Count)
            {
                lock (lockObj)
                {
                    var commitingList = new List<IEnumerator>(coroutines);
                    coroutines.Clear();

                    foreach (var coroutine in commitingList)
                    {
                        // Debug.Log("commiting:" + coroutine);
                        StartCoroutine(coroutine);
                    }
                }

            }
        }

        /**
            automatically destory this gameObject.
        */
        private void OnApplicationQuit()
        {
            Destroy(gameObject);
        }

        public void Destroy()
        {
            GameObject.Destroy(gameObject);
        }
    }

    /**
        Update() runner class for Editor.
*/
    public class EditorUpdator : ICoroutineUpdater
    {
        private List<IEnumerator> readyCoroutines = new List<IEnumerator>();

        public EditorUpdator()
        {
#if UNITY_EDITOR
            {
                UnityEditor.EditorApplication.update += this.EditorCoroutineUpdate;
            }
#endif
        }

        public void Commit(IEnumerator iEnum)
        {
            readyCoroutines.Add(iEnum);
        }

        public void Commit(params IEnumerator[] iEnums)
        {
            var cor = CombineCoroutines(iEnums);
            readyCoroutines.Add(cor);
        }

        private IEnumerator CombineCoroutines(IEnumerator[] iEnums)
        {
            var index = 0;
            while (index < iEnums.Length)
            {
                yield return iEnums[index];
                index++;
            }
        }

        private static Dictionary<string, Action<string>> nativeObserver = new Dictionary<string, Action<string>>();
        public static void AddNativeObserver(string key, Action<string> act)
        {
            nativeObserver[key] = act;
        }

        public static void OnNativeEvent(string param)
        {
            var keyAndValue = param.Split(new char[':'], 1);
            var key = keyAndValue[0];
            if (nativeObserver.ContainsKey(key))
            {
                nativeObserver[key](keyAndValue[1]);
            }
        }

        private List<IEnumerator> runningCoroutines = new List<IEnumerator>();
        private List<IEnumerator> finishedCoroutines = new List<IEnumerator>();

        private void EditorCoroutineUpdate()
        {
            // run coroutines.
            {
                foreach (var runningCoroutine in runningCoroutines)
                {
                    if (!runningCoroutine.MoveNext())
                    {
                        finishedCoroutines.Add(runningCoroutine);
                    }
                }

                foreach (var finishedCoroutine in finishedCoroutines)
                {
                    runningCoroutines.Remove(finishedCoroutine);
                }

                finishedCoroutines.Clear();
            }

            // add new coroutines.
            if (0 < readyCoroutines.Count)
            {
                foreach (var coroutine in readyCoroutines)
                {
                    // Debug.Log("commiting:" + coroutine);
                    runningCoroutines.Add(coroutine);
                }
                readyCoroutines.Clear();
            }
        }

        public void Destroy()
        {
            // do nothing.
        }
    }

    public partial class Autoya
    {
        /*
            public api.
        */
        public static void Mainthread_Commit(IEnumerator iEnum)
        {
            autoya.mainthreadDispatcher.Commit(iEnum);
        }
    }
}