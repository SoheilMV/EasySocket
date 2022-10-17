using System.Net.Sockets;

namespace EasySocket
{
    internal class TcpState
    {
        public Socket Socket { get; set; }
        public byte[] Buffer { get; set; }
        public int Count { get; set; }

        public TcpState(Socket client, int buffersize)
        {
            Buffer = new byte[buffersize];
            Count = 0;
            Socket = client;
            Socket.ReceiveBufferSize = buffersize;
            Socket.SendBufferSize = buffersize;
        }
    }
}
