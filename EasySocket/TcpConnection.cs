using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;

namespace EasySocket
{
    public class TcpConnection : IDisposable
    {
        private Socket _client;
        private bool _connect = false;
        private int _buffersize = 4096;
        private Encoding _encoding = Encoding.UTF8;
        private int _timeout = 0;
        private bool _receiving = false;

        public event EventHandler<ClientResevedEventArgs> OnReseved;
        public event EventHandler<EventArgs> OnClosed;
        public event EventHandler<ErrorHandlerEventArgs> OnErrorHandler;

        public bool IsConnect { get { return _connect; } }
        public Socket Socket { get { return _client; } }
        public IEncryptor Encryptor { get; set; }
        public int BufferSize
        {
            get { return _buffersize; }
            set { _buffersize = value; }
        }
        public Encoding Encoding
        {
            get { return _encoding; }
            set { _encoding = value; }
        }
        public int Timeout
        {
            get { return _timeout; }
            set
            {
                _timeout = value;
                _client.ReceiveTimeout = _timeout;
                _client.SendTimeout = _timeout;
            }
        }

        public TcpConnection()
        {
        }

        public TcpConnection(Socket socket)
        {
            if (socket == null)
                throw new ArgumentNullException(nameof(socket));
            if (!socket.Connected)
                throw new SocketException((int)SocketError.NotConnected);

            _connect = true;
            _client = socket;
        }

        #region Method

        #region Public

        public ConnectionResult Connect(string ip, int port)
        {
            return Connect(IPAddress.Parse(ip), port);
        }

        public ConnectionResult Connect(IPAddress ip, int port)
        {
            IPEndPoint iep = new IPEndPoint(ip, port);
            return Connect(iep);
        }

        public ConnectionResult Connect(IPEndPoint iep)
        {
            var result = new ConnectionResult();
            if (!_connect)
            {
                try
                {
                    _connect = true;
                    _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    _client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    _client.Connect(iep);

                    result.Status = true;
                    result.Message = "Connected!";
                }
                catch
                {
                    result.Status = false;
                    result.Message = "The server is off!";
                    _connect = false;
                }
            }
            else
            {
                result.Status = false;
                result.Message = "You are connected to the server!";
            }
            return result;
        }

        public ConnectionResult Disconnect()
        {
            var result = new ConnectionResult();
            if (_connect)
            {
                try
                {
                    _client.Close();
                    _client.Dispose();
                    _connect = false;
                    result.Status = true;
                    result.Message = "Disconnected!";
                }
                catch (Exception ex)
                {
                    OnErrorHandler?.Invoke(this, new ErrorHandlerEventArgs() { Exception = ex });
                }
            }
            else
            {
                result.Status = false;
                result.Message = "You are not connected to the server!";
            }
            return result;
        }

        public void StartReceiving()
        {
            if (!_connect)
                throw new Exception("The connection to the server is lost!");
            else if (_receiving)
                return;

            _receiving = true;

            TcpState state = new TcpState(_client, _buffersize);
            _client.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, ReceiveCallback, state);
        }

        public void StopReceiving()
        {
            if (!_connect)
                throw new Exception("The connection to the server is lost!");

            _receiving = false;
        }

        public int Send(byte[] message)
        {
            if (message.Length > _buffersize)
                throw new ArgumentOutOfRangeException();
            else if (!_connect)
                throw new Exception("The connection to the server is lost!");

            SendState sendState = new SendState(_client);

            try
            {
                if (Encryptor != null)
                {
                    byte[] encMessage = Encryptor.Encrypt(message);
                    _client.BeginSend(encMessage, 0, encMessage.Length, SocketFlags.None, SendCallback, sendState);
                }
                else
                    _client.BeginSend(message, 0, message.Length, SocketFlags.None, SendCallback, sendState);
            }
            catch (Exception ex)
            {
                OnErrorHandler?.Invoke(this, new ErrorHandlerEventArgs() { Exception = ex });
            }

            return sendState.Count;
        }

        public int Send(string message)
        {
            byte[] buffer = _encoding.GetBytes(message);
            return Send(buffer);
        }

        public SocketReader GetReader()
        {
            SocketReader reader = new SocketReader(_client);
            reader.Encryptor = Encryptor;
            reader.BufferSize = _buffersize;
            return reader;
        }

        public SocketWriter GetWriter()
        {
            SocketWriter writer = new SocketWriter(_client);
            writer.Encryptor = Encryptor;
            writer.BufferSize = _buffersize;
            return writer;
        }

        public NetworkStream GetStream()
        {
            return new NetworkStream(_client, true);
        }

        public void Dispose()
        {
            Disconnect();
        }

        #endregion

        #region Private

        private void ReceiveCallback(IAsyncResult ar)
        {
            TcpState state = (TcpState)ar.AsyncState;
            if (state.Socket.Connected && _receiving)
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        state.Count = state.Socket.EndReceive(ar);
                        if (state.Count > 0)
                        {
                            ms.Write(state.Buffer, 0, state.Count);
                            if (Encryptor != null)
                                OnReseved?.Invoke(this, new ClientResevedEventArgs() { Receive = Encryptor.Decrypt(ms.ToArray()) });
                            else
                                OnReseved?.Invoke(this, new ClientResevedEventArgs() { Receive = ms.ToArray() });
                            ms.Flush();
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnErrorHandler?.Invoke(this, new ErrorHandlerEventArgs() { Exception = ex });
                }

                TcpState newState = new TcpState(state.Socket, _buffersize);
                newState.Socket.BeginReceive(newState.Buffer, 0, newState.Buffer.Length, SocketFlags.None, ReceiveCallback, newState);
            }
            else if (_connect)
            {
                OnClosed?.Invoke(this, new EventArgs());
                Disconnect();
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                SendState sendState = (SendState)ar.AsyncState;
                sendState.Count = sendState.Socket.EndSend(ar);
            }
            catch (Exception ex)
            {
                OnErrorHandler?.Invoke(this, new ErrorHandlerEventArgs() { Exception = ex });
            }
        }

        #endregion

        #endregion
    }
}
