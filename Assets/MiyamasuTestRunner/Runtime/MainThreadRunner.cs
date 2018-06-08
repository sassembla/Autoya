using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UUebView;

namespace Miyamasu
{
    public class MainThreadRunner : MonoBehaviour, IUUebViewEventHandler
    {
        private int index = 0;
        private bool started;

        private UUebViewComponent currentUUebViewComponent;
        private RectTransform scrollViewRect;

        private RectTransform contentRect;
        private string htmlContent = @"<!DOCTYPE uuebview href='resources://Views/ConsoleTag/UUebTags'>";

        IEnumerator Start()
        {
            while (iEnumGens == null)
            {
                // wait to set enumGens;
                yield return null;
            }

            this.index = 0;
            var totalCount = iEnumGens.Length;

            // wait for check UnityTest is running or not.
            yield return new WaitForSeconds(1);

            if (Miyamasu.Recorder.isRunning)
            {
                Destroy(this);
                yield break;
            }

            var canvasCor = Resources.LoadAsync<GameObject>("MiyamasuPrefabs/MiyamasuCanvas");

            while (!canvasCor.isDone)
            {
                yield return null;
            }

            var canvasPrefab = canvasCor.asset as GameObject;
            var canvas = Instantiate(canvasPrefab);
            canvas.name = "MiyamasuCanvas";

            var scrollViewRectCandidates = canvas.transform.GetComponentsInChildren<RectTransform>();
            GameObject attachTargetView = null;


            // get Content of ScrollView.
            foreach (var rect in scrollViewRectCandidates)
            {
                if (rect.name == "Scroll View")
                {
                    scrollViewRect = rect.gameObject.GetComponent<RectTransform>();
                }
                if (rect.name == "Content")
                {
                    attachTargetView = rect.gameObject;
                    contentRect = attachTargetView.GetComponent<RectTransform>();
                }
            }

            var scrollViewWidth = contentRect.rect.width;
            Recorder.logAct = this.AddLog;

            var view = UUebViewComponent.GenerateSingleViewFromHTML(this.gameObject, htmlContent, new Vector2(scrollViewWidth, 100));
            view.name = "MiyamasuRuntimeConsole";
            view.transform.SetParent(attachTargetView.transform, false);

            currentUUebViewComponent = view.GetComponent<UUebViewComponent>();

            started = true;
            yield return ContCor();
        }

        private List<GameObject> errorMarkOnVerticalBar;

        void Update()
        {
            if (started && Recorder.isStoppedByFail)
            {
                Recorder.isStoppedByFail = false;

                // continue test.
                StartCoroutine(ContCor());
            }

            if (loaded)
            {
                if (logList.Any())
                {
                    loaded = false;

                    var message = string.Join("", logList.ToArray());
                    logList.Clear();

                    currentUUebViewComponent.AppendContentToLast(message);
                }
            }
        }

        private Func<IEnumerator>[] iEnumGens;
        public void SetTests(Func<IEnumerator>[] iEnumGens)
        {
            this.iEnumGens = iEnumGens;
        }

        private IEnumerator ContCor()
        {
            while (index < iEnumGens.Length)
            {
                yield return iEnumGens[index++]();
            }

            Debug.Log("all tests finished.");
        }

        private bool loaded;
        private List<string> logList = new List<string>();
        /**
			this method will be called from jumper lib.
		 */
        public void AddLog(string[] message, Recorder.ReportType type, Exception e)
        {
            var icon = "pass";

            switch (type)
            {
                case Recorder.ReportType.AssertionFailed:
                    {
                        icon = "fail";
                        break;
                    }
                case Recorder.ReportType.FailedByTimeout:
                    {
                        icon = "timeout";
                        break;
                    }
                case Recorder.ReportType.Error:
                    {
                        icon = "error";
                        break;
                    }
                case Recorder.ReportType.Passed:
                    {
                        icon = "pass";
                        break;
                    }
                default:
                    {
                        icon = "error";
                        break;
                    }
            }

            var messageBlock = message[0] + " / " + message[1];
            if (2 < message.Length)
            {
                messageBlock += " line:" + message[2];
            }

            var error = string.Empty;
            // if (e != null) {
            // 	var id = Guid.NewGuid().ToString();
            // 	error =  @" button='true' src='" + Base64Encode(e.ToString()) + @"' id='" + id + @"'";
            // }

            logList.Add(@"
				<bg" + error + @">
					<textbg>
						<contenttext>" + messageBlock + @"</contenttext>
					</textbg>
					<iconbg><" + icon + @"/></iconbg>
				</bg><br>");

        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        void IUUebViewEventHandler.OnLoadStarted()
        {
            // throw new NotImplementedException();
        }

        void IUUebViewEventHandler.OnProgress(double progress)
        {
            // throw new NotImplementedException();
        }

        private List<float> onVerticalBarErrorPos = new List<float>();
        void IUUebViewEventHandler.OnLoaded(string[] treeIds)
        {
            loaded = true;

            if (logList.Any())
            {
                loaded = false;

                var message = string.Join("", logList.ToArray());
                logList.Clear();

                currentUUebViewComponent.AppendContentToLast(message);
            }

            ShowErrorMark(treeIds);
        }

        private List<GameObject> goPool = new List<GameObject>();
        void IUUebViewEventHandler.OnUpdated(string[] newTreeIds)
        {
            loaded = true;
            if (logList.Any())
            {
                loaded = false;

                var message = string.Join("", logList.ToArray());
                logList.Clear();

                currentUUebViewComponent.AppendContentToLast(message);
            }

            ShowErrorMark(newTreeIds);
        }

        private void ShowErrorMark(string[] newTreeIds)
        {
            foreach (var contentId in newTreeIds)
            {
                var yPos = currentUUebViewComponent.GetTreeById(contentId)[0].offsetY;
                onVerticalBarErrorPos.Add(yPos);
            }

            var beforePos = -scrollViewRect.rect.height;
            var markIndex = 0;
            foreach (var pos in onVerticalBarErrorPos)
            {
                // 位置差が一画面内より小さかったら無視する
                if (pos - beforePos < scrollViewRect.rect.height)
                {
                    continue;
                }

                beforePos = pos;

                // 最大でこの数だけ、verticalScrollBar上に赤いマークを出す。
                var ratio = (pos / contentRect.rect.height) * scrollViewRect.rect.height;

                if (goPool.Count <= markIndex)
                {
                    var cursorObj = new GameObject();
                    var rectTrans = cursorObj.AddComponent<RectTransform>();

                    rectTrans.anchorMin = Vector2.one;
                    rectTrans.anchorMax = Vector2.one;
                    rectTrans.pivot = Vector2.one;

                    var image = cursorObj.AddComponent<Image>();
                    image.color = Color.red;

                    goPool.Add(cursorObj);
                }

                var go = goPool[markIndex++];
                var cursorObjRect = go.GetComponent<RectTransform>();
                cursorObjRect.anchoredPosition = new Vector2(32, -ratio);
                cursorObjRect.sizeDelta = new Vector2(30, 4);

                go.transform.SetParent(scrollViewRect.transform, false);
            }
        }

        void IUUebViewEventHandler.OnLoadFailed(ContentType type, int code, string reason)
        {
            Debug.LogError("loadFailed:" + type + " code:" + code + " reason:" + reason);
        }

        private InputField detailText;
        void IUUebViewEventHandler.OnElementTapped(ContentType type, GameObject element, string param, string id)
        {
            var e = Base64Decode(param);
            // で、エラー詳細を表示する。
            if (detailText == null)
            {
                detailText = GameObject.Find("MiyamasuCanvas/DetailBG/InputField").GetComponent<InputField>();
            }
            detailText.text = e;
        }

        void IUUebViewEventHandler.OnElementLongTapped(ContentType type, string param, string id)
        {
            // throw new NotImplementedException();
        }
    }
}