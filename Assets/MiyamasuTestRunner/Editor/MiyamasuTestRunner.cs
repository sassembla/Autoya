using System;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using UniRx;
using AutoyaFramework;

/**
	MiyamasuTestRunner
		this testRunner is only test runner which contains "WaitUntil" method.
		"WaitUntil" method can wait Async code execution until specified frame passed.

		This can be replacable when Unity adopts C# 4.0 by using "async" and "await" keyword.
*/
namespace Miyamasu {
	public class MiyamasuTestRunner {
		public MiyamasuTestRunner () {}

		private struct TypeAndMedhods {
			public Type type;
			public MethodInfo[] asyncMethodInfos;

			public TypeAndMedhods (Type t) {
				this.type = t;
				this.asyncMethodInfos = t.GetMethods()
					.Where(methods => 0 < methods.GetCustomAttributes(typeof(MTestAttribute), false).Length)
					.ToArray();
			}
		}

		private string dataPath;
		private bool Setup () {
			Autoya.TestEntryPoint(dataPath);

			var authorized = false;
			Autoya.Auth_SetOnLoginSucceeded(
				() => {
					authorized = true;
				}
			);

			return WaitUntil(() => authorized, 10); 
		}

		private void Teardown () {
			// do nothing.
		}

		public void RunTestsOnEditorMainThread () {
			var syncAndAsyncTypeAndMethodInfos = Assembly.GetExecutingAssembly().GetTypes()
				.Select(t => new TypeAndMedhods(t))
				.Where(tAndMInfo => 0 < tAndMInfo.asyncMethodInfos.Length)
				.ToArray();

			
			if (!syncAndAsyncTypeAndMethodInfos.Any()) {
				TestLogger.Log("no tests found. please set \"[MTest]\" or \"[MTestOnMainThread]\" attribute to method.", true);
				return;
			}

			var asyncTypeAndMethodInfos = syncAndAsyncTypeAndMethodInfos.Where(tAndMInfo => 0 < tAndMInfo.asyncMethodInfos.Length).ToList();

			var passed = 0;
			var failed = 0;

			TestLogger.Log("tests started.", true);
			
			// このメソッドとかがMainThreadでしか使えないんで困ってるっていう感じ。
			dataPath = UnityEngine.Application.persistentDataPath;

			// generate waiting thread for waiting asynchronous(=running on MainThread or other thread) ops on Not-MainThread.
			Thread thread = null;
			thread = new Thread(
				() => {
					foreach (var typeAndMethodInfo in asyncTypeAndMethodInfos) {
						
						var instance = Activator.CreateInstance(typeAndMethodInfo.type);
						foreach (var methodInfo in typeAndMethodInfo.asyncMethodInfos) {
							var methodName = methodInfo.Name;
							if (!Setup()) {
								TestLogger.Log("test:" + methodName + " failed by setup timeout.", true);
								failed++;
								continue;
							}
							
							try {
								var succeeded = (bool)methodInfo.Invoke(instance, null);
								if (succeeded) {
									passed++;
								} else {
									failed++;
									TestLogger.Log("test:" + methodName + " failed.", true);
								}
							} catch (Exception e) {
								failed++;
								TestLogger.Log("test:" + methodName + " FAILED by exception:" + e, true);
							}
							Teardown();
						}
					}

					TestLogger.Log("tests end. passed:" + passed + " failed:" + failed, true);
					thread.Abort();
				}
			);
			try {
				thread.Start();
			} catch (Exception e) {
				TestLogger.Log("Miyamasu TestRunner error:" + e);
			}
		}
		
		
		public bool WaitUntil (Func<bool> WaitFor, int timeoutSec=1) {
			var methodName = new StackFrame(1).GetMethod().Name;

			var resetEvent = new ManualResetEvent(false);
			var succeeded = true;
			var waitingThread = new Thread(
				() => {
					resetEvent.Reset();
					var startTime = DateTime.Now;
					
					try {
						while (!WaitFor()) {
							var current = DateTime.Now;
							var distanceSeconds = (current - startTime).Seconds;
							
							if (0 < timeoutSec && timeoutSec < distanceSeconds) {
								TestLogger.Log("timeout:" + methodName);
								succeeded = false;
								break;
							}
							
							System.Threading.Thread.Sleep(10);
						}
					} catch (Exception e) {
						TestLogger.Log("methodName:" + methodName + " error:" + e.Message, true);
					}
					
					resetEvent.Set();
				}
			);
			
			waitingThread.Start();
			
			resetEvent.WaitOne();
			return succeeded;
		}
		
		public void Assert (bool condition, string message) {
			var methodName = new StackFrame(1).GetMethod().Name;
			if (!condition) {
				var situation = "test:" + methodName + " ASSERT FAILED:" + message;
				TestLogger.Log(situation);
				throw new Exception(situation);
			}
		}
		
		public void Assert (object expected, object actual, string message) {
			var methodName = new StackFrame(1).GetMethod().Name;
			if (expected.ToString() != actual.ToString()) {
				var situation = "test:" + methodName + " ASSERT FAILED:" + message + " expected:" + expected + " actual:" + actual;
				TestLogger.Log(situation);
				throw new Exception(situation);
			} 
		}

		public static class TestLogger {
			private static object lockObject = new object();

			private static string pathOfLogFile;
			private static StringBuilder _logs = new StringBuilder();
			
			public static void Log (string message, bool export=false) {
				lock (lockObject) {
					if (!export) {
						_logs.AppendLine(message);
						return;
					}

					pathOfLogFile = "miyamasu_test.log";
					
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
		}
	}

	/**
		attribute for TestRunner.
	*/
	[AttributeUsage(AttributeTargets.Method)] public class MTestAttribute : Attribute {
		public MTestAttribute() {}
	}
}