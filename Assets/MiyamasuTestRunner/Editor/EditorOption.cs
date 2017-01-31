using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Miyamasu {

	public class EditorOption {
		
		[MenuItem("Window/Miyamasu Test Runner/Run On Compiled", false, 0)] public static void RunOnCompiled () {
			var menuPath = "Window/Miyamasu Test Runner/Run On Compiled";
			var settings = Settings.LoadSettings();
			
			settings.runOnCompiled = !settings.runOnCompiled;
			Settings.WriteSettings(settings);
			
			Menu.SetChecked(menuPath, settings.runOnCompiled);
		}

		[MenuItem("Window/Miyamasu Test Runner/Run On Play", false, 1)] public static void RunOnPlay () {
			var menuPath = "Window/Miyamasu Test Runner/Run On Play";
			var settings = Settings.LoadSettings();

			settings.runOnPlay = !settings.runOnPlay;
			Settings.WriteSettings(settings);

			Menu.SetChecked(menuPath, settings.runOnPlay);
		}

		[MenuItem("Window/Miyamasu Test Runner/Update UnityPackage", false, 21)] public static void UnityPackage () {
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