using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Connections.IP;
using AutoyaFramework.Connections.Udp;
using AutoyaFramework.Settings.AssetBundles;
using Miyamasu;
using UnityEngine;



/**
	tests for Autoya Udp feature.
*/
public class UdpTests : MiyamasuTestRunner
{
    private UdpReceiver udpReceiver;
    private UdpSender udpSender;

    [MSetup]
    public void Setup()
    {

    }

    [MTeardown]
    public IEnumerator Teardown()
    {
        udpReceiver.Close();
        udpSender.Close();
        yield return new WaitForSeconds(1);
    }

    [MTest]
    public IEnumerator SetReceiverThenSend()
    {
        var received = false;
        var port = 8888;
        try
        {
            udpReceiver = new UdpReceiver(
                IP.LocalIPAddressSync(),
                port,
                bytes =>
                {
                    True(bytes.Length == 4);
                    received = true;
                }
            );

            // send udp data.
            udpSender = new UdpSender(IP.LocalIPAddressSync(), port);
            udpSender.Send(new byte[] { 1, 2, 3, 4 });
        }
        catch (Exception e)
        {
            Fail("fail, " + e.ToString());
        }

        yield return WaitUntil(
            () => received,
            () => { throw new TimeoutException("failed."); }
        );
    }

    [MTest]
    public IEnumerator SetReceiverThenSendTwice()
    {
        var count = 2;
        var receivedCount = 0;
        var port = 8888;
        try
        {
            udpReceiver = new UdpReceiver(
                IP.LocalIPAddressSync(),
                port,
                bytes =>
                {
                    True(bytes.Length == 4);
                    receivedCount++;
                }
            );

            // send udp data.
            udpSender = new UdpSender(IP.LocalIPAddressSync(), port);
            udpSender.Send(new byte[] { 1, 2, 3, 4 });
            udpSender.Send(new byte[] { 1, 2, 3, 4 });
        }
        catch (Exception e)
        {
            Fail("fail, " + e.ToString());
        }

        yield return WaitUntil(
            () => count == receivedCount,
            () => { throw new TimeoutException("failed."); }
        );
    }

    [MTest]
    public IEnumerator SetReceiverThenSendManyTimes()
    {
        var count = 10;
        var receivedCount = 0;
        var port = 8888;
        try
        {
            udpReceiver = new UdpReceiver(
                IP.LocalIPAddressSync(),
                port,
                bytes =>
                {
                    True(bytes.Length == 4);
                    receivedCount++;
                }
            );

            // send udp data.
            udpSender = new UdpSender(IP.LocalIPAddressSync(), port);
            for (var i = 0; i < count; i++)
            {
                udpSender.Send(new byte[] { 1, 2, 3, 4 });
            }
        }
        catch (Exception e)
        {
            Fail("fail, " + e.ToString());
        }

        yield return WaitUntil(
            () => count == receivedCount,
            () => { throw new TimeoutException("failed. receivedCount:" + receivedCount); }
        );
    }

    [MTest]
    public IEnumerator SetReceiverThenSendManyTimesWithValidation()
    {
        var count = 10;
        var receivedCount = 0;
        var port = 8888;
        try
        {
            var data = new byte[100];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (byte)UnityEngine.Random.Range(byte.MinValue, byte.MaxValue);
            }

            // validate by hash. checking send data == received data or not.
            var md5 = MD5.Create();
            var origin = md5.ComputeHash(data);
            var hashLen = origin.Length;

            // add hash data to head of new data.
            var newData = new byte[data.Length + hashLen];
            for (var i = 0; i < newData.Length; i++)
            {
                if (i < hashLen)
                {
                    newData[i] = origin[i];
                }
                else
                {
                    newData[i] = data[i - hashLen];
                }
            }

            udpReceiver = new UdpReceiver(
                IP.LocalIPAddressSync(),
                port,
                bytes =>
                {
                    True(bytes.Length == newData.Length);

                    var hash = md5.ComputeHash(bytes, hashLen, bytes.Length - hashLen);
                    for (var i = 0; i < hash.Length; i++)
                    {
                        True(hash[i] == bytes[i]);
                    }

                    receivedCount++;
                }
            );

            // send udp data.
            udpSender = new UdpSender(IP.LocalIPAddressSync(), port);
            for (var i = 0; i < count; i++)
            {
                udpSender.Send(newData);
            }
        }
        catch (Exception e)
        {
            Fail(e.ToString());
        }

        yield return WaitUntil(
            () => count == receivedCount,
            () => { throw new TimeoutException("failed. receivedCount:" + receivedCount); }
        );
    }
}