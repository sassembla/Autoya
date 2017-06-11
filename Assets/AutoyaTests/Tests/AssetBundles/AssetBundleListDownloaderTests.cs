using System;
using System.Collections.Generic;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using Miyamasu;
using UnityEngine;



/**
	tests for Autoya Download list of whole AssetBundles.
*/
public class AssetBundleListDownloaderTests : MiyamasuTestRunner {
	[MTest] public void GetAssetBundleList () {
		var listPath = "https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/Mac/1.0.0/AssetBundles.StandaloneOSXIntel64_1_0_0.json";
		var listDownloader = new AssetBundleListDownloader();

		var done = false;
		var cor = listDownloader.DownloadAssetBundleList(
			listPath, 
			list => {
				done = true;
			}, 
			(code, reason, autoyaStatus) => {
				// do nothing.
			}
		);
		RunEnumeratorOnMainThread(
			cor
		);

		WaitUntil(
			() => done, 5, "not yet get assetBundleList."
		);
	}

	[MTest] public void GetAssetBundleListFailed () {
		var listPath = "https://dl.dropboxusercontent.com/u/36583594/outsource/Autoya/AssetBundle/FAKEPATH";
		var loader = new AssetBundleListDownloader();

		var done = false;
		var cor = loader.DownloadAssetBundleList(
			listPath, 
			list => {
				Assert(false, "should not be succeeded.");
			}, 
			(code, reason, autoyaStatus) => {
				Assert(code == 404, "error code does not match.");
				done = true;
			}
		);

		RunEnumeratorOnMainThread(
			cor
		);

		WaitUntil(
			() => done, 5, "not yet get assetBundleList."
		);
	}
}
