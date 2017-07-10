using YamlDotNet.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using AutoyaFramework.AssetBundles;
using UnityEditor;

public class AssetBundleListMaker {
	// this list make feature expect that folder is exists on the top level of YOUR_PROJECT_FOLDER.
	const string PATH_ASSETBUNDLES_EXPORTED = "AssetBundles";// whole path is "YOUR_PROJECT_FOLDER/AssetBundles" by default.

	public string version;
	
	public BuildTarget targetOS;

	public bool shouldOverwrite;

	public AssetBundleListMaker () {
		version = "1.0.0";
		targetOS = EditorUserBuildSettings.activeBuildTarget;
		shouldOverwrite = false;
	}
	/*
{
	"ManifestFileVersion": "0",
	"CRC": "2462514955",
	"AssetBundleManifest": {
		"AssetBundleInfos": {
			"Info_0": {
				"Name": "bundlename",
				"Dependencies": {}
			},
			"Info_1": {
				"Name": "dependsbundlename",
				"Dependencies": {// うーん複数階層持つことがあり得るのか〜〜きっちーな。このレイヤーは紛れもなく辞書なんだ。
					"Dependency_0": "bundlename"
				}
			},
			"Info_2": {
				"Name": "dependsbundlename2",
				"Dependencies": {
					"Dependency_0": "bundlename"
				}
			},
			"Info_3": {
				"Name": "nestedprefab",
				"Dependencies": {
					"Dependency_0": "dependsbundlename"
				}
			},
			"Info_4": {
				"Name": "updatable",
				"Dependencies": {
					"Dependency_0": "bundlename"
				}
			}
		}
	}
}	
	
	 */
	
	
	public void MakeList () {
		var targetOSStr = targetOS.ToString();

		if (!Directory.Exists(PATH_ASSETBUNDLES_EXPORTED)) {
			Debug.LogError("no directory found:" + PATH_ASSETBUNDLES_EXPORTED);
			return;
		}

		var platformPath = FileController.PathCombine(PATH_ASSETBUNDLES_EXPORTED, targetOSStr);
		if (!Directory.Exists(platformPath)) {
			Debug.LogError("no platform folder found:" + platformPath);
			return;
		}

		var assumedListFilePath = FileController.PathCombine(platformPath, "AssetBundles." + targetOSStr + "_" + version.Replace(".", "_") + ".json");
		if (File.Exists(assumedListFilePath) && !shouldOverwrite) {
			Debug.LogError("same version file:" + assumedListFilePath + " is already exists.");
			return;
		}

		var bundleAndDependencies = new List<AssetBundleInfo>();
		
		/*
			load root manifest file and get assetBundle names and dependencies.
		*/
		
		using (var sr = new StreamReader(FileController.PathCombine(PATH_ASSETBUNDLES_EXPORTED, targetOSStr, targetOSStr + ".manifest"))) {
			var rootManifest = sr.ReadToEnd();
			var deserializer = new DeserializerBuilder().Build();
			
			var yamlObject = deserializer.Deserialize(new StringReader(rootManifest));
			// このへんを換えればいけそうな気がする。yamlObject自体から列挙とかがとれるはず。
			
			var serializer = new SerializerBuilder()
				.JsonCompatible()
				.Build();

			var json = serializer.Serialize(yamlObject);
			Debug.LogError("json:" + json);
			// var rootManifestHashTable = json.HashtableFromJson();

			// /*
			// 	C#でUnity JsonUtilityが読めるような型を作るには、manifestの型情報は汚すぎる。
			// 	よって、Unityに同梱されているMiniJsonを使って、manifestをyaml -> json -> objectへと変換する。
			// */
			// foreach (var has in rootManifestHashTable) {
			// 	if (has.Key == "AssetBundleManifest") {
			// 		var manifestVal = has.Value as Dictionary<string, object>;
			// 		foreach (var k in manifestVal) {
			// 			if (k.Key == "AssetBundleInfos") {
			// 				var infoDict = k.Value as Dictionary<string, object>;
			// 				foreach (var l in infoDict) {
			// 					var bundleInfo = new AssetBundleInfo();

			// 					/*
			// 						each assetBundle infos in root manifest are here.
			// 					*/
			// 					{
			// 						var bundleKv = l.Value as Dictionary<string, object>;
			// 						foreach (var m in bundleKv) {
			// 							var bundleKvKey = m.Key;
			// 							switch (bundleKvKey) {
			// 								case "Name": {
			// 									bundleInfo.bundleName = m.Value.ToString();
			// 									break;
			// 								}
			// 								case "Dependencies": {
			// 									var dependenciesDict = m.Value as Dictionary<string, object>;
			// 									var dependentBundleNames = dependenciesDict.Values.Select(t => t.ToString()).ToArray();
			// 									bundleInfo.dependsBundleNames = dependentBundleNames;
			// 									break;
			// 								}
			// 							}	
			// 						}
			// 					}
			// 					bundleAndDependencies.Add(bundleInfo);
			// 				}
			// 			}
			// 		}
			// 	}
			// }
		}

		var assetBundleInfos = new List<AssetBundleInfo>();

		/*
			load each assetBundle info from bundle manifests.
		*/
		foreach (var bundleAndDependencie in bundleAndDependencies) {
			var targetBundleName = bundleAndDependencie.bundleName;
			var newAssetBundleInfo = new AssetBundleInfo();
			newAssetBundleInfo.bundleName = targetBundleName;
			newAssetBundleInfo.dependsBundleNames = bundleAndDependencie.dependsBundleNames;
			
			using (var sr = new StreamReader(FileController.PathCombine(PATH_ASSETBUNDLES_EXPORTED, targetOSStr, targetBundleName + ".manifest"))) {
				var bundleManifest = sr.ReadToEnd();

				var deserializer = new DeserializerBuilder().Build();
				
				var yamlObject = deserializer.Deserialize(new StringReader(bundleManifest));
				var serializer = new SerializerBuilder().JsonCompatible().Build();

				var json = serializer.Serialize(yamlObject);
				
				Debug.LogError("json.HashtableFromJson がなくなったので封印中");
				// var bundleManifestHashTable = json.HashtableFromJson();
				// foreach (var k in bundleManifestHashTable) {
				// 	switch (k.Key) {
				// 		case "CRC": {
				// 			var crc = Convert.ToUInt32(k.Value);
				// 			newAssetBundleInfo.crc = crc;
				// 			break;
				// 		}
				// 		case "Assets": {
				// 			var assetNames = (k.Value as List<object>).Select(n => n.ToString()).ToArray();
				// 			newAssetBundleInfo.assetNames = assetNames;
				// 			break;
				// 		}
				// 		case "Hashes": {
				// 			var hashDict = k.Value as Dictionary<string, object>;
				// 			foreach (var hashItem in hashDict) {
				// 				if (hashItem.Key == "AssetFileHash") {
				// 					var assetFileHashDict = hashItem.Value as Dictionary<string, object>;
				// 					foreach (var assetFileHashItem in assetFileHashDict) {
				// 						if (assetFileHashItem.Key == "Hash") {
				// 							var hashStr = assetFileHashItem.Value.ToString();
				// 							newAssetBundleInfo.hash = hashStr;
				// 						}
				// 					}
				// 				}
				// 			}
				// 			break;
				// 		}
				// 	}
				// }
				
				// newAssetBundleInfo.size = new FileInfo(FileController.PathCombine(PATH_ASSETBUNDLES_EXPORTED, targetOSStr, targetBundleName)).Length;
				// Debug.LogError("newAssetBundleInfo.size:" + newAssetBundleInfo.size);
				// assetBundleInfos.Add(newAssetBundleInfo);
			}
		}
		
		var assetBundleList = new AssetBundleList(targetOSStr, version, assetBundleInfos.ToArray());
		var str = JsonUtility.ToJson(assetBundleList, true);

		var listExportPath = FileController.PathCombine(PATH_ASSETBUNDLES_EXPORTED, targetOSStr, "AssetBundles." + targetOSStr + "_" + version.Replace(".", "_") + ".json");
		/*
			write out to file.
				"AssetBundles/OS/AssetBundles.OS_v_e_r.json".
		*/
		using (var sw = new StreamWriter(listExportPath)) {
			sw.WriteLine(str);
		}
		Debug.Log("list exported at:" + listExportPath);
	}
}