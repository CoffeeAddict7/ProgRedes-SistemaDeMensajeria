using MessengerDomain;
using ServerMessenger;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClientMessenger
{
    public class Client
    {
        private static Socket tcpClient;
        private static NetworkStream netStream;
        private static Byte[] data;
        private static ProtocolManager chatManager;
        static void Main()
        {
            ConnectToServer();
        }

        private static bool loggedIn = true;

        private static void ConnectToServer()
        {
            InitializeClientConfiguration();
            netStream = new NetworkStream(tcpClient);

            Console.WriteLine("[00] Logout\n" + "[01] Login\n" + "[02] Register & Login");
            
            while (loggedIn)
            {
                try
                {
                    SendRequestToServer();
                    ReceiveResponseFromServer();
                    //Ask for commands
                    //Send Message
                    //Receive response
                    //Process Response

                }
                catch (Exception ex) { 
                    Console.WriteLine(ex.Message);
                }
            }
            netStream.Close();
            tcpClient.Close();
        }

        private static void ReceiveResponseFromServer()
        {
            data = new Byte[256];
            String responseData = String.Empty;
            Int32 bytes = netStream.Read(data, 0, data.Length);
            responseData = Encoding.ASCII.GetString(data, 0, bytes);
            ProcessResponse(responseData);
        }

        private static void ProcessResponse(String responseData)
        {
            ChatProtocol chatPackage = new ChatProtocol(responseData);
            Console.WriteLine("Server -> " + chatPackage.Payload);
            DisplayClientMenu(chatPackage);
        }

        private static void DisplayClientMenu(ChatProtocol chatPackage)
        {
            if (chatPackage.GetCommandNumber() != 0)
                Console.WriteLine("[00] Logout\n" + "[03] Online users\n" + "[04] Friend list\n" + "[05] Send friend request\n");
            else
                Console.WriteLine("[00] Logout\n" + "[01] Login\n" + "[02] Register & Login\n");
        }

        private static void SendRequestToServer()
        {
            String message = Console.ReadLine();
            var messageInfo = ExtractMessageCommandAndContent(message);
            string command = messageInfo.Item1;
            string content = messageInfo.Item2;
            int protocolLen = ChatData.PROTOCOL_FIXED_BYTES + Encoding.ASCII.GetBytes(content).Length;
            SendPackage(command, content, protocolLen);
        }

        private static void SendPackage(string command, string content, int protocolLen)
        {
            string protocolFixedSize = chatManager.BuildProtocolHeaderLength(protocolLen);
            string protocolMsg = ChatData.REQUEST_HEADER + command + protocolFixedSize + content;
            data = Encoding.ASCII.GetBytes(protocolMsg);
            netStream.Write(data, 0, data.Length);
        }

        private static Tuple<string,string> ExtractMessageCommandAndContent(String message)
        {
            int cmd;
            string command = "", content = "";
            if (message.Length >= 2)
            {
                command = new String(message.Take(2).ToArray());
                content = new String(message.Skip(2).Take(message.Length - 2).ToArray());
                if (!Int32.TryParse(command, out cmd))                
                    throw new Exception("Error: Server request must be preceded by a numeric command");                
            }
            return new Tuple<string, string>(command, content);
        }

        private static void InitializeClientConfiguration()
        {
            tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            chatManager = new ProtocolManager();
            var clientEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            tcpClient.Bind(clientEndPoint);
            Console.WriteLine("Connecting to server...");
            tcpClient.Connect(ChatData.SERVER_IP_END_POINT);
            Console.WriteLine("Connected to server");
        }
    }
}
