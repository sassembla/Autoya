using System;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using Diag = System.Diagnostics;
using UnityEngine;
using UnityEditor;

/**
	MiyamasuTestRunner
		this testRunner is only test runner which contains "WaitUntil" method.
		"WaitUntil" method can wait Async code execution until specified sec passed.

		This can be replacable when Unity adopts C# 4.0 by using "async" and "await" keyword.
*/
namespace Miyamasu {
	public class MiyamasuTestRunner {
		public MiyamasuTestRunner () {}

		private class TypeAndMedhods {
			public Type type;

			public bool hasTests = false;
			
			public MethodInfo[] asyncMethodInfos;
			public MethodInfo setupMethodInfo = null;
			public MethodInfo teardownMethodInfo = null;

			public TypeAndMedhods (Type t) {
				var testMethods = t.GetMethods()
					.Where(methods => 0 < methods.GetCustomAttributes(typeof(MTestAttribute), false).Length)
					.ToArray();

				if (!testMethods.Any()) return; 
				this.hasTests = true;

				/*
					hold type.
				*/
				this.type = t;

				/*
					collect tests.
				*/
				this.asyncMethodInfos = testMethods;
				
				/*
					collect setup and teardown.
				*/
				this.setupMethodInfo = t.GetMethods().Where(methods => 0 < methods.GetCustomAttributes(typeof(MSetupAttribute), false).Length).FirstOrDefault();
				this.teardownMethodInfo = t.GetMethods().Where(methods => 0 < methods.GetCustomAttributes(typeof(MTeardownAttribute), false).Length).FirstOrDefault();
			}
		}

		public object lockObj = new object();

		public IEnumerator RunTestsOnEditorMainThread () {
			var typeAndMethodInfos = Assembly.GetExecutingAssembly().GetTypes()
				.Select(t => new TypeAndMedhods(t))
				.Where(tAndMInfo => tAndMInfo.hasTests)
				.ToArray();

			
			if (!typeAndMethodInfos.Any()) {
				TestLogger.Log("no tests found. please set \"[MTest]\" attribute to method.", true);
				yield break;
			}

			var passed = 0;
			var failed = 0;

			TestLogger.Log("tests started.", true);
			
			var totalMethodCount = typeAndMethodInfos.Count();
			var allTestsDone = false;

			// generate waitingThread for waiting asynchronous(=running on MainThread or other thread) ops on Not-MainThread.
			Thread thread = null;
			thread = new Thread(
				() => {
					var count = 0;
					foreach (var typeAndMethodInfo in typeAndMethodInfos) {
						var instance = Activator.CreateInstance(typeAndMethodInfo.type);

						foreach (var methodInfo in typeAndMethodInfo.asyncMethodInfos) {
							if (typeAndMethodInfo.setupMethodInfo != null) typeAndMethodInfo.setupMethodInfo.Invoke(instance, null);
							var methodName = methodInfo.Name;
							
							try {
								methodInfo.Invoke(instance, null);
								passed++;
							} catch (Exception e) {
								failed++;
								
								var location = string.Empty;
								var errorStackLines = e.ToString().Split('\n');
								
								for (var i = 0; i < errorStackLines.Length; i++) {
									var line = errorStackLines[i];
									
									if (line.StartsWith("  at Miyamasu.MiyamasuTestRunner.Assert")) {
										location = errorStackLines[i+1].Substring("  at ".Length);
										break;
									}
									if (line.StartsWith("  at Miyamasu.MiyamasuTestRunner.WaitUntil")) {
										location = errorStackLines[i+1].Substring("  at ".Length);
										break;
									}
								}

								TestLogger.Log("test FAILED @ " + location + "by:" + e.InnerException.Message, true);
							}
							if (typeAndMethodInfo.teardownMethodInfo != null) typeAndMethodInfo.teardownMethodInfo.Invoke(instance, null);
						}
						count++;
						TestLogger.Log("tests of class:" + typeAndMethodInfo.type + " done. classes:" + count + " of " + totalMethodCount, true);
					}

					allTestsDone = true;
				}
			);
			

			try {
				thread.Start();
			} catch (Exception e) {
				TestLogger.Log("Miyamasu TestRunner error:" + e);
			}
			
			yield return null;

			while (true) {
				if (allTestsDone) break; 
				yield return null;
			}
			
			TestLogger.Log("tests end. passed:" + passed + " failed:" + failed, true);
			TestLogger.LogEnd();
			
		}
		
		/**
			can wait Async code execution until specified sec passed.
		*/
		public void WaitUntil (Func<bool> isCompleted, int timeoutSec=1, string message="") {
			var methodName = new Diag.StackFrame(1).GetMethod().Name;
			Exception error = null;

			var resetEvent = new ManualResetEvent(false);
			var waitingThread = new Thread(
				() => {
					resetEvent.Reset();
					var startTime = DateTime.Now.Second;
					
					while (!isCompleted()) {
						var current = DateTime.Now.Second;
						var distanceSeconds = (current - startTime);

						if (0 < timeoutSec && timeoutSec < distanceSeconds) {
							if (!string.IsNullOrEmpty(message)) error = new Exception("timeout. reason:" + message);
							else error = new Exception("timeout.");
							break;
						}
						
						System.Threading.Thread.Sleep(10);
					}

					resetEvent.Set();
				}
			);
			
			waitingThread.Start();
			
			resetEvent.WaitOne();
			if (error != null) {
				throw error;
			}
		}

		/**
			Run action on UnityEditor's MainThread.
			let set [bool sync] = false if you want to execute action on MainThread but async.
			default is sync.
		*/
		public void RunOnMainThread (Action invokee, bool @sync = true) {
			UnityEditor.EditorApplication.CallbackFunction runner = null;
			
			var done = false;
			
			runner = () => {
				// run only once.
				EditorApplication.update -= runner;
				if (invokee != null) invokee();
				done = true;
				// 実際にインスタンスを作ってUpdateさせるモード、大差なかった。残念。
				// var sr = new GameObject("test");
				// var c = sr.AddComponent<CoroutineExecutor>();
				// c.Set(invokee);
			};
			
			EditorApplication.update += runner;
			if (@sync) {
				WaitUntil(() => done);
			}
		}
		

		public void Assert (bool condition, string message) {
			if (!condition) {
				throw new Exception("assert failed:" + message);
			}
		}

		public const string MIYAMASU_TESTLOG_FILE_NAME = "miyamasu_test.log";

		public static class TestLogger {
			public static bool outputLog = true;
			private static object lockObject = new object();

			private static string pathOfLogFile;
			private static StringBuilder _logs = new StringBuilder();
			
			public static void Log (string message, bool export=false) {
				if (outputLog) UnityEngine.Debug.Log("log:" + message);
				lock (lockObject) {
					if (!export) {
						_logs.AppendLine(message);
						return;
					}

					pathOfLogFile = MIYAMASU_TESTLOG_FILE_NAME;
					
					// file write
					using (var fs = new FileStream(
						pathOfLogFile,
						FileMode.Append,
						FileAccess.Write,
						FileShare.ReadWrite)
					) {
						using (var sr = new StreamWriter(fs)) {
							if (0 < _logs.Length) {
								sr.WriteLine(_logs.ToString());
								_logs = new StringBuilder();
							}
							sr.WriteLine("log:" + message);
						}
					}
				}
			}

			public static void LogEnd () {
				lock (lockObject) {
					// file write
					using (var fs = new FileStream(
						pathOfLogFile,
						FileMode.Append,
						FileAccess.Write,
						FileShare.ReadWrite)
					) {
						using (var sr = new StreamWriter(fs)) {
							if (0 < _logs.Length) {
								sr.WriteLine(_logs.ToString());
								_logs = new StringBuilder();
							}
						}
					}
				}
			}
		}
	}

	/**
		attributes for TestRunner.
	*/
	[AttributeUsage(AttributeTargets.Method)] public class MSetupAttribute : Attribute {
		public MSetupAttribute() {}
	}

	[AttributeUsage(AttributeTargets.Method)] public class MTestAttribute : Attribute {
		public MTestAttribute() {}
	}

	[AttributeUsage(AttributeTargets.Method)] public class MTeardownAttribute : Attribute {
		public MTeardownAttribute() {}
	}
}