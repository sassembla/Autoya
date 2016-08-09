using System;
using System.IO;
using AutoyaFramework;
using Miyamasu;
using UniRx;
using UnityEngine;

/**
	test for file persist controll.
*/
public class PersistImplementationTests : MiyamasuTestRunner {
	private const string AutoyaFilePersistTestsFileDomain = "AutoyaFilePersistTestsFileDomain";
	private const string AutoyaFilePersistTestsFileName = "persist.txt";

	private void RefreshData () {
		var basePath = Application.persistentDataPath;
		
		Autoya.TestEntryPoint(basePath);

		// cancelが効くようになるか、それとも、、っていう感じ
		// Autoya.Auth_CancelLogIn();
		
		var domainPath = Path.Combine(basePath, AutoyaFilePersistTestsFileDomain);
		if (Directory.Exists(domainPath)) {
			var filePath = Path.Combine(domainPath, AutoyaFilePersistTestsFileName);
			if (File.Exists(filePath)) File.Delete(filePath);
		}
	}
	
	[MTestOnMainThread] public bool Update () {
		RefreshData();

		var data = "new data " + Guid.NewGuid().ToString();
		
		var result = Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);
		Assert(result, "not successed.");
		
		return true;
	}


	[MTestOnMainThread] public bool Load () {
		RefreshData();

		var data = "new data " + Guid.NewGuid().ToString();

		var result = Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);
		Assert(result, "not successed.");
		
		var loadedData = Autoya.Persist_Load(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
		Assert(loadedData == data, "not match.");

		return true;
	}

	// [MTestOnMainThread] public bool LoadNotExistFile () {
	// 	RefreshData();

	// 	var emptyData = Autoya.Persist_Load(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
	// 	Assert(string.IsNullOrEmpty(emptyData), "not successed.");

	// 	return true;
	// }

	// [MTestOnMainThread] public bool Delete () {
	// 	RefreshData();

	// 	var data = "new data " + Guid.NewGuid().ToString();

	// 	var result = Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);
	// 	Assert(result, "not successed.");

	// 	var deleteResult = Autoya.Persist_Delete(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
	// 	Assert(deleteResult, "not successed.");

	// 	return true;
	// }

	// [MTestOnMainThread] public bool DeleteByDomain () {
	// 	RefreshData();

	// 	var data = "new data " + Guid.NewGuid().ToString();

	// 	var result = Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);
	// 	Assert(result, "not successed.");

	// 	var deleteResult = Autoya.Persist_DeleteByDomain(AutoyaFilePersistTestsFileDomain);
	// 	Assert(deleteResult, "not successed.");

	// 	return true;
	// }
	
	// [MTestOnMainThread] public bool EmptyDelete () {
	// 	RefreshData();

	// 	var deleteResult = Autoya.Persist_Delete(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
	// 	Assert(!deleteResult, "unintentional successed.");
		
	// 	return true;
	// }

}