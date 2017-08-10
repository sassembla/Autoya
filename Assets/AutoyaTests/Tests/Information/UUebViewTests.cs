
using AutoyaFramework.Information;
using Miyamasu;
using UnityEngine;

/**
    test for UUebView generator.
*/
public class UUebViewTests : MiyamasuTestRunner {
    GameObject eventReceiverGameObj;
    GameObject view;
    
    private int index;
    private void Show (GameObject view) {
        RunOnMainThread(
            () => {
                var canvas = GameObject.Find("Canvas/UUebViewTestPlace");
                var baseObj = new GameObject("base");

                baseObj.transform.SetParent(canvas.transform, false);
                view.transform.SetParent(baseObj.transform);

                // ベースオブジェクトを見やすい位置に移動
                var baseObjRect = baseObj.AddComponent<RectTransform>();
                baseObjRect.anchoredPosition += new Vector2(100 * index, 0);

                index++;
            }
        );
    }

    [MSetup] public void Setup () {
        RunOnMainThread(
            () => {
                eventReceiverGameObj = new GameObject("controller");
            }
        );
    }

    [MTest] public void GenerateSingleViewFromSource () {
        var source = @"
<body>
    something
    <img src='https://dummyimage.com/100.png/09f/fff'/>
</body>";

        RunOnMainThread(
            () => {
                view = UUebViewCore.GenerateSingleViewFromSource(eventReceiverGameObj, source, new Vector2(100,100));
            }
        );
        
        Show(view);

        WaitUntil(
            () => eventReceiverGameObj == null, 5, "too late."
        );
    }

    [MTest] public void GenerateSingleViewFromUrl () {
        var url = "resources://UUebViewTest/UUebViewTest.html";

        RunOnMainThread(
            () => {
                view = UUebViewCore.GenerateSingleViewFromUrl(eventReceiverGameObj, url, new Vector2(100,100));
            }
        );
        
        Show(view);

        WaitUntil(
            () => eventReceiverGameObj == null, 5, "too late."
        );
    }

    [MTest] public void LoadThenReload () {
        var source = @"
<body>
    something
    <img src='https://dummyimage.com/100.png/09f/fff'/>
</body>";

        RunOnMainThread(
            () => {
                view = UUebViewCore.GenerateSingleViewFromSource(eventReceiverGameObj, source, new Vector2(100,100));
            }
        );
        
        Show(view);

        WaitUntil(
            () => eventReceiverGameObj == null, 5, "too late."
        );

        RunOnMainThread(
            () => {
                var core = view.GetComponent<UUebView>().Core;
                core.Reload();
            }
        );
    }
}