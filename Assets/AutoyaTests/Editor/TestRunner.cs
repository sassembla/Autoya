using UnityEngine;
using AutoyaFramework;
using NUnit.Framework;
using System;
using UnityEditor;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;

[InitializeOnLoad] public class UTestRunner {
	static UTestRunner () {
		/*
			フレームワークが準備完了かどうかを試したいんだよな〜っていう感じで、

			ログイン/ログアウト
				Authentication(このへん綺麗に切り直したいところ。実際のところなんなんだ？っていう。)

			オンライン/オフライン
				Connection

			手元にアセットあり/なし
				AssetBundle

			サーバとの関係性ステータス、Maintenance/ShouldUpdateApp/PleaseUpdateApp/UpdateAssets
				ConditionAgainstServer(仮)
				
			frameworkが持ってることで使いたい機能
			・通信周り
				通信のヘッダとか保持しててほしい、毎回入れるのめんどい(authと近い)

			・ログイン
				勝手にログインしててほしい、それを待ちたい

			・Asset
				リソース一覧取得したい
				必要なリソース取得したい
				
			・サーバとの関係性を察知して特定の関数を呼びたい
				オフライン > オフラインのハンドラ着火
				サーバがメンテ中 > メンテ中のハンドラ着火
				サーバがお前のAppちょっと古いからどうにかしたら？って返してくる > ShallUpdateApp着火
				サーバがお前のAppほんと古いからアプデしないとダメって返してくる > ShouldUpdateApp着火
				サーバがお前のAppの持ってるリソースリスト古いからリソース取得しないとダメって返してくる > ShouldUpdateAssets着火
				通信の失敗を返してくる
				通信の成功を返してくる

			・ビルド番号
				保持したいねえ。どっかにテキスト吐くか。StandardSettingsとかかな。得意なスタイルだ。
		
			・その他の情報
				urlとかは管理しなくてよくね？ うん、やめよう。
				ただし入れやすいような何かを用意するかな？
				ログインとかはあるんで、それらのための情報をどうにかして入れようっていう感じがする。
				URLHolderみたいなのがあればそれで良い気がする。

			これらのハンドラをちゃんとデザインしよう。
			
		*/
		// tests.Add(_0_0_GetBuildNumber) 
		// var buildNumber = Autoya.BuildNumber();
		

		Debug.LogError("ignite");
		var testRunner = new UTestRunner();
		testRunner.RunTests();
	}

	public UTestRunner () {}

	private struct TypeAndMedhods {
		public Type type;
		public MethodInfo[] methodInfos;

		public TypeAndMedhods (Type t) {
			this.type = t;
			this.methodInfos = t.GetMethods()
				.Where(methods => 0 < methods.GetCustomAttributes(typeof(UTestAttribute), false).Length)
				.ToArray();
		}
	}
	private void RunTests () {
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
			Debug.LogError("instance;" + instance);
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

				logPath = "test.log";
				
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


public class SampleTestClass : UTestRunner {
	[UTest] public bool EditorTest () {
		Autoya.EntryPoint();

		var result = string.Empty;
		var connectionId = Autoya.HttpGet(
			"https://google.com", 
			(string conId, string resultData) => {
				result = resultData;
			},
			(string conId, int code, string reason) => {
				
			}
		);

		var wait = WaitUntil(
			"EditorTest,,,この部分なんとかならんかな、、CoreCLRで実行中のメソッドの名前撮りたいんだよな。あっそうか、Runnerがメソッド保持してるから、、なんとかなる、、ような、、、", 
			() => !string.IsNullOrEmpty(result), 
			1
		);
		if (!wait) return false; 

		// //Arrange
		// var gameObject = new GameObject();

		// Assert.NotNull(gameObject, "obj is null");
		return true;
	}
	
	[UTest] public bool EditorTest2 () {
		// //Arrange
		// var gameObject = new GameObject();

		// Assert.NotNull(gameObject, "obj is null");
		return false;
	}
}


/**
	attribute for TestRunner.
*/
[AttributeUsage(AttributeTargets.Method)] public class UTestAttribute : Attribute {
	public UTestAttribute() {}
}