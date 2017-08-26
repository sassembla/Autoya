using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace AutoyaFramework.Information {

    public struct ParseError {
        public readonly int code;
        public readonly string reason;

        public ParseError (int code, string reason) {
            this.code = code;
            this.reason = reason;
        }
    }

    public class UUebViewCore {
        private Dictionary<string, List<TagTree>> listenerDict = new Dictionary<string, List<TagTree>>();
        public readonly UUebView view;
        public readonly ResourceLoader resLoader;
        private LayoutMachine layoutMachine;
        private MaterializeMachine materializeMachine;


        public static GameObject GenerateSingleViewFromHTML(
            GameObject eventReceiverGameObj, 
            string source, 
            Vector2 viewRect, 
            Autoya.HttpRequestHeaderDelegate requestHeader=null,
            Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate=null,
            string viewName=ConstSettings.ROOTVIEW_NAME
        ) {
            var viewObj = new GameObject("UUebView");
            viewObj.AddComponent<RectTransform>();
            var uuebView = viewObj.AddComponent<UUebView>();
            var uuebViewCore = new UUebViewCore(uuebView, viewName, requestHeader, httpResponseHandlingDelegate);
            uuebViewCore.LoadHtml(source, viewRect, eventReceiverGameObj);

            return viewObj;
        }

        public static GameObject GenerateSingleViewFromUrl(
            GameObject eventReceiverGameObj, 
            string url, 
            Vector2 viewRect, 
            Autoya.HttpRequestHeaderDelegate requestHeader=null,
            Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate=null,
            string viewName=ConstSettings.ROOTVIEW_NAME
        ) {
            var viewObj = new GameObject("UUebView");
            viewObj.AddComponent<RectTransform>();
            var uuebView = viewObj.AddComponent<UUebView>();
            var uuebViewCore = new UUebViewCore(uuebView, viewName, requestHeader, httpResponseHandlingDelegate);
            uuebViewCore.DownloadHtml(url, viewRect, eventReceiverGameObj);

            return viewObj;
        }

        public UUebViewCore (UUebView uuebView, string viewName=ConstSettings.ROOTVIEW_NAME, Autoya.HttpRequestHeaderDelegate requestHeader=null, Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate=null) {
            this.view = uuebView;
            this.view.name = viewName;
            uuebView.Core = this;

            resLoader = new ResourceLoader(this.LoadParallel, requestHeader, httpResponseHandlingDelegate);
            resLoader.cacheBox.transform.SetParent(this.view.transform);
            
            layoutMachine = new LayoutMachine(resLoader);
            materializeMachine = new MaterializeMachine(resLoader);
        }


        public bool IsLoading () {
            return view.IsLoading();
        }

        private void StartCalculateProgress () {
            if (!IsLoading()) {
                if (eventReceiverGameObj != null) {
                    ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data)=>handler.OnProgress(1));
                    ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data)=>handler.OnLoaded());
                }
                return;
            }

            var progressCor = CreateProgressCoroutine();
            view.Internal_CoroutineExecutor(progressCor);
        }

        private void UpdateParentViewSizeIfExist () {
            // set content size to view's parent if exist.
            if (view.transform.parent != null) {
                var parentRectTrans = view.transform.parent.GetComponent<RectTransform>();
                if (parentRectTrans != null) {
                    parentRectTrans.sizeDelta = new Vector2(layoutedTree.viewWidth, layoutedTree.viewHeight);
                }
            }
        }

        private IEnumerator CreateProgressCoroutine () {            
            while (view.IsWaitStartLoading()) {
                yield return null;
            }

            var loadingActions = view.LoadingActs();
            var loadingCount = loadingActions.Length;
            var perProgressUnit = 1.0 / loadingCount;
            var perProgress = perProgressUnit;
            
           
            while (view.IsLoading()) {
                var currentLoadingCount = view.LoadingActs().Length;
                if (currentLoadingCount != loadingCount) {
                    var diff = loadingCount - currentLoadingCount;
                    perProgress = perProgress + (perProgressUnit * diff);

                    // notify.
                    if (eventReceiverGameObj != null) {
                        ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data)=>handler.OnProgress(perProgress));
                    }
                    
                    // update count.
                    loadingCount = currentLoadingCount;
                }

                yield return null;
            }

            // loaded.
            if (eventReceiverGameObj != null) {
                ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data)=>handler.OnLoaded());
            }
        }
        

        public TagTree layoutedTree {
            private set; get;
        }
        
        private Vector2 viewRect;
        private GameObject eventReceiverGameObj;
        
        public void LoadHtml (string source, Vector2 viewRect, GameObject eventReceiverGameObj=null) {
            if (this.viewRect != viewRect) {
                if (eventReceiverGameObj != null) {
                    ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data)=>handler.OnLoadStarted());
                }
                
                this.viewRect = viewRect;
                this.eventReceiverGameObj = eventReceiverGameObj;
            }

            var cor = Parse(source);
            view.CoroutineExecutor(cor);
        }

        public void DownloadHtml (string url, Vector2 viewRect, GameObject eventReceiverGameObj=null) {
            if (eventReceiverGameObj != null) {
                ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data)=>handler.OnLoadStarted());
            }
            
            this.viewRect = viewRect;
            this.eventReceiverGameObj = eventReceiverGameObj;
            
            var cor = DownloadHTML(url);
            view.CoroutineExecutor(cor);
        }

        private IEnumerator DownloadHTML (string url) {
            var uri = new Uri(url);
            var scheme = uri.Scheme;
            
            var html = string.Empty;
            switch (scheme) {
                case "http":
                case "https": {
                    var downloadFailed = false;
                    Action<ContentType, int, string> failed = (contentType, code, reason) => {
                        downloadFailed = true;
                        
                        if (eventReceiverGameObj != null) {
                            ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data)=>handler.OnLoadFailed(contentType, code, reason));
                        }
                    };

                    var cor = resLoader.DownloadHTMLFromWeb(url, failed);

                    while (cor.MoveNext()) {
                        if (cor.Current != null) {
                            break;
                        }
                        yield return null;
                    }

                    if (downloadFailed) {
                        yield break;
                    }

                    html = cor.Current;
                    break;
                }
                case "resources": {
                    var resourcePathWithExtension = url.Substring("resources://".Length);
                    var resourcePath = Path.ChangeExtension(resourcePathWithExtension, null);
                    var cor = Resources.LoadAsync(resourcePath);
                    while (!cor.isDone) {
                        yield return null;
                    }
                    var res = cor.asset as TextAsset;
                    if (res == null) {
                        if (eventReceiverGameObj != null) {
                            ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data)=>handler.OnLoadFailed(ContentType.HTML, 0, "could not found html:" + url));
                        }
                        yield break;
                    }

                    html = res.text;                    
                    break;
                }
            }

            var parse = Parse(html);

            while (parse.MoveNext()) {
                yield return null;
            }
        }

        private IEnumerator Parse (string source) {
            IEnumerator reload = null;

            var parser = new HTMLParser(resLoader);
            var parse = parser.ParseRoot(
                source,
                parsedTagTree => {
                    if (parsedTagTree.errors.Any()) {
                        // このへんをイベントにしないとな〜〜という感じ
                        Debug.LogError("parse errors:" + parsedTagTree.errors.Count);
                        foreach (var error in parsedTagTree.errors) {
                            Debug.LogError("code:" + error.code + " reason:" + error.reason);
                        }
                        return;
                    }
                    reload = Load(parsedTagTree, viewRect, eventReceiverGameObj);
                }
            );

            while (parse.MoveNext()) {
                yield return null;
            }

            if (reload == null) {
                yield break;
            }

            while (reload.MoveNext()) {
                yield return null;
            }
        }

        /**
            layout -> materialize.
            if parsedTagTree was changed, materialize dirty flagged content only.
         */
        private IEnumerator Load (TagTree tree, Vector2 viewRect, GameObject eventReceiverGameObj=null) {
            var usingIds = TagTree.CorrectTrees(tree);
            
            IEnumerator materialize = null;
            var layout = layoutMachine.Layout(
                tree, 
                viewRect, 
                layoutedTree => {
                    // update layouted tree.
                    this.layoutedTree = layoutedTree;
                    
                    UpdateParentViewSizeIfExist();

                    resLoader.BackGameObjects(usingIds);
                    materialize = materializeMachine.Materialize(
                        view.gameObject, 
                        this, 
                        this.layoutedTree, 
                        0f, 
                        () => {
                            StartCalculateProgress();
                        }
                    );
                }
            );

            while (layout.MoveNext()) {
                yield return null;
            }
            
            if (materialize == null) {
                yield break;
            }

            while (materialize.MoveNext()) {
                yield return null;
            }
        }

        /**
            update contents.
         */
        private IEnumerator Update (TagTree tree, Vector2 viewRect, GameObject eventReceiverGameObj=null) {
            var usingIds = TagTree.CorrectTrees(tree);
            
            IEnumerator materialize = null;
            var layout = layoutMachine.Layout(
                tree, 
                viewRect, 
                layoutedTree => {
                    // update layouted tree.
                    this.layoutedTree = layoutedTree;
                    
                    UpdateParentViewSizeIfExist();

                    resLoader.BackGameObjects(usingIds);
                    materialize = materializeMachine.Materialize(
                        view.gameObject, 
                        this, 
                        this.layoutedTree, 
                        0f, 
                        () => {
                            if (eventReceiverGameObj != null) {
                                ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data)=>handler.OnUpdated());
                            }
                        }
                    );
                }
            );

            while (layout.MoveNext()) {
                yield return null;
            }
            
            Debug.Assert(materialize != null, "materialize is null.");

            while (materialize.MoveNext()) {
                yield return null;
            }
        }

        /**
            すべてのGameObjectを消して、コンテンツをリロードする
         */
        public void Reload () {
            resLoader.Reset();
            view.CoroutineExecutor(Load(layoutedTree, viewRect, eventReceiverGameObj));
        }

        /**
            コンテンツのパラメータを初期値に戻す
         */
        public void Reset () {
            TagTree.ResetHideFlags(layoutedTree);
        }

        public void Update () {
            view.CoroutineExecutor(Update(layoutedTree, viewRect, eventReceiverGameObj));
        }

        public void OnImageTapped (GameObject tag, string src, string buttonId="") {
            // Debug.LogError("image. tag:" + tag + " key:" + key + " buttonId:" + buttonId);

            if (!string.IsNullOrEmpty(buttonId)) {
                if (listenerDict.ContainsKey(buttonId)) {
                    listenerDict[buttonId].ForEach(t => t.ShowOrHide());
                    Update();
                }
            }

            if (eventReceiverGameObj != null) {
                ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data)=>handler.OnElementTapped(ContentType.IMAGE, tag, src, buttonId));
            }
        }

        public void OnImageTapped (string buttonId) {
            var tagAndKey = materializeMachine.eventObjectCache[buttonId];
            // Debug.LogError("image. tag:" + tag + " key:" + key + " buttonId:" + buttonId);

            if (!string.IsNullOrEmpty(buttonId)) {
                if (listenerDict.ContainsKey(buttonId)) {
                    listenerDict[buttonId].ForEach(t => t.ShowOrHide());
                    Update();
                }
            }

            if (eventReceiverGameObj != null) {
                ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data)=>handler.OnElementTapped(ContentType.IMAGE, tagAndKey.Key, tagAndKey.Value, buttonId));
            }
        }

        public void OnLinkTapped (GameObject tag, string href, string linkId="") {
            // Debug.LogError("link. tag:" + tag + " key:" + key + " linkId:" + linkId);

            if (!string.IsNullOrEmpty(linkId)) {
                if (listenerDict.ContainsKey(linkId)) {
                    listenerDict[linkId].ForEach(t => t.ShowOrHide());
                    Update();
                }
            }

            if (eventReceiverGameObj != null) {
                ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data)=>handler.OnElementTapped(ContentType.LINK, tag, href, linkId));
            }
        }

        public void OnLinkTapped (string linkId) {
            var tagAndKey = materializeMachine.eventObjectCache[linkId];
            // Debug.LogError("link. tag:" + tag + " key:" + key + " linkId:" + linkId);

            if (!string.IsNullOrEmpty(linkId)) {
                if (listenerDict.ContainsKey(linkId)) {
                    listenerDict[linkId].ForEach(t => t.ShowOrHide());
                    Update();
                }
            }

            if (eventReceiverGameObj != null) {
                ExecuteEvents.Execute<IUUebViewEventHandler>(eventReceiverGameObj, null, (handler, data)=>handler.OnElementTapped(ContentType.LINK, tagAndKey.Key, tagAndKey.Value, linkId));
            }
        }
        
        public void AddListener(TagTree tree, string listenTargetId) {
            if (!listenerDict.ContainsKey(listenTargetId)) {
                listenerDict[listenTargetId] = new List<TagTree>();
            }

            if (!listenerDict[listenTargetId].Contains(tree)) {
                listenerDict[listenTargetId].Add(tree);
            }
        }

        public void LoadParallel (IEnumerator cor) {
            view.CoroutineExecutor(cor);
        }
    }


    public interface IUUebViewEventHandler : IEventSystemHandler {
        void OnLoadStarted ();
        void OnProgress (double progress);
        void OnLoaded ();
        void OnUpdated ();
        void OnLoadFailed (ContentType type, int code, string reason);
        void OnElementTapped (ContentType type, GameObject element, string param, string id);
        void OnElementLongTapped (ContentType type, string param, string id);
    }
}