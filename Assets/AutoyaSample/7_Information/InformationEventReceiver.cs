using System.Collections;
using System.Collections.Generic;
using AutoyaFramework;
using UUebView;
using UnityEngine;

public class InformationEventReceiver : MonoBehaviour, IUUebViewEventHandler
{

    // Use this for initialization
    void Start()
    {
        var scrollView = GameObject.Find("Scroll View");
        AutoyaFramework.Autoya.Info_Show(scrollView, "resources://items.html");
    }


    void IUUebViewEventHandler.OnLoadStarted()
    {
        Debug.Log("load started.");
    }

    void IUUebViewEventHandler.OnProgress(double progress)
    {
        Debug.Log("loading.. progress:" + progress);
    }

    void IUUebViewEventHandler.OnLoaded()
    {
        Debug.Log("loaded.");
    }

    void IUUebViewEventHandler.OnLoadFailed(ContentType type, int code, string reason)
    {
        Debug.LogError("load failed:" + type + " code:" + code + " reason:" + reason);
    }


    void IUUebViewEventHandler.OnUpdated()
    {
        Debug.Log("updated.");
    }


    void IUUebViewEventHandler.OnElementTapped(ContentType type, GameObject element, string param, string id)
    {
        Debug.Log("element tapped:" + type + " id:" + id);
        StartCoroutine(RotateElement(element));
    }

    void IUUebViewEventHandler.OnElementLongTapped(ContentType type, string param, string id)
    {
        throw new System.NotImplementedException();
        // このイベントを呼ぶ実装がどこにもないのでまだ着火されない。
    }
    



    /**
        適当に時間をかけてボタンアイコンを半回転させるアニメーション。
     */
    private IEnumerator RotateElement(GameObject target)
    {
        var rectTrans = target.GetComponent<RectTransform>();
        var count = 0;
        var max = 18;
        var diff = new Vector3(0, 0, -(180f / max));

        var start = rectTrans.rotation;

        while (true)
        {
            rectTrans.Rotate(diff);
            count++;

            if (count == max)
            {
                break;
            }
            yield return null;
        }

        rectTrans.rotation = Quaternion.Euler(0, 0, start.eulerAngles.z - 180f);
    }


}
