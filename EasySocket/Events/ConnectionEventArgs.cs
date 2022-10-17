using System;

namespace EasySocket
{
    public class ConnectionEventArgs : EventArgs
    {
        public Connection Connection { get; internal set; }

        public string Message { get; internal set; }
    }
}
