using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Miyamasu {
	public class EditorOption {
		
		[MenuItem("Window/Miyamasu Test Runner/Update UnityPackage")] public static void UnityPackage () {
			var assetPaths = new List<string>();
			var dirPaths = Directory.GetDirectories("Assets/MiyamasuTestRunner");
			
			foreach (var dir in dirPaths) {
				var files = Directory.GetFiles(dir);
				foreach (var file in files) {
					assetPaths.Add(file);
				}
			}

			AssetDatabase.ExportPackage(assetPaths.ToArray(), "MiyamasuTestRunner.unitypackage", ExportPackageOptions.IncludeDependencies);
		}
	}
}