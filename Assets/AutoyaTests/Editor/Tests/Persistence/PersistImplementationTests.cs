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

	[MSetup] public void RefreshData () {
		Debug.LogError("setup C.");
		var dataPath = string.Empty;
		RunOnMainThread(
			() => {
				Autoya.TestEntryPoint(dataPath);
			}
		);
	}

	
	[MTeardown] public void Teardown () {
		Debug.LogError("teardown C.");
		RunOnMainThread(
			() => {
				var obj = GameObject.Find("MainThreadDispatcher");
				if (obj != null) GameObject.DestroyImmediate(obj); 
			}
		);
	}
	
	[MTest] public void Update () {
		var data = "new data " + Guid.NewGuid().ToString();
		
		var result = Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);
		Assert(result, "not successed.");
	}


	[MTest] public void Load () {
		Autoya.Persist_Delete(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);

		var data = "new data " + Guid.NewGuid().ToString();

		var result = Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);
		Assert(result, "not successed.");
		
		var loadedData = Autoya.Persist_Load(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
		Assert(loadedData == data, "data does not match. loadedData:" + loadedData);
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