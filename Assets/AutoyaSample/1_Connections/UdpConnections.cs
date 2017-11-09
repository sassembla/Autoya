using System.Collections;
using System.Collections.Generic;
using System.Text;
using AutoyaFramework.Connections.IP;
using AutoyaFramework.Connections.Udp;
using UnityEngine;

/**
	send and receive udp data.
 */
public class UdpConnections : MonoBehaviour {
	private UdpReceiver udpReceiver;
	private UdpSender udpSender;

	// Use this for initialization
	void Start () {
		udpReceiver = new UdpReceiver(
			IP.LocalIPAddressSync(), 
			9999, 
			bytes => {
				Debug.Log("received udp data:" + Encoding.UTF8.GetString(bytes));
			}
		);

		udpSender = new UdpSender(
			IP.LocalIPAddressSync(), 
			9999
		);
		
		udpSender.Send(
			Encoding.UTF8.GetBytes("hello via udp.")
		);
	}
	
	void OnApplicationQuit () {
		udpReceiver.Close();
		udpSender.Close();
	}
}
