using System;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;

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
			public MethodInfo[] methodInfos;

			public TypeAndMedhods (Type t) {
				this.type = t;
				this.methodInfos = t.GetMethods()
					.Where(methods => 0 < methods.GetCustomAttributes(typeof(MTestAttribute), false).Length)
					.ToArray();
			}
		}
		public void RunTests () {
			/*
				このへんで、GUIと関連付けてテストのon/offとかできると楽そうね。自分GUI使わないからやんないけど。
			*/
			var typeAndMethodInfos = Assembly.GetExecutingAssembly().GetTypes()
				.Select(t => new TypeAndMedhods(t))
				.Where(tAndMInfo => 0 < tAndMInfo.methodInfos.Length)
				.ToArray();

			if (!typeAndMethodInfos.Any()) {
				TestLogger.Log("no tests found. please set \"[UTest]\" attribute to method.", true);
				return;
			}

			var passed = 0;
			var failed = 0;

			TestLogger.Log("tests started.", true);

			foreach (var typeAndMethodInfo in typeAndMethodInfos) {
				var instance = Activator.CreateInstance(typeAndMethodInfo.type);
				foreach (var methodInfo in typeAndMethodInfo.methodInfos) {
					var methodName = methodInfo.Name;
					
					try {
						var succeeded = (bool)methodInfo.Invoke(instance, null);
						if (succeeded) {
							passed++;
						} else {
							failed++;
							TestLogger.Log("test:" + methodName + " failed.");
						}
					} catch (Exception e) {
						failed++;
						TestLogger.Log("test:" + methodName + " FAILED by exception:" + e, true);
					}
				}
			}
			
			TestLogger.Log("tests end. passed:" + passed + " failed:" + failed, true);
		}
		
		
		public bool WaitUntil (string methodName, Func<bool> WaitFor, int timeoutSec) {
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
							
							if (timeoutSec < distanceSeconds) {
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
		
		public void Assert (string methodName, bool condition, string message) {
			if (!condition) TestLogger.Log("test:" + methodName + " FAILED:" + message); 
		}
		
		public void Assert (string methodName, object expected, object actual, string message) {
			if (expected.ToString() != actual.ToString()) TestLogger.Log("test:" + methodName + " FAILED:" + message + " expected:" + expected + " actual:" + actual); 
		}

		public static class TestLogger {
			private static object lockObject = new object();

			public static string logPath;
			public static StringBuilder logs = new StringBuilder();
			public static void Log (string message, bool export=false) {
				lock (lockObject) {
					if (!export) {
						logs.AppendLine(message);
						return;
					}

					logPath = "miyamasu_test.log";
					
					// file write
					using (var fs = new FileStream(
						logPath,
						FileMode.Append,
						FileAccess.Write,
						FileShare.ReadWrite)
					) {
						using (var sr = new StreamWriter(fs)) {
							if (0 < logs.Length) {
								sr.WriteLine(logs.ToString());
								logs = new StringBuilder();
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