using System;
using Diag = System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.ComponentModel;

namespace Miyamasu {
	/*
		Run tests when Initialize on load.
		this thread is Unity Editor's MainThread, is ≒ Unity Player's MainThread.
	*/
	[InitializeOnLoad] public class MiyamasuTestIgniter {
		static MiyamasuTestIgniter () {
			/*
				・CLOUDBUILD フラグがついてる場合は実行しない
				・コマンドラインからの実行時にはテストを実行しない(コマンドラインで特定のメソッドから実行する)
				・上記以外のケースでは、ローカルでのビルド時には、コンパイル後ごとにテストを実行する
			*/

			// このコンパイルフラグが実行時に判断できるといいんだけどな〜 取得法がわからん。

			#if CLOUDBUILD
			{
				// do nothing.
				return;
			}
			#else
			{
				var commandLineOptions = System.Environment.CommandLine;
				if (commandLineOptions.Contains("-batchmode")) {
					// do nothing.
					return;
				}
			}
			#endif

			RunTests();
		}

		
		/**
			テスト実行
		*/
		public static void RunTests () {
			var testRunner = new MiyamasuTestRunner();
			var cor = testRunner.RunTestsOnEditorMainThread();
			
			// var sr = new GameObject("test");
			// var c = sr.AddComponent<CoroutineExecutor>();
			// c.Set(cor);

			// これで、MainThreadの所在は変わってないんだよな。うーーんやっぱりコマンドライン系の知見が要る。
			
			// bool createdNew;
			// var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "CF2D4313-33DE-489D-9721-6AFF69841DEA", out createdNew);
			// waitHandle.Set();

			// var signaled = false;
			// do {
			// 	// signaled = waitHandle.WaitOne(TimeSpan.FromSeconds(0.01));
			// 	// Debug.LogError("signaled:" + signaled);
			// 	Thread.Sleep(10);
			// } while (true);
			UnityEditor.EditorApplication.CallbackFunction exe = () => {
				// if (sr == null) return; 
				// sr.name = sr.name + "1";
				// if (sr.name.Length == 10) {
				// 	sr.name = "test";
				// }
				var contiune = cor.MoveNext();
				if (!contiune) {
					var commandLineOptions = System.Environment.CommandLine;
					if (commandLineOptions.Contains("-batchmode")) {	
						EditorApplication.Exit(0);
					}
				}
			};

			EditorApplication.update += exe;

			// // これでコンソールアプリとしての寿命が伸びてくれるといいな~、、、と思うのだが、なかなか上手くいかない。

			// x
			// m_LoginBackgroundWorker = new BackgroundWorker();
			// m_LoginBackgroundWorker.DoWork += LoginBackgroundWorker_DoWork;
			// m_LoginBackgroundWorker.RunWorkerCompleted += LoginBackgroundWorker_RunWorkerCompleted;
			// m_LoginBackgroundWorker.RunWorkerAsync(sr);

			// x
			// Diag.Process p = new Diag.Process();
			// p.StartInfo.FileName = "TextEdit";
			// p.Start();

			// Debug.LogError("end of method.");
		}

		// private static void LoginBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
		// 	// this.successfullyLogin();
		// 	Debug.LogError("done.");
		// }
		// private static void LoginBackgroundWorker_DoWork (object sender, DoWorkEventArgs e) {
		// 	// exe();
		// 	while (true) {
		// 		if (false) break; 
		// 	}
		// }

		// private static BackgroundWorker m_LoginBackgroundWorker;
		
		// static UnityEditor.EditorApplication.CallbackFunction loop;
	}


	/**
		in cloudBuild, this NUnit test method is as entrypoint of UnitTest.
		but this mechanism is imcomplete.
	*/
	public class CloudBuildTestEntryPoint {
		[Test] public static void Start () {
			MiyamasuTestIgniter.RunTests();
		}
	}

	public class TestEntryPoint {
		[Test] public static void Start () {
			MiyamasuTestIgniter.RunTests();
		}
	}
}