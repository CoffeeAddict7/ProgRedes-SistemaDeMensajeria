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
        private static string serverErrorMsg = "Server disconnected. Unable to establish connection";
        static void Main()
        {
            ConnectToServer();
        }

        private static bool connected = true;

        private static void ConnectToServer()
        {
            BeginInteractionWithServer();

            while (connected)
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
            Console.Read();
        }

        private static void BeginInteractionWithServer()
        {
            try
            {
                InitializeClientConfiguration();
                netStream = new NetworkStream(tcpClient);
                ShowMenu();
            }
            catch (Exception)
            {
                Console.WriteLine(serverErrorMsg);
            }
        }

        private static void ServerConnectionLost()
        {
            throw new Exception(serverErrorMsg);
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
            catch (Exception)
            {
                ServerConnectionLost();
            }
        }

        private static void ProcessResponse(String responseData)
        {
            ChatProtocol chatPackage = new ChatProtocol(responseData);
            DisplayResponseByType(chatPackage);               
        }

        private static void DisplayResponseByType(ChatProtocol chatPackage)
        {
            string message = chatPackage.Payload;
            string[] typeAndData = message.Split('$');
            string responseType = typeAndData[0];
            string responseData = typeAndData[1]; //Los errores ni los usuarios pueden contener "$"

            if (responseType.Equals(ChatData.RESPONSE_ERROR))
                Console.WriteLine("> " + responseData);
            else
                ProcessResponseOkByCommand(chatPackage.GetCommandNumber(), responseData);
        }

        private static void ProcessResponseOkByCommand(int command, string data)
        {
            switch (command)
            {
                case 0:
                    ShowMenu();
                    Console.WriteLine("> Disconnected ");
                    break;
                case 1:
                    Console.WriteLine("> Connected ");
                    break;
                case 2:
                    Console.WriteLine("> Connected ");
                    break;
                case 3:
                    ShowUserList(data, "No users online", "Online:");
                    break;
                case 4:
                    ShowFriendList(data);
                    break;
                case 5:
                    Console.WriteLine("> Request sent!");
                    break;
                case 6:
                    ShowUserList(data,"You have no pending friend requests", "Friend requests:");
                    break;
                case 7:
                    Console.WriteLine("> Reply done!");
                    break;
                case 99:
                    connected = false;
                    Console.WriteLine("> {Server closed connection} ");
                    break;
                default:
                    Console.WriteLine("> Response not implemented");
                    break;
            }
        }

        private static void ShowUserList(string data, string emptyListMsg, string usersMsg)
        {
            if (data.Equals(String.Empty))
                Console.WriteLine("> " + emptyListMsg);
            else
            {
                string[] users = data.Split('#');
                Console.WriteLine(usersMsg);
                foreach (var user in users)
                    Console.WriteLine("- " + user);
            }
        }

        private static void ShowFriendList(string data)
        {
            if (data.Equals(String.Empty))
            {
                Console.WriteLine("> No friends added");
            }else
            {
                Console.WriteLine("> Friend list: ");
                string[] friends = data.Split('#');
                foreach (var friend in friends)
                {
                    string[] friendInfo = friend.Split('_');
                    Console.WriteLine("- {" + friendInfo[0] + "} has (" + friendInfo[1] + ") friends");
                }
            }           
        }

        private static char[] TakeWhileNonEndString(string message, string endString)
        {
            return message.TakeWhile(str => !str.Equals(endString)).ToArray().ToString().ToCharArray();
        }

        private static void DisplayClientMenu(ChatProtocol protocol)
        {
            if (protocol.GetCommandNumber().Equals(0))
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
            String message = Console.ReadLine();            
            ChatProtocol request = chatManager.CreateRequestProtocolFromInput(message);            
            SendPackage(request);                         
        }

        private static void SendPackage(ChatProtocol request)
        {
            try
            {
                data = Encoding.ASCII.GetBytes(request.Package);
                if(netStream.CanWrite)
                    netStream.Write(data, 0, data.Length);
                else
                    ServerConnectionLost();
            }
            catch (Exception)
            {
                ServerConnectionLost();
            }
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
