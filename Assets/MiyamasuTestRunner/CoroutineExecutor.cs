using UnityEngine;
using System.Collections;
using System;

/*
	Unity Editorのupdateから、特定の操作でこのExecuteInEditModeが付いているインスタンスのUpdateとかを「直に触らず」実行することができることがわかった。
	が、特にUnityEditorそれ自体から実行する場合との差がなかった(直接的にinvoke recordが残らないという差はあるが)ので、
	お蔵入り。

	コードとしてユーザーが使うコードを模倣して描くにはすごくいいと思うが、それ以外の用途はなさげ。
*/
namespace Miyamasu {
	[ExecuteInEditMode] public class CoroutineExecutor : MonoBehaviour {
		IEnumerator enu;
		Action act;

		public void Set (IEnumerator enu) {
			this.enu = enu;
		}
		public void Set (Action act) {
			this.act = act;
		}
		// Use this for initialization
		void Start () {
			StartCoroutine(enu);// これで渡されてきたcoroutineを引き回してくれる
		}
		
		bool first = true;
		// Update is called once per frame
		public void Update () {
			// このUpdateを動かすのとUnityEditor.Updateは変わらなかった。
			// if (first) {
			// 	// Debug.LogError("before!");
			// 	first = false;
			// 	act();
			// 	// Debug.LogError("done!");

			// }

			// Debug.LogError("updating");
			// StartCoroutine(WaitOne());
			// 一回は呼ばれるので、次のフレームでまた呼ばれるようにすればよさげ。
			// var obj = new GameObject();
			// obj.transform.SetParent(this.gameObject.transform);
			
		}

		// private IEnumerator WaitOne () {
		// 	yield return new WaitForSeconds(1);
		// // 	Update();
		// }
	}
}