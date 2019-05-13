using System;
using System.Collections;
using AutoyaFramework;
using Miyamasu;
using UnityEngine;

public class CacheHitImplementationTest : MiyamasuTestRunner
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
    public IEnumerator CacheThenUpdateCache()
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
    }


    /*
        cache all data then hit.
    */
    [MTest]
    public IEnumerator CacheThenHit()
    {
        yield return Cache();

        var hit = Autoya.Persist_HitHash(AutoyaFilePersistTestsFileDomain, "h");

        True(hit);
        yield break;
    }


    /*
        cache all data then not hit by uncached item.
    */
    [MTest]
    public IEnumerator CacheThenNotHitByNotCachedItem()
    {
        yield return Cache();

        var hit = Autoya.Persist_HitHash(AutoyaFilePersistTestsFileDomain, "x");

        False(hit);
        yield break;
    }


    /*
        hit.
    */
    [MTest]
    public IEnumerator HitWithCached()
    {
        yield return Cache();

        Autoya.Persist_ClearOnMemoryHashCache();

        var hit = Autoya.Persist_HitHash(AutoyaFilePersistTestsFileDomain, "h");

        True(hit);
        yield break;
    }

    [MTest]
    public IEnumerator HitWithCachedButNotExist()
    {
        yield return Cache();

        Autoya.Persist_ClearOnMemoryHashCache();

        var hit = Autoya.Persist_HitHash(AutoyaFilePersistTestsFileDomain, "x");

        False(hit);
        yield break;
    }

    /*
        存在するケースは、これらのすべての掛け算
        ・ヒットのタイミング キャッシュ 前 or 後
        ・対象の有無
        ・同じイニシャルに複数の対象がある場合のチェック
        ・キャッシュファイルの更新
     */


    /*
        同じinitialのファイルがキャッシュされてる系
     */
    [MTest]
    public IEnumerator CacheMultipleSameInitial()
    {
        var result = Autoya.Persist_IsExist(AutoyaFilePersistTestsFileDomain, AutoyaFilePersistTestsFileName);
        True(!result, "should not be exist.");

        var done = false;
        Autoya.Persist_CacheHashes(
            AutoyaFilePersistTestsFileDomain,
            "h e ll l oo w o r l d".Split(' '),// l and ll, o and oo.
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
    public IEnumerator CacheThenUpdateCacheMultipleSameInitial()
    {
        yield return CacheMultipleSameInitial();

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
    }


    /*
        cache all data then hit.
    */
    [MTest]
    public IEnumerator CacheThenHitMultipleSameInitial()
    {
        yield return CacheMultipleSameInitial();

        var hit = Autoya.Persist_HitHash(AutoyaFilePersistTestsFileDomain, "h");

        True(hit);
        yield break;
    }


    /*
        cache all data then not hit by uncached item.
    */
    [MTest]
    public IEnumerator CacheThenNotHitByNotCachedItemMultipleSameInitial()
    {
        yield return CacheMultipleSameInitial();

        var hit = Autoya.Persist_HitHash(AutoyaFilePersistTestsFileDomain, "x");

        False(hit);
        yield break;
    }


    /*
        hit.
    */
    [MTest]
    public IEnumerator HitWithCachedMultipleSameInitial()
    {
        yield return CacheMultipleSameInitial();

        Autoya.Persist_ClearOnMemoryHashCache();

        var hit = Autoya.Persist_HitHash(AutoyaFilePersistTestsFileDomain, "h");

        True(hit);
        yield break;
    }

    [MTest]
    public IEnumerator HitWithCachedButNotExistMultipleSameInitial()
    {
        yield return CacheMultipleSameInitial();

        Autoya.Persist_ClearOnMemoryHashCache();

        var hit = Autoya.Persist_HitHash(AutoyaFilePersistTestsFileDomain, "x");

        False(hit);
        yield break;
    }
}