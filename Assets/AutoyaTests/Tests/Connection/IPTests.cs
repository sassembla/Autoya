using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Connections.IP;
using AutoyaFramework.Settings.AssetBundles;
using Miyamasu;
using UnityEngine;



/**
	tests for Autoya IP feature.
*/
public class IPTests : MiyamasuTestRunner
{
    [MTest]
    public IEnumerator GetLocalIPSync()
    {
        var ip = IP.LocalIPAddressSync();
        True(ip.GetAddressBytes()[0] != 0);
        yield break;
    }

    [MTest]
    public IEnumerator GetLocalIP()
    {
        var done = false;
        IP.LocalIPAddress(
            ipAddress =>
            {
                True(ipAddress.GetAddressBytes()[0] != 0);
                // Debug.Log("ipAddress:" + ipAddress);
                done = true;
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("too late."); }
        );
    }
}