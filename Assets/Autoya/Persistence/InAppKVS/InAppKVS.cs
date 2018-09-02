// in development, stay tunes!


// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Collections.ObjectModel;
// using AutoyaFramework;
// using UnityEngine;
// using UnityEngine.Purchasing.MiniJSON;

// namespace KVS
// {
//     public enum InAppKey
//     {
//         Key1,
//         Key2
//     }

//     public class KVBind
//     {
//         public readonly Dictionary<InAppKey, Type> keyValueBinder = new Dictionary<InAppKey, Type>()
//         {
//             {InAppKey.Key1, typeof(bool)},
//             {InAppKey.Key2, typeof(Enums[])}
//         };
//     }

//     public class InAppKVS
//     {
//         private static KVBind bind;
//         private static ReadOnlyDictionary<InAppKey, object> kvs;// use hashtable instead.
//         static InAppKVS()
//         {
//             bind = new KVBind();

//             var kvsStr = Autoya.Persist_Load("kvs", "kvs.bin");
//             if (string.IsNullOrEmpty(kvsStr))
//             {
//                 kvs = new ReadOnlyDictionary<InAppKey, object>(new Dictionary<InAppKey, object>());
//                 return;
//             }

//             var dec = OverridePoints.Dec(kvsStr);

//             var sourceDict = Json.Deserialize(dec) as Dictionary<string, object>;
//             var targetDict = new Dictionary<InAppKey, object>();
//             foreach (var k in sourceDict)
//             {
//                 var key = (InAppKey)Enum.Parse(typeof(InAppKey), k.Key);
//                 switch (k.Value.GetType().ToString())
//                 {
//                     case "System.Collections.Generic.List`1[System.Object]":
//                         var list = k.Value as List<object>;
//                         var type = bind.keyValueBinder[key];
//                         var arrayType = type.GetElementType();

//                         if (arrayType.IsEnum)
//                         {
//                             // pass.
//                         }
//                         else
//                         {
//                             Debug.LogError("unsupported type.");
//                             continue;
//                         }

//                         // 遷移によってAOTで死ぬ、回避策を考えないとな〜
//                         // var castedArray = typeof(InAppKVS)
//                         //     .GetMethod("GetGenericArray")
//                         //     .MakeGenericMethod(arrayType)
//                         //     .Invoke(null, new object[] { list });
//                         var castedArray = new LinkedPlatform[list.Count];
//                         for (var i = 0; i < castedArray.Length; i++)
//                         {
//                             castedArray[i] = (LinkedPlatform)Enum.Parse(typeof(LinkedPlatform), list[i].ToString());
//                         }

//                         targetDict[key] = castedArray;
//                         break;
//                     default:
//                         targetDict[key] = k.Value;
//                         break;
//                 }
//             }
//             kvs = new ReadOnlyDictionary<InAppKey, object>(targetDict);
//         }

//         public static T[] GetGenericArray<T>(List<object> sourceObjectArray) where T : new()
//         {
//             var newTArray = new T[sourceObjectArray.Count];
//             for (var i = 0; i < newTArray.Length; i++)
//             {
//                 newTArray[i] = (T)Enum.Parse(typeof(T), sourceObjectArray[i].ToString());
//             }
//             return newTArray;
//         }

//         public static T Read<T>(InAppKey key)
//         {
//             bind = new KVBind();
//             if (!bind.keyValueBinder.ContainsKey(key))
//             {
//                 throw new Exception("no key found. key:" + key);
//             }

//             var type = bind.keyValueBinder[key];
//             if (type != typeof(T))
//             {
//                 throw new Exception("no target type found:" + type + " is not binded against key:" + key);
//             }

//             if (!kvs.ContainsKey(key))
//             {
//                 throw new Exception("no value found against key:" + key);
//             }

//             var ret = (T)kvs[key];
//             return ret;
//         }

//         public static void Overwrite(Dictionary<InAppKey, object> dict)
//         {
//             kvs = new ReadOnlyDictionary<InAppKey, object>(dict);

//             // 内容をファイルに吐き出す
//             var json = Json.Serialize(dict);
//             var enc = OverridePoints.Enc(json);
//             Autoya.Persist_Update("kvs", "kvs.bin", enc);
//         }


//     }
// }