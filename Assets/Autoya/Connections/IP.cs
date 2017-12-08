using System;
using System.Net;
using System.Net.Sockets;

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
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            var connectArgs = new SocketAsyncEventArgs();
            connectArgs.AcceptSocket = socket;
            connectArgs.RemoteEndPoint = new IPEndPoint(new IPAddress(new byte[] { 8, 8, 8, 8 }), 65530);

            Action<object, SocketAsyncEventArgs> onConnected = (a, b) =>
            {
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                var localIP = endPoint.Address;

                onDone(localIP);

                socket.Disconnect(false);
            };

            connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(onConnected);

            if (!socket.ConnectAsync(connectArgs))
            {
                onConnected(socket, connectArgs);
            }
        }
    }
}