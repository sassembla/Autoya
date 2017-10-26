using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.Purchasing
{
    static class UnityIAPInstaller
    {
        private static bool m_Trace = false;

        static readonly string k_ServiceName = "IAP";
        static readonly string k_PackageName = "Unity IAP";

// The initial prompt delays the AssetDatabase.ImportAssetPackage call until after all
// assemblies are loaded. Without this delay in Unity 5.5 (on Windows specifically), 
// the editor crashes when the method is called with DidReloadScripts.
#if UNITY_EDITOR_WIN && UNITY_5_5_OR_NEWER
        static bool m_EnableInstallerPrompt = true;
#else
        static bool m_EnableInstallerPrompt = false;
#endif

        private class Artifact
        {
            private readonly string _relativePath;
            private readonly Func<bool> _shouldInstall;
            private readonly bool _installInteractively;

            public Artifact(string relativePath, Func<bool> shouldInstall, bool installInteractively)
            {
                _relativePath = relativePath;
                _shouldInstall = shouldInstall;
                _installInteractively = installInteractively;
            }

            public bool ShouldInstall()
            {
                return _shouldInstall == null || _shouldInstall();
            }

            public bool CanInstall()
            {
                return AssetExists(AssetPath());
            }

            public string AssetPath()
            {
                return GetAssetPath(_relativePath);
            }

            public bool InstallInteractively()
            {
                return _installInteractively;
            }

            public override string ToString()
            {
                return "Artifact: RelativePath=" + _relativePath + " ShouldInstall=" + ShouldInstall();
            }
        }

        /// <summary>
        /// Install assets, in dependency-order
        /// NOTE: Only the last Artifact can be interactive, else it will be interrupted a later artifact.
        /// </summary>
        private static readonly Artifact[] k_Artifacts =
        {
            // E.g.: new Artifact("Sample.unitypackage", () => ShouldInstallSamplePackage() == true, false),
            new Artifact("Plugins/UnityPurchasing/UnityChannel.unitypackage", () => ShouldInstallUnityChannel(), false),
            new Artifact("Plugins/UnityPurchasing/UnityIAP.unitypackage", null, true),
        };

        static readonly string k_InstallerFile = "Plugins/UnityPurchasing/Editor/UnityIAPInstaller.cs";
        static readonly string k_ObsoleteFilesCSVFile = "Plugins/UnityPurchasing/Editor/ObsoleteFilesOrDir.csv";
        static readonly string k_ObsoleteGUIDsCSVFile = "Plugins/UnityPurchasing/Editor/ObsoleteGUIDs.csv";
        static readonly string k_IAPHelpURL = "https://docs.unity3d.com/Manual/UnityIAPSettingUp.html";
        static readonly string k_ProjectHelpURL = "https://docs.unity3d.com/Manual/SettingUpProjectServices.html";

        /// <summary>
        /// Prevent multiple simultaneous installs
        ///  0 or none   = installation not started
        ///  1           = installation starting
        ///  2 or higher = installing artifact (2 - thisIndex)
        /// </summary>
        static readonly string k_PrefsKey_ImportingAssetPackage = "UnityIAPInstaller_ImportingAssetPackage";
        static readonly string k_PrefsKey_LastAssetPackageImport = "UnityIAPInstaller_LastAssetPackageImportDateTimeBinary";
		static readonly double k_MaxLastImportReasonableTicks = 30 * 10000000; // Installs started n seconds from 'now' are not considered 'simultaneous'

        static readonly string[] k_ObsoleteFilesOrDirectories = GetFromCSV(GetAbsoluteFilePath(k_ObsoleteFilesCSVFile));
        static readonly string[] k_ObsoleteGUIDs = GetFromCSV(GetAbsoluteFilePath(k_ObsoleteGUIDsCSVFile));

        static readonly bool k_RunningInBatchMode = Environment.CommandLine.ToLower().Contains(" -batchmode");

        private static Type GetPurchasing()
        {
            return (
                from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where type.Name == "UnityPurchasing" && type.GetMethods().Any(m => m.Name == "Initialize")
                select type).FirstOrDefault();
        }

        // Assets/Plugins//UnityChannel/UnityStore.dll.meta
        static readonly string k_UnityChannel_UnityStoreDll = "b4a9e40b1d1574159814dede56a53cb3";
        
        /// <summary>
        /// Install UnityChannel if it's not already installed by packman.
        /// </summary>
        private static bool ShouldInstallUnityChannel()
        {
            bool result = true;

            // Unity 2017.2 has a package manager where files are stored under the Packages/ folder.
            // Packages/com.unity.xiaomi@1.0.0/UnityChannel/UnityStore.dll
            string path = AssetDatabase.GUIDToAssetPath(k_UnityChannel_UnityStoreDll);
            if (m_Trace) Debug.Log("ShouldInstallUnityChannel path = " + path);

            if (!string.IsNullOrEmpty(path) && path.StartsWith("Packages/"))
            {
                return false;
            }

            return result;
        }

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
        static readonly bool k_IsIAPSupported = true;
#else
        static readonly bool k_IsIAPSupported = false;
#endif

#if UNITY_5_5_OR_NEWER && false // Service window prevents this from working properly. Disabling for now.
        static readonly bool k_IsEditorSettingsSupported = true;
#else
        static readonly bool k_IsEditorSettingsSupported = false;
#endif

        private enum DialogKind
        {
            Installer = 0,
            CanceledInstaller,
            ProjectConfig,
            EnableService,
            EnableServiceManually,
            CanceledEnableService,
            DeleteAssets,
            CanceledDeleteAssets,
            UnityRequirement,
            MissingPackage,
        }

        private class Dialog
        {
            public Dialog(DialogKind kind, string[] fields)
            {
                _kind = kind;
                _fields = fields;
            }

            private readonly DialogKind _kind;
            private readonly string[] _fields;

            public static bool DisplayDialog(DialogKind dialogKind)
            {
                var dialog = k_Dialogs[(int)dialogKind];
                Assert.AreEqual(dialog._kind, dialogKind);

                if (k_RunningInBatchMode)
                {
                    if (dialog._fields[0] != null || dialog._fields[0].Length != 0)
                    {
                        Debug.Log(dialog._fields[0]);
                    }
                    return true;
                }

                return EditorUtility.DisplayDialog(
                    dialog._fields[1],
                    dialog._fields[2],
                    dialog._fields[3],
                    dialog._fields[4]);
            }
        }

        private static readonly Dialog[] k_Dialogs =
        {
            new Dialog(DialogKind.Installer, new [] {
                null,
                k_PackageName + " Installer",
                "The " + k_PackageName + " installer will determine if your project is configured properly " +
                "before importing the " + k_PackageName + " asset package.\n\n" +
                "Would you like to run the " + k_PackageName + " installer now?",
                "Install Now",
                "Cancel",
            }),
            new Dialog(DialogKind.CanceledInstaller, new [] {
                string.Format("User declined to run the {0} installer. Canceling installer process now...", k_PackageName),
                k_PackageName + " Installer",
                "The " + k_PackageName + " installer has been canceled. " +
                "Please import the " + k_PackageName + " asset package again to continue the install.",
                "OK",
                null,
            }),
            new Dialog(DialogKind.ProjectConfig, new [] {
                "Unity Project ID is not currently set. Canceling installer process now...",
                k_PackageName + " Installer",
                "A Unity Project ID is not currently configured for this project.\n\n" +
                "Before the " + k_ServiceName + " service can be enabled, a Unity Project ID must first be " +
                "linked to this project. Once linked, please import the " + k_PackageName + " asset package again" +
                "to continue the install.\n\n" +
                "Select 'Help...' to see further instructions.",
                "OK",
                "Help...",
            }),
            new Dialog(DialogKind.EnableService, new [] {
                string.Format("The {0} service is currently disabled. Enabling the {0} Service now...", k_ServiceName),
                k_PackageName + " Installer",
                "The " + k_ServiceName + " service is currently disabled.\n\n" +
                "To avoid encountering errors when importing the " + k_PackageName + " asset package, " +
                "the " + k_ServiceName + " service must be enabled first before importing the latest " +
                k_PackageName + " asset package.\n\n" +
                "Would you like to enable the " + k_ServiceName + " service now?",
                "Enable Now",
                "Cancel",
            }),
            new Dialog(DialogKind.EnableServiceManually, new [] {
                string.Format("The {0} service is currently disabled. Canceling installer process now...", k_ServiceName),
                k_PackageName + " Installer",
                "The " + k_ServiceName + " service is currently disabled.\n\n" +
                "Canceling the install process now to avoid encountering errors when importing the " +
                k_PackageName + " asset package. The " + k_ServiceName + " service must be enabled first " +
                "before importing the latest " + k_PackageName + " asset package.\n\n" +
                "Please enable the " + k_ServiceName + " service through the Services window. " +
                "Then import the " + k_PackageName + " asset package again to continue the install.\n\n" +
                "Select 'Help...' to see further instructions.",
                "OK",
                "Help...",
            }),
            new Dialog(DialogKind.CanceledEnableService, new [] {
                string.Format("User declined to enable the {0} service. Canceling installer process now...", k_ServiceName),
                k_PackageName + " Installer",
                "The " + k_PackageName + " installer has been canceled.\n\n" +
                "Please enable the " + k_ServiceName + " service through the Services window. " +
                "Then import the " + k_PackageName + " asset package again to continue the install.\n\n" +
                "Select 'Help...' to see further instructions.",
                "OK",
                "Help...",
            }),
            new Dialog(DialogKind.DeleteAssets, new [] {
                string.Format("Found obsolete {0} assets. Deleting obsolete assets now...", k_PackageName),
                k_PackageName + " Installer",
                "Found obsolete assets from an older version of the " + k_PackageName + " asset package.\n\n" +
                "Would you like to remove these obsolete " + k_PackageName + " assets now?",
                "Delete Now",
                "Cancel",
            }),
            new Dialog(DialogKind.CanceledDeleteAssets, new [] {
                string.Format("User declined to remove obsolete {0} assets. Canceling installer process now...", k_PackageName),
                k_PackageName + " Installer",
                "The " + k_PackageName + " installer has been canceled.\n\n" +
                "Please delete any previously imported " + k_PackageName + " assets from your project. " +
                "Then import the " + k_PackageName + " asset package again to continue the install.",
                "OK",
                null,
            }),
            new Dialog(DialogKind.UnityRequirement, new [] {
                "Installer requires Unity 5.3 or higher, cancelling now...",
                k_PackageName + " Installer",
                "The " + k_PackageName + " installer has been canceled.\n\n" +
                "Requires Unity 5.3 or higher.",
                "OK",
                null,
            }),
            new Dialog(DialogKind.MissingPackage, new [] {
                "Installer corrupt, missing package. Cancelling now...",
                k_PackageName + " Installer",
                "The " + k_PackageName + " installer has been canceled.\n\n" +
                "This installer is corrupt, and is missing one or more unitypackages.",
                "OK",
                null,
            }),
        };

#if !DISABLE_UNITY_IAP_INSTALLER
        [Callbacks.DidReloadScripts]
#endif
        /// <summary>
        /// * Install may be called multiple times during the AssetDatabase.ImportPackage
        ///   process. Detect this and avoid restarting installation.
        /// * Install may fail unexpectedly in the middle due to crash. Detect
        ///   this heuristically with a timestamp, deleting mutex for multiple
        ///   install detector.
        /// </summary>
        private static void Install ()
        {
            // Detect and fix interrupted installation
            FixInterruptedInstall();

            if (k_RunningInBatchMode)
            {
                Debug.LogFormat("Preparing to install the {0} asset package...", k_PackageName);
            }

            // Detect multiple calls to this method and ignore
            if (PlayerPrefs.HasKey(k_PrefsKey_ImportingAssetPackage))
            {
                // Resubscribe to "I'm done installing" callback as it's lost
                // on each Reload.
                EditorApplication.delayCall += OnStepCallback;
            }
            else if (!k_IsIAPSupported)
            {
                Dialog.DisplayDialog(DialogKind.UnityRequirement);
                OnComplete();
            }
            else if (m_EnableInstallerPrompt && !Dialog.DisplayDialog(DialogKind.Installer))
            {
                Dialog.DisplayDialog(DialogKind.CanceledInstaller);
                OnComplete();
            }
            else if (!DeleteObsoleteAssets(k_ObsoleteFilesOrDirectories, k_ObsoleteGUIDs))
            {
                OnComplete();
            }
            else if (!EnablePurchasingService())
            {
                OnComplete();
            }
            else
            {
                // Start installing
                PlayerPrefs.SetInt(k_PrefsKey_ImportingAssetPackage, 1);
                OnStep();
            }
        }

        /// <summary>
        /// Detects and fixes the interrupted install.
        /// </summary>
        private static void FixInterruptedInstall()
        {
            if (!PlayerPrefs.HasKey(k_PrefsKey_LastAssetPackageImport)) return;

            var lastImportDateTimeBinary = PlayerPrefs.GetString(k_PrefsKey_LastAssetPackageImport);

            long lastImportLong = 0;
            try {
                lastImportLong = Convert.ToInt64(lastImportDateTimeBinary);
            } catch (SystemException) {
                // Ignoring exception converting long
                // By default '0' value will trigger install-cleanup
            }

            var lastImport = DateTime.FromBinary(lastImportLong);
            double dt = Math.Abs(DateTime.UtcNow.Ticks - lastImport.Ticks);

            if (dt > k_MaxLastImportReasonableTicks)
            {
                Debug.Log("Installer detected interrupted installation (" + dt / 10000000 + " seconds ago). Reenabling install.");

                // Fix it!
                PlayerPrefs.DeleteKey(k_PrefsKey_ImportingAssetPackage);
                PlayerPrefs.DeleteKey(k_PrefsKey_LastAssetPackageImport);
            }

            // else dt is not too large, installation okay to proceed
        }

        public static void OnStepCallback()
        {
            EditorApplication.delayCall -= OnStepCallback;
            OnStep();
        }

        /// <summary>
        /// Kicks off installation, depending upon step-counter
        /// </summary>
        private static void OnStep ()
        {
            var isInstalling = false;

            // can be zero, or any of the indices in k_PackageFiles (+1)
            var importStep = PlayerPrefs.GetInt(k_PrefsKey_ImportingAssetPackage);
            var nextImportStep = importStep + 1;

            var packageIndex = importStep - 1;

            // Install if there's a package yet to be installed, then try to install it
            if (packageIndex >= 0 && packageIndex <= k_Artifacts.Length - 1)
            {
                var artifact = k_Artifacts[packageIndex];

                if (m_Trace) Debug.Log("Installing: packageIndex=" + packageIndex + " artifact=" + artifact);

                if (artifact.CanInstall())
                {
                    // Record fact installation has started
                    PlayerPrefs.SetInt(k_PrefsKey_ImportingAssetPackage, nextImportStep);
                    // Record time installation started
                    PlayerPrefs.SetString(k_PrefsKey_LastAssetPackageImport, DateTime.UtcNow.ToBinary().ToString());

                    if (artifact.ShouldInstall())
                    {
                        if (m_Trace) Debug.Log("Artifact.ShouldInstall passed: importing ...");
                        
                        // Start async ImportPackage operation, causing one or more
                        // Domain Reloads as a side-effect
                        AssetDatabase.ImportPackage(artifact.AssetPath(), artifact.InstallInteractively());
                        // All in-memory values hereafter may be cleared due to Domain
                        // Reloads by async ImportPackage operation
                        EditorApplication.delayCall += OnStepCallback;
                    }
                    else
                    {
                        if (m_Trace) Debug.Log("Artifact.ShouldInstall failed: moving on to next artifact");

                        OnStep(); // WARNING: recursion
                    }

                    isInstalling = true;
                }
                else
                {
                    Dialog.DisplayDialog(DialogKind.MissingPackage);
                }
            }

            if (!isInstalling)
            {
                // No more packages to be installed
                OnComplete();
            }
        }

        private static void OnComplete()
        {
            if (PlayerPrefs.HasKey(k_PrefsKey_ImportingAssetPackage))
            {
                // Cleanup mutexes for next install
                PlayerPrefs.DeleteKey(k_PrefsKey_ImportingAssetPackage);
                PlayerPrefs.DeleteKey(k_PrefsKey_LastAssetPackageImport);

                if (k_RunningInBatchMode)
                {
                    Debug.LogFormat("Successfully imported the {0} asset package.", k_PackageName);
                }
            }

            if (k_RunningInBatchMode)
            {
                Debug.LogFormat("Deleting {0} package installer files...", k_PackageName);
            }

            foreach (var asset in k_Artifacts)
            {
                AssetDatabase.DeleteAsset(asset.AssetPath());
            }
            AssetDatabase.DeleteAsset(GetAssetPath(k_InstallerFile));
            AssetDatabase.DeleteAsset(GetAssetPath(k_ObsoleteFilesCSVFile));
            AssetDatabase.DeleteAsset(GetAssetPath(k_ObsoleteGUIDsCSVFile));

            AssetDatabase.Refresh();
            SaveAssets();

            if (k_RunningInBatchMode)
            {
                Debug.LogFormat("{0} asset package install complete.", k_PackageName);
                EditorApplication.Exit(0);
            }
        }

        private static bool EnablePurchasingService ()
        {
            if (GetPurchasing() != null)
            {
                // Service is enabled, early return
                return true;
            }

            if (!k_IsEditorSettingsSupported)
            {
                if (!Dialog.DisplayDialog(DialogKind.EnableServiceManually))
                {
                    Application.OpenURL(k_IAPHelpURL);
                }

                return false;
            }

            if (string.IsNullOrEmpty(PlayerSettings.cloudProjectId))
            {
                if (!Dialog.DisplayDialog(DialogKind.ProjectConfig))
                {
                    Application.OpenURL(k_ProjectHelpURL);
                }

                return false;
            }

            if (Dialog.DisplayDialog(DialogKind.EnableService))
            {
#if UNITY_5_5_OR_NEWER
                Analytics.AnalyticsSettings.enabled = true;
                PurchasingSettings.enabled = true;
#endif

                SaveAssets();
                return true;
            }

            if (!Dialog.DisplayDialog(DialogKind.CanceledEnableService))
            {
                Application.OpenURL(k_IAPHelpURL);
            }

            return false;
        }

        private static string GetAssetPath (string path)
        {
            return string.Concat("Assets/", path);
        }

        private static string GetAbsoluteFilePath (string path)
        {
            return Path.Combine(Application.dataPath, path.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string[] GetFromCSV (string filePath)
        {
            var lines = new List<string>();
            int row = 0;

            if (File.Exists(filePath))
            {
                try
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        while (!reader.EndOfStream)
                        {
                            string[] line = reader.ReadLine().Split(',');
                            lines.Add(line[0].Trim().Trim('"'));
                            row++;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            return lines.ToArray();
        }

        private static bool AssetExists (string path)
        {
            if (path.Length > 7)
                path = path.Substring(7);
            else return false;

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                path = path.Replace("/", @"\");
            }

            path = Path.Combine(Application.dataPath, path);

            return File.Exists(path) || Directory.Exists(path);
        }

        private static bool AssetsExist (string[] legacyAssetPaths, string[] legacyAssetGUIDs, out string[] existingAssetPaths)
        {
            var paths = new List<string>();

            for (int i = 0; i < legacyAssetPaths.Length; i++)
            {
                if (AssetExists(legacyAssetPaths[i]))
                {
                    paths.Add(legacyAssetPaths[i]);
                }
            }

            for (int i = 0; i < legacyAssetGUIDs.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(legacyAssetGUIDs[i]);

                if (AssetExists(path) && !paths.Contains(path))
                {
                    paths.Add(path);
                }
            }

            existingAssetPaths = paths.ToArray();

            return paths.Count > 0;
        }

        private static bool DeleteObsoleteAssets (string[] paths, string[] guids)
        {
            var assets = new string[0];

            if (!AssetsExist(paths, guids, out assets)) return true;

            if (Dialog.DisplayDialog(DialogKind.DeleteAssets))
            {
                for (int i = 0; i < assets.Length; i++)
                {
                    FileUtil.DeleteFileOrDirectory(assets[i]);
                }

                AssetDatabase.Refresh();
                SaveAssets();
                return true;
            }

            Dialog.DisplayDialog(DialogKind.CanceledDeleteAssets);
            return false;
        }

        /// <summary>
        /// Solves issues seen in projects when deleting other files in projects
        /// after installation but before project is closed and reopened.
        /// Script continue to live as compiled entities but are not stored in
        /// the AssetDatabase.
        /// </summary>
        private static void SaveAssets ()
        {
#if UNITY_5_5_OR_NEWER
            AssetDatabase.SaveAssets(); // Not reliable prior to major refactoring in Unity 5.5.
#else
            EditorApplication.SaveAssets(); // Reliable, but removed in Unity 5.5.
#endif
        }
    }
}
