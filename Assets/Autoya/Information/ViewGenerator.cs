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
	 */
    public class ViewGenerator {
		private readonly Action<IEnumerator> executor;
		private readonly InformationResourceLoader infoResLoader;
		public ViewGenerator (
			Action<IEnumerator> executor,
			Autoya.HttpRequestHeaderDelegate requestHeader=null,
			Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate=null
		) {
			this.executor = executor;
			this.infoResLoader = new InformationResourceLoader(executor, requestHeader, httpResponseHandlingDelegate);
		}
		
		public GameObject GenerateViewFromSource (string viewName, string source, ViewBox view, Action<double> progress, Action loadDone) {
			// generate and return root object first.
			var rootObj = new GameObject(viewName + HtmlTag._ROOT.ToString());
			{
				var rootRectTrans = rootObj.AddComponent<RectTransform>();
				rootObj.AddComponent<InformationRootMonoBehaviour>();
				
				// set anchor to left top.
				rootRectTrans.anchorMin = Vector2.up;
				rootRectTrans.anchorMax = Vector2.up;
				rootRectTrans.pivot = Vector2.up;
				rootRectTrans.position = Vector2.zero;
			}

			GenerateView(rootObj, viewName, source, view, progress, loadDone);
			return rootObj;
		}
		

        private void GenerateView (GameObject rootObj, string viewName, string source, ViewBox view, Action<double> progress, Action loadDone) {
			// parse html string to tree.
			var parsedRootTree = new HTMLParser().ParseRoot(source);

			// layout -> materialize.
			new LayoutMachine(
				viewName,
				parsedRootTree,
				infoResLoader,
				view, 
				executor, 
				layoutedTree => {
					/*
						attributes and depth are ready for each tree.
					 */
					var total = 0.0;
					var done = 0.0;

					Action<IEnumerator> act = iEnum => {
						total++;
						var loadAct = LoadingDone(
							iEnum, 
							() => {
								done++;

								if (progress != null) {
									var progressRate = done / total;
									
									if (done == total) {
										progressRate = 1.0;
									}

									progress(progressRate);

									if (done == total) {
										loadDone();
									}
								}
							}
						);
						executor(loadAct);
					};
					
					new MaterializeMachine(viewName, infoResLoader, layoutedTree, rootObj, view, act);
				}
			);
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