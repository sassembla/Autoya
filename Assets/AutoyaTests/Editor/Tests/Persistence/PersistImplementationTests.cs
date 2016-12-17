using System;
using System.IO;
using AutoyaFramework;
using Miyamasu;
using UnityEngine;

/**
	test for file persist controll.
*/
public class PersistImplementationTests : MiyamasuTestRunner {
	private const string AutoyaFilePersistTestsFileDomain = "AutoyaFilePersistTestsFileDomain";
	private const string AutoyaFilePersistTestsFileName = "persist.txt";

	[MSetup] public void Setup () {
		var loginDone = false;

		var dataPath = string.Empty;
		RunOnMainThread(
			() => {
				Autoya.TestEntryPoint(dataPath);
				Autoya.Auth_SetOnLoginSucceeded(
					() => {
						loginDone = true;
					}
				);
			}
		);

		WaitUntil(() => loginDone, 5, "failed to log in.");
		Autoya.Persist_DeleteByDomain(AutoyaFilePersistTestsFileDomain);
	}
	
	
	[MTest] public void Update () {
		Action onMainThread = () => {
			var data = "new data " + Guid.NewGuid().ToString();
			
			var result = Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);
			Assert(result, "not successed.");
		};
		RunOnMainThread(onMainThread);
	}

	[MTest] public void Append () {
		Action onMainThread = () => {
			var data = "new data " + Guid.NewGuid().ToString();
			var result = Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);
			Assert(result, "not successed 1.");

			var appendData = "append data " + Guid.NewGuid().ToString();
			var appendResult = Autoya.Persist_Append(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, appendData);
			Assert(result, "not successed 1.");

			var loadedData = Autoya.Persist_Load(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
			Assert(loadedData == data + appendData, "data does not match. loadedData:" + loadedData);
		};
		RunOnMainThread(onMainThread);
	}
	
	[MTest] public void Load () {
		Action onMainThread = () => {
			var data = "new data " + Guid.NewGuid().ToString();

			var result = Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);
			Assert(result, "not successed.");
			
			var loadedData = Autoya.Persist_Load(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
			Assert(loadedData == data, "data does not match. loadedData:" + loadedData);
		};
		RunOnMainThread(onMainThread);
	}

	// [MTest] public void LoadNotExistFile () {
	// 	RefreshData();

	// 	var emptyData = Autoya.Persist_Load(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
	// 	Assert(string.IsNullOrEmpty(emptyData), "not successed.");

	// 	return true;
	// }

	// [MTest] public void Delete () {
	// 	RefreshData();

	// 	var data = "new data " + Guid.NewGuid().ToString();

	// 	var result = Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);
	// 	Assert(result, "not successed.");

	// 	var deleteResult = Autoya.Persist_Delete(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
	// 	Assert(deleteResult, "not successed.");

	// 	return true;
	// }

	// [MTest] public void DeleteByDomain () {
	// 	RefreshData();

	// 	var data = "new data " + Guid.NewGuid().ToString();

	// 	var result = Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);
	// 	Assert(result, "not successed.");

	// 	var deleteResult = Autoya.Persist_DeleteByDomain(AutoyaFilePersistTestsFileDomain);
	// 	Assert(deleteResult, "not successed.");

	// 	return true;
	// }
	
	// [MTest] public void EmptyDelete () {
	// 	RefreshData();

	// 	var deleteResult = Autoya.Persist_Delete(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
	// 	Assert(!deleteResult, "unintentional successed.");
		
	// 	return true;
	// }

}