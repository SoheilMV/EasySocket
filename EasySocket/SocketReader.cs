using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Net.Sockets;

namespace EasySocket
{
    public class SocketReader
    {
        private Socket _socket;
        private int _buffersize = 4096;

        public IEncryptor Encryptor { get; set; }
        public int BufferSize
        {
            get { return _buffersize; }
            set { _buffersize = value; }
        }
        public int Timeout 
        {
            get { return _socket.ReceiveTimeout; }
            set { _socket.ReceiveTimeout = value; }
        }

        public SocketReader(Socket socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        public SocketReader(Socket socket, IEncryptor encryptor) : this(socket)
        {
            Encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
        }

        public byte[] Read()
        {
            byte[] buffer = new byte[_buffersize];
            int received = _socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
            if (received > 0)
            {
                if(Encryptor != null)
                    return Encryptor.Decrypt(buffer.Take<byte>(received).ToArray<byte>());
                else
                    return buffer.Take<byte>(received).ToArray<byte>();
            }
            else
                return new byte[0];
        }

        public byte[] ReadFrom(ref EndPoint endpoint)
        {
            byte[] buffer = new byte[_buffersize];
            int received = _socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint);
            if (received > 0)
            {
                if (Encryptor != null)
                    return Encryptor.Decrypt(buffer.Take<byte>(received).ToArray<byte>());
                else
                    return buffer.Take<byte>(received).ToArray<byte>();
            }
            else
                return new byte[0];
        }
    }
}
