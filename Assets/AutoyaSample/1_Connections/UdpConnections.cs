﻿using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using AutoyaFramework.Connections.IP;
using AutoyaFramework.Connections.Udp;
using UnityEngine;

/**
	send and receive udp data.
 */
public class UdpConnections : MonoBehaviour
{
    private UdpReceiver udpReceiver;
    private UdpSender udpSender;

    // Use this for initialization
    void Start()
    {

        // create receiver.
        udpReceiver = new UdpReceiver(
            IP.LocalIPAddressSync(),
            9339,
            bytes =>
            {
                Debug.Log("received udp data:" + Encoding.UTF8.GetString(bytes));
            }
        );

        // send data to receiver.
        udpSender = new UdpSender(
            IPAddress.Parse("127.0.0.1"),
            9339
        );

        udpSender.Send(
            Encoding.UTF8.GetBytes("hello via udp.")
        );
    }

    void OnApplicationQuit()
    {
        udpReceiver.Close();
        udpSender.Close();
    }
}
