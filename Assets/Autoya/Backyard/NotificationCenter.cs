using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.Notification;
using UnityEngine;

/*
	notification center
*/
namespace AutoyaFramework
{
    public partial class Autoya
    {
        private Notifications _notification;

        public static void Notification_SetURLSchemeReceiver(Action<Dictionary<string, string>> onReceive)
        {
            autoya._notification.SetURLSchemeReceiver(
                data => onReceive(data)
            );
        }
    }
}