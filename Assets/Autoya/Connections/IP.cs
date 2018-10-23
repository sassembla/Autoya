using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/**
	implementation of IP services.
*/
namespace AutoyaFramework.Connections.IP
{
    public class IP
    {

        public static IPAddress LocalIPAddressSync()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address;
            }
        }

        public static void LocalIPAddress(Action<IPAddress> onDone)
        {
            ThreadPool.QueueUserWorkItem(
                a =>
                {
                    var localIP = LocalIPAddressSync();
                    onDone(localIP);
                }
            );
        }
    }
}