// using YamlDotNet.RepresentationModel;
// using YamlDotNet.Serialization;
// using YamlDotNet.Serialization.NamingConventions;
// using System;
// using System.Collections.Generic;
// using System.IO;
// using UnityEngine;
// using System.Linq;
// using AutoyaFramework.AssetBundles;
// using UnityEditor;
// using System.Text;

// public class AssetBundleListMaker
// {
//     // this list make feature expect that folder is exists on the top level of YOUR_PROJECT_FOLDER.
//     const string PATH_ASSETBUNDLES_EXPORTED = "AssetBundles";// whole path is "YOUR_PROJECT_FOLDER/AssetBundles" by default.

//     public string version;

//     public BuildTarget targetOS;

//     public bool shouldOverwrite;

//     public AssetBundleListMaker()
//     {
//         version = "1.0.0";
//         targetOS = EditorUserBuildSettings.activeBuildTarget;
//         shouldOverwrite = false;
//     }
//     /*
// {
// 	"ManifestFileVersion": "0",
// 	"CRC": "2462514955",
// 	"AssetBundleManifest": {
// 		"AssetBundleInfos": {
// 			"Info_0": {
// 				"Name": "bundlename",
// 				"Dependencies": {}
// 			},
// 			"Info_1": {
// 				"Name": "dependsbundlename",
// 				"Dependencies": {// うーん複数階層持つことがあり得るのか〜〜きっちーな。このレイヤーは紛れもなく辞書なんだ。
// 					"Dependency_0": "bundlename"
// 				}
// 			},
// 			"Info_2": {
// 				"Name": "dependsbundlename2",
// 				"Dependencies": {
// 					"Dependency_0": "bundlename"
// 				}
// 			},
// 			"Info_3": {
// 				"Name": "nestedprefab",
// 				"Dependencies": {
// 					"Dependency_0": "dependsbundlename"
// 				}
// 			},
// 			"Info_4": {
// 				"Name": "updatable",
// 				"Dependencies": {
// 					"Dependency_0": "bundlename"
// 				}
// 			}
// 		}
// 	}
// }	

// 	 */


//     public void MakeList()
//     {
//         var targetOSStr = targetOS.ToString();

//         if (!Directory.Exists(PATH_ASSETBUNDLES_EXPORTED))
//         {
//             Debug.LogError("no directory found:" + PATH_ASSETBUNDLES_EXPORTED);
//             return;
//         }

//         var platformPath = FileController.PathCombine(PATH_ASSETBUNDLES_EXPORTED, targetOSStr);
//         if (!Directory.Exists(platformPath))
//         {
//             Debug.LogError("no platform folder found:" + platformPath);
//             return;
//         }

//         var assumedListFilePath = FileController.PathCombine(platformPath, "AssetBundles." + targetOSStr + "_" + version.Replace(".", "_") + ".json");
//         if (File.Exists(assumedListFilePath) && !shouldOverwrite)
//         {
//             Debug.LogError("same version file:" + assumedListFilePath + " is already exists.");
//             return;
//         }

//         var bundleAndDependencies = new List<AssetBundleInfo>();

//         /*
// 			load root manifest file and get assetBundle names and dependencies.
// 		*/

//         using (var sr = new StreamReader(FileController.PathCombine(PATH_ASSETBUNDLES_EXPORTED, targetOSStr, targetOSStr + ".manifest")))
//         {
//             // read root manifest file.
//             {
//                 var rootYaml = new YamlStream();
//                 rootYaml.Load(sr);

//                 var rootMapping = (YamlMappingNode)rootYaml.Documents[0].RootNode;
//                 foreach (var root_item in rootMapping)
//                 {
//                     var rootKey = ((YamlScalarNode)root_item.Key).Value;
//                     switch (rootKey)
//                     {
//                         case "ManifestFileVersion":
//                             {
//                                 // Debug.LogError("ManifestFileVersion:" + ((YamlScalarNode)root_item.Value).Value);
//                                 break;
//                             }
//                         case "AssetBundleManifest":
//                             {
//                                 var assetBundleManifestMapping = (YamlMappingNode)root_item.Value;
//                                 foreach (var assetBundleManifestMapping_item in assetBundleManifestMapping)
//                                 {
//                                     var manifestKey = ((YamlScalarNode)assetBundleManifestMapping_item.Key).Value;
//                                     switch (manifestKey)
//                                     {
//                                         case "AssetBundleInfos":
//                                             {

//                                                 var manifestInfoSeq = (YamlMappingNode)assetBundleManifestMapping_item.Value;
//                                                 foreach (var manifestInfo_item in manifestInfoSeq)
//                                                 {

//                                                     var bundleInfo = new AssetBundleInfo();


//                                                     var bundleInfoMapping = (YamlMappingNode)manifestInfo_item.Value;
//                                                     foreach (var info_item in bundleInfoMapping)
//                                                     {
//                                                         var infoKey = ((YamlScalarNode)info_item.Key).Value;
//                                                         switch (infoKey)
//                                                         {
//                                                             case "Name":
//                                                                 {
//                                                                     var name = ((YamlScalarNode)info_item.Value).Value;
//                                                                     // Debug.LogError("name:" + name);

//                                                                     bundleInfo.bundleName = name;
//                                                                     break;
//                                                                 }
//                                                             case "Dependencies":
//                                                                 {
//                                                                     var dependenciesMapping = (YamlMappingNode)info_item.Value;
//                                                                     foreach (var dependency_item in dependenciesMapping)
//                                                                     {
//                                                                         var dependentBundleName = ((YamlScalarNode)dependency_item.Value).Value;
//                                                                         // Debug.LogError("dependentBundleName:" + dependentBundleName);
//                                                                     }

//                                                                     var dependentBundleNames = dependenciesMapping.Select(t => ((YamlScalarNode)t.Value).Value).ToArray();
//                                                                     bundleInfo.dependsBundleNames = dependentBundleNames;
//                                                                     break;
//                                                                 }
//                                                         }
//                                                     }


//                                                     bundleAndDependencies.Add(bundleInfo);
//                                                 }

//                                                 break;
//                                             }
//                                     }

//                                 }
//                                 break;
//                             }
//                     }
//                 }
//             }
//         }

//         var assetBundleInfos = new List<AssetBundleInfo>();

//         /*
// 			load each assetBundle info from bundle manifests.
// 		*/
//         foreach (var bundleAndDependencie in bundleAndDependencies)
//         {
//             var targetBundleName = bundleAndDependencie.bundleName;

//             var newAssetBundleInfo = new AssetBundleInfo();
//             newAssetBundleInfo.bundleName = targetBundleName;
//             newAssetBundleInfo.dependsBundleNames = bundleAndDependencie.dependsBundleNames;

//             using (var sr = new StreamReader(FileController.PathCombine(PATH_ASSETBUNDLES_EXPORTED, targetOSStr, targetBundleName + ".manifest")))
//             {
//                 var rootYaml = new YamlStream();
//                 rootYaml.Load(sr);

//                 var rootMapping = (YamlMappingNode)rootYaml.Documents[0].RootNode;
//                 foreach (var root_item in rootMapping)
//                 {
//                     var rootKey = ((YamlScalarNode)root_item.Key).Value;
//                     switch (rootKey)
//                     {
//                         case "CRC":
//                             {
//                                 var crc = Convert.ToUInt32(((YamlScalarNode)root_item.Value).Value);
//                                 // Debug.LogError("crc:" + crc);

//                                 newAssetBundleInfo.crc = crc;
//                                 break;
//                             }
//                         case "Assets":
//                             {
//                                 var assetNamesSeq = (YamlSequenceNode)root_item.Value;
//                                 var assetNames = assetNamesSeq.Select(n => ((YamlScalarNode)n).Value).ToArray();

//                                 // foreach (var assetName in assetNames) {
//                                 // 	Debug.LogError("assetName:" + assetName);
//                                 // }

//                                 newAssetBundleInfo.assetNames = assetNames;
//                                 break;
//                             }
//                         case "Hashes":
//                             {
//                                 var hashMapping = (YamlMappingNode)root_item.Value;
//                                 foreach (var hash_item in hashMapping)
//                                 {
//                                     var hashKey = ((YamlScalarNode)hash_item.Key).Value;
//                                     switch (hashKey)
//                                     {
//                                         case "AssetFileHash":
//                                             {
//                                                 var assetHashMapping = (YamlMappingNode)hash_item.Value;
//                                                 foreach (var assetHash_item in assetHashMapping)
//                                                 {
//                                                     var assetHashKey = ((YamlScalarNode)assetHash_item.Key).Value;
//                                                     switch (assetHashKey)
//                                                     {
//                                                         case "Hash":
//                                                             {
//                                                                 var hashStr = ((YamlScalarNode)assetHash_item.Value).Value;

//                                                                 // Debug.LogError("hashStr:" + hashStr);

//                                                                 newAssetBundleInfo.hash = hashStr;
//                                                                 break;
//                                                             }
//                                                     }
//                                                 }

//                                                 break;
//                                             }
//                                     }
//                                 }
//                                 break;
//                             }
//                     }
//                 }

//                 // set size.
//                 newAssetBundleInfo.size = new FileInfo(FileController.PathCombine(PATH_ASSETBUNDLES_EXPORTED, targetOSStr, targetBundleName)).Length;

//                 // Debug.LogError("newAssetBundleInfo.size:" + newAssetBundleInfo.size);

//                 assetBundleInfos.Add(newAssetBundleInfo);
//             }
//         }

//         var assetBundleList = new AssetBundleList(identity, targetOSStr, version, assetBundleInfos.ToArray());
//         var str = JsonUtility.ToJson(assetBundleList, true);

//         var listExportPath = FileController.PathCombine(PATH_ASSETBUNDLES_EXPORTED, targetOSStr, "AssetBundles." + targetOSStr + "_" + version.Replace(".", "_") + ".json");
//         /*
// 			write out to file.
// 				"AssetBundles/OS/AssetBundles.OS_v_e_r.json".
// 		*/
//         using (var sw = new StreamWriter(listExportPath))
//         {
//             sw.WriteLine(str);
//         }
//         Debug.Log("list exported at:" + listExportPath);
//     }
// }