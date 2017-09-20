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
        private static bool displayMenu = true;
        static void Main()
        {
            ConnectToServer();
        }

        private static bool loggedIn = true;

        private static void ConnectToServer()
        {
            BeginInteractionWithServer();

            while (loggedIn)
            {
                try
                {
                    SendRequestToServer();
                    ReceiveResponseFromServer();                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            netStream.Close();
            tcpClient.Close();
        }

        private static void BeginInteractionWithServer()
        {
            try
            {
                InitializeClientConfiguration();
                netStream = new NetworkStream(tcpClient);
                ShowMenu();
            }
            catch (Exception ex)
            {
                ServerConnectionLost();
            }
        }

        private static void ServerConnectionLost()
        {
            throw new Exception("Server disconnected. Unable to establish connection");
        }

        private static void ReceiveResponseFromServer()
        {
            try
            {
                data = new Byte[256];
                String responseData = String.Empty;
                Int32 bytes = netStream.Read(data, 0, data.Length);
                responseData = Encoding.ASCII.GetString(data, 0, bytes);
                ProcessResponse(responseData);
            }
            catch (Exception ex)
            {
                ServerConnectionLost();
            }
        }

        private static void ProcessResponse(String responseData)
        {
            ChatProtocol chatPackage = new ChatProtocol(responseData);
            Console.WriteLine("Server -> " + chatPackage.Payload);
            DisplayClientMenu(chatPackage);                    
        }

        private static void DisplayClientMenu(ChatProtocol chatPackage)
        {
            if (chatPackage.GetCommandNumber().Equals(0))
                displayMenu = true;

            if (displayMenu)            
                ShowMenu();            
        }
        private static void ShowMenu()
        {
            Console.WriteLine("[00] Logout\n" +"[01] Login\n" + "[02] Register & Login\n" + "[03] Online users\n" +
                "[04] Friend list\n" + "[05] Send friend request\n" + "[06] Pending friend requests\n"+ "[07] Reply to friend requests");
            displayMenu = false;
        }

        private static void SendRequestToServer()
        {
            try
            {
                String message = Console.ReadLine();
                if (message.Equals(String.Empty))
                    throw new Exception("Error: Can't send empty text");
                var messageInfo = ExtractMessageCommandAndContent(message);
                string command = messageInfo.Item1;
                string content = messageInfo.Item2;
                int protocolLen = ChatData.PROTOCOL_FIXED_BYTES + Encoding.ASCII.GetBytes(content).Length;
                SendPackage(command, content, protocolLen);
            }
            catch(Exception ex)
            {
                ServerConnectionLost();
            }                                    
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
