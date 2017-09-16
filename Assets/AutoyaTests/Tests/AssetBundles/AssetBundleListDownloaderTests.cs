using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using Miyamasu;
using UnityEngine;



/**
	tests for Autoya Download list of whole AssetBundles.
*/
public class AssetBundleListDownloaderTests : MiyamasuTestRunner {
	[MTest] public IEnumerator GetAssetBundleList () {
		var listPath = "localhost:8081//Mac/1.0.0/AssetBundles.StandaloneOSXIntel64_1_0_0.json";
		var listDownloader = new AssetBundleListDownloader();

		var done = false;
		yield return listDownloader.DownloadAssetBundleList(
			listPath, 
			list => {
				done = true;
			}, 
			(code, reason, autoyaStatus) => {
				Fail("failed to get list." + code + " reason:" + reason);
				// do nothing.
			}
		);
		
		yield return WaitUntil(
			() => done, () => {throw new TimeoutException("not yet get assetBundleList.");}
		);
	}

	[MTest] public IEnumerator GetAssetBundleListFailed () {
		var listPath = "localhost:8081//FAKEPATH";
		var loader = new AssetBundleListDownloader();

		var done = false;
		yield return loader.DownloadAssetBundleList(
			listPath, 
			list => {
				True(false, "should not be succeeded.");
			}, 
			(code, reason, autoyaStatus) => {
				True(code == 404, "error code does not match.");
				done = true;
			}
		);
		
		yield return WaitUntil(
			() => done, () => {throw new TimeoutException("not yet get assetBundleList.");}
		);
	}
}
