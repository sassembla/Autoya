using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Suekko {
	public class SuekkoWindow : EditorWindow {
		/*
			target type instance.
		*/
		static AssetBundleListMaker target = new AssetBundleListMaker();// 
	
		[MenuItem("Window/Suekko/Open")] public static void OpenSuekkoWindow () {
			var window = GetWindow<SuekkoWindow>();
			window.titleContent = new GUIContent(target.GetType().ToString());
			window.Setup(target);
		}

		/*
			reload if focused.
		*/
        public void OnFocus() {
			if (fieldActions == null) {
				Setup(target);
			}
        }

		private Dictionary<NameAndType, Action<object>> fieldActions;
		private object[] param;

		private Dictionary<string, MethodInfo> methodActions;

		private void Setup<T> (T target) {
			var fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

			fieldActions = new Dictionary<NameAndType, Action<object>>();
			var fieldParams = new List<object>();

			/*
				collect fields.
			*/
			foreach (var field in fields) {
				var fieldName = field.Name;
				var fieldType = field.FieldType;
				
				{
					var f = field;
					Action<object> act = null;
					switch (fieldType.ToString()) {
						case "System.Boolean": {
							act = obj => {
								f.SetValue(target, (bool)obj);
							};
							break;
						}
						case "System.String": {
							act = obj => {
								f.SetValue(target, (string)obj);
							};
							break;
						}
						default: {
							Debug.LogError("FieldType.ToString():" + field.FieldType.ToString());
							break;
						}
					}
					fieldActions[new NameAndType(fieldName, fieldType)] = act;
					fieldParams.Add(field.GetValue(target));		
				}

				// set default params.
				param = fieldParams.Select(v => (object)v).ToArray();
			}

			/*
				collect methods.
			*/
			methodActions = new Dictionary<string, MethodInfo>();

			var methods = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			foreach (var method in methods) {
				var len = method.GetParameters().Length;
				if (0 < len) {
					continue;
				}

				var methodName = method.Name;
				methodActions[methodName] = method;
			}
		}

		private class NameAndType {
			public readonly string name;
			public readonly Type type;
			public NameAndType (string name, Type type) {
				this.name = name;
				this.type = type;
			}
		}

		/*
			欲しい機能は、
			・ボタンを置く -> 特定のメソッドを実行する -> その実行結果を特殊なコンソールに出す というやつ。
			で、これはロガーと、コンソールビューと、ボタンでできそう。
		*/

		
		void OnGUI () {
			GUILayout.Label("Params", EditorStyles.boldLabel);

			for (var i = 0; i < fieldActions.Count; i++) {
				var key = fieldActions.Keys.ToArray()[i];
				var fieldAction = fieldActions[key];

				switch (key.type.ToString()) {
					case "System.Boolean": {
						param[i] = EditorGUILayout.Toggle(key.name, (bool)param[i]);
						fieldAction(param[i]);
						break;
					}
					case "System.String": {
						var before = (string)param[i];
						param[i] = EditorGUILayout.TextField(key.name, before);
						if (before != (string)param[i]) {
							fieldAction(param[i]);
						}
						break;
					}
					default: {
						break;
					}
				}
			}


			/*
				order method as button.
			*/
			foreach (var methodAction in methodActions) {
				if (GUILayout.Button(methodAction.Key)) {
					var methodInfo = methodAction.Value;
					methodInfo.Invoke(target, null);
				}
			}
			
			/*
				コンソールの表示(したいな〜〜)
			*/
		}

		/*
			logger
			なんか勝手にDebug.Logを奪って、かつ内容がこのインスタンスだったらっていう区切りでいい気がする。
		*/
		private void Log (string message) {
			
		}

		private StringBuilder b = new StringBuilder();
	}
}