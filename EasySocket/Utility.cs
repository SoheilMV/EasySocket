using System.Net.Sockets;
using System.Collections.Generic;

namespace EasySocket
{
    public static class Utility
    {
        public static byte[] GetBytes(byte[] buffer, int offset, int count)
        {
            List<byte> result = new List<byte>();
            for (int i = offset; i < count; i++)
            {
                result.Add(buffer[i]);
            }
            return result.ToArray();
        }

        public static TcpConnection ToTcpConnection(this Socket socket)
        {
            return new TcpConnection(socket);
        }

        public static UdpConnection ToUdpConnection(this Socket socket)
        {
            return new UdpConnection(socket);
        }

        public static SocketReader ToSocketReader(this Socket socket)
        {
            return new SocketReader(socket);
        }

        public static SocketWriter ToSocketWriter(this Socket socket)
        {
            return new SocketWriter(socket);
        }

        public static TcpClient ToTcpClient(this Socket socket, int bufferSize)
        {
            TcpClient tcp = new TcpClient()
            {
                Client = socket,
                ReceiveBufferSize = bufferSize,
                SendBufferSize = bufferSize
            };
            return tcp;
        }
    }
}
