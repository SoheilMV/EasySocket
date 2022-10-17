using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Net.Sockets;

namespace EasySocket
{
    public class UdpServer : IDisposable
    {
        private Socket _server;
        private bool _run = false;
        private int _buffersize = 4096;
        private Encoding _encoding = Encoding.UTF8;
        private int _timeout = 0;

        public event EventHandler<UdpServerResevedEventArgs> OnReseved;
        public event EventHandler<ErrorHandlerEventArgs> OnErrorHandler;

        public Socket Socket { get { return _server; } }
        public bool IsRun { get { return _run; } }
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
                _server.ReceiveTimeout = _timeout;
                _server.SendTimeout = _timeout;
            }
        }

        public UdpServer()
        {
        }

        #region Method

        #region Public

        public ConnectionResult Start(string ip, int port)
        {
            return Start(IPAddress.Parse(ip), port);
        }

        public ConnectionResult Start(IPAddress ip, int port)
        {
            IPEndPoint iep = new IPEndPoint(ip, port);
            return Start(iep);
        }

        public ConnectionResult Start(IPEndPoint iep)
        {
            var result = new ConnectionResult();
            try
            {
                if (!_run)
                {
                    _run = true;
                    _server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    _server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                    _server.Bind(iep);

                    UdpState state = new UdpState(_server, _buffersize);
                    _server.BeginReceiveFrom(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, ref state.EndPoint, ReceiveCallback, state);

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

        public int Send(byte[] message, EndPoint ep)
        {
            if (message.Count() > _buffersize)
                throw new ArgumentOutOfRangeException();

            SendState sendState = new SendState(_server);

            try
            {
                if (Encryptor != null)
                {
                    byte[] encMessage = Encryptor.Encrypt(message);
                    _server.BeginSendTo(encMessage, 0, encMessage.Length, SocketFlags.None, ep, SendCallback, sendState);
                }
                else
                    _server.BeginSendTo(message, 0, message.Length, SocketFlags.None, ep, SendCallback, sendState);
            }
            catch(Exception ex)
            {
                OnErrorHandler?.Invoke(this, new ErrorHandlerEventArgs() { Exception = ex });
            }

            return sendState.Count;
        }

        public int Send(string message, EndPoint ep)
        {
            return Send(_encoding.GetBytes(message), ep);
        }

        public void Dispose()
        {
            Stop();
        }

        #endregion

        #region Private

        private void ReceiveCallback(IAsyncResult ar)
        {
            UdpState state = (UdpState)ar.AsyncState;

            if (_run)
            {
                UdpState newState = new UdpState(state.Socket, _buffersize);
                newState.Socket.BeginReceiveFrom(newState.Buffer, 0, newState.Buffer.Length, SocketFlags.None, ref newState.EndPoint, ReceiveCallback, newState);
            }

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    do
                    {
                        state.Count = state.Socket.EndReceiveFrom(ar, ref state.EndPoint);
                        ms.Write(state.Buffer, 0, state.Count);
                    }
                    while (_server.Available > 0);

                    if (ms.Length > 0)
                    {
                        var endPoint = state.EndPoint;
                        if (Encryptor != null)
                            OnReseved?.Invoke(this, new UdpServerResevedEventArgs() { EndPoint = endPoint, Receive = Encryptor.Decrypt(ms.ToArray()) });
                        else
                            OnReseved?.Invoke(this, new UdpServerResevedEventArgs() { EndPoint = endPoint, Receive = ms.ToArray() });
                        ms.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorHandler?.Invoke(this, new ErrorHandlerEventArgs() { Exception = ex });
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                SendState sendState = (SendState)ar.AsyncState;
                sendState.Count = sendState.Socket.EndSendTo(ar);
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
