using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UUebView
{
    public class Reporter
    {
        [MenuItem("Window/UUebView/Report Problem With Selection")]
        public static void Report()
        {
            var window = EditorWindow.GetWindow<HTMLReportWindow>(typeof(HTMLReportWindow));
            window.Init("paste your html here.", t => StartReport(t));
        }

        public static void StartReport(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                Debug.LogError("html is empty. please set the html which you want to report.");
                return;
            }

            // validateしといたほうがいいような気もするが。

            var reportTargetUUebTagName = string.Empty;
            var uuebTagsCandidate = Selection.activeGameObject;
            if (uuebTagsCandidate != null && uuebTagsCandidate.transform.parent != null && uuebTagsCandidate.transform.parent.GetComponent<Canvas>() != null)
            {
                reportTargetUUebTagName = uuebTagsCandidate.name;
            }
            else
            {
                Debug.LogError("please select the source GameObject of the UUebTags which you generated in hierarchy. source GameObject should locate under Canvas component.");
                return;
            }

            // 選択対象のuuebTagsが存在して、とりあえずフォルダがあるはず。
            var targetUUebTagsPath = Path.Combine(ConstSettings.FULLPATH_INFORMATION_RESOURCE, reportTargetUUebTagName);
            if (!Directory.Exists(targetUUebTagsPath))
            {
                Debug.LogError("no UUebTags named:" + " exists. please generate UUebTags from Uniyt > Window > UUebView > ");
                return;
            }

            // unitypackageを作り出す。
            // defaultと、htmlと、あるならば選択されているUUebTag。
            // htmlを書くと、テキストファイルが作られて、レポート時にファイルとして入る、と言う風にするか。
            // レポートが終わったら消す。
            var tempPath = "Assets/UUebView/Report/Resources/UUebViewReport/report.txt";

            if (!Directory.Exists("Assets/UUebView/Report"))
            {
                Directory.CreateDirectory("Assets/UUebView/Report");
            }

            if (!Directory.Exists("Assets/UUebView/Report/Resources"))
            {
                Directory.CreateDirectory("Assets/UUebView/Report/Resources");
            }

            if (!Directory.Exists("Assets/UUebView/Report/Resources/UUebViewReport"))
            {
                Directory.CreateDirectory("Assets/UUebView/Report/Resources/UUebViewReport");
            }


            File.WriteAllText(tempPath, html);

            var reportTargetAssetPaths = new List<string>();

            using (new ShouldDeleteFileAtPathContstraint(tempPath))
            {
                // 作成したhtmlを依存に巻き込む。
                reportTargetAssetPaths.Add(tempPath);

                // この対象のUUebTagが存在しているはず。
                var targetUUebTagFilePaths = Directory.GetFiles(targetUUebTagsPath);
                reportTargetAssetPaths.AddRange(targetUUebTagFilePaths);


                // デフォルトもあるはずなので、そちらも。なかったらいらない。
                if (Directory.Exists(ConstSettings.FULLPATH_DEFAULT_TAGS))
                {
                    var rargetDefaultUUebTagFilePaths = Directory.GetFiles(ConstSettings.FULLPATH_DEFAULT_TAGS);
                    reportTargetAssetPaths.AddRange(rargetDefaultUUebTagFilePaths);
                }

                // unitypackageを吐き出す
                AssetDatabase.ExportPackage(reportTargetAssetPaths.ToArray(), "Report_" + uuebTagsCandidate + ".unitypackage", ExportPackageOptions.IncludeDependencies);
            }
            Debug.Log("report exported as unitypackage:" + "Report_" + uuebTagsCandidate + ".unitypackage" + ". above waring about meta data is harmless. please send exported unitypackage to github issue. https://github.com/sassembla/UUebView-freeversion/issues");
        }

        private class ShouldDeleteFileAtPathContstraint : IDisposable
        {
            private string targetFilePath;
            public ShouldDeleteFileAtPathContstraint(string targetFilePath)
            {
                AssetDatabase.Refresh();
                this.targetFilePath = targetFilePath;
            }

            private bool disposedValue = false;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // dispose.
                        File.Delete(targetFilePath);
                        AssetDatabase.Refresh();
                    }
                    disposedValue = true;
                }
            }

            void IDisposable.Dispose()
            {
                Dispose(true);
            }
        }
    }
}

public class HTMLReportWindow : EditorWindow
{
    private string txt;
    Action<string> onReport;
    public void Init(string defaultTxt, Action<string> onReport)
    {
        txt = defaultTxt;
        this.onReport = onReport;
    }

    void OnGUI()
    {
        using (new GUILayout.HorizontalScope())
        {
            txt = GUILayout.TextArea(txt);

            if (GUILayout.Button("Report"))
            {
                onReport(txt);
            }
        }
    }
}