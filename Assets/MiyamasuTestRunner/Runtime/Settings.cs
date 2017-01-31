using System;
using System.IO;
using UnityEngine;

namespace Miyamasu {

    [Serializable] public class RunnerSettings {
        [SerializeField] public bool runOnPlay = true;
        [SerializeField] public bool runOnCompiled = true;
    }

    /**
        interface for settings of Miyamasu.
    */
    public class Settings {
        public static RunnerSettings staticSettings;

        public const string SETTING_DIRECTORY = "Assets/MiyamasuTestRunner/Runtime/Resources/";
        public const string SETTING_FILE_PATH = SETTING_DIRECTORY + "MiyamasuSettings.txt";
        
        public static RunnerSettings LoadSettings () {
            #if UNITY_EDITOR
            {
                if (!Directory.Exists(SETTING_DIRECTORY)) {
                    Directory.CreateDirectory(SETTING_DIRECTORY);
                }

                if (!File.Exists(SETTING_FILE_PATH)) {
                    File.CreateText(SETTING_FILE_PATH);
                }
            }
            #endif

            var settingDataStr = string.Empty;
            using (var sr = new StreamReader(SETTING_FILE_PATH)) {
                settingDataStr = sr.ReadToEnd();
            }

            if (string.IsNullOrEmpty(settingDataStr)) {
                #if UNITY_EDITOR
                {
                    WriteSettings(new RunnerSettings());
                }
                #endif
                staticSettings = new RunnerSettings();
                return staticSettings;
            }
            staticSettings = JsonUtility.FromJson<RunnerSettings>(settingDataStr);
            return staticSettings;
        }

        public static void WriteSettings (RunnerSettings newSettings) {
            staticSettings = newSettings;
            #if UNITY_EDITOR
            {
                var newSettingsStr = JsonUtility.ToJson(newSettings);

                using (var sw = new StreamWriter(SETTING_FILE_PATH)) {
                    sw.Write(newSettingsStr);
                }
            }
            #endif
        }
    }
}