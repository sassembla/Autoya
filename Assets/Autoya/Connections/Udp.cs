using System;
using System.Net;
using System.Net.Sockets;
using AutoyaFramework.Connections.IP;
using UnityEngine;

/**
	implementation of Udp send/receive.
*/
namespace AutoyaFramework.Connections.Udp {
    /**
        udp receiver feature.
        you MUST Close() when you finish using this receiver.
     */
    public class UdpReceiver {
        private readonly UdpClient udp;
        private readonly object lockObj;
        private bool closed;

        public UdpReceiver (IPAddress target, int port, Action<byte[]> receiver) {
            var endpoint = new IPEndPoint(target, port);
            udp = new UdpClient(endpoint);
            lockObj = new object();

            ContinueReceive(receiver, endpoint);
        }

        private void ContinueReceive (Action<byte[]> receiver, IPEndPoint endpoint) {
            udp.BeginReceive(
                ar => {
                    var receivedBytes = udp.EndReceive(ar, ref endpoint);
                    if (receivedBytes.Length == 0) {
                        throw new Exception("receivedBytes is 0.");
                    }

                    if (receiver != null) {
                        receiver(receivedBytes);
                    }

                    if (!closed) {
                        ContinueReceive(receiver, endpoint);
                    }
                }, 
                lockObj
            );
        }

        public void Close () {
            if (closed) {
                return;
            } 

            closed = true;
            udp.Close();
        }
    }

    /**
        udp sender feature.
        you MUST Close() when you finish using this sender.
     */
    public class UdpSender {
        private readonly UdpClient udp;
        private readonly object lockObj;
        private bool closed;

        public UdpSender (IPAddress target, int port) {
            udp = new UdpClient();
            udp.Connect(target, port);
            lockObj = new object();
        }

        public int SendSync (byte[] data) {
            return udp.Send(data, data.Length);
        }

        public void Send (byte[] data) {
            try {
                udp.BeginSend(
                    data, 
                    data.Length, 
                    ar => {
                        udp.EndSend(ar);
                    }, 
                    lockObj
                );
            } catch (Exception e) {
                Debug.Log("どんなエラーでるのこれ:" + e);
            }
        }

        public void Close () {
            if (closed) {
                return;
            } 

            closed = true;
            udp.Close();
        }
    }
}