using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerMessenger
{
    public class Server
    {
        public static IPEndPoint SERVER_IP_END_POINT = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6500);

        private static int MAX_ACTIVE_CONN = 100;
        private static Dictionary<Socket, Thread> activeClientThreads;
        private static Boolean acceptingConnections = true;
        private static int PROTOCOL_FIXED_BYTES = 9;

        static void Main(string[] args)
        {
            activeClientThreads = new Dictionary<Socket, Thread>();
            StartServer();
        }

        private static void StartServer()
        {
            Socket tcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpServer.Bind(SERVER_IP_END_POINT);
            tcpServer.Listen(MAX_ACTIVE_CONN);

            Console.WriteLine("Start waiting for clients");

            while (acceptingConnections) //Cambiar
            {
                var clientSocket = tcpServer.Accept();
                var thread = new Thread(() => ClientHandler(clientSocket));
                thread.Start();                
                //Checkear si ya lo ingrese
                activeClientThreads.Add(clientSocket, thread);
            }

            CloseConnectionWithClients();
        }

        private static void CloseConnectionWithClients()
        {
            foreach(KeyValuePair<Socket,Thread> entry in activeClientThreads)
            {
                entry.Key.Shutdown(SocketShutdown.Both);
                entry.Key.Close();
               
            }
        }

        private static void ClientHandler(Socket client)
        {

            var buffer = new Byte[256];
            int received = 0, localReceived = 0;
            while (received != PROTOCOL_FIXED_BYTES)
            {
                int bytesToReceive = PROTOCOL_FIXED_BYTES - received;
                try
                {
                    localReceived = client.Receive(buffer, received, bytesToReceive,SocketFlags.None);                
                }
                catch (Exception ex) { }
                if(localReceived == 0)
                {
                    //Shutdown
                    //Close
                }
                received += localReceived;
            }
            var text = GetString(buffer);
            Console.WriteLine(text);

        }
        static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

    }
}

