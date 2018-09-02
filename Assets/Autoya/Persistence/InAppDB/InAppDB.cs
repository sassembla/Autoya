// in development, stay tunes!

// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
// using UnityEngine.Purchasing.MiniJSON;

// namespace DB
// {
//     public class InAppDB
//     {
//         public class A
//         {
//             public string name;
//         }

//         public class B
//         {
//             public string id;
//             public string name;
//         }

//         public static A[] LoadAllTabsFronDB()
//         {
//             var a = new A[rawTabs.Count];
//             for (var k = 0; k < a.Length; k++)
//             {
//                 a[k] = new A();
//                 var tab = a[k];

//                 var d = rawTabs[k] as Dictionary<string, object>;
//                 // var order = (int)tabDict["tabId"];
//             }

//             return a;
//         }


//         private static List<object> rawTabs;
//         private static Dictionary<string, object> rawItems;

//         public static void Append<T>(string sourceJson)
//         {
//             if (typeof(T) == typeof(A[]))
//             {
//                 rawTabs = Json.Deserialize(sourceJson) as List<object>;
//             }


//             if (typeof(T) == typeof(B))
//             {
//                 rawItems = Json.Deserialize(sourceJson) as Dictionary<string, object>;

//                 //Debug.Log("item! rawItems:" + rawItems);

//             }
//         }
//     }
// }