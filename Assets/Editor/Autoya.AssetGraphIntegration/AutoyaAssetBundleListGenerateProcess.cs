using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using UnityEngine.AssetGraph;
using System.Linq;
using AutoyaFramework.AssetBundles;
using YamlDotNet.RepresentationModel;

/**
Example code for asset bundle build postprocess.
*/
public class AutoyaAssetBundleListGenerateProcess : IPostprocess
{

    [MenuItem("Window/Autoya/Open AssetBundleVersionEditor")]
    public static void EditVersionJsonForEdit()
    {
        EditVersionJson(string.Empty);
    }

    private static string EditVersionJson(string graphName)
    {
        // popupを開く。
        // ファイルが存在しない場合、ファイルを作成
        // ファイルはAssetBundleListの作成に使用される。
        // Autoyaが入っているフォルダを見つける必要がある。
        var autoyaAssetGraphIntegrationInstalledPath = GetTargetFolderPath("Autoya.AssetGraphIntegration", "Assets");
        if (string.IsNullOrEmpty(autoyaAssetGraphIntegrationInstalledPath))
        {
            Debug.LogError("Autoya.AssetGraphIntegration folder not found.");
            return string.Empty;
        }

        var targetPath = Path.Combine(autoyaAssetGraphIntegrationInstalledPath, "ListVersionSettings");

        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }

        var settingFilePaths = Directory.GetFiles(targetPath).ToList();


        var generatedFilePath = string.Empty;

        // graph名の指定がある場合、内部からの呼び出しなので、ファイルがなければ作成する。
        if (!string.IsNullOrEmpty(graphName))
        {
            var settingFileNames = settingFilePaths.Select(s => Path.GetFileNameWithoutExtension(s));
            if (settingFileNames.Contains(graphName))
            {
                var filePath = Path.Combine(targetPath, graphName + ".json");
                return filePath;
            }
            else
            {
                var targetListVersionSettingPath = Path.Combine(targetPath, graphName + ".json");

                var defaultVersionJson = new ListProfile();
                defaultVersionJson.identity = graphName;
                defaultVersionJson.version = "0";
                var jsonStr = JsonUtility.ToJson(defaultVersionJson);

                using (var sw = new StreamWriter(targetListVersionSettingPath, false))
                {
                    sw.WriteLine(jsonStr);
                }
                AssetDatabase.Refresh();

                return targetListVersionSettingPath;
            }
        }

        var saveActions = new List<VersionSettingAndSaveAction>();
        var allFilePaths = Directory.GetFiles(targetPath).Where(p => !p.EndsWith(".meta")).ToArray();
        foreach (var filePath in allFilePaths)
        {
            var contents = string.Empty;
            using (var sr = new StreamReader(filePath))
            {
                contents = sr.ReadToEnd();
            }

            var listVersinSetting = JsonUtility.FromJson<ListProfile>(contents);

            Action<string> saveAct = newVersion =>
            {
                if (newVersion != listVersinSetting.version)
                {
                    listVersinSetting.version = newVersion;
                    var jsonStr = JsonUtility.ToJson(listVersinSetting);
                    using (var sw = new StreamWriter(filePath, false))
                    {
                        sw.WriteLine(jsonStr);
                    }

                    AssetDatabase.Refresh();
                }
            };

            var isNew = filePath == generatedFilePath;
            var versionSettingAndSaveAction = new VersionSettingAndSaveAction(listVersinSetting.identity, listVersinSetting.version, saveAct);
            saveActions.Add(versionSettingAndSaveAction);
        }

        // ポップアップを開く
        var window = EditorWindow.GetWindow<AssetBundleListVersionSettingsEditWindow>(typeof(AssetBundleListVersionSettingsEditWindow));
        window.Init(saveActions);
        window.titleContent = new GUIContent("ABListVer Edit");

        return string.Empty;
    }

    public struct VersionSettingAndSaveAction
    {
        public readonly string listIdentity;
        public readonly string listVersion;
        public Action<string> saveAction;

        public VersionSettingAndSaveAction(string listIdentity, string listVersion, Action<string> saveAction)
        {
            this.listIdentity = listIdentity;
            this.listVersion = listVersion;
            this.saveAction = saveAction;
        }
    }

    public class AssetBundleListVersionSettingsEditWindow : EditorWindow
    {
        private List<VersionSettingAndSaveAction> data;
        private string[] defaultVersions;
        public void Init(List<VersionSettingAndSaveAction> data)
        {
            this.data = data;
            this.defaultVersions = data.Select(d => d.listVersion).ToArray();
        }


        void OnGUI()
        {
            GUILayout.Label("Set ListVersion and update if need.");

            GUILayout.Space(5);
            try
            {
                for (var i = 0; i < data.Count; i++)
                {
                    var versionSettingAndAct = data[i];
                    using (new GUILayout.VerticalScope())
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("identity:");
                            GUILayout.Label(versionSettingAndAct.listIdentity);
                            GUILayout.Label("version:");

                            var updated = GUILayout.TextField(defaultVersions[i]);
                            defaultVersions[i] = updated;

                            if (GUILayout.Button("Update"))
                            {
                                versionSettingAndAct.saveAction(defaultVersions[i]);
                            }
                        }
                        GUILayout.Space(5);
                    }
                }

                GUILayout.Space(10);

                if (GUILayout.Button("Close"))
                {
                    this.Close();
                }
            }
            catch
            {
                GameObject.DestroyImmediate(this);
            }
        }

    }

    private static string GetTargetFolderPath(string targetFolderName, string findStartPath)
    {
        var childDirectoryPaths = Directory.GetDirectories(findStartPath);

        return Recursive(targetFolderName, childDirectoryPaths);
    }

    private static string Recursive(string targetFolderName, string[] paths)
    {
        foreach (var dirPath in paths)
        {
            // チェックを行う
            if (dirPath.Contains(targetFolderName))
            {
                return dirPath;
            }

            // まだパスに欲しいフォルダ名が含まれていない。

            // 下の階層があるかチェック
            var child2 = Directory.GetDirectories(dirPath);
            if (child2.Length == 0)
            {
                continue;
            }

            // 下の階層があるので、読み込み
            var result = Recursive(targetFolderName, child2);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
        }

        return string.Empty;
    }

    /* 
     * DoPostprocess() is called when build performed.
     * @param [in] reports	collection of AssetBundleBuildReport from each BundleBuilders.
     */
    public void DoPostprocess(IEnumerable<AssetBundleBuildReport> buildReports, IEnumerable<ExportReport> exportReports)
    {
        // リスト名とversionから、出力するlistの名称を決め、ファイルを移動させる。
        // なんらか対象の設定の把握ができるといいんだけど、exportPathからとるか。

        var sampleExportArray = exportReports.ToArray();
        if (sampleExportArray == null || sampleExportArray.Length == 0)
        {
            // no exports found.
            return;
        }

        // pick first exporter only.
        if (!sampleExportArray[0].ExportedItems.Any())
        {
            // empty exports.
            return;
        }

        var exportPlatformStr = BuildTargetUtility.TargetToAssetBundlePlatformName(EditorUserBuildSettings.activeBuildTarget);

        var platformDelimiter = exportPlatformStr + "/" + exportPlatformStr + ".manifest";// XPlatform/XPlatform.manifest

        var rootManifestEntry = sampleExportArray[0].ExportedItems.Where(p => p.destination.Contains(platformDelimiter)).FirstOrDefault();
        if (rootManifestEntry == null)
        {
            Debug.Log("no exported root manifest with :" + platformDelimiter + " found.");
            return;
        }

        // full path for export base path.
        var wholeExportFolderPath = rootManifestEntry.destination.Substring(0, rootManifestEntry.destination.IndexOf(platformDelimiter) - 1/*remove last / */);

        var graphName = new DirectoryInfo(wholeExportFolderPath).Name;
        var settingFilePath = EditVersionJson(graphName);

        var settingFileData = string.Empty;
        using (var sr = new StreamReader(settingFilePath))
        {
            settingFileData = sr.ReadToEnd();
        }

        var settingProfile = JsonUtility.FromJson<ListProfile>(settingFileData);

        var listIdentity = settingProfile.identity;
        var listVersion = settingProfile.version;

        var rootManifestPath = rootManifestEntry.destination;
        Debug.Log("generating AssetBundleList. rootManifestPath:" + rootManifestPath + " generate list identity:" + listIdentity + " version:" + listVersion);

        var targetDirectory = FileController.PathCombine(wholeExportFolderPath, exportPlatformStr, listVersion);

        // check if version folder is exists.
        if (Directory.Exists(targetDirectory))
        {
            Debug.Log("same version files are already exists. list identity:" + listIdentity + " version:" + listVersion + " path:" + targetDirectory + " need to delete directory or modify list version. for editing list version, open Window > Autoya > Open AssetBundleListVersionEditor. ");
            return;
        }

        // create version directory under exportPlatformStr.
        // then copy necessary files.
        {
            Directory.CreateDirectory(targetDirectory);

            foreach (var exportReport in exportReports)
            {
                var items = exportReport.ExportedItems;
                foreach (var item in items)
                {
                    var currentPath = item.destination;

                    // skip root manifest and root file.
                    if (currentPath.Contains(exportPlatformStr + "/" + exportPlatformStr))
                    {
                        continue;
                    }

                    // skip manifest file.
                    if (currentPath.EndsWith(".manifest"))
                    {
                        continue;
                    }

                    var fileName = Path.GetFileName(currentPath);
                    if (fileName == listIdentity + ".json")
                    {
                        throw new Exception("generated AssetBundle name:" + listIdentity + ".json is overlapped with list name. please change assetBundle name, extension or list identity.");
                    }

                    var dirPath = Path.GetDirectoryName(currentPath);
                    var destPath = FileController.PathCombine(dirPath, listVersion, fileName);
                    File.Copy(currentPath, destPath);
                }
            }
        }

        // root manifest から全てのbundleの依存関係を取り出す + 各bundleのManifestから詳細を取得する。
        {
            var bundleAndDependencies = new List<AssetBundleInfo>();

            /*
                load root manifest file and get assetBundle names and dependencies.
            */

            using (var sr = new StreamReader(rootManifestPath))
            {
                // read root manifest file.
                {
                    var rootYaml = new YamlStream();
                    rootYaml.Load(sr);

                    var rootMapping = (YamlMappingNode)rootYaml.Documents[0].RootNode;
                    foreach (var root_item in rootMapping)
                    {
                        var rootKey = ((YamlScalarNode)root_item.Key).Value;
                        switch (rootKey)
                        {
                            case "ManifestFileVersion":
                                {
                                    // Debug.LogError("ManifestFileVersion:" + ((YamlScalarNode)root_item.Value).Value);
                                    break;
                                }
                            case "AssetBundleManifest":
                                {
                                    var assetBundleManifestMapping = (YamlMappingNode)root_item.Value;
                                    foreach (var assetBundleManifestMapping_item in assetBundleManifestMapping)
                                    {
                                        var manifestKey = ((YamlScalarNode)assetBundleManifestMapping_item.Key).Value;
                                        switch (manifestKey)
                                        {
                                            case "AssetBundleInfos":
                                                {

                                                    var manifestInfoSeq = (YamlMappingNode)assetBundleManifestMapping_item.Value;
                                                    foreach (var manifestInfo_item in manifestInfoSeq)
                                                    {

                                                        var bundleInfo = new AssetBundleInfo();


                                                        var bundleInfoMapping = (YamlMappingNode)manifestInfo_item.Value;
                                                        foreach (var info_item in bundleInfoMapping)
                                                        {
                                                            var infoKey = ((YamlScalarNode)info_item.Key).Value;
                                                            switch (infoKey)
                                                            {
                                                                case "Name":
                                                                    {
                                                                        var name = ((YamlScalarNode)info_item.Value).Value;
                                                                        // Debug.LogError("name:" + name);

                                                                        bundleInfo.bundleName = name;
                                                                        break;
                                                                    }
                                                                case "Dependencies":
                                                                    {
                                                                        var dependenciesMapping = (YamlMappingNode)info_item.Value;
                                                                        foreach (var dependency_item in dependenciesMapping)
                                                                        {
                                                                            var dependentBundleName = ((YamlScalarNode)dependency_item.Value).Value;
                                                                            // Debug.LogError("dependentBundleName:" + dependentBundleName);
                                                                        }

                                                                        var dependentBundleNames = dependenciesMapping.Select(t => ((YamlScalarNode)t.Value).Value).ToArray();
                                                                        bundleInfo.dependsBundleNames = dependentBundleNames;
                                                                        break;
                                                                    }
                                                            }
                                                        }


                                                        bundleAndDependencies.Add(bundleInfo);
                                                    }

                                                    break;
                                                }
                                        }

                                    }
                                    break;
                                }
                        }
                    }
                }
            }


            // create assetBundleList.

            var assetBundleInfos = new List<AssetBundleInfo>();
            var classIdSet = new HashSet<int>();

            /*
                load each assetBundle info from bundle manifests.
            */
            foreach (var bundleAndDependencie in bundleAndDependencies)
            {
                var targetBundleName = bundleAndDependencie.bundleName;

                var newAssetBundleInfo = new AssetBundleInfo();
                newAssetBundleInfo.bundleName = targetBundleName;
                newAssetBundleInfo.dependsBundleNames = bundleAndDependencie.dependsBundleNames;

                using (var sr = new StreamReader(FileController.PathCombine(wholeExportFolderPath, exportPlatformStr, targetBundleName + ".manifest")))
                {
                    var rootYaml = new YamlStream();
                    rootYaml.Load(sr);

                    var rootMapping = (YamlMappingNode)rootYaml.Documents[0].RootNode;
                    foreach (var root_item in rootMapping)
                    {
                        var rootKey = ((YamlScalarNode)root_item.Key).Value;
                        switch (rootKey)
                        {
                            case "CRC":
                                {
                                    var crc = Convert.ToUInt32(((YamlScalarNode)root_item.Value).Value);
                                    // Debug.LogError("crc:" + crc);

                                    newAssetBundleInfo.crc = crc;
                                    break;
                                }
                            case "Assets":
                                {
                                    var assetNamesSeq = (YamlSequenceNode)root_item.Value;
                                    var assetNames = assetNamesSeq.Select(n => ((YamlScalarNode)n).Value).ToArray();

                                    // foreach (var assetName in assetNames) {
                                    // 	Debug.LogError("assetName:" + assetName);
                                    // }

                                    newAssetBundleInfo.assetNames = assetNames;
                                    break;
                                }
                            case "Hashes":
                                {
                                    var hashMapping = (YamlMappingNode)root_item.Value;
                                    foreach (var hash_item in hashMapping)
                                    {
                                        var hashKey = ((YamlScalarNode)hash_item.Key).Value;
                                        switch (hashKey)
                                        {
                                            case "AssetFileHash":
                                                {
                                                    var assetHashMapping = (YamlMappingNode)hash_item.Value;
                                                    foreach (var assetHash_item in assetHashMapping)
                                                    {
                                                        var assetHashKey = ((YamlScalarNode)assetHash_item.Key).Value;
                                                        switch (assetHashKey)
                                                        {
                                                            case "Hash":
                                                                {
                                                                    var hashStr = ((YamlScalarNode)assetHash_item.Value).Value;

                                                                    // Debug.LogError("hashStr:" + hashStr);

                                                                    newAssetBundleInfo.hash = hashStr;
                                                                    break;
                                                                }
                                                        }
                                                    }

                                                    break;
                                                }
                                        }
                                    }
                                    break;
                                }
                            case "ClassTypes":
                                {
                                    var seq = (YamlSequenceNode)root_item.Value;
                                    foreach (var iSeq in seq)
                                    {
                                        var innerMap = (YamlMappingNode)iSeq;
                                        foreach (var map in innerMap)
                                        {
                                            switch ((string)map.Key)
                                            {
                                                case "Class":
                                                    {
                                                        classIdSet.Add(Convert.ToInt32((string)map.Value));
                                                        break;
                                                    }
                                            }
                                        }
                                    }
                                    break;
                                }
                            default:
                                {
                                    // ignore.
                                    break;
                                }
                        }
                    }

                    // set size.
                    newAssetBundleInfo.size = new FileInfo(FileController.PathCombine(wholeExportFolderPath, exportPlatformStr, targetBundleName)).Length;

                    // Debug.LogError("newAssetBundleInfo.size:" + newAssetBundleInfo.size);

                    assetBundleInfos.Add(newAssetBundleInfo);
                }
            }

            var assetBundleList = new AssetBundleList(listIdentity, exportPlatformStr, listVersion, assetBundleInfos.ToArray());
            var str = JsonUtility.ToJson(assetBundleList, true);


            var listOutputPath = FileController.PathCombine(wholeExportFolderPath, exportPlatformStr, listVersion, listIdentity + ".json");
            using (var sw = new StreamWriter(listOutputPath))
            {
                sw.WriteLine(str);
            }

            // generate Link.xml
            LinkXMLGenerator.ExportLinkXMLWithUsingClassIds(Application.dataPath, classIdSet.ToArray());
        }
    }
}

[Serializable]
public class ListProfile
{
    [SerializeField] public string identity;
    [SerializeField] public string version;
}
