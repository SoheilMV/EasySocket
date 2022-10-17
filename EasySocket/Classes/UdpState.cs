using System.Net;
using System.Net.Sockets;

namespace EasySocket
{
    internal class UdpState
    {
        public EndPoint EndPoint;
        public Socket Socket { get; set; }
        public byte[] Buffer { get; set; }
        public int Count { get; set; }

        public UdpState(Socket socket, int buffersize)
        {
            EndPoint = new IPEndPoint(IPAddress.Any, 0);
            Buffer = new byte[buffersize];
            Count = 0;
            Socket = socket;
            Socket.ReceiveBufferSize = buffersize;
            Socket.SendBufferSize = buffersize;
        }
    }
}
