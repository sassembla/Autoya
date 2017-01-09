using System;
using System.Collections;
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
		this thread is Unity Editor's MainThread, is ≒ Unity Player's MainThread.

		Run Miyamasu on Player : simply play app.
		Run Miyamasu on Editor : write code & compile it, then Miyamasu start running tests.
		Run Miyamasu on Batch  : 'sh run_miyamasu_tests.sh' or 'exec run_miyamasu_tests.bat'.
	*/
    [InitializeOnLoad] public class MiyamasuTestIgniter {
		static MiyamasuTestIgniter () {
			/*
				cloudbuild is not supported yet. it's hard to run asynchronous tests in NUnit.
				because NUnit uses main thread, and asynchronous test requires sub-thread.

				NUnit for Unity only supports main thread && synchrouns things only. 
				it can not wait any events running on other thread. (even on main thread.)
			*/
			#if CLOUDBUILD
			{
				// do nothing.
				return;
			}
			#endif

			var commandLineOptions = System.Environment.CommandLine;
			if (commandLineOptions.Contains("-batchmode")) {
				// do nothing if not in playmode. batchmode will run from "RunButchMode" method.

				/*
					first boot (from batch) -> start playmode.
					second boot (from start playmode) -> wait start of play.
					when Application.isPlaying == true, start tests on player.
				*/
				if (EditorApplication.isPlayingOrWillChangePlaymode) {
					var coroutine = WaitUntilTrueThen(
						() => {
							return Application.isPlaying;
						},
						() => {
							RunTests(
								TestRunnerMode.Batch,
								(iEnum) => {
									/*
										set gameObject from Editor thread(pseudo-mainThread.)
									*/
									RunOnEditorThread(
										() => {
											var go = new GameObject("MiyamasuTestMainThreadRunner");
											var mb = go.AddComponent<MainThreadRunner>();
											mb.Commit(
												iEnum,
												() => {
													GameObject.Destroy(go);
												}
											);
										}
									);
								}, 
								() => {
									// all test done. exit application.
									EditorApplication.Exit(0);
								}
							);
						}
					);

					/*
						set coroutine to Editor thread.
					*/
					EditorApplication.update += () => {
						coroutine.MoveNext();
					};
				}
				return;
			}
			
			// playing mode on Editor.
			if (EditorApplication.isPlayingOrWillChangePlaymode) {
				var coroutine = WaitUntilTrueThen(
					() => {
						return Application.isPlaying;
					},
					() => {
						RunTests(
							TestRunnerMode.Player,
							(iEnum) => {
								/*
									set gameObject from Editor thread(pseudo-mainThread.)
								*/
								
								RunOnEditorThread(
									() => {
										var go = new GameObject("MiyamasuTestMainThreadRunner");
										go.hideFlags = go.hideFlags | HideFlags.HideAndDontSave;
										
										var mb = go.AddComponent<MainThreadRunner>();
										mb.Commit(
											iEnum,
											() => {
												GameObject.Destroy(go);
											}
										);
									}
								);
							}, 
							() => {
								// do nothing.
							}
						);
					}
				);

				EditorApplication.update += () => {
					coroutine.MoveNext();
				};
			} else {
				// editor mode.
				RunTests(
					TestRunnerMode.Editor, 
					RunEnumeratorOnEditorThread, 
					() => {
						// do nothing.
					}
				);
			}
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

		public static void RunTests (TestRunnerMode mode, Action<IEnumerator> mainThreadDispatcher, Action onEnd) {
			Debug.Log("start test, mode:" + mode);
			var testRunner = new MiyamasuTestRunner(mainThreadDispatcher, onEnd);
			testRunner.RunTests();
		}

		private static void RunEnumeratorOnEditorThread (IEnumerator cor) {
			UnityEditor.EditorApplication.CallbackFunction exe = null;
			
			exe = () => {
				var contiune = cor.MoveNext();
				if (!contiune) {
					EditorApplication.update -= exe;
				}
			};

			EditorApplication.update += exe;
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
	}

	/**
		in cloudBuild, this NUnit test method is as entrypoint of UnitTest.
		but this mechanism does not work well.
	*/
	public class CloudBuildTestEntryPoint {
		[Test] public static void RunFromNUnit () {
			var go = new GameObject("MiyamasuTestMainThreadRunner");
			var mb = go.AddComponent<MainThreadRunner>();
			
			MiyamasuTestIgniter.RunTests(
				TestRunnerMode.NUnit, 
				(iEnum) => {
					mb.Commit(
						iEnum,
						() => {
							// GameObject.Destroy(go);
						}
					);
				},
				() => {
					
				}
			);

			// ここでテストの終了を待てないと、非同期メソッドのテストの際に使い物にならない。
			// また、このメソッド起動中はEditorUpdateも走らないので、mainThread的な動作を待つこともできない。

			// ここでwait系をかけてしまうと、mainThreadがロックしてしまって死ぬので、asyncができればいいというものでもない。
			// できて欲しいのは、「このメソッドが実行されているスレッドをロックせずに」「特定の処理が終わるまでこのメソッドの終了を遅らせる」という矛盾した処理。

			// IEnumeratorを返すことができるユニットテストが欲しい。
		}
	}
}