
using AutoyaFramework.Information;
using Miyamasu;
using UnityEngine;

/**
    test for UUebView generator.
*/
public class UUebViewCoreTests : MiyamasuTestRunner {
    GameObject eventReceiverGameObj;
    GameObject view;
    
    private int index;
    private void Show (GameObject view) {
        RunOnMainThread(
            () => {
                var canvas = GameObject.Find("Canvas/UUebViewCoreTestPlace");
                var baseObj = new GameObject("base");
                

                // ベースオブジェクトを見やすい位置に移動
                var baseObjRect = baseObj.AddComponent<RectTransform>();
                baseObjRect.anchoredPosition = new Vector2(100 * index, 0);

                baseObj.transform.SetParent(canvas.transform, false);

                view.transform.SetParent(baseObj.transform, false);

                index++;
            }
        );
    }

    [MSetup] public void Setup () {
        RunOnMainThread(
            () => {
                eventReceiverGameObj = new GameObject("controller");
                eventReceiverGameObj.AddComponent<TestReceiver>();
            }
        );
    }

    [MTest] public void GenerateSingleViewFromSource () {
        var source = @"
<body>something1.<img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        
        var done = false;
        
        RunOnMainThread(
            () => {
                eventReceiverGameObj.GetComponent<TestReceiver>().OnContentLoaded = () => {
                    done = true;
                };
                view = UUebViewCore.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
            }
        );
        
        Show(view);

        WaitUntil(
            () => done, 5, "too late."
        );
    }

    [MTest] public void GenerateSingleViewFromUrl () {
        var url = "resources://UUebViewTest/UUebViewTest.html";

        var done = false;
        
        RunOnMainThread(
            () => {
                eventReceiverGameObj.GetComponent<TestReceiver>().OnContentLoaded = () => {
                    done = true;
                };
                view = UUebViewCore.GenerateSingleViewFromUrl(eventReceiverGameObj, url, new Vector2(100,100));
            }
        );
        
        Show(view);

        WaitUntil(
            () => done, 5, "too late."
        );
    }

    [MTest] public void LoadThenReload () {
        var source = @"
<body>
    reload sample.
    <img src='https://dummyimage.com/100.png/09f/fff'/>
</body>";

        var done = false;
        
        RunOnMainThread(
            () => {
                eventReceiverGameObj.GetComponent<TestReceiver>().OnContentLoaded = () => {
                    done = true;
                };
                view = UUebViewCore.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
            }
        );
        
        Show(view);

        WaitUntil(
            () => done, 5, "too late."
        );

        var done2 = false;
        
        RunOnMainThread(
            () => {
                eventReceiverGameObj.GetComponent<TestReceiver>().OnContentLoaded = () => {
                    done2 = true;
                };
                var core = view.GetComponent<UUebView>().Core;
                core.Reload();
            }
        );

        WaitUntil(
            () => done2, 5, "too late."
        );
    }

    [MTest] public void ShowAndHide () {
        var source = @"
<body>
    something3.
    <img src='https://dummyimage.com/100.png/09f/fff' id='button' button='true'/>
    <p hidden='false' listen='button'>else</p>
</body>";

        var done = false;
        
        RunOnMainThread(
            () => {
                eventReceiverGameObj.GetComponent<TestReceiver>().OnContentLoaded = () => {
                    done = true;
                };
                view = UUebViewCore.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
            }
        );
        
        Show(view);

        WaitUntil(
            () => done, 5, "too late."
        );
    }

    [MTest] public void HideAndShow () {
        var source = @"
<body>
    something3.
    <img src='https://dummyimage.com/100.png/09f/fff' id='button' button='true'/>
    <p hidden='true' listen='button'>else</p>
</body>";

        var done = false;
        
        RunOnMainThread(
            () => {
                eventReceiverGameObj.GetComponent<TestReceiver>().OnContentLoaded = () => {
                    done = true;
                };
                view = UUebViewCore.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
            }
        );
        
        Show(view);

        WaitUntil(
            () => done, 5, "too late."
        );
    }

    [MTest] public void CascadeButton () {
        var source = @"
<body>
    <img src='https://dummyimage.com/100.png/09f/fff' id='button' button='true'/>
    <img hidden='true' src='https://dummyimage.com/100.png/08f/fff' id='button2' button='true' listen='button'/>
    <img hidden='true' src='https://dummyimage.com/100.png/07f/fff' id='button3' button='true' listen='button2'/>
    <img hidden='true' src='https://dummyimage.com/100.png/06f/fff' id='button4' button='true' listen='button3'/>
</body>";

        var done = false;
        
        RunOnMainThread(
            () => {
                eventReceiverGameObj.GetComponent<TestReceiver>().OnContentLoaded = () => {
                    done = true;
                };
                view = UUebViewCore.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
            }
        );
        
        Show(view);

        WaitUntil(
            () => done, 5, "too late."
        );
    }

    [MTest] public void CachedContent () {
        var source = @"
<body>
    <img src='https://dummyimage.com/100.png/09f/fff' id='button' button='true'/>
    <img hidden='true' src='https://dummyimage.com/100.png/08f/fff' id='button2' button='true' listen='button'/>
    <img hidden='true' src='https://dummyimage.com/100.png/07f/fff' id='button3' button='true' listen='button2'/>
    <img hidden='true' src='https://dummyimage.com/100.png/06f/fff' id='button4' button='true' listen='button3'/>
</body>";

        var done = false;
        
        RunOnMainThread(
            () => {
                eventReceiverGameObj.GetComponent<TestReceiver>().OnContentLoaded = () => {
                    done = true;
                };
                view = UUebViewCore.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
            }
        );
        
        Show(view);

        WaitUntil(
            () => done, 5, "too late."
        );
    }

    [MTest] public void ShowLinkByButton () {
        var source = @"
<body>
    something3.
    <img src='https://dummyimage.com/100.png/09f/fff' id='button1' button='true'/>
    <a href='href test' hidden='true' listen='button1'>link</a>
</body>";

        var done = false;
        
        RunOnMainThread(
            () => {
                eventReceiverGameObj.GetComponent<TestReceiver>().OnContentLoaded = () => {
                    done = true;
                };
                view = UUebViewCore.GenerateSingleViewFromHTML(eventReceiverGameObj, source, new Vector2(100,100));
            }
        );
        
        Show(view);

        WaitUntil(
            () => done, 5, "too late."
        );
    }
}