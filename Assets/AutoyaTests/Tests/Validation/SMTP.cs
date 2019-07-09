using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using AutoyaFramework;
using AutoyaFramework.AssetBundles;
using AutoyaFramework.Settings.AssetBundles;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Miyamasu;
using UnityEngine;



/**
	tests for Autoya Validations.
*/
public class ValidationTests : MiyamasuTestRunner
{
    [MTest]
    public IEnumerator GetAssetBundleList()
    {
        // var url = "mx://gmail.com";
        // var url = "smtp://gmail.com";
        // var port = 587;
        // var uri = new Uri(url);
        // Debug.Log("scheme:" + uri.Scheme);

        // IPEndPoint endPoint = null;
        // Dns.BeginGetHostEntry(
        //     uri.Host,
        //     new AsyncCallback(
        //         result =>
        //         {
        //             IPHostEntry addresses = null;

        //             try
        //             {
        //                 addresses = Dns.EndGetHostEntry(result);
        //             }
        //             catch (Exception e)
        //             {
        //                 Debug.Log("1e:" + e);
        //                 return;
        //             }

        //             if (addresses.AddressList.Length == 0)
        //             {
        //                 Debug.Log("not found.");
        //                 return;
        //             }

        //             // choose valid ip.
        //             foreach (IPAddress ipaddress in addresses.AddressList)
        //             {
        //                 if (ipaddress.AddressFamily == AddressFamily.InterNetwork || ipaddress.AddressFamily == AddressFamily.InterNetworkV6)
        //                 {
        //                     endPoint = new IPEndPoint(ipaddress, port);
        //                     Debug.Log("endPoint:" + endPoint);// [2404:6800:4004:818::2005]:25



        //                     var clientSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        //                     clientSocket.NoDelay = true;
        //                     clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

        //                     var connectArgs = new SocketAsyncEventArgs();
        //                     connectArgs.AcceptSocket = clientSocket;
        //                     connectArgs.RemoteEndPoint = endPoint;
        //                     connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnect);

        //                     if (!clientSocket.ConnectAsync(connectArgs))
        //                     {
        //                         OnConnect(clientSocket, connectArgs);
        //                     }

        //                     return;
        //                 }
        //             }

        //             Debug.Log("domain unresolved.");
        //         }
        //     ),
        //     this
        // );

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("GTS", "toru.inoue@kissaki.tv"));
        message.To.Add(new MailboxAddress("Me", "toru.inoue@kissaki.tv"));
        message.Subject = "Test mail";
        message.Body = new TextPart("plain")
        {
            Text = @"Hello, This is the test"
        };

        using (var client = new SmtpClient())
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            // 587 だとエラーが出る。いろいろやり方がありそう。
            client.Connect("smtp.gmail.com", 465, SecureSocketOptions.Auto);

            // client.Authenticate("toru.inoue@kissaki.tv", "kDb-SFT-6nV-keR");
            Debug.Log("ここまで動作する。");

            client.Send(message);

            client.Disconnect(true);
        }

        yield return WaitUntil(
            () => false,
            () => { throw new TimeoutException("timeout!"); },
            30
        );
    }

    private void OnConnect(object unused, SocketAsyncEventArgs args)
    {
        Debug.Log("here!");
    }
}
