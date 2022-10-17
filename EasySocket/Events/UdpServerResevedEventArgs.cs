using System;
using System.Net;

namespace EasySocket
{
    public class UdpServerResevedEventArgs : EventArgs
    {
        public EndPoint EndPoint { get; internal set; }
        public byte[] Receive { get; internal set; }
    }
}
