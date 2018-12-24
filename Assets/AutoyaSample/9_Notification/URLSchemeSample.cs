using AutoyaFramework;
using UnityEngine;

public class URLSchemeSample : MonoBehaviour
{
    public void Start()
    {
        /*
            please set URL Scheme for your app into Player Settings.
            this handler will fire when you tap your URL Scheme on browser/mail/other app.
            
            e,g,
                sctest://heheh?herecomes=daredevil&you=good
         */
        Autoya.Notification_SetURLSchemeReceiver(
            schemeParameterDict =>
            {
                foreach (var item in schemeParameterDict)
                {
                    Debug.Log("item:" + item.Key + " val:" + item.Value);
                }
            }
        );
    }
}