using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/**
	support features for control data storage for app.
*/
namespace AutoyaFramework.Persistence.Files
{

    public class PersistenceSupport
    {
        [MenuItem("Window/Autoya/Clean Cached AssetBundles")]
        public static void CleanCache()
        {
            Caching.ClearCache();
        }


        [MenuItem("Window/Autoya/Persistence/Delete Stored Persistent Folders")]
        public static void DeleteFiles()
        {
            var persistencePath = Application.persistentDataPath;
            var filePaths = Directory.GetDirectories(persistencePath);
            if (filePaths.Any())
            {
                var window = EditorWindow.GetWindow<DeleteWindow>(typeof(DeleteWindow));
                window.Init(filePaths.Select(p => new DirectoryInfo(p).Name).ToArray());
                window.titleContent = new GUIContent("Delete Folders");
                return;
            }

            Debug.Log("no persist file found.");
        }
    }

    public class DeleteWindow : EditorWindow
    {
        private string[] dirNames;
        private bool[] buttons;

        private string basePath;
        public void Init(string[] dirNames)
        {
            this.dirNames = dirNames;
            this.buttons = new bool[dirNames.Length];
            this.basePath = Application.persistentDataPath;
        }


        private bool deleteAll;
        void OnGUI()
        {
            GUILayout.Label("Select folder(s) to delete.");

            var before = deleteAll;
            var delAll = GUILayout.Toggle(deleteAll, "DELETE ALL");
            deleteAll = delAll;

            GUILayout.Space(10);

            for (var i = 0; i < buttons.Length; i++)
            {
                if (before != deleteAll)
                {
                    buttons[i] = deleteAll;
                }

                using (new GUILayout.VerticalScope())
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        var buttonName = dirNames[i];
                        buttons[i] = GUILayout.Toggle(buttons[i], buttonName);
                    }
                }
            }

            if (GUILayout.Button("Delete Selected Folders"))
            {
                var deleteTargetPaths = new List<string>();
                for (var i = 0; i < buttons.Length; i++)
                {
                    var button = buttons[i];
                    if (button)
                    {
                        var folderName = dirNames[i];
                        deleteTargetPaths.Add(Path.Combine(basePath, folderName));
                    }
                }

                foreach (var path in deleteTargetPaths)
                {
                    Directory.Delete(path, true);
                    Debug.Log("folder deleted:" + path);
                }
                this.Close();
            }

            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label("note:these folders are located at persistentDataPath:");
                GUILayout.TextField(basePath);
                GUILayout.Label("the 'Unity' folder contains Purchase record datas.");
                GUILayout.Space(10);
                GUILayout.Label("Downloaded AssetBundles are located at /Users/USER_NAME/Library/Caches/Unity/ on macOS, ");
                GUILayout.Label("and, located at Users\\USER_NAME\\AppData\\LocalLow\\Unity\\WebPlayer\\Cache on Windows.");
                GUILayout.Label("to delete these cache files, use Window > Autoya > Clean Cached AssetBundles.");

            }
        }
    }
}