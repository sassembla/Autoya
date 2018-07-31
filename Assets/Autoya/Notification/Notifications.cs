using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AutoyaFramework.Notification
{
    public enum Notification
    {
        URLScheme,
    }

    public class Notifications
    {
        private enum FilePath
        {
            URLSchemeFile,

        }

        private readonly Action<string, Action<string>> observerMethod;
        public Notifications(Action<string, Action<string>> observerMethod)
        {
            this.observerMethod = observerMethod;
        }

        public void SetURLSchemeReceiver(Action<Dictionary<string, string>> onURLScheme)
        {
            var dataPath = Application.persistentDataPath;
            var targetFilePath = Path.Combine(dataPath, FilePath.URLSchemeFile.ToString());


            // セットを行う
            observerMethod(
                Notification.URLScheme.ToString(),
                data =>
                {
                    if (File.Exists(targetFilePath))
                    {
                        File.Delete(targetFilePath);
                    }

                    var paramDict = ReadURLScheme(data);
                    onURLScheme(paramDict);
                }
            );

            // ファイルがすでに存在していたら、消しつつイベントを着火する
            if (File.Exists(targetFilePath))
            {
                var url = File.ReadAllText(targetFilePath);
                File.Delete(targetFilePath);

                var paramDict = ReadURLScheme(url);

                onURLScheme(paramDict);
            }
        }

        private Dictionary<string, string> ReadURLScheme(string url)
        {
            var scheme = url.Split(':')[0];
            var encodedPathAndQueriesBase = url.Split('/');

            if (encodedPathAndQueriesBase.Length < 2)
            {
                return new Dictionary<string, string>();
            }

            var encodedPathAndQueries = encodedPathAndQueriesBase[2];
            var decodedPath = encodedPathAndQueries.Replace("%3F", "?").Replace("%3D", "=").Replace("%26", "&");

            var pathAndQueries = decodedPath.Split('?');
            var path = pathAndQueries[0];

            var paramDict = new Dictionary<string, string>();
            if (1 < pathAndQueries.Length)
            {
                var keyValues = pathAndQueries[1].Split('&');
                foreach (var kv in keyValues)
                {
                    var keyAndVal = kv.Split('=');
                    if (1 < keyAndVal.Length)
                    {
                        paramDict[keyAndVal[0]] = keyAndVal[1];
                    }
                }
            }

            return paramDict;
        }




        public void Debug_WriteURLScheme(string rawParam)
        {
            var dataPath = Application.persistentDataPath;
            var targetFilePath = Path.Combine(dataPath, FilePath.URLSchemeFile.ToString());
            using (var a = File.CreateText(targetFilePath))
            {
                var param = rawParam.Replace("?", "%3F").Replace("=", "%3D").Replace("&", "%26");
                a.WriteLine(param);
            }
        }
    }
}