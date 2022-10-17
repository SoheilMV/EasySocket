using System;

namespace EasySocket
{
    public class ErrorHandlerEventArgs : EventArgs
    {
        public Exception Exception { get; internal set; }
    }
}
