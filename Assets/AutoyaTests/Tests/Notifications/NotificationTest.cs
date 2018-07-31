using System;
using System.Collections.Generic;
using AutoyaFramework.Notification;
using Miyamasu;
using UnityEngine;

public class NotificationTest : MiyamasuTestRunner
{
    private Notifications notif;

    private Dictionary<string, Action<string>> dict = new Dictionary<string, Action<string>>();

    [MSetup]
    public void Setup()
    {
        // 登録を行う
        notif = new Notifications(
            (a, b) =>
            {
                dict[a] = b;
            }
        );
    }

    [MTest]
    public void GetStoredURLSchemeOnBoot()
    {
        // 書き込みを行い、イベントを着火させる
        notif.Debug_WriteURLScheme("scheme://app?key=value&and=other");

        var result = new Dictionary<string, string>();

        // tempに保存してあるNotificationファイルを取得する
        notif.SetURLSchemeReceiver(
            data =>
            {
                result = data;
            }
        );

        True(0 < result.Count, "empty.");
    }

    [MTest]
    public void GetStoredURLSchemeAfterBooted()
    {
        var result = new Dictionary<string, string>();

        // tempに保存してあるNotificationファイルを取得する
        notif.SetURLSchemeReceiver(
            data =>
            {
                result = data;
            }
        );

        // sendMessageを受け取り -> 辞書関数が着火したことにする
        dict[Notification.URLScheme.ToString()]("scheme://app?key=value&and=other");

        True(0 < result.Count, "empty.");
    }
}
