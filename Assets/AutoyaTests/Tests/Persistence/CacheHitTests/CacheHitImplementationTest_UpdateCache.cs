using System;
using System.Collections;
using System.IO;
using System.Linq;
using AutoyaFramework;
using Miyamasu;
using UnityEngine;

public class CacheHitImplementationTest_UpdateCache : MiyamasuTestRunner
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
        cache all data with fastest way.
    */
    [MTest]
    public IEnumerator Cache()
    {
        var result = Autoya.Persist_IsExist(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
        True(!result, "should not be exist.");

        var done = false;
        Autoya.Persist_CacheHashes(
            AutoyaFilePersistTestsFileDomain,
            "h e l l o w o r l d".Split(' '),
            () =>
            {
                done = true;
            },
            (code, reason) =>
            {
                Fail("failed to cache items:" + reason);
            }
        );

        yield return WaitUntil(
            () =>
            {
                return done;
            },
            () => { throw new TimeoutException("timeout."); }
        );
    }

    [MTest]
    public IEnumerator InitialRemoved()
    {
        yield return Cache();

        var done = false;
        Autoya.Persist_CacheHashes(
            AutoyaFilePersistTestsFileDomain,
            "h e o w o r d".Split(' '),// l removed.
            () =>
            {
                done = true;
            },
            (code, reason) =>
            {
                Fail("failed to cache items:" + reason);
            }
        );

        yield return WaitUntil(
            () =>
            {
                return done;
            },
            () => { throw new TimeoutException("timeout."); }
        );

        // ここで件数チェックを行う。initialが減ってるはず
        var initialFilePaths = Autoya.Persist_DirectoryNamesInDomain(AutoyaFilePersistTestsFileDomain);
        var expected = "h e o w o r d".Split(' ').Distinct().ToArray().Length;
        True(initialFilePaths.Length == expected, "not match. " + initialFilePaths.Length + " vs " + expected);
    }

    [MTest]
    public IEnumerator InitialAdded()
    {
        yield return Cache();

        var done = false;
        Autoya.Persist_CacheHashes(
            AutoyaFilePersistTestsFileDomain,
            "h e l l o w o r l d 0".Split(' '),// 0, added.
            () =>
            {
                done = true;
            },
            (code, reason) =>
            {
                Fail("failed to cache items:" + reason);
            }
        );

        yield return WaitUntil(
            () =>
            {
                return done;
            },
            () => { throw new TimeoutException("timeout."); }
        );

        // ここで件数チェックを行う。initialが増えているはず
        var initialFilePaths = Autoya.Persist_DirectoryNamesInDomain(AutoyaFilePersistTestsFileDomain);
        var expected = "h e l l o w o r l d 0".Split(' ').Distinct().ToArray().Length;
        True(initialFilePaths.Length == expected, "not match. " + initialFilePaths.Length + " vs " + expected);
    }



    /*
        cache all data with fastest way.
    */
    [MTest]
    public IEnumerator SameInitialCache()
    {
        var result = Autoya.Persist_IsExist(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
        True(!result, "should not be exist.");

        var done = false;
        Autoya.Persist_CacheHashes(
            AutoyaFilePersistTestsFileDomain,
            "h e ll o w o r l d".Split(' '),// ll and l will be into same folder.
            () =>
            {
                done = true;
            },
            (code, reason) =>
            {
                Fail("failed to cache items:" + reason);
            }
        );

        yield return WaitUntil(
            () =>
            {
                return done;
            },
            () => { throw new TimeoutException("timeout."); }
        );

        var cacheReference = Autoya.Debug_Persist_HashCountByDomain(
            AutoyaFilePersistTestsFileDomain,
            'l'
        );
        True(cacheReference == 2, "not match, cacheReference:" + cacheReference);
    }


    [MTest]
    public IEnumerator ContentRemoved()
    {
        yield return SameInitialCache();

        var initialFilePaths = Autoya.Persist_DirectoryNamesInDomain(AutoyaFilePersistTestsFileDomain);

        var done = false;
        Autoya.Persist_CacheHashes(
            AutoyaFilePersistTestsFileDomain,
            "h e o w o r l d".Split(' '),// ll removed.
            () =>
            {
                done = true;
            },
            (code, reason) =>
            {
                Fail("failed to cache items:" + reason);
            }
        );

        yield return WaitUntil(
            () =>
            {
                return done;
            },
            () => { throw new TimeoutException("timeout."); }
        );

        var newInitialFilePaths = Autoya.Persist_DirectoryNamesInDomain(AutoyaFilePersistTestsFileDomain);
        True(initialFilePaths.Length == newInitialFilePaths.Length, "not match.");

        var targetPath = initialFilePaths.Where(p => p.EndsWith("l")).First();

        var lFilePaths = Directory.GetFiles(targetPath);
        True(lFilePaths.Length == 1, "not match, expect:1, actual:" + lFilePaths.Length);
    }

    [MTest]
    public IEnumerator ContentAdded()
    {
        yield return Cache();
        var cacheReference = Autoya.Debug_Persist_HashCountByDomain(
            AutoyaFilePersistTestsFileDomain,
            'l'
        );

        var initialFilePaths = Autoya.Persist_DirectoryNamesInDomain(AutoyaFilePersistTestsFileDomain);

        var done = false;
        Autoya.Persist_CacheHashes(
            AutoyaFilePersistTestsFileDomain,
            "h e ll o w o r l d".Split(' '),// ll, added.
            () =>
            {
                done = true;
            },
            (code, reason) =>
            {
                Fail("failed to cache items:" + reason);
            }
        );

        yield return WaitUntil(
            () =>
            {
                return done;
            },
            () => { throw new TimeoutException("timeout."); }
        );

        var newInitialFilePaths = Autoya.Persist_DirectoryNamesInDomain(AutoyaFilePersistTestsFileDomain);
        True(initialFilePaths.Length == newInitialFilePaths.Length, "not match.");

        var targetPath = initialFilePaths.Where(p => p.EndsWith("l")).First();

        var lFilePaths = Directory.GetFiles(targetPath);
        True(lFilePaths.Length == 2, "not match, expect:2, actual:" + lFilePaths.Length);

        cacheReference = Autoya.Debug_Persist_HashCountByDomain(
            AutoyaFilePersistTestsFileDomain,
            'l'
        );
        True(cacheReference == 2, "not match, cacheReference:" + cacheReference);
    }
}