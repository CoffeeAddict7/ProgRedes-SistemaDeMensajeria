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

            CloseServerConnection();
        }

        private static void InitializeServerConfiguration()
        {
            activeClientThreads = new Dictionary<Socket, Thread>();
            tcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpServer.Bind(SERVER_IP_END_POINT);
            tcpServer.Listen(MAX_ACTIVE_CONN);
            Console.WriteLine("Start waiting for clients");
        }

        private static void CloseServerConnection()
        {
            foreach (KeyValuePair<Socket, Thread> entry in activeClientThreads)
            {
                entry.Key.Shutdown(SocketShutdown.Both);
                entry.Key.Close();
            }
            tcpServer.Close();
        }

        private static int ReadFixedBytesFromPackage(Socket client ,StreamReader reader, ref StringBuilder sb)
        {
            var buffer = new char[10000];
            int received = 0, localReceived = 0, bytesLeftToRead = 0, packageLength = PROTOCOL_FIXED_BYTES;
            while(received != PROTOCOL_FIXED_BYTES)
            {
                bytesLeftToRead = PROTOCOL_FIXED_BYTES - received;
                localReceived = reader.Read(buffer, received, bytesLeftToRead);
                received += localReceived;

                if (localReceived > 0)
                {
                    AppendBufferToStringBuilder(ref sb, buffer, received);
                    Int32.TryParse(sb.ToString().Substring(5), out packageLength);
                }
                else
                {
                    EndConnection(client);
                }
            }
            return packageLength;
        }

        private static void ReadPayloadBytesFromPackage(Socket client, StreamReader reader, ref StringBuilder sb, int packageLength)
        {
            var payloadBuffer = new char[10000];
            int received = 0, localReceived = 0, bytesLeftToRead = 0;
            int payloadLength = packageLength - PROTOCOL_FIXED_BYTES;
            while (received != payloadLength)
            {
                bytesLeftToRead = payloadLength - received;
                localReceived = reader.Read(payloadBuffer, received, bytesLeftToRead);
                received += localReceived;

                if (localReceived > 0)
                {
                    AppendBufferToStringBuilder(ref sb, payloadBuffer, payloadLength);
                }
                else
                {
                    EndConnection(client);
                }

            }
        }

        private static void ClientHandler(Socket client)
        {
            NetworkStream stream = new NetworkStream(client);
            using (var reader = new StreamReader(stream))
            {
                var sb = new StringBuilder();
                int packageLength = ReadFixedBytesFromPackage(client, reader, ref sb);
                ReadPayloadBytesFromPackage(client, reader, ref sb, packageLength);

                var payloadLength = packageLength - PROTOCOL_FIXED_BYTES;
                var package = sb.ToString();

                try { 
                    ChatProtocol chatMsg = new ChatProtocol(package, payloadLength);
                    ProcessMessage(client, chatMsg);
                }catch(Exception ex)
                {
                    //Show in console
                }
            }
            stream.Close();//
        }

        private static void ProcessMessage(Socket client, ChatProtocol chatMsg)
        {
            Console.WriteLine(chatMsg.Payload);
        }

        private static void EndConnection(Socket client)
        {
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        private static void AppendBufferToStringBuilder(ref StringBuilder sb, char[] payloadBuffer, int payloadLength)
        {
            var bufferCopy = new char[payloadLength];
            Array.Copy(payloadBuffer, bufferCopy, payloadLength);
            sb.Append(bufferCopy);
        }
    }
}

