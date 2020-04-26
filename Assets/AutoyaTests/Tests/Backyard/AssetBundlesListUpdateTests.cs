using System;
using System.Collections;
using System.IO;
using System.Linq;
using AutoyaFramework;
using AutoyaFramework.Settings.App;
using AutoyaFramework.Settings.AssetBundles;
using Miyamasu;
using NUnit.Framework;
using UnityEngine;

public class AssetBundleListUpdateTests : MiyamasuTestRunner
{
    private const string AutoyaFilePersistTestsFileDomain = "AutoyaFilePersistTestsFileDomain";

    /*
        storedABList > codedRuntimeManifest
    */
    [MTest]
    public IEnumerator RemoveUnnecessaryStoredAssetBundleListOnBoot()
    {
        // 事前に保存済みのデータを消す、これでほかのテストの影響を受けない初期化されたデータだけができる。
        Autoya.Persist_DeleteByDomain(AppSettings.APP_STORED_RUNTIME_MANIFEST_DOMAIN);
        Autoya.Persist_DeleteByDomain(AssetBundlesSettings.ASSETBUNDLES_LIST_STORED_DOMAIN);

        var dataPath = Application.persistentDataPath;

        Autoya.TestEntryPoint(dataPath);
        {
            var loginDone = false;
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
        }
        var abReady = false;

        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            results =>
            {
                abReady = true;
            },
            (code, reason, status) =>
            {
                Debug.LogError("failed to download ABList, code:" + code + " reason:" + reason);
            }
        );
        while (!abReady)
        {
            yield return null;
        }

        var defaultGeneratedABListIdentities = Autoya.AssetBundle_AssetBundleLists().Select(list => list.identity).ToArray();

        // このあとまたAutoyaを起動するので、ABListがあるdomainに存在するファイルに、独自の「アプリのアプデでいらなくなった」という状態のリストを追加する。
        var dummyListIdentity = "dummy_reomved";
        var removedABListStr = "{\"identity\":\"" + dummyListIdentity + "\",\"target\":\"iOS\",\"version\":\"1.0.0\",\"assetBundles\":[{\"bundleName\":\"sample\",\"assetNames\":[\"Assets/AutoyaTests/RuntimeData/AssetBundles/SubResources/sample.txt\"],\"dependsBundleNames\":[],\"crc\":1672014196,\"hash\":\"720461ec2bb1aecd2ce41903f3a7d205\",\"size\":754}]}";
        Autoya.Persist_Update(AssetBundlesSettings.ASSETBUNDLES_LIST_STORED_DOMAIN, "dummyList.json", removedABListStr);

        Autoya.TestEntryPoint(dataPath);

        {
            var loginDone = false;
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
        }

        abReady = false;

        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            results =>
            {
                abReady = true;
            },
            (code, reason, status) =>
            {
                Debug.LogError("failed to download ABList, code:" + code + " reason:" + reason);
            }
        );

        while (!abReady)
        {
            yield return null;
        }

        // dummy abList should be deleted.
        var lists = Autoya.AssetBundle_AssetBundleLists();
        var storedIdentities = lists.Select(list => list.identity).ToArray();
        Assert.True(!storedIdentities.Contains(dummyListIdentity), "contained.");

        // all identites are matched.
        foreach (var defaultGeneratedABListIdentitiy in defaultGeneratedABListIdentities)
        {
            Assert.True(storedIdentities.Contains(defaultGeneratedABListIdentitiy), "not contained.");
        }

        foreach (var storedIdentity in storedIdentities)
        {
            Assert.True(defaultGeneratedABListIdentities.Contains(storedIdentity), "not contained.");
        }
    }

    /*
        storedABList <<<< codedRuntimeManifest
    */
    [MTest]
    public IEnumerator StoredAssetBundleListIsEmptyOnBoot()
    {
        // 事前に保存済みのデータを消す、これでほかのテストの影響を受けない初期化されたデータだけができる。
        Autoya.Persist_DeleteByDomain(AppSettings.APP_STORED_RUNTIME_MANIFEST_DOMAIN);
        Autoya.Persist_DeleteByDomain(AssetBundlesSettings.ASSETBUNDLES_LIST_STORED_DOMAIN);

        var dataPath = Application.persistentDataPath;

        Autoya.TestEntryPoint(dataPath);
        {
            var loginDone = false;
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
        }

        Assert.True(!Autoya.AssetBundle_IsAssetBundleFeatureReady(), "ready.");
    }

    /*
        storedABList < codedRuntimeManifest
    */
    [MTest]
    public IEnumerator StoredAssetBundleListIsNotEnoughOnBoot()
    {
        // 事前に保存済みのデータを消す、これでほかのテストの影響を受けない初期化されたデータだけができる。
        Autoya.Persist_DeleteByDomain(AppSettings.APP_STORED_RUNTIME_MANIFEST_DOMAIN);
        Autoya.Persist_DeleteByDomain(AssetBundlesSettings.ASSETBUNDLES_LIST_STORED_DOMAIN);

        var dataPath = Application.persistentDataPath;

        Autoya.TestEntryPoint(dataPath);
        {
            var loginDone = false;
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
        }

        var abReady = false;

        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            results =>
            {
                abReady = true;
            },
            (code, reason, status) =>
            {
                Debug.LogError("failed to download ABList, code:" + code + " reason:" + reason);
            }
        );
        while (!abReady)
        {
            yield return null;
        }

        // remove one of stored ABList.
        var storedABListPaths = Autoya.Persist_FileNamesInDomain(AssetBundlesSettings.ASSETBUNDLES_LIST_STORED_DOMAIN);
        var targetFileName = Path.GetFileName(storedABListPaths[0]);
        Autoya.Persist_Delete(AssetBundlesSettings.ASSETBUNDLES_LIST_STORED_DOMAIN, targetFileName);

        // reboot autoya.
        Autoya.TestEntryPoint(dataPath);

        {
            var loginDone = false;
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
        }

        Assert.True(!Autoya.AssetBundle_IsAssetBundleFeatureReady(), "ready.");
    }

    /*
        storedABList == codedRuntimeManifest
    */
    [MTest]
    public IEnumerator StoredAssetBundleListIsEnoughOnBoot()
    {
        // 事前に保存済みのデータを消す、これでほかのテストの影響を受けない初期化されたデータだけができる。
        Autoya.Persist_DeleteByDomain(AppSettings.APP_STORED_RUNTIME_MANIFEST_DOMAIN);
        Autoya.Persist_DeleteByDomain(AssetBundlesSettings.ASSETBUNDLES_LIST_STORED_DOMAIN);

        var dataPath = Application.persistentDataPath;

        Autoya.TestEntryPoint(dataPath);
        {
            var loginDone = false;
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
        }

        var abReady = false;

        Autoya.AssetBundle_DownloadAssetBundleListsIfNeed(
            results =>
            {
                abReady = true;
            },
            (code, reason, status) =>
            {
                Debug.LogError("failed to download ABList, code:" + code + " reason:" + reason);
            }
        );
        while (!abReady)
        {
            yield return null;
        }

        // reboot autoya.
        Autoya.TestEntryPoint(dataPath);

        {
            var loginDone = false;
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
        }

        Assert.True(Autoya.AssetBundle_IsAssetBundleFeatureReady(), "not ready.");
    }
}