using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ClientMessenger
{
    class Client
    {
        static void Main(string[] args)
        {
            ConnectToServer();
        }

        private static Boolean finishConnection = false;

        private static void ConnectToServer()
        {
            Socket tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var clientEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            tcpClient.Bind(clientEndPoint);

            Console.WriteLine("Connecting to server...");
            tcpClient.Connect(ServerMessenger.Server.SERVER_IP_END_POINT);
            Console.WriteLine("Connected to server");

            while (!finishConnection)
            {
                var text = Console.ReadLine();
                var dataToSend = GetBytes(text); //?
                tcpClient.Send(dataToSend);
                //Ask for commands
                //Send Message
                //Receive response
                //Process Response
                tcpClient.Close();
            }
            Console.ReadLine();
        }
        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
