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
public class UdpTests : MiyamasuTestRunner {
    private UdpReceiver udpReceiver;
    private UdpSender udpSender;

    [MSetup] public void Setup () {

    }

    [MTeardown] public void Teardown () {
        udpReceiver.Close();
        udpSender.Close();
    }
    
    [MTest] public IEnumerator SetReceiverThenSend () {
        var received = false;
        var port = 8888;

        udpReceiver = new UdpReceiver(
            IP.LocalIPAddressSync(),
            port, 
            bytes => {
                True(bytes.Length == 4);
                received = true;
            }
        );

        // send udp data.
        udpSender = new UdpSender(IP.LocalIPAddressSync(), port);
        udpSender.Send(new byte[]{1,2,3,4});

        yield return WaitUntil(
            () => received,
            () => {throw new TimeoutException("failed.");}
        );
    }

    [MTest] public IEnumerator SetReceiverThenSendTwice () {
        var count = 2;
        var receivedCount = 0;
        var port = 8888;

        udpReceiver = new UdpReceiver(
            IP.LocalIPAddressSync(),
            port, 
            bytes => {
                True(bytes.Length == 4);
                receivedCount++;
            }
        );

        // send udp data.
        udpSender = new UdpSender(IP.LocalIPAddressSync(), port);
        udpSender.Send(new byte[]{1,2,3,4});
        udpSender.Send(new byte[]{1,2,3,4});

        yield return WaitUntil(
            () => count == receivedCount,
            () => {throw new TimeoutException("failed.");}
        );
    }

    [MTest] public IEnumerator SetReceiverThenSendManyTimes () {
        var count = 1000;
        var receivedCount = 0;
        var port = 8888;

        udpReceiver = new UdpReceiver(
            IP.LocalIPAddressSync(),
            port, 
            bytes => {
                True(bytes.Length == 4);
                receivedCount++;
            }
        );

        // send udp data.
        udpSender = new UdpSender(IP.LocalIPAddressSync(), port);
        for (var i = 0; i < count; i++) {
            udpSender.Send(new byte[]{1,2,3,4});
        }

        yield return WaitUntil(
            () => count == receivedCount,
            () => {throw new TimeoutException("failed.");}
        );
    }

    [MTest] public IEnumerator SetReceiverThenSendManyTimesWithValidation () {
        var count = 1000;
        var receivedCount = 0;
        var port = 8888;

        var data = new byte[100];
        for (var i = 0; i < data.Length; i++) {
            data[i] = (byte)UnityEngine.Random.Range(byte.MinValue, byte.MaxValue);
        }

        // validate by hash. checking send data == received data or not.
        var md5 = MD5.Create();
        var origin = md5.ComputeHash(data);
        
        udpReceiver = new UdpReceiver(
            IP.LocalIPAddressSync(),
            port, 
            bytes => {
                True(bytes.Length == data.Length);

                var hash = md5.ComputeHash(bytes);
                for (var i = 0; i < hash.Length; i++) {
                    True(hash[i] == origin[i]);
                }
                receivedCount++;
            }
        );

        // send udp data.
        udpSender = new UdpSender(IP.LocalIPAddressSync(), port);
        for (var i = 0; i < count; i++) {
            udpSender.Send(data);
        }

        yield return WaitUntil(
            () => count == receivedCount,
            () => {throw new TimeoutException("failed. receivedCount:" + receivedCount);}
        );
    }

    
}