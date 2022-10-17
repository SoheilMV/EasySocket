using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace EasySocket
{
    public class ClientEventArgs : EventArgs
    {
        public Socket Socket { get; internal set; }
    }
}
