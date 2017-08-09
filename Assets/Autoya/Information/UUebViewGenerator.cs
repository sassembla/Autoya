using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Connections.HTTP;
using UnityEngine;

namespace AutoyaFramework.Information {
	/**
		ビューの生成を行う。

		このクラスは最終的にUUebViewを返せればそれで良さそう。
	 */
    public class UUebViewGenerator {
		private readonly Action<IEnumerator> executor;
		private readonly ResourceLoader infoResLoader;
		public UUebViewGenerator (
			Action<IEnumerator> executor,
			Autoya.HttpRequestHeaderDelegate requestHeader=null,
			Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate=null
		) {
			this.executor = executor;
			this.infoResLoader = new ResourceLoader(executor, requestHeader, httpResponseHandlingDelegate);
		}
		
        public GameObject GenerateViewFromSource (string source, ViewBox view, Action<Rect>layoutDone, Action<double> progress, Action loadDone) {
			// generate and return root object first.
			var rootObj = new GameObject(HtmlTag._ROOT.ToString());
			{
				var rootRectTrans = rootObj.AddComponent<RectTransform>();
				rootObj.AddComponent<UUebView>();
				
				// set anchor to left top.
				rootRectTrans.anchorMin = Vector2.up;
				rootRectTrans.anchorMax = Vector2.up;
				rootRectTrans.pivot = Vector2.up;
				rootRectTrans.position = Vector2.zero;
			}

			GenerateView(rootObj, source, view, layoutDone, progress, loadDone);
			return rootObj;
		}
		
		/**
			parse -> layout -> materialize
		 */
        private void GenerateView (GameObject rootObj, string source, ViewBox view, Action<Rect> layoutDone, Action<double> progress, Action loadDone) {
			// parse html string to tree.
			var cor = new HTMLParser(infoResLoader).ParseRoot(
				source, 
				parsedRootTree => {

					// layout -> materialize.
					// new LayoutMachine(
					// 	parsedRootTree,
					// 	infoResLoader,
					// 	view, 
					// 	executor, 
					// 	layoutedTree => {
					// 		Debug.LogWarning("封印中");
					// 		// layout is done.
					// 		// layoutDone(new Rect(0,0, view.width, layoutedTree.totalHeight));

					// 		/*
					// 			attributes and depth are ready for each tree.
					// 		*/
					// 		var total = 0.0;
					// 		var done = 0.0;

					// 		Action<IEnumerator> act = iEnum => {
					// 			total++;
					// 			var loadAct = LoadingDone(
					// 				iEnum, 
					// 				() => {
					// 					done++;

					// 					if (progress != null) {
					// 						var progressRate = done / total;
											
					// 						if (done == total) {
					// 							progressRate = 1.0;
					// 						}

					// 						progress(progressRate);

					// 						if (done == total) {
					// 							loadDone();
					// 						}
					// 					}
					// 				}
					// 			);
					// 			executor(loadAct);
					// 		};
							
					// 		// new MaterializeMachine(infoResLoader, layoutedTree, rootObj, view, act);
					// 	}
					// );
				}
			);

			executor(cor);
        }

		private static IEnumerator LoadingDone (IEnumerator loadingCoroutine, Action loadDone) {
			while (loadingCoroutine.MoveNext()) {
				yield return null;
			}

			if (loadDone != null) {
				loadDone();
			}
		}
    }
}