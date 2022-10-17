using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace EasySocket.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "EasySocket Test";

            #region TCP Example

            MyTcpServer tcpServer = new MyTcpServer();
            tcpServer.Start();

            MyTcpClient tcpClient = new MyTcpClient();
            tcpClient.Connect();
            tcpClient.Message();

            #endregion

            #region UDP Example

            MyUdpServer udpServer = new MyUdpServer();
            udpServer.Start();

            MyUdpClient udpClient = new MyUdpClient();
            udpClient.Connect();
            udpClient.Message();

            #endregion

            Console.ReadKey();
        }

        #region TCP

        private class MyTcpServer
        {
            TcpServer _server = null;

            public MyTcpServer()
            {
                _server = new TcpServer();
                _server.AutoReceive = false;
                _server.Encryptor = new MySecurity();
                _server.OnAccept += _server_OnAccept;
                _server.OnReseved += _server_OnReseved;
                _server.OnErrorHandler += _server_OnErrorHandler;
            }

            private void _server_OnAccept(object sender, ClientEventArgs e)
            {
                TcpServer server = (TcpServer)sender;

                if (!server.AutoReceive)
                {
                    SocketReader reader = new SocketReader(e.Socket, server.Encryptor);
                    SocketWriter writer = new SocketWriter(e.Socket, server.Encryptor);

                    var bytes = reader.Read();
                    var num = writer.Send(Encoding.UTF8.GetBytes("Hello, welcome to the TCP Server."));

                    Console.WriteLine($"TCP Client : {Encoding.UTF8.GetString(bytes)}");
                }
            }

            public void Start()
            {
                _server.Start("127.0.0.1", 1111);
            }

            private void _server_OnReseved(object sender, TcpServerResevedEventArgs e)
            {
                Console.WriteLine($"TCP Client : {Encoding.UTF8.GetString(e.Receive)}");

                int count = _server.Send(e.Socket, "Hello, welcome to the TCP Server.");
            }

            private void _server_OnErrorHandler(object sender, ErrorHandlerEventArgs e)
            {
                Console.WriteLine($"Error : {e.Exception.Message}");
            }
        }

        private class MyTcpClient
        {
            TcpConnection _client = null;

            public MyTcpClient()
            {
                _client = new TcpConnection();
                _client.Encryptor = new MySecurity();
                _client.OnReseved += _client_OnReseved;
                _client.OnErrorHandler += _client_OnErrorHandler;
            }

            public void Connect()
            {
                _client.Connect("127.0.0.1", 1111);
                _client.StartReceiving();
            }

            public void Message()
            {
                int count = _client.Send("Hello, I am a TCP Client.");
            }

            private void _client_OnReseved(object sender, ClientResevedEventArgs e)
            {
                Console.WriteLine($"TCP Server : {Encoding.UTF8.GetString(e.Receive)}");
            }

            private void _client_OnErrorHandler(object sender, ErrorHandlerEventArgs e)
            {
                Console.WriteLine($"Error : {e.Exception.Message}");
            }
        }

        #endregion

        #region UDP

        private class MyUdpServer
        {
            UdpServer _server = null;

            public MyUdpServer()
            {
                _server = new UdpServer();
                _server.Encryptor = new MySecurity();
                _server.OnReseved += _server_OnReseved;
                _server.OnErrorHandler += _server_OnErrorHandler;
            }

            public void Start()
            {
                _server.Start("127.0.0.1", 2222);
            }

            private void _server_OnReseved(object sender, UdpServerResevedEventArgs e)
            {
                Console.WriteLine($"UDP Client : {Encoding.UTF8.GetString(e.Receive)}");

                int count = _server.Send("Hello, welcome to the UDP Server.", e.EndPoint);
            }

            private void _server_OnErrorHandler(object sender, ErrorHandlerEventArgs e)
            {
                Console.WriteLine($"Error : {e.Exception.Message}");
            }
        }

        private class MyUdpClient
        {
            UdpConnection _client = null;

            public MyUdpClient()
            {
                _client = new UdpConnection();
                _client.Encryptor = new MySecurity();
                _client.OnReseved += _client_OnReseved;
                _client.OnErrorHandler += _client_OnErrorHandler;
            }

            public void Connect()
            {
                _client.Connect("127.0.0.1", 2222);
                _client.StartReceiving();
            }

            public void Message()
            {
                int count = _client.Send("Hello, I am a UDP Client.");
            }

            private void _client_OnReseved(object sender, ClientResevedEventArgs e)
            {
                Console.WriteLine($"UDP Server : {Encoding.UTF8.GetString(e.Receive)}");
            }

            private void _client_OnErrorHandler(object sender, ErrorHandlerEventArgs e)
            {
                Console.WriteLine($"Error : {e.Exception.Message}");
            }
        }

        #endregion

        #region Encryptor

        private class MySecurity : IEncryptor
        {
            private byte[] _password = Encoding.UTF8.GetBytes("Soheil MV");
            private byte[] _salt = Encoding.UTF8.GetBytes("12345678");

            public byte[] Decrypt(byte[] input)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (RijndaelManaged AES = new RijndaelManaged())
                    {
                        AES.KeySize = 256;
                        AES.BlockSize = 128;

                        var key = new Rfc2898DeriveBytes(_password, _salt, 1000);
                        AES.Key = key.GetBytes(AES.KeySize / 8);
                        AES.IV = key.GetBytes(AES.BlockSize / 8);

                        AES.Mode = CipherMode.CBC;
                        AES.Padding = PaddingMode.PKCS7;

                        using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(input, 0, input.Length);
                            cs.Close();
                        }
                        return ms.ToArray();
                    }
                }
            }

            public byte[] Encrypt(byte[] input)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (RijndaelManaged AES = new RijndaelManaged())
                    {
                        AES.KeySize = 256;
                        AES.BlockSize = 128;

                        var key = new Rfc2898DeriveBytes(_password, _salt, 1000);
                        AES.Key = key.GetBytes(AES.KeySize / 8);
                        AES.IV = key.GetBytes(AES.BlockSize / 8);

                        AES.Mode = CipherMode.CBC;
                        AES.Padding = PaddingMode.PKCS7;

                        using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(input, 0, input.Length);
                            cs.Close();
                        }
                        return ms.ToArray();
                    }
                }
            }
        }

        #endregion
    }
}
