using System;

namespace EasySocket
{
    public class ClientResevedEventArgs : EventArgs
    {
        public byte[] Receive { get; internal set; }
    }
}
