using System;
using System.Collections.Generic;
using System.IO;
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
        private static int PROTOCOL_FIXED_BYTES = 9;
        private static Boolean acceptingConnections = true;

        private static Dictionary<Socket, Thread> activeClientThreads;
        private static Socket tcpServer;

        static void Main(string[] args)
        {
            StartServer();
        }

        private static void StartServer()
        {
            InitializeServerConfiguration();

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

        private static void InitializeServerConfiguration()
        {
            activeClientThreads = new Dictionary<Socket, Thread>();
            tcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpServer.Bind(SERVER_IP_END_POINT);
            tcpServer.Listen(MAX_ACTIVE_CONN);
            Console.WriteLine("Start waiting for clients");
        }

        private static void CloseConnectionWithClients()
        {
            foreach(KeyValuePair<Socket,Thread> entry in activeClientThreads)
            {
                entry.Key.Shutdown(SocketShutdown.Both);
                entry.Key.Close();               
            }
        }

        /*
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
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                }
                received += localReceived;
            }
            var text = GetString(buffer);
            
            Console.WriteLine(text);            
        }*/

        private static void ClientHandler(Socket client)
        {
            NetworkStream stream = new NetworkStream(client);

            using (var reader = new StreamReader(stream))
            {
                var sb = new StringBuilder();
                var buffer = new char[8192];
                int received = 0, localReceived = 0;

                while (received != PROTOCOL_FIXED_BYTES)
                {
                    int bytesToReceive = PROTOCOL_FIXED_BYTES - received;
                    localReceived = reader.Read(buffer, received, bytesToReceive);

                    if (localReceived == 0)
                    {
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();
                    }
                    received += localReceived;

                    if (localReceived > 0)
                    {
                        var buffer2 = new char[received];
                        Array.Copy(buffer, buffer2, received);
                        sb.Append(buffer2);

                        // if sb meets some criteria, process the data...
                        Console.WriteLine(sb.ToString());
                    }
                    else
                        Console.WriteLine("Client disconnected!");
                }
            }
        }
    }
}

