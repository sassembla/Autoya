using System;
using System.Collections;
using System.IO;
using AutoyaFramework;
using Miyamasu;
using UnityEngine;

/**
	test for file persist controll.
*/
public class FilePersistImplementationTests : MiyamasuTestRunner
{
    private const string AutoyaFilePersistTestsFileDomain = "AutoyaFilePersistTestsFileDomain";
    private const string AutoyaFilePersistTestsFileName = "persist.txt";

    [MSetup]
    public IEnumerator Setup()
    {
        var loginDone = false;

        var dataPath = Application.persistentDataPath;
        Autoya.TestEntryPoint(dataPath);
        Autoya.Auth_SetOnAuthenticated(
            () =>
            {
                loginDone = true;
            }
        );

        yield return WaitUntil(
            () =>
            {
                return loginDone;
            },
            () => { throw new TimeoutException("timeout."); }
        );

        // delete all.
        Autoya.Persist_DeleteByDomain(AutoyaFilePersistTestsFileDomain);
    }

    /*
		sync series.
	*/

    [MTest]
    public IEnumerable IsNeverExists()
    {
        var result = Autoya.Persist_IsExist(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
        True(!result, "should not be exist.");
        yield break;
    }

    [MTest]
    public IEnumerable IsMustBeExist()
    {
        var data = "new data " + Guid.NewGuid().ToString();
        Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);

        var result = Autoya.Persist_IsExist(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
        True(!result, "should not be exist.");
        yield break;
    }


    [MTest]
    public IEnumerator Update()
    {
        var data = "new data " + Guid.NewGuid().ToString();

        var result = Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);
        True(result, "not successed.");
        yield break;
    }

    [MTest]
    public IEnumerator Append()
    {
        var data = "new data " + Guid.NewGuid().ToString();
        Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);

        var appendData = "append data " + Guid.NewGuid().ToString();
        Autoya.Persist_Append(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, appendData);

        var loadedData = Autoya.Persist_Load(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
        True(loadedData == data + appendData, "data does not match. loadedData:" + loadedData);
        yield break;
    }

    [MTest]
    public IEnumerator Load()
    {
        var data = "new data " + Guid.NewGuid().ToString();

        Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);

        var loadedData = Autoya.Persist_Load(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
        True(loadedData == data, "data does not match. loadedData:" + loadedData);
        yield break;
    }

    [MTest]
    public IEnumerator LoadFail()
    {
        var loadedData = Autoya.Persist_Load(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
        True(string.IsNullOrEmpty(loadedData), "data should not be exist.");
        yield break;
    }

    [MTest]
    public IEnumerator Delete()
    {
        var data = "new data " + Guid.NewGuid().ToString();

        Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);

        var deleted = Autoya.Persist_Delete(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
        True(deleted, "failed to delete.");
        yield break;
    }

    [MTest]
    public IEnumerator DeleteByDomain()
    {
        var data = "new data " + Guid.NewGuid().ToString();

        Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName + "1", data);
        Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName + "2", data);
        Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName + "3", data);

        var deleted = Autoya.Persist_DeleteByDomain(AutoyaFilePersistTestsFileDomain);
        True(deleted, "failed to delete.");
        yield break;
    }

    [MTest]
    public IEnumerator DeleteNonExist()
    {
        var deleteResult = Autoya.Persist_Delete(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
        True(!deleteResult, "should not be true.");
        yield break;
    }

    [MTest]
    public IEnumerator FileNamesInDomain()
    {
        var data = "new data " + Guid.NewGuid().ToString();
        Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);

        var fileNamesInDomain = Autoya.Persist_FileNamesInDomain(AutoyaFilePersistTestsFileDomain);
        True(fileNamesInDomain.Length == 1, "not match.");

        yield break;
    }

    [MTest]
    public IEnumerator EmptyFileNamesInDomain()
    {
        var fileNamesInDomain = Autoya.Persist_FileNamesInDomain(AutoyaFilePersistTestsFileDomain);
        True(fileNamesInDomain.Length == 0, "not match.");
        yield break;
    }

    [MTest]
    public IEnumerator CreateFileThenDeleteFileThenFileNamesInDomain()
    {
        Autoya.Persist_Update("testdomain", "testfile", "test");
        Autoya.Persist_Delete("testdomain", "testfile");
        var fileNamesInDomain = Autoya.Persist_FileNamesInDomain("testdomain");
        True(fileNamesInDomain.Length == 0, "not match.");
        yield break;
    }


    /*
		async series.
	*/

    [MTest]
    public IEnumerator UpdateAsync()
    {
        var data = "new data " + Guid.NewGuid().ToString();
        var succeeded = false;

        Autoya.Persist_Update(
            AutoyaFilePersistTestsFileDomain,
            AutoyaFilePersistTestsFileName,
            data,
            () =>
            {
                succeeded = true;
            },
            reason => { }
        );

        yield return WaitUntil(
            () => succeeded,
            () => { throw new TimeoutException("too late."); }
        );
    }

    [MTest]
    public IEnumerator AppendAsync()
    {
        var data = "new data " + Guid.NewGuid().ToString();
        Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);

        var loadedData = string.Empty;

        var appendData = "append data " + Guid.NewGuid().ToString();
        Autoya.Persist_Append(
            AutoyaFilePersistTestsFileDomain,
            AutoyaFilePersistTestsFileName,
            appendData,
            () =>
            {
                loadedData = Autoya.Persist_Load(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
            },
            reason => { }
        );

        yield return WaitUntil(
            () => loadedData == data + appendData,
            () => { throw new TimeoutException("too late."); }
        );
    }

    [MTest]
    public IEnumerator LoadAsync()
    {
        var data = "new data " + Guid.NewGuid().ToString();
        var loadedData = string.Empty;

        Autoya.Persist_Update(
            AutoyaFilePersistTestsFileDomain,
            AutoyaFilePersistTestsFileName,
            data,
            () =>
            {
                loadedData = Autoya.Persist_Load(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
            },
            reason => { }
        );

        yield return WaitUntil(
            () => loadedData == data,
            () => { throw new TimeoutException("too late."); }
        );
    }

    [MTest]
    public IEnumerator LoadFailAsync()
    {
        var loadedData = string.Empty;

        Autoya.Persist_Load(
            AutoyaFilePersistTestsFileDomain,
            AutoyaFilePersistTestsFileName,
            data =>
            {
                loadedData = data;
            },
            reason => { }
        );

        yield return WaitUntil(
            () => string.IsNullOrEmpty(loadedData),
            () => { throw new TimeoutException("too late."); }
        );
    }

    [MTest]
    public IEnumerator DeleteAsync()
    {
        var data = "new data " + Guid.NewGuid().ToString();

        Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName, data);

        var deleted = false;

        Autoya.Persist_Delete(
            AutoyaFilePersistTestsFileDomain,
            AutoyaFilePersistTestsFileName,
            () =>
            {
                deleted = true;
            },
            reason => { }
        );

        yield return WaitUntil(
            () => deleted,
            () => { throw new TimeoutException("too late."); }
        );
    }

    [MTest]
    public IEnumerator DeleteByDomainAsync()
    {
        var data = "new data " + Guid.NewGuid().ToString();

        Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName + "1", data);
        Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName + "2", data);
        Autoya.Persist_Update(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName + "3", data);

        var deleted = false;
        Autoya.Persist_DeleteByDomain(
            AutoyaFilePersistTestsFileDomain,
            () =>
            {
                deleted = true;
            },
            reason => { }
        );

        yield return WaitUntil(
            () => deleted,
            () => { throw new TimeoutException("too late."); }
        );
    }

    [MTest]
    public IEnumerator DeleteNonExistAsync()
    {
        var deleteFailed = false;
        Autoya.Persist_Delete(
            AutoyaFilePersistTestsFileDomain,
            AutoyaFilePersistTestsFileName,
            () => { },
            reason =>
            {
                deleteFailed = true;
            }
        );

        yield return WaitUntil(
            () => deleteFailed,
            () => { throw new TimeoutException("too late."); }
        );
    }
}