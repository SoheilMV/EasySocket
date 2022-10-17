using System;
using System.Net.Sockets;

namespace EasySocket
{
    public class TcpServerResevedEventArgs : EventArgs
    {
        public Socket Socket { get; internal set; }
        public byte[] Receive { get; internal set; }
    }
}
