using System;
using System.Collections;
using System.IO;
using AutoyaFramework;
using AutoyaFramework.Settings.Auth;
using Miyamasu;
using UnityEngine;



/**
	tests for Maintenance feature.
*/
public class MaintenanceTests : MiyamasuTestRunner
{

    private void DeleteAllData(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    [MSetup]
    public IEnumerator Setup()
    {
        Autoya.ResetAllForceSetting();

        DeleteAllData(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN);

        var authorized = false;

        var dataPath = Application.persistentDataPath;
        Autoya.TestEntryPoint(dataPath);

        Autoya.Auth_SetOnAuthenticated(
            () =>
            {
                authorized = true;
            }
        );

        yield return WaitUntil(
            () =>
            {
                return authorized;
            },
            () => { throw new TimeoutException("timeout in setup."); }
        );

        True(Autoya.Auth_IsAuthenticated(), "not logged in.");

        Autoya.forceMaintenance = true;
    }
    [MTeardown]
    public void Teardown()
    {
        Autoya.ResetAllForceSetting();
    }


    [MTest]
    public IEnumerator Maintenance()
    {
        var isUnderMaintenance = false;

        // start connection -> Maintenance mode notification will return.
        Autoya.Http_Get(
            "https://github.com",
            (conId, data) =>
            {
                // do nothing.
            },
            (conId, code, reason, autoyaStatus) =>
            {
                isUnderMaintenance = autoyaStatus.inMaintenance;
            }
        );

        yield return WaitUntil(() => isUnderMaintenance, () => { throw new TimeoutException("not in maintenance."); });
    }



    private bool onMaintenanceCalled = false;
    [MTest]
    public IEnumerator SetOnMaintenance()
    {
        /*
           ready for handle maintenance mode.
           you can set the method which will be called on maintenance.
       */
        Autoya.Maintenance_SetOnMaintenance(
            MyOnMaintenance
        );

        // start connection -> Maintenance mode notification will return.
        Autoya.Http_Get(
            "https://github.com",
            (conId, data) =>
            {
                // do nothing.
            },
            (conId, code, reason, autoyaStatus) =>
            {
                // do nothing.
            }
        );

        yield return WaitUntil(() => onMaintenanceCalled, () => { throw new TimeoutException("onMaintenanceCalled does not be called."); });
    }



    private IEnumerator MyOnMaintenance()
    {
        onMaintenanceCalled = true;
        yield break;
    }

}