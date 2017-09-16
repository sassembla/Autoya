using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Miyamasu {
	public enum TestRunnerMode {
		None,
		Editor,
		Batch,
		Player,
		NUnit,
	}

	
	
	/*
		Run tests when Initialize on load.
		this thread : Unity Editor's MainThread, is ≒ Unity Player's MainThread.

		Run Miyamasu on Player : simply play app.
		Run Miyamasu on Editor : compile tests then Miyamasu start running these.
		Run Miyamasu on Batch  : 'sh run_miyamasu_tests.sh' or 'exec run_miyamasu_tests.bat'.
	*/
    [InitializeOnLoad] public class MiyamasuTestIgniter {
		const string GENERATED_ENTRYPOINT_FOLDER_PATH = "Assets/MiyamasuTestRunner/Runtime/Generated/";
		const string GENERATED_ENTRYPOINT_CS_PATH = GENERATED_ENTRYPOINT_FOLDER_PATH + "GeneratedTestEntryPoints.cs";
		

		static MiyamasuTestIgniter () {
			GenerateUnityTestEntryPointsIfNeed();

			#if CLOUDBUILD
			{
				// do nothing yet.
				// now we can run async test on Unity NUnit tests.
				return;
			}
			#endif

			// check is batch mode or not.
			var commandLineOptions = System.Environment.CommandLine;
			if (commandLineOptions.Contains("-batchmode")) {
				// do nothing if not in playmode. batchmode will run from "RunButchMode" method.

				/*
					first boot (from batch) -> start playmode.
					second boot (from start playmode) -> wait start of play.
					when Application.isPlaying == true, start tests on player.
				*/
				if (EditorApplication.isPlayingOrWillChangePlaymode) {
					// var coroutine = WaitUntilTrueThen(
					// 	() => {
					// 		return Application.isPlaying;
					// 	},
					// 	() => {
					// 		RunTests(
					// 			TestRunnerMode.Batch,
					// 			(iEnum) => {
					// 				/*
					// 					set gameObject from Editor thread(pseudo-mainThread.)
					// 				*/
					// 				RunOnEditorThread(
					// 					() => {
					// 						var go = new GameObject("MiyamasuTestMainThreadRunner");
					// 						var mb = go.AddComponent<MainThreadRunner>();
					// 						mb.Commit(
					// 							iEnum,
					// 							() => {
					// 								GameObject.Destroy(go);
					// 							}
					// 						);
					// 					}
					// 				);
					// 			}, 
					// 			() => {
					// 				// all test done. exit application.
					// 				EditorApplication.Exit(0);
					// 			}
					// 		);
					// 	}
					// );

					// /*
					// 	set coroutine to Editor thread.
					// */
					// EditorApplication.update += () => {
					// 	coroutine.MoveNext();
					// };
				}
				return;
			}
		}

		private static void GenerateUnityTestEntryPointsIfNeed () {
			byte[] currentHash = null;
			if (File.Exists(GENERATED_ENTRYPOINT_CS_PATH)) {
				using (var sr = new StreamReader(GENERATED_ENTRYPOINT_CS_PATH)) {
					var current = sr.ReadToEnd();
					using (MD5 md5 = MD5.Create()) {
						md5.Initialize();
						md5.ComputeHash(Encoding.UTF8.GetBytes(current));
						currentHash = md5.Hash;
					}
				}
			}
			
			var targetClassDesc = Miyamasu2UnityTestConverter.GenerateRuntimeTests();
			
			byte[] newHash;
            using (MD5 md5 = MD5.Create()) {
                md5.Initialize();
                md5.ComputeHash(Encoding.UTF8.GetBytes(targetClassDesc));
                newHash = md5.Hash;
            }

            if (currentHash != null && currentHash.SequenceEqual(newHash)) {
				return;
            }

			Debug.Log("generating test entrypoint code.");

			FileController.RemakeDirectory(GENERATED_ENTRYPOINT_FOLDER_PATH);
			using (var sw = new StreamWriter(GENERATED_ENTRYPOINT_CS_PATH)) {
				sw.Write(targetClassDesc);
			}

			AssetDatabase.Refresh();
		}


		/**
			entry point for BatchMode.
		*/
		public static void RunBatchMode () {
			/*
				set isPlaying true will run app, that raises re-compile and call InitializeOnLoad constructor of above.
			*/
			EditorApplication.isPlaying = true;
		}


		/**
			wait condition in enum, then execute action.
		*/
		private static IEnumerator WaitUntilTrueThen (Func<bool> waitUntil, Action act) {
			while (true) {
				yield return null;
				if (waitUntil()) break;
			}
			act();
		}


		private static void RunOnEditorThread (Action act) {
			// create enum which contains act().
			var waitThenEnum = WaitUntilTrueThen(
				() => {
					return true;
				},
				() => {
					act();
				}
			);

			/*
				set enum to Editor main thread.
			*/
			UnityEditor.EditorApplication.CallbackFunction exe = null;
			
			exe = () => {
				var contiune = waitThenEnum.MoveNext();
				if (!contiune) {
					EditorApplication.update -= exe;
				}
			};

			EditorApplication.update += exe;
		}
		
		public static void RunTests (TestRunnerMode mode, Action<Func<IEnumerator>[]> mainThreadDispatcher) {
			Debug.Log("start test, mode:" + mode);
			// new MiyamasuTestRunner(acts => mainThreadDispatcher(acts));
		}

		private static void RunEnumeratorOnEditorThread (Func<IEnumerator>[] cor) {
			UnityEditor.EditorApplication.CallbackFunction exe = null;
			var cors = Cors(cor);

			exe = () => {
				var contiune = cors.MoveNext();
				if (!contiune) {
					EditorApplication.update -= exe;
				}
			};

			EditorApplication.update += exe;
		}

		private static IEnumerator Cors (Func<IEnumerator>[] cor) {
			var index = 0;

			while (index < cor.Length) {
				var iEnum = cor[index]();
				while (true) {
					var cont = iEnum.MoveNext();
					Debug.LogError("move! cont:" + cont);
					if (!cont) {
						break;
					}
					yield return null;
				}
				Debug.LogError("move,,!!");
				index++;
			}
		}
	}

	/**
		in cloudBuild, this NUnit test method is as entrypoint of UnitTest.
		but this mechanism does not work well.
	*/
	// public class CloudBuildTestEntryPoint {
	// 	[Test] public static void RunFromNUnit () {
	// 		var go = new GameObject("MiyamasuTestMainThreadRunner");
	// 		var mb = go.AddComponent<MainThreadRunner>();
			
	// 		MiyamasuTestIgniter.RunTests(
	// 			TestRunnerMode.NUnit, 
	// 			(iEnum) => {
	// 				mb.Commit(
	// 					iEnum,
	// 					() => {
	// 						// GameObject.Destroy(go);
	// 					}
	// 				);
	// 			},
	// 			() => {
					
	// 			}
	// 		);

	// 		/*
			
	// 			5.6以上が普通になった現在、やりようがあるはず。
			
	// 		 */
	// 	}
	// }
}