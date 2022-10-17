using System.Net.Sockets;

namespace EasySocket
{
    internal class SendState
    {
        public Socket Socket { get; set; }
        public int Count { get; set; }

        public SendState(Socket socket)
        {
            Socket = socket;
            Count = 0;
        }
    }
}
