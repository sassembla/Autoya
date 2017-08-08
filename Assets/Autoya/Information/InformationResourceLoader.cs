using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AutoyaFramework.Information {

    [Serializable] public class CustomTagList {
        [SerializeField] public string viewName;
        [SerializeField] public ContentInfo[] contents;
        [SerializeField] public LayerInfo[] layerConstraints;
        
        public CustomTagList (string viewName, ContentInfo[] contents, LayerInfo[] constraints) {
            this.viewName = viewName;
            this.contents = contents;
            this.layerConstraints = constraints;
        }

        public Dictionary<string, TreeType> GetTagTypeDict () {
            var tagNames = new Dictionary<string, TreeType>();
            foreach (var content in contents) {
                tagNames[content.contentName] = content.type;
            }
            foreach (var constraint in layerConstraints) {
                if (constraint.constraints.Any()) {
                    tagNames[constraint.layerName] = TreeType.CustomLayer;
                } else {
                    tagNames[constraint.layerName] = TreeType.CustomEmptyLayer;
                }
            }
            
            return tagNames;
        }
    }

    [Serializable] public class ContentInfo {
        [SerializeField] public string contentName;
        [SerializeField] public TreeType type;
        [SerializeField] public string loadPath;

        public ContentInfo (string contentName, TreeType type, string loadPath) {
            this.contentName = contentName;
            this.type = type;
            this.loadPath = loadPath;
        }
    }

    [Serializable] public class LayerInfo {
        [SerializeField] public string layerName;
        [SerializeField] public BoxConstraint[] constraints;
        [SerializeField] public string loadPath;
        public LayerInfo (string layerName, BoxConstraint[] constraints, string loadPath) {
            this.layerName = layerName;
            this.constraints = constraints;
            this.loadPath = loadPath;
        }
    }

    [Serializable] public class BoxConstraint {
        [SerializeField] public string boxName;
        [SerializeField] public BoxPos rect;

        public BoxConstraint (string boxName, BoxPos rect) {
            this.boxName = boxName.ToLower();
            this.rect = rect;
        }
    }

    [Serializable] public class BoxPos {
        [SerializeField] public Vector2 anchoredPosition;
        [SerializeField] public Vector2 sizeDelta;
        [SerializeField] public Vector2 offsetMin;
        [SerializeField] public Vector2 offsetMax;
        [SerializeField] public Vector2 pivot;

        [SerializeField] public Vector2 anchorMin;
        [SerializeField] public Vector2 anchorMax;

        public BoxPos (RectTransform rect) {
            this.anchoredPosition = rect.anchoredPosition;
            this.sizeDelta = rect.sizeDelta;
            this.offsetMin = rect.offsetMin;
            this.offsetMax = rect.offsetMax;
            this.pivot = rect.pivot;
            this.anchorMin = rect.anchorMin;
            this.anchorMax = rect.anchorMax;
        }
        
        override public string ToString () {
            return "anchoredPosition:" + this.anchoredPosition + " sizeDelta:" + this.sizeDelta + " offsetMin:" + this.offsetMin + " offsetMax:" + this.offsetMax + " pivot:" +this.pivot + " anchorMin:" + this.anchorMin + " anchorMax:" + this.anchorMax;
        }
    }

    public class InformationResourceLoader {
        private class SpriteCache : Dictionary<string, Sprite> {};
        private class PrefabCache : Dictionary<string, GameObject> {};

        /*
            information feature global cache.

            sprites and prefabs are cached statically.
         */
        private static SpriteCache spriteCache = new SpriteCache();
        private static List<string> spriteDownloadingUris = new List<string>();


        private static PrefabCache prefabCache = new PrefabCache();
        private static List<string> loadingPrefabNames = new List<string>();


        private readonly Autoya.HttpRequestHeaderDelegate requestHeader;
		private Dictionary<string, string> BasicRequestHeaderDelegate (HttpMethod method, string url, Dictionary<string, string> requestHeader, string data) {
			return requestHeader;
		}

		private readonly Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate;
		private void BasicResponseHandlingDelegate (string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed) {
			if (200 <= httpCode && httpCode < 299) {
				succeeded(connectionId, data);
				return;
			}
			failed(connectionId, httpCode, errorReason, new AutoyaStatus());
		}

        public int GetAdditionalTagCount () {
            return undefinedTagDict.Count;
        }

        private readonly Action<IEnumerator> executor;
        public InformationResourceLoader (Action<IEnumerator> executor, Autoya.HttpRequestHeaderDelegate requestHeader, Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate) {
            defaultTagStrIntPair = new Dictionary<string, int>();
            defaultTagIntStrPair = new Dictionary<int, string>();
           
            foreach (var tag in Enum.GetValues(typeof(HtmlTag))) {
                var tagStr = tag.ToString();
                var index = (int)tag;

                defaultTagStrIntPair[tagStr] = index;
                defaultTagIntStrPair[index] = tagStr;
            }

            this.executor = executor;

            /*
                set request header generation func and response validation delegate.
             */
            if (requestHeader != null) {
				this.requestHeader = requestHeader;
			} else {
				this.requestHeader = BasicRequestHeaderDelegate;
			}
			
			if (httpResponseHandlingDelegate != null) {
				this.httpResponseHandlingDelegate = httpResponseHandlingDelegate;
			} else {
				this.httpResponseHandlingDelegate = BasicResponseHandlingDelegate;
			}
        }

        public IEnumerator<GameObject> LoadGameObjectFromPrefab (int parsedTag, TreeType treeType, bool prefabOnly=false) {
            GameObject gameObj = null;
            var tagName = GetTagFromIndex(parsedTag);

            switch (IsDefaultTag(parsedTag)) {
                case true: {

                    switch (treeType) {
                        case TreeType.Container: {
                            Debug.LogWarning("コンテナをキャッシュ化できるかもしれない。まあただの箱なんで、その意味はないか。");
                            var containerObj = new GameObject(tagName);
                            var trans = containerObj.AddComponent<RectTransform>();
                            trans.anchorMin = Vector2.up;
                            trans.anchorMax = Vector2.up;
                            trans.offsetMin = Vector2.up;
                            trans.offsetMax = Vector2.up;
                            trans.pivot = Vector2.up;

                            gameObj = containerObj;
                            break;
                        }
                        default: {
                            // コンテナ以外だと、いろんなデフォルトコンテンツがここにくる。
                            var prefabName = GetTagFromIndex(parsedTag);
                            var loadingPrefabName = InformationConstSettings.PREFIX_PATH_INFORMATION_RESOURCE + InformationConstSettings.VIEWNAME_DEFAULT + "/" + prefabName;

                            var cor = LoadPrefabFromResourcesOrCache(loadingPrefabName);
                            while (cor.MoveNext()) {
                                yield return null;
                            }

                            var loadedPrefab = cor.Current;
                            
                            if (prefabOnly) {
                                gameObj = loadedPrefab;
                            } else {
                                gameObj = GameObject.Instantiate(loadedPrefab);
                            }
                            break;
                        }
                    }
                    break;
                }

                // 非デフォルトタグ、customBox以外はloadpathが存在する。
                default: {
                    switch (treeType) {
                        case TreeType.Container: {
                            Debug.LogWarning("コンテナをキャッシュ化できるかもしれない。まあただの箱なんで、その意味はないか。");
                            var containerObj = new GameObject(tagName);
                            var trans = containerObj.AddComponent<RectTransform>();
                            trans.anchorMin = Vector2.up;
                            trans.anchorMax = Vector2.up;
                            trans.offsetMin = Vector2.up;
                            trans.offsetMax = Vector2.up;
                            trans.pivot = Vector2.up;

                            gameObj = containerObj;
                            break;
                        }
                        case TreeType.CustomBox: {
                            var customBoxObj = new GameObject(tagName);
                            var trans = customBoxObj.AddComponent<RectTransform>();
                            trans.anchorMin = Vector2.up;
                            trans.anchorMax = Vector2.up;
                            trans.offsetMin = Vector2.up;
                            trans.offsetMax = Vector2.up;
                            trans.pivot = Vector2.up;

                            gameObj = customBoxObj;
                            break;
                        }
                        default: {
                            var tag = GetTagFromIndex(parsedTag);
                            var loadPath = GetCustomTagLoadPath(parsedTag, treeType);

                            var cor = LoadCustomPrefabFromLoadPathOrCache(loadPath);
                            while (cor.MoveNext()) {
                                yield return null;
                            }

                            var loadedPrefab = cor.Current;
                            
                            if (prefabOnly) {
                                gameObj = loadedPrefab;
                            } else {
                                gameObj = GameObject.Instantiate(loadedPrefab);
                            }
                            break;
                        }
                    }
                    break;
                }
            }

            gameObj.name = tagName;
            yield return gameObj;
        }

        private string GetCustomTagLoadPath (int parsedTag, TreeType treeType) {
            var tag = GetTagFromIndex(parsedTag);
            var targetPrefab = string.Empty;

            switch (treeType) {
                case TreeType.CustomLayer:
                case TreeType.CustomEmptyLayer: {
                    return customTagList.layerConstraints.Where(t => t.layerName == tag).Select(t => t.loadPath).FirstOrDefault();
                }
                case TreeType.Content_Img:
                case TreeType.Content_Text: {
                    return customTagList.contents.Where(t => t.contentName == tag).Select(t => t.loadPath).FirstOrDefault();
                }
                default: {
                    throw new Exception("unexpected tree type:" + treeType + " of tag:" + tag);
                }
            }            
        }

         /**
            loadPathからcustomTag = レイヤーのprefabをロードし、GameObjectを返す。
         */
        public IEnumerator<GameObject> LoadCustomPrefabFromLoadPathOrCache (string loadPath) {
            // schemeをみてロード方法を決定する。

            var schemeAndPath = loadPath.Split(new char[]{'/'}, 2);
            var scheme = schemeAndPath[0];

            var extLen = Path.GetExtension(loadPath).Length;
            var uri = loadPath.Substring(0, loadPath.Length - extLen);
            
            IEnumerator<GameObject> cor = null;

            switch (scheme) {
                case "assetbundle:": {
                    cor = LoadPrefabFromAssetBundle(uri);
                    break;
                }
                case "https:":
                case "http:": {
                    throw new Exception("http|https are not supported scheme for downloading prefab. use assetbundle:// instead.");
                }
                case "resources:": {
                    cor = LoadPrefabFromResourcesOrCache(uri.Substring("resources://".Length));
                    break;
                }
                default: {// other.
                    throw new Exception("unsupported scheme:" + scheme + " found when loading custom tag prefab:" + loadPath);
                }
            }

            while (cor.MoveNext()) {
                yield return null;
            }

            yield return cor.Current;
        }

        /**
            キャッシュヒット処理込み。
            resourcesからprefabをロードし、GameObjectを返す。
         */
        private IEnumerator<GameObject> LoadPrefabFromResourcesOrCache (string loadingPrefabName) {
            // Debug.LogError("loadingPrefabName:" + loadingPrefabName);

            GameObject loadedPrefab = null;

            if (prefabCache.ContainsKey(loadingPrefabName)) {
                var cachedPrefab = prefabCache[loadingPrefabName];
                var newGameObjectFromCachedPrefab = GameObject.Instantiate(cachedPrefab);
                newGameObjectFromCachedPrefab.name = loadingPrefabName;

                loadedPrefab = newGameObjectFromCachedPrefab;
            }

            // no cache hit. start loading prefab.

            // wait the end of other loading for same prefab.
            else if (loadingPrefabNames.Contains(loadingPrefabName)) {
                while (loadingPrefabNames.Contains(loadingPrefabName)) {
                    yield return null;
                }

                if (!prefabCache.ContainsKey(loadingPrefabName)) {
                    Debug.LogError("キャッシュされたはずなんだけどロードに失敗");

                    var failedObj = new GameObject();
                    failedObj.name = loadingPrefabName;

                    loadedPrefab = failedObj;
                } else {
                    
                    var cachedPrefab = prefabCache[loadingPrefabName];
                    var newGameObjectFromCachedPrefab = GameObject.Instantiate(cachedPrefab);
                    newGameObjectFromCachedPrefab.name = loadingPrefabName;

                    loadedPrefab = newGameObjectFromCachedPrefab;
                }
            } else {
                // start loading.
                using (new AssetLoadingConstraint(loadingPrefabName, loadingPrefabNames)) {
                    GameObject obj = null;
                    var cor = Resources.LoadAsync(loadingPrefabName);
                
                    while (!cor.isDone) {
                        yield return null;
                    }
                    
                    obj = cor.asset as GameObject;

                    if (obj == null) {
                        // no prefab found.
                        Debug.LogError("no prefab found in Resources:" + loadingPrefabName);

                        var failedObj = new GameObject();
                        failedObj.name = loadingPrefabName;
                        loadedPrefab = failedObj;
                    } else {
                        loadedPrefab = obj;
                    }
                }
            }
            yield return loadedPrefab;
        }

        private IEnumerator<GameObject> LoadPrefabFromAssetBundle (string path) {
            // アセット名が書いてあると思うんで、assetBundleListとかから取り寄せる
            Debug.LogError("まだ実装してないassetBundleからprefabを読む仕掛け");
            yield return null;
        }

        public bool IsDefaultTag (int tag) {
            if (defaultTagIntStrPair.ContainsKey(tag)) {
                return true;
            }
            return false;
        }

        public TreeType GetTreeType (int tag) {
            // 組み込みtagであれば、静的に解決できる。
            if (defaultTagIntStrPair.ContainsKey(tag)) {
				switch (tag) {
                    case (int)HtmlTag.a: {
                        return TreeType.Content_Text;
                    }
                    case (int)HtmlTag.img: {
                        return TreeType.Content_Img;
                    }
                    case (int)HtmlTag.hr:
                    case (int)HtmlTag.br: {
                        return TreeType.Content_Empty;
                    }
                    default: {
                        return TreeType.Container;
                    }
                }
			}

            // tag is not default.
            
            var customTagStr = GetTagFromIndex(tag);
            // Debug.LogError("customTagStr:" + customTagStr);
            // foreach (var s in customTagTypeDict.Keys) {
            //     Debug.LogError("s:" + s);
            // }

            return customTagTypeDict[customTagStr];
        }

        /**
            materialize時に画像を読み込む。
         */
        public void LoadImageAsync (string uriSource, Action<Sprite> loaded, Action loadFailed) {
            IEnumerator coroutine;

            /*
                supported schemes are,
                    
                    ^http://		http scheme => load asset from web.
                    ^https://		https scheme => load asset from web.
                    ^assetbundle://	assetbundle scheme => load asset from assetBundle.
                    ^resources://   resources scheme => (Resources/)somewhere/resource path.
                    ^./				relative path => (Resources/)somewhere/resource path.
                    ^/              absolute path => unsupported.
                    ^.*				path => (Resources/)somewhere/resource path.
            */
            var schemeAndPath = uriSource.Split(new char[]{'/'}, 2);
            var scheme = schemeAndPath[0];

            switch (scheme) {
                case "assetbundle:": {
                    var bundleName = uriSource;
                    coroutine = LoadImageFromAssetBundle(uriSource, loaded, loadFailed);
                    break;
                }
                case "https:":
                case "http:": {
                    coroutine = LoadImageFromWeb(uriSource, loaded, loadFailed);
                    break;
                }
                case ".": {
                    var resourcePath = uriSource.Substring(2);
                    coroutine = LoadImageFromResources(resourcePath, loaded, loadFailed);
                    break;
                }
                case "resources:": {
                    coroutine = LoadImageFromResources(uriSource, loaded, loadFailed);
                    break;
                }
                case "/:": {
                    throw new Exception("unsupported scheme:/");
                }
                default: {// other.
                    if (string.IsNullOrEmpty(scheme)) {
                        Debug.LogError("empty uri found:" + uriSource);
                        return;
                    }

                    // not empty. treat as resource file path.
                    coroutine = LoadImageFromResources(uriSource, loaded, loadFailed);
                    break;
                }
            }
            
            // execute loading.
            executor(coroutine);
        }


        /*
            return imageLoad iEnum functions.   
         */

        private IEnumerator LoadImageFromAssetBundle (string assetName, Action<Sprite> loaded, Action loadFailed) {
            yield return null;
            Debug.LogError("LoadImageFromAssetBundle bundleName:" + assetName);
        }

        private IEnumerator LoadImageFromResources (string uriSource, Action<Sprite> loaded, Action loadFailed) {
            var extLen = Path.GetExtension(uriSource).Length;
            var uri = uriSource.Substring(0, uriSource.Length - extLen);

            var resourceLoadingCor = Resources.LoadAsync(uri);
            while (!resourceLoadingCor.isDone) {
                yield return null;
            }
            
            if (resourceLoadingCor.asset == null) {
                loadFailed();
                yield break;
            }

            // create tex.
            var tex = resourceLoadingCor.asset as Texture2D;
            var spr = Sprite.Create(tex, new Rect(0,0, tex.width, tex.height), Vector2.zero);
            
            loaded(spr);
        }

        private IEnumerator LoadImageFromWeb (string url, Action<Sprite> loaded, Action loadFailed) {
            if (spriteCache.ContainsKey(url)) {
                loaded(spriteCache[url]);
                yield break;
            }

            if (spriteDownloadingUris.Contains(url)) {
                while (spriteDownloadingUris.Contains(url)) {
                    yield return null;
                }

                if (spriteCache.ContainsKey(url)) {
                    // download is done. cached sprite exists.
                    loaded(spriteCache[url]);
                    yield break;
                }

                loadFailed();
                yield break;
            }

            // start downloading.
            using (new AssetLoadingConstraint(url, spriteDownloadingUris)) {
                var connectionId = InformationConstSettings.CONNECTIONID_DOWNLOAD_IMAGE_PREFIX + Guid.NewGuid().ToString();
                var reqHeaders = requestHeader(HttpMethod.Get, url, new Dictionary<string, string>(), string.Empty);

                // start download tex from url.
                using (var request = UnityWebRequest.GetTexture(url)) {
                    foreach (var reqHeader in reqHeaders) {
                        request.SetRequestHeader(reqHeader.Key, reqHeader.Value);
                    }

                    var p = request.Send();

                    var timeoutSec = InformationConstSettings.TIMEOUT_SEC;
                    var limitTick = DateTime.UtcNow.AddSeconds(timeoutSec).Ticks;

                    while (!p.isDone) {
                        yield return null;

                        // check timeout.
                        if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks) {
                            Debug.LogError("timeout. load aborted, dataPath:" + url);
                            request.Abort();
                            loadFailed();
                            yield break;
                        }
                    }

                    var responseCode = (int)request.responseCode;
                    var responseHeaders = request.GetResponseHeaders();
                    
                    
                    if (request.isError) {
                        httpResponseHandlingDelegate(
                            connectionId,
                            responseHeaders,
                            responseCode,
                            null, 
                            request.error, 
                            (conId, data) => {
                                throw new Exception("request encountered some kind of error.");
                            }, 
                            (conId, code, reason, autoyaStatus) => {
                                Debug.LogError("failed to download image:" + url + " code:" + code + " reason:" + reason);
                                loadFailed();
                            }
                        );
                        yield break;
                    }

                    httpResponseHandlingDelegate(
                        connectionId,
                        responseHeaders,
                        responseCode,
                        request, 
                        request.error,
                        (conId, data) => {
                            // create tex.
                            var tex = DownloadHandlerTexture.GetContent(request);

                            // cache this sprite for other requests.
                            var spr = Sprite.Create(tex, new Rect(0,0, tex.width, tex.height), Vector2.zero);
                            spriteCache[url] = spr;
                            
                            loaded(spriteCache[url]);
                        }, 
                        (conId, code, reason, autoyaStatus) => {
                            Debug.LogError("failed to download image:" + url + " code:" + code + " reason:" + reason);
                            loadFailed();
                        }
                    );
                }
            }
        }

        private class AssetLoadingConstraint : IDisposable {
			private string target;
			private List<string> list;
			
			public AssetLoadingConstraint (string target, List<string> list) {
				this.target = target;
				this.list = list;

				this.list.Add(this.target);
			}

			private bool disposedValue = false;

			protected virtual void Dispose (bool disposing) {
				if (!disposedValue) {
					if (disposing) {
						list.Remove(target);
					}
					disposedValue = true;
				}
			}

			void IDisposable.Dispose () {
				Dispose(true);
			}
		}








        private CustomTagList customTagList;
        public bool IsLoadingDepthAssetList {
            get; private set;
        }

        private Dictionary<string, TreeType> customTagTypeDict = new Dictionary<string, TreeType>();
        private Dictionary<string, BoxConstraint[]> constraintsDict;
        

        public IEnumerator LoadCustomTagList (string uriSource) {
            if (IsLoadingDepthAssetList) {
                throw new Exception("multiple depth description found. only one description is valid.");
            }

            var schemeEndIndex = uriSource.IndexOf("//");
            var scheme = uriSource.Substring(0, schemeEndIndex);
            
            IsLoadingDepthAssetList = true;


            Action<CustomTagList> succeeded = customTagList => {
                this.customTagList = customTagList;
                this.customTagTypeDict = this.customTagList.GetTagTypeDict();

                // レイヤー名:constraintsの辞書を生成しておく。
                this.constraintsDict = new Dictionary<string, BoxConstraint[]>();

                var constraints = customTagList.layerConstraints;

                foreach (var constraint in constraints) {
                    constraintsDict[constraint.layerName.ToLower()] = constraint.constraints;
                }

                IsLoadingDepthAssetList = false;
            };
            
            Action failed = () => {
                Debug.LogError("failed to load depthAssetList from url:" + uriSource + ". use default empty customTagList automatically.");
                this.customTagList = new CustomTagList(InformationConstSettings.VIEWNAME_DEFAULT, new ContentInfo[0], new LayerInfo[0]);// set empty list.
                IsLoadingDepthAssetList = false;
            };

            IEnumerator cor = null;
            switch (scheme) {
                case "assetbundle:": {
                    var bundleName = uriSource;
                    cor = LoadListFromAssetBundle(uriSource, succeeded, failed);
                    break;
                }
                case "https:":
                case "http:": {
                    cor =  LoadListFromWeb(uriSource, succeeded, failed);
                    break;
                }
                case "resources:": {
                    var resourcePath = uriSource.Substring("resources:".Length + 2);
                    cor = LoadListFromResources(resourcePath, succeeded, failed);
                    break;
                }
                default: {// other.
                    throw new Exception("unsupported scheme found, scheme:" + scheme);
                }
            }

            while (cor.MoveNext()) {
                yield return null;
            }
        }

        public BoxConstraint[] GetConstraints (int parsedTag) {
            var key = GetTagFromIndex(parsedTag);
            return constraintsDict[key];
        }

        public string GetLayerBoxName (int layerTag, int boxTag) {
            return GetTagFromIndex(layerTag) + "_" + GetTagFromIndex(boxTag);
        }

        private IEnumerator LoadListFromAssetBundle (string url, Action<CustomTagList> succeeded, Action failed) {
            Debug.LogError("not yet applied. LoadListFromAssetBundle url:" + url);
            failed();
            yield break;
        }
        
        private IEnumerator LoadListFromWeb (string url, Action<CustomTagList> loadSucceeded, Action loadFailed) {
            var connectionId = InformationConstSettings.CONNECTIONID_DOWNLOAD_DEPTHASSETLIST_PREFIX + Guid.NewGuid().ToString();
            var reqHeaders = requestHeader(HttpMethod.Get, url, new Dictionary<string, string>(), string.Empty);

            using (var request = UnityWebRequest.Get(url)) {
                foreach (var reqHeader in reqHeaders) {
                    request.SetRequestHeader(reqHeader.Key, reqHeader.Value);
                }

                var p = request.Send();

                var timeoutSec = InformationConstSettings.TIMEOUT_SEC;
                var limitTick = DateTime.UtcNow.AddSeconds(timeoutSec).Ticks;

                while (!p.isDone) {
                    yield return null;

                    // check timeout.
                    if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks) {
                        Debug.LogError("timeout. load aborted, dataPath:" + url);
                        request.Abort();
                        loadFailed();
                        yield break;
                    }
                }

                var responseCode = (int)request.responseCode;
                var responseHeaders = request.GetResponseHeaders();
                
                if (request.isError) {
                    httpResponseHandlingDelegate(
                        connectionId,
                        responseHeaders,
                        responseCode,
                        null, 
                        request.error, 
                        (conId, data) => {
                            throw new Exception("request encountered some kind of error.");
                        }, 
                        (conId, code, reason, autoyaStatus) => {
                            Debug.LogError("failed to download list:" + url + " code:" + code + " reason:" + reason);
                            loadFailed();
                        }
                    );
                    yield break;
                }

                httpResponseHandlingDelegate(
                    connectionId,
                    responseHeaders,
                    responseCode,
                    string.Empty, 
                    request.error,
                    (conId, unusedData) => {
                        var jsonStr = request.downloadHandler.text;
                        var newDepthAssetList = JsonUtility.FromJson<CustomTagList>(jsonStr);

                        loadSucceeded(newDepthAssetList);
                    }, 
                    (conId, code, reason, autoyaStatus) => {
                        Debug.LogError("failed to download list:" + url + " code:" + code + " reason:" + reason);
                        loadFailed();
                    }
                );
            }
        }
        
        private IEnumerator LoadListFromResources (string path, Action<CustomTagList> succeeded, Action failed) {
            var requestCor = Resources.LoadAsync(path);

            while (!requestCor.isDone) {
                yield return null;
            }

            if (requestCor.asset == null) {
                failed();
                yield break;
            }

            var jsonStr = (requestCor.asset as TextAsset).text;
            var depthAssetList = JsonUtility.FromJson<CustomTagList>(jsonStr);
            succeeded(depthAssetList);
		}




        private readonly Dictionary<string, int> defaultTagStrIntPair;
        private readonly Dictionary<int, string> defaultTagIntStrPair;

        private Dictionary<string, int> undefinedTagDict = new Dictionary<string, int>();

        public string GetTagFromIndex (int index) {
			if (index < defaultTagStrIntPair.Count) {
				return defaultTagIntStrPair[index];
			}

			if (undefinedTagDict.ContainsValue(index)) {
				return undefinedTagDict.FirstOrDefault(x => x.Value == index).Key;
			}
			
			throw new Exception("failed to get tag from index. index:" + index);
		}

        public int FindOrCreateTag (string tagCandidateStr) {
            if (defaultTagStrIntPair.ContainsKey(tagCandidateStr)) {
				return defaultTagStrIntPair[tagCandidateStr];
			}
            // collect undefined tag.
            // Debug.LogError("tagCandidateStr:" + tagCandidateStr);

            if (undefinedTagDict.ContainsKey(tagCandidateStr)) {
                return undefinedTagDict[tagCandidateStr];
            }
            
            var count = (int)HtmlTag._END + undefinedTagDict.Count + 1;
            undefinedTagDict[tagCandidateStr] = count;
            return count;
        }

    }
}