using System;
using System.Net;
using System.Net.Sockets;

namespace EasySocket
{
    public class SocketWriter
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
            get { return _socket.SendTimeout; }
            set { _socket.SendTimeout = value; }
        }

        public SocketWriter(Socket socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        public SocketWriter(Socket socket, IEncryptor encryptor) : this(socket)
        {
            Encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
        }

        public int Send(byte[] message)
        {
            if (message.Length > _buffersize)
                throw new ArgumentOutOfRangeException();
            else if (!_socket.Connected)
                throw new SocketException((int)SocketError.NotConnected);

            int result = 0;
            if (Encryptor != null)
            {
                byte[] encMessage = Encryptor.Encrypt(message);
                if(encMessage.Length > _buffersize)
                    throw new ArgumentOutOfRangeException();

                result = _socket.Send(encMessage, 0, encMessage.Length, SocketFlags.None);
            }
            else
                result = _socket.Send(message, 0, message.Length, SocketFlags.None);

            return result;
        }

        public int SendTo(byte[] message, EndPoint remoteEP)
        {
            if (message.Length > _buffersize)
                throw new ArgumentOutOfRangeException();
            else if (!_socket.Connected)
                throw new SocketException((int)SocketError.NotConnected);

            int result = 0;
            if (Encryptor != null)
            {
                byte[] encMessage = Encryptor.Encrypt(message);
                if (encMessage.Length > _buffersize)
                    throw new ArgumentOutOfRangeException();

                result = _socket.SendTo(encMessage, 0, encMessage.Length, SocketFlags.None, remoteEP);
            }
            else
                result = _socket.SendTo(message, 0, message.Length, SocketFlags.None, remoteEP);

            return result;
        }
    }
}
