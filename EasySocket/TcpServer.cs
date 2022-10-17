using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Net.Sockets;
using System.Collections.Generic;

namespace EasySocket
{
    public class TcpServer : IDisposable
    {
        private Socket _server;
        private List<Socket> _clients = new List<Socket>();
        private bool _run = false;
        private int _buffersize = 4096;
        private Encoding _encoding = Encoding.UTF8;
        private bool _autoReceive = true;
        private int _timeout = 0;

        public event EventHandler<ClientEventArgs> OnAccept;
        public event EventHandler<TcpServerResevedEventArgs> OnReseved;
        public event EventHandler<ErrorHandlerEventArgs> OnErrorHandler;

        public Socket Socket { get { return _server; } }
        public bool IsRun { get { return _run; } }
        public IEncryptor Encryptor { get; set; }
        public List<Socket> Clients
        {
            get
            {
                _clients = _clients.Where(x => x.Connected).ToList();
                return _clients;
            }
        }
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
        public bool AutoReceive 
        {
            get { return _autoReceive; }
            set { _autoReceive = value; }
        }
        public int Timeout
        {
            get { return _timeout; }
            set
            {
                _timeout = value;
                _server.ReceiveTimeout = _timeout;
                _server.SendTimeout = _timeout;
            }
        }

        public TcpServer()
        {
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        #region Method

        #region Public

        public ConnectionResult Start(string ip, int port, int backlog = 20)
        {
            return Start(IPAddress.Parse(ip), port, backlog);
        }

        public ConnectionResult Start(IPAddress ip, int port, int backlog = 20)
        {
            IPEndPoint iep = new IPEndPoint(ip, port);
            return Start(iep, backlog);
        }

        public ConnectionResult Start(IPEndPoint iep, int backlog = 20)
        {
            var result = new ConnectionResult();
            try
            {
                if (!_run)
                {
                    _run = true;
                    _server.Bind(iep);
                    _server.Listen(backlog);
                    _server.BeginAccept(AcceptCallback, _server);
                    result.Status = true;
                    result.Message = $"The server turned on -> {iep.Address.ToString()}:{iep.Port}!";
                }
                else
                {
                    result.Status = true;
                    result.Message = "You have already turned on the server!";
                }
            }
            catch
            {
                _run = false;
                result.Status = false;
                result.Message = "The address is not valid!";
            }
            return result;
        }

        public ConnectionResult Stop()
        {
            var result = new ConnectionResult();
            if (_run)
            {
                _server.Close();
                _server.Dispose();
                foreach (var client in _clients.ToList())
                {
                    if (client.Connected)
                    {
                        client.Close();
                        client.Dispose();
                    }
                }
                _clients.Clear();
                result.Status = true;
                result.Message = $"The server turned off!";
                _run = false;
            }
            else
            {
                result.Status = false;
                result.Message = $"You have already turned off the server!";
            }
            return result;
        }

        public int Send(Socket client, byte[] message)
        {
            if (message.Count() > _buffersize)
                throw new ArgumentOutOfRangeException();
            else if (!_run)
                throw new Exception("The server is not running!");
            else if (!client.Connected)
                throw new Exception("The connection to the client is lost!");

            SendState sendState = new SendState(client);

            try
            {
                if (Encryptor != null)
                {
                    byte[] encMessage = Encryptor.Encrypt(message);
                    client.BeginSend(encMessage, 0, encMessage.Length, SocketFlags.None, SendCallback, sendState);
                }
                else
                    client.BeginSend(message, 0, message.Length, SocketFlags.None, SendCallback, sendState);
            }
            catch (Exception ex)
            {
                OnErrorHandler?.Invoke(this, new ErrorHandlerEventArgs() { Exception = ex });
            }

            return sendState.Count;
        }

        public int Send(Socket client, string message)
        {
            byte[] data = _encoding.GetBytes(message);
            return Send(client, data);
        }

        public void SendToAll(string message)
        {
            if (_clients.Count > 0)
            {
                foreach (var client in _clients)
                {
                    Send(client, message);
                }
            }
        }

        public void SendToAll(byte[] message)
        {
            if (_clients.Count > 0)
            {
                foreach (var client in _clients)
                {
                    Send(client, message);
                }
            }
        }

        public void Receive(Socket client)
        {
            if (!_run)
                throw new Exception("The server is not running!");
            else if (!client.Connected)
                throw new Exception("The connection to the client is lost!");

            TcpState state = new TcpState(client, BufferSize);
            client.BeginReceive(state.Buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, state);
        }

        public void Dispose()
        {
            Stop();
        }

        #endregion

        #region Private

        private void AcceptCallback(IAsyncResult ar)
        {
            Socket server = (Socket)ar.AsyncState;
            try
            {
                Socket client = server.EndAccept(ar);

                if (_run)
                    server.BeginAccept(AcceptCallback, server);

                if (!_clients.Contains(client))
                {
                    _clients.Add(client);

                    OnAccept?.Invoke(this, new ClientEventArgs() { Socket = client });

                    if (_autoReceive)
                    {
                        TcpState state = new TcpState(client, BufferSize);
                        client.BeginReceive(state.Buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, state);
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorHandler?.Invoke(this, new ErrorHandlerEventArgs() { Exception = ex });
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            TcpState state = (TcpState)ar.AsyncState;
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    int num = state.Socket.EndReceive(ar);
                    if (num > 0)
                    {
                        ms.Write(state.Buffer, 0, num);
                        if (Encryptor != null)
                            OnReseved?.Invoke(this, new TcpServerResevedEventArgs() { Socket = state.Socket, Receive = Encryptor.Decrypt(ms.ToArray()) });
                        else
                            OnReseved?.Invoke(this, new TcpServerResevedEventArgs() { Socket = state.Socket, Receive = ms.ToArray() });
                        ms.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorHandler?.Invoke(this, new ErrorHandlerEventArgs() { Exception = ex });
            }

            if (state.Socket.Connected)
            {
                state.Buffer = new byte[BufferSize];
                state.Socket.BeginReceive(state.Buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, state);
            }
            else
            {
                if (_clients.Contains(state.Socket))
                    _clients.Remove(state.Socket);

                state.Socket.Close();
                state.Socket.Dispose();
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
