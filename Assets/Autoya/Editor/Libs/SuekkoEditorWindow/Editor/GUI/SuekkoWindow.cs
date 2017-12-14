// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using UnityEditor;
// using UnityEngine;

// namespace Suekko
// {
//     public class SuekkoWindow : EditorWindow
//     {
//         /*
// 			target type instance.
// 		*/
//         static Type targetType = typeof(AssetBundleListMaker);

//         [MenuItem("Window/Suekko/Open")]
//         public static void OpenSuekkoWindow()
//         {
//             var window = GetWindow<SuekkoWindow>();
//             window.titleContent = new GUIContent(targetType.ToString());
//             window.Setup(targetType);
//         }

//         /*
// 			reload if focused.
// 		*/
//         public void OnFocus()
//         {
//             if (fieldActions == null)
//             {
//                 Setup(targetType);
//             }
//         }

//         private Dictionary<NameAndType, Action<object>> fieldActions;
//         private object[] param;

//         private Dictionary<string, Action> methodActions;

//         private void Setup(Type target)
//         {
//             var instance = Activator.CreateInstance(target);
//             var fields = instance.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

//             fieldActions = new Dictionary<NameAndType, Action<object>>();
//             var fieldParams = new List<object>();

//             /*
// 				collect fields.
// 			*/
//             foreach (var field in fields)
//             {
//                 var fieldName = field.Name;
//                 var fieldType = field.FieldType;

//                 {
//                     var f = field;
//                     Action<object> act = null;

//                     switch (fieldType.ToString())
//                     {
//                         case "System.Boolean":
//                             {
//                                 act = obj =>
//                                 {
//                                     f.SetValue(instance, (bool)obj);
//                                 };
//                                 break;
//                             }
//                         case "System.String":
//                             {
//                                 act = obj =>
//                                 {
//                                     f.SetValue(instance, (string)obj);
//                                 };
//                                 break;
//                             }
//                         default:
//                             {
//                                 if (field.FieldType.IsEnum)
//                                 {
//                                     act = obj => f.SetValue(instance, Convert.ChangeType(obj, field.FieldType));
//                                     break;
//                                 }

//                                 // ignore.
//                                 break;
//                             }
//                     }

//                     fieldActions[new NameAndType(fieldName, fieldType)] = act;
//                     fieldParams.Add(field.GetValue(instance));
//                 }

//                 // set default params.
//                 param = fieldParams.Select(v => (object)v).ToArray();
//             }

//             /*
// 				collect methods.
// 			*/
//             methodActions = new Dictionary<string, Action>();

//             var methods = instance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
//             foreach (var method in methods)
//             {
//                 var len = method.GetParameters().Length;
//                 if (0 < len)
//                 {
//                     continue;
//                 }

//                 var methodName = method.Name;
//                 methodActions[methodName] = () =>
//                 {
//                     method.Invoke(instance, null);
//                 };
//             }

//             /*
// 				collect logs of target class.
// 			*/
//             UnityEngine.Application.LogCallback logAct = (logString, stackTrace, type) =>
//             {
//                 var lines = stackTrace.Split('\n');
//                 if (2 < lines.Length)
//                 {
//                     var nearest = lines[1];
//                     if (nearest.StartsWith(instance.ToString()))
//                     {
//                         logs.Add(logString);
//                     }
//                 }
//             };

//             Application.logMessageReceived += logAct;
//         }




//         private class NameAndType
//         {
//             public readonly string name;
//             public readonly Type type;
//             public NameAndType(string name, Type type)
//             {
//                 this.name = name;
//                 this.type = type;
//             }
//         }


//         void OnGUI()
//         {
//             GUILayout.Label("Params", EditorStyles.boldLabel);

//             EditorGUI.indentLevel++;
//             for (var i = 0; i < fieldActions.Count; i++)
//             {
//                 var key = fieldActions.Keys.ToArray()[i];
//                 var fieldAction = fieldActions[key];

//                 switch (key.type.ToString())
//                 {
//                     case "System.Boolean":
//                         {
//                             param[i] = EditorGUILayout.Toggle(key.name, (bool)param[i]);
//                             fieldAction(param[i]);
//                             break;
//                         }
//                     case "System.String":
//                         {
//                             var before = (string)param[i];
//                             param[i] = EditorGUILayout.TextField(key.name, before);
//                             if (before != (string)param[i])
//                             {
//                                 fieldAction(param[i]);
//                             }
//                             break;
//                         }
//                     default:
//                         {
//                             if (key.type.IsEnum)
//                             {
//                                 using (var hor = new GUILayout.HorizontalScope())
//                                 {
//                                     EditorGUILayout.LabelField(key.name);
//                                     var before = Convert.ChangeType(param[i], key.type);
//                                     if (GUILayout.Button(before.ToString(), "Popup"))
//                                     {
//                                         var menu = new GenericMenu();
//                                         foreach (var enumOrg in Enum.GetValues(before.GetType()))
//                                         {
//                                             var typedEnum = Convert.ChangeType(enumOrg, key.type); ;
//                                             var same = (typedEnum.ToString() == before.ToString());

//                                             var index = i;

//                                             Action act = () =>
//                                             {
//                                                 param[index] = typedEnum;
//                                                 fieldAction(param[index]);
//                                             };

//                                             menu.AddItem(
//                                                 new GUIContent(
//                                                     typedEnum.ToString()),
//                                                     same,
//                                                     () =>
//                                                     {
//                                                         act();
//                                                     }
//                                             );
//                                         }
//                                         menu.ShowAsContext();
//                                     }
//                                 }
//                                 break;
//                             }
//                             break;
//                         }
//                 }
//             }
//             EditorGUI.indentLevel--;

//             GUILayout.Space(10);

//             /*
// 				order method as button.
// 			*/
//             GUILayout.Label("Methods", EditorStyles.boldLabel);
//             using (var ver = new GUILayout.VerticalScope(GUI.skin.box))
//             {
//                 foreach (var methodAction in methodActions)
//                 {
//                     if (GUILayout.Button(methodAction.Key))
//                     {
//                         var act = methodAction.Value;
//                         act();
//                     }
//                 }
//             }

//             GUILayout.Space(10);


//             /*
// 				show console of code.
// 			*/
//             using (var hor = new GUILayout.HorizontalScope())
//             {
//                 GUILayout.Label("Console", EditorStyles.boldLabel);
//                 var style = new GUIStyle("minibuttonmid");
//                 // style.alignment = TextAnchor.MiddleCenter;
//                 var cleared = GUILayout.Button("Clear", style, GUILayout.Width(80));
//                 if (cleared)
//                 {
//                     logs.Clear();
//                 }
//             }
//             EditorGUI.indentLevel++;
//             using (new GUILayout.VerticalScope(GUI.skin.box))
//             {
//                 using (var scr = new GUILayout.ScrollViewScope(scrollPosition))
//                 {
//                     foreach (var log in logs)
//                     {
//                         GUILayout.Label(log);
//                     }
//                     scrollPosition = scr.scrollPosition;
//                 }
//             }
//             EditorGUI.indentLevel--;
//         }

//         private List<string> logs = new List<string>();

//         private Vector2 scrollPosition;
//     }
// }