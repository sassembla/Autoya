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

        /**
            load prefab from AssetBundle or Resources.
         */
        public IEnumerator LoadPrefab (ParsedTree tree, Action<GameObject> onLoaded, Action onLoadFailed) {
            IEnumerator coroutine = null;

            if (this.customTagList != null) {
                // pass.
            } else {
                // create default list for default assets.
                this.customTagList = new CustomTagList(InformationConstSettings.VIEWNAME_DEFAULT, new ContentInfo[0], new LayerInfo[0]);
            }

            var viewName = this.customTagList.viewName;
            
            switch (viewName) {
                case InformationConstSettings.VIEWNAME_DEFAULT: {
                    coroutine = LoadPrefabFromDefaultResources(tree, onLoaded, onLoadFailed);
                    break;
                }
                default: {
                    Debug.LogWarning("LoadPrefab viewName:" + viewName);
                    coroutine = LoadPrefabByDepth(tree, onLoaded, onLoadFailed);
                    break;
                }
            }

            while (coroutine.MoveNext()) {
                yield return null;
            }
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

        public IEnumerator<GameObject> LoadTextPrefab (string textPrefabName) {
            GameObject loadedPrefab = null;
            
            Action<GameObject> onLoaded = obj => {
                loadedPrefab = obj;
            };

            Action onLoadFailed = () => {
                // do nothing.
            };

            // load text prefab from loadPath.
            if (customTagTypeDict.ContainsKey(textPrefabName.ToLower())) {
                var loadPath = customTagList.contents.Where(t => t.contentName == textPrefabName.ToLower()).FirstOrDefault().loadPath;
                Debug.LogWarning("これはloadPathから読むべきなのだな。loadPath:" + loadPath);

                // specific view path.
                var viewPath = InformationConstSettings.PREFIX_PATH_INFORMATION_RESOURCE + DepthAssetList().viewName + "/";

                var loadingPrefabName = viewPath + textPrefabName;
                
                var cor = LoadPrefabFromResources(loadingPrefabName, onLoaded, onLoadFailed);
                while (cor.MoveNext()) {
                    yield return null;
                }
            } else {
                var defaultPath = InformationConstSettings.PREFIX_PATH_INFORMATION_RESOURCE + InformationConstSettings.VIEWNAME_DEFAULT + "/";

                var loadingPrefabName = defaultPath + textPrefabName;
                
                var cor = LoadPrefabFromResources(loadingPrefabName, onLoaded, onLoadFailed);
                while (cor.MoveNext()) {
                    yield return null;
                }
            }
            
            if (loadedPrefab == null) {
                throw new Exception("failed to load text prefab:" + textPrefabName);
            }
            yield return loadedPrefab;
        }





        private IEnumerator LoadPrefabByDepth (ParsedTree tree, Action<GameObject> onLoaded, Action onLoadFailed) {
            IEnumerator coroutine = null;

            Debug.LogError("tree:" + tree.parsedTag);
            Debug.LogError("ここまできた。で、DL情報はAntimaterializerでconstraintsに入れるようにしよう。デフォResources tree.keyValueStore:" + tree.keyValueStore);
            var kvs = tree.keyValueStore;
            foreach (var kv in kvs) {
                Debug.LogError("k:" + kv.Key);
            }
            
            Debug.LogError("prefabのロードを行わないといけない、layoutをせねば、みたいな。んーーでももうrectTransformのパラメータが手に入ってるんだよな。なので、カスタム情報だけが取れればそれでいいのか。");


            // var assetName = info.depthAssetName;
            // var uriSource = info.loadPath;

            // var schemeEndIndex = uriSource.IndexOf("//");
            // if (schemeEndIndex == -1) {
            //     throw new Exception("failed to get scheme from loadPath:" + uriSource);
            // }
            // var scheme = uriSource.Substring(0, schemeEndIndex);
            
            // switch (scheme) {
            //     case "assetbundle:": {
            //         Debug.LogError("まだ未実装");
            //         // var bundleName = uriSource;
            //         // coroutine = LoadListFromAssetBundle(uriSource, succeeded, failed);
            //         break;
            //     }
            //     case "resources:": {
            //         var resourcePath = uriSource.Substring("resources:".Length + 2);
            //         coroutine = LoadPrefabFromResources(resourcePath, tree, onLoaded, onLoadFailed);
            //         break;
            //     }
            //     default: {// other.
            //         throw new Exception("unsupported scheme found, scheme:" + scheme);
            //     }
            // }

            // while (coroutine.MoveNext()) {
            //     yield return null;
            // }
            yield break;
        }

        private IEnumerator LoadPrefabFromResources (string loadingPrefabName, Action<GameObject> onLoaded, Action onLoadFailed) {
            
            // cached.
            if (prefabCache.ContainsKey(loadingPrefabName)) {
                onLoaded(prefabCache[loadingPrefabName]);
                yield break;
            }
            
            // wait the end of other loading for same prefab.
            if (loadingPrefabNames.Contains(loadingPrefabName)) {
                while (loadingPrefabNames.Contains(loadingPrefabName)) {
                    yield return null;
                }

                if (!prefabCache.ContainsKey(loadingPrefabName)) {
                    onLoadFailed();
                    yield break;
                }

                onLoaded(prefabCache[loadingPrefabName]);
                yield break;
            }
            
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

                    onLoadFailed();
                    yield break;
                }
                
                // set cache.
                prefabCache[loadingPrefabName] = obj;

                onLoaded(obj);
            }
        }


        private IEnumerator LoadPrefabFromDefaultResources (ParsedTree tree, Action<GameObject> onLoaded, Action onLoadFailed) {
            // default path.
            var defaultPath = InformationConstSettings.PREFIX_PATH_INFORMATION_RESOURCE + InformationConstSettings.VIEWNAME_DEFAULT + "/";

            var loadingPrefabName = string.Empty;
            Debug.LogError("ちょっと動きを見て見ないとわからん。");
            switch (tree.treeType) {
                case TreeType.Container: {
                    loadingPrefabName = defaultPath + InformationConstSettings.NAME_PREFAB_CONTAINER;
                    break;
                }
                default: {
                    loadingPrefabName = defaultPath + GetTagFromIndex(tree.parsedTag);
                    break;
                }
            }
            
            var cor = LoadPrefabFromResources(loadingPrefabName, onLoaded, onLoadFailed);
            while (cor.MoveNext()) {
                yield return null;
            }
        }
        

        private CustomTagList customTagList;
        public bool IsLoadingDepthAssetList {
            get; private set;
        }
        private Dictionary<string, TreeType> customTagTypeDict = new Dictionary<string, TreeType>();

        public IEnumerator LoadDepthAssetList (string uriSource) {
            if (IsLoadingDepthAssetList) {
                throw new Exception("multiple depth description found. only one description is valid.");
            }

            var schemeEndIndex = uriSource.IndexOf("//");
            var scheme = uriSource.Substring(0, schemeEndIndex);
            
            IsLoadingDepthAssetList = true;


            Action<CustomTagList> succeeded = customTagList => {
                this.customTagList = customTagList;
                this.customTagTypeDict = this.customTagList.GetTagTypeDict();
                IsLoadingDepthAssetList = false;
            };
            
            Action failed = () => {
                Debug.LogError("failed to load depthAssetList from url:" + uriSource);
                this.customTagList = new CustomTagList(InformationConstSettings.VIEWNAME_DEFAULT, new ContentInfo[0], new LayerInfo[0]);// set empty list.
                IsLoadingDepthAssetList = false;
            };

            switch (scheme) {
                case "assetbundle:": {
                    var bundleName = uriSource;
                    return LoadListFromAssetBundle(uriSource, succeeded, failed);
                }
                case "https:":
                case "http:": {
                    return LoadListFromWeb(uriSource, succeeded, failed);
                }
                case "resources:": {
                    var resourcePath = uriSource.Substring("resources:".Length + 2);
                    return LoadListFromResources(resourcePath, succeeded, failed);
                }
                default: {// other.
                    throw new Exception("unsupported scheme found, scheme:" + scheme);
                }
            }
        }


        public CustomTagList DepthAssetList () {
            if (this.customTagList == null) {
                return new CustomTagList(InformationConstSettings.VIEWNAME_DEFAULT, new ContentInfo[0], new LayerInfo[0]);
            }

            return this.customTagList;
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

        public bool IsCustomTag (int parsedTag) {
            if (parsedTag < (int)HtmlTag._END) {
				return false;
			}

			if (undefinedTagDict.ContainsValue(parsedTag)) {
				return true;
			}

            throw new Exception("failed to determine tag from parsedTag. parsedTag:" + parsedTag);
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




        public static GameObject LoadGameObject (GameObject prefab) {
            // Debug.LogWarning("ここを後々、可視範囲へのオブジェクトプールからの取得に変える。タグ単位でGameObjectのプールを作るか。スクロールとかで可視範囲に入ったら内容を当てる、みたいなのがやりたい。");
            return GameObject.Instantiate(prefab);
        }
    }
}