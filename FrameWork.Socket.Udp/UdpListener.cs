using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace FrameWork.Socket.Udp;
public class UdpListener : IDisposable
{
    System.Net.Sockets.Socket socket;

    public IPAddress Address { get; }
    public int Port { get; }

    public UdpListener(int port, int bufferSize) : this(IPAddress.Loopback, port, bufferSize)
    {
    }
    private readonly int bufferSize;

    public UdpListener(IPAddress address, int port, int bufferSize)
    {
        this.bufferSize = bufferSize;
        socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        Address = address;
        Port = port;
    }

    public async Task Listen(Action<byte[]> onDataReceived, CancellationToken? cancellationToken = null)
    {
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
        socket.Bind(new IPEndPoint(Address, Port));

        if (cancellationToken == null) {
            await Task.Run(async () =>
            {   
                while (true)
                {
                    var buffer = new byte[bufferSize];
                    await socket.ReceiveAsync(buffer, SocketFlags.None);
                    onDataReceived.Invoke(TrimTailingZeros(buffer));
                }
            });
        } else {
            await Task.Run(async () =>
            {
                while (!cancellationToken.Value.IsCancellationRequested)
                {
                    var buffer = new byte[bufferSize];
                    await socket.ReceiveAsync(buffer, SocketFlags.None);
                }
            }, cancellationToken.Value);
        }
    }

    public static byte[] TrimTailingZeros(byte[] arr)
    {
        if (arr == null || arr.Length == 0)
            return arr;
        return arr.Reverse().SkipWhile(x => x == 0).Reverse().ToArray();
    }

    public void Dispose()
    {
        socket.Dispose();
    }

}
