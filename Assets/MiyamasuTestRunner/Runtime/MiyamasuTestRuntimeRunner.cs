using System;
using System.Collections;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;

/**
	MiyamasuRuntimeRunnerGenerator
*/
namespace Miyamasu
{
    public class MiyamasuRuntimeRunnerGenerator
    {
        /**
			run on app playing handler.
		 */
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void RunTestsFromCode()
        {
            var runnerSettings = Settings.LoadSettings();
            if (!runnerSettings.runOnPlay)
            {
                // do nothing.
                return;
            }

            // ready running.

            var go = new GameObject("MiyamasuTestMainThreadRunner");
            go.hideFlags = go.hideFlags | HideFlags.HideAndDontSave;

            var runner = go.AddComponent<MainThreadRunner>();
            var testRunnerGen = new MiyamasuRuntimeRunnerGenerator();

            runner.SetTests(testRunnerGen.TestMethodEnums());
        }

        public MiyamasuRuntimeRunnerGenerator() { }
        public Func<IEnumerator>[] TestMethodEnums()
        {
            var testTargetMethods = Assembly.GetExecutingAssembly().
                GetTypes().SelectMany(t => t.GetMethods()).
                Where(method => 0 < method.GetCustomAttributes(typeof(UnityTestAttribute), false).Length).ToArray();

            var typeAndMethogs = new Dictionary<Type, List<MethodInfo>>();

            foreach (var method in testTargetMethods)
            {
                var type = method.DeclaringType;
                if (!typeAndMethogs.ContainsKey(type))
                {
                    typeAndMethogs[type] = new List<MethodInfo>();
                }
                typeAndMethogs[type].Add(method);
            }

            var enums = typeAndMethogs.SelectMany(
                t =>
                {
                    var i = 0;
                    var ss = new List<Func<IEnumerator>>();
                    foreach (var method in t.Value)
                    {
                        Func<IEnumerator> s = () =>
                        {
                            Debug.Log("セットアップ type:" + t.Key + " index:" + i + " method:" + method.Name);
                            i++;
                            return MethodCoroutines(t.Key, method);
                        };
                        ss.Add(s);
                    }
                    return ss;
                }
            ).ToArray();

            return enums;
        }

        private IEnumerator MethodCoroutines(Type type, MethodInfo methodInfo)
        {
            var instance = Activator.CreateInstance(type);
            var cor = methodInfo.Invoke(instance, null) as IEnumerator;
            yield return cor;
        }

        // private class RunnerInstance {
        // 	public IEnumerator Runner (Action act) {
        // 		act();
        // 		yield break;
        // 	}

        // 	public IEnumerator Runner (IEnumerator actEnum, Action done) {
        // 		while (actEnum.MoveNext()) {
        // 			yield return null;
        // 		}
        // 		done();
        // 	}
        // }

        // public bool IsTestRunningInPlayingMode () {
        // 	bool isRunningInPlayingMode = false;
        // 	RunOnMainThread(
        // 		() => {
        // 			isRunningInPlayingMode = Application.isPlaying;
        // 		}
        // 	);
        // 	return isRunningInPlayingMode;
        // }

        public const string MIYAMASU_TESTLOG_FILE_NAME = "miyamasu_test.log";
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class MSetupAttribute : Attribute
    {
        public MSetupAttribute() { }
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class MTeardownAttribute : Attribute
    {
        public MTeardownAttribute() { }
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class MTestAttribute : Attribute
    {
        public MTestAttribute() { }
    }
}