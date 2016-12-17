using System;
using System.IO;
using AutoyaFramework;
using Miyamasu;

/**
	test for file persist controll.
*/
public class FilePersistImplementationTests : MiyamasuTestRunner {
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
	
	/*
		sync series.
	*/
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
			Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);
			
			var appendData = "append data " + Guid.NewGuid().ToString();
			var appendResult = Autoya.Persist_Append(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, appendData);
			
			var loadedData = Autoya.Persist_Load(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
			Assert(loadedData == data + appendData, "data does not match. loadedData:" + loadedData);
		};
		RunOnMainThread(onMainThread);
	}
	
	[MTest] public void Load () {
		Action onMainThread = () => {
			var data = "new data " + Guid.NewGuid().ToString();

			Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);
			
			var loadedData = Autoya.Persist_Load(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
			Assert(loadedData == data, "data does not match. loadedData:" + loadedData);
		};
		RunOnMainThread(onMainThread);
	}

	[MTest] public void LoadFail () {
		Action onMainThread = () => {
			var loadedData = Autoya.Persist_Load(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
			Assert(string.IsNullOrEmpty(loadedData), "data should not be exist.");
		};
		RunOnMainThread(onMainThread);
	}

	[MTest] public void Delete () {
		Action onMainThread = () => {
			var data = "new data " + Guid.NewGuid().ToString();

			Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);
			
			var deleted = Autoya.Persist_Delete(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
			Assert(deleted, "failed to delete.");
		};
		RunOnMainThread(onMainThread);
	}

	[MTest] public void DeleteByDomain () {
		Action onMainThread = () => {
			var data = "new data " + Guid.NewGuid().ToString();

			Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName + "1", data);
			Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName + "2", data);
			Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName + "3", data);
			
			var deleted = Autoya.Persist_DeleteByDomain(AutoyaFilePersistTestsFileDomain);
			Assert(deleted, "failed to delete.");
		};
		RunOnMainThread(onMainThread);
	}
	
	[MTest] public void DeleteNonExist () {
		Action onMainThread = () => {
			var deleteResult = Autoya.Persist_Delete(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
			Assert(!deleteResult, "should not be true.");
		};
		RunOnMainThread(onMainThread);
	}

	[MTest] public void FileNamesInDomain () {
		Action onMainThread = () => {
			var data = "new data " + Guid.NewGuid().ToString();
			Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);

			var fileNamesInDomain = Autoya.Persist_FileNamesInDomain(AutoyaFilePersistTestsFileDomain);
			Assert(fileNamesInDomain.Length == 1, "not match.");
		};
		RunOnMainThread(onMainThread);
	}
	
	[MTest] public void EmptyFileNamesInDomain () {
		Action onMainThread = () => {
			var fileNamesInDomain = Autoya.Persist_FileNamesInDomain(AutoyaFilePersistTestsFileDomain);
			Assert(fileNamesInDomain.Length == 0, "not match.");
		};
		RunOnMainThread(onMainThread);
	}

	/*
		async series.
	*/

}