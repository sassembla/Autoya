using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using UnityEngine.AssetBundles.GraphTool;
using System.Linq;
using AutoyaFramework.AssetBundles;
using YamlDotNet.RepresentationModel;

/**
Example code for asset bundle build postprocess.
*/
public class AutoyaAssetBundleListGenerateProcess : IPostprocess
{
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

        // Debug.Log("sampleExport destination:" + sampleExportArray[0].ExportedItems[0].destination);
        // Debug.Log("currentTargetPlatform:" + EditorUserBuildSettings.activeBuildTarget + " export platform str is:" + BuildTargetUtility.TargetToAssetBundlePlatformName(EditorUserBuildSettings.activeBuildTarget));

        /*
			ここは大変まどろっこしいことをしていて、
			exporterからexportPathを取得できないので、exportされたAssetから"現在のプラットフォームString/現在のプラットフォームString" というパス/ファイル名になるmanifestファイルを探し出し、
			そのパスの直上のディレクトリ名がexportPathの最下位のフォルダ名になるので、それを取得しリスト名として使用している。
		 */
        var exportPlatformStr = BuildTargetUtility.TargetToAssetBundlePlatformName(EditorUserBuildSettings.activeBuildTarget);
        var platformDelimiter = exportPlatformStr + "/" + exportPlatformStr + ".manifest";// XPlatform/XPlatform.manifest

        var rootManifestEntry = sampleExportArray[0].ExportedItems.Where(p => p.destination.Contains(platformDelimiter)).FirstOrDefault();
        if (rootManifestEntry == null)
        {
            Debug.Log("no exported root manifest with :" + platformDelimiter + " found.");
            return;
        }

        var wholeExportFolderName = rootManifestEntry.destination.Substring(0, rootManifestEntry.destination.IndexOf(platformDelimiter) - 1/*remove last / */);

        var settingFilePath = wholeExportFolderName + ".json";

        if (!File.Exists(settingFilePath))
        {
            Debug.Log("no setting file exists:" + settingFilePath);
            return;
        }


        var settingFileData = File.ReadAllBytes(settingFilePath);
        if (settingFileData.Length == 0)
        {
            Debug.Log("setting file is empty:" + settingFilePath + " need to define ListProfile class parameters.");
            return;
        }

        var settingProfile = JsonUtility.FromJson<ListProfile>(Encoding.UTF8.GetString(settingFileData));

        var listIdentity = settingProfile.identity;
        var listVersion = settingProfile.version;

        var rootManifestPath = rootManifestEntry.destination;
        Debug.Log("generating AssetBundleList. rootManifestPath:" + rootManifestPath + " generate list identity:" + listIdentity + " version:" + listVersion);

        var targetDirectory = FileController.PathCombine(wholeExportFolderName, exportPlatformStr, listVersion);

        // check if version folder is exists.
        if (Directory.Exists(targetDirectory))
        {
            Debug.Log("same version files are already exists. list identity:" + listIdentity + " version:" + listVersion + " path:" + targetDirectory + " need to delete directory.");
            return;
            // Directory.Delete(targetDirectory, true);
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

            /*
				load each assetBundle info from bundle manifests.
			*/
            foreach (var bundleAndDependencie in bundleAndDependencies)
            {
                var targetBundleName = bundleAndDependencie.bundleName;

                var newAssetBundleInfo = new AssetBundleInfo();
                newAssetBundleInfo.bundleName = targetBundleName;
                newAssetBundleInfo.dependsBundleNames = bundleAndDependencie.dependsBundleNames;

                using (var sr = new StreamReader(FileController.PathCombine(wholeExportFolderName, exportPlatformStr, targetBundleName + ".manifest")))
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
                        }
                    }

                    // set size.
                    newAssetBundleInfo.size = new FileInfo(FileController.PathCombine(wholeExportFolderName, exportPlatformStr, targetBundleName)).Length;

                    // Debug.LogError("newAssetBundleInfo.size:" + newAssetBundleInfo.size);

                    assetBundleInfos.Add(newAssetBundleInfo);
                }
            }

            var assetBundleList = new AssetBundleList(listIdentity, exportPlatformStr, listVersion, assetBundleInfos.ToArray());
            var str = JsonUtility.ToJson(assetBundleList, true);


            var listOutputPaht = FileController.PathCombine(wholeExportFolderName, exportPlatformStr, listVersion, listIdentity + ".json");
            using (var sw = new StreamWriter(listOutputPaht))
            {
                sw.WriteLine(str);
            }
        }
    }
}

[Serializable]
public class ListProfile
{
    [SerializeField] public string identity;
    [SerializeField] public string version;
}
