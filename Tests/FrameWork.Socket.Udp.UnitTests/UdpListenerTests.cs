using System.Net;
using System.Text;
using System.Net.Sockets;
using NUnit.Framework;
using FrameWork.Socket.Udp;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System;

namespace FrameWork.Socket.Udp.UnitTests;

public class UdpListenerTests
{
    [OneTimeSetUp]
    public void StartTest()
    {
        Trace.Listeners.Add(new ConsoleTraceListener());
    }

    [OneTimeTearDown]
    public void EndTest()
    {
        Trace.Flush();
    }

    [Test]
    public async Task ListenForUTF8Bytes()
    {
        using (var listener = new UdpListener(11234, 1024)) {
            var received = new List<byte[]>();
            var task = listener.Listen(data => {
                Debug.WriteLine("Data received");
                received.Add(data);
            });

            //await Task.Delay(1000);
            
            var expected = "This is the expected string";

            using (var client = new UdpClient(11234)) {
                var bytes = Encoding.UTF8.GetBytes(expected);
                client.Connect(IPAddress.Loopback, 11234);
                var bytesSent = client.Send(bytes);
                Assert.Greater(bytesSent, 0);
            }

            await Task.Delay(100);

            Assert.That(received.Count == 1, $"Received count is {received.Count} not 1");
            Debug.Write(BitConverter.ToString(received[0]).Replace("-",""));
            Assert.AreEqual(expected, Encoding.UTF8.GetString(received[0]));
        }
    }
}