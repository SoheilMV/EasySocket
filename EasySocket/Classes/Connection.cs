using System.Net.Sockets;

namespace EasySocket
{
    public class Connection
    {
        public bool IsConnect { get; internal set; }
        public Socket Socket { get; internal set; }
    }
}
