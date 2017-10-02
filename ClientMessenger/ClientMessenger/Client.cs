using MessengerDomain;
using ServerMessenger;
using System;
using System.IO;
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
        private static Byte[] dataRead;
        private static Byte[] dataWrite;
        private static ProtocolManager chatManager;
        private static string serverErrorMsg = "Server disconnected. Unable to establish connection";
        private static bool liveChatting = false;
        private static bool displayMenu = true;
        private static string liveChatUser = "";
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
            StreamReader reader = new StreamReader(netStream); //Si no anda subirlo
 
            try
            {
                /*
                var sb = new StringBuilder();
                int packageLength = chatManager.ReadFixedBytesFromPackage(tcpClient, reader, ref sb);
                chatManager.ReadPayloadBytesFromPackage(tcpClient, reader, ref sb, packageLength);
                var package = sb.ToString();*/
                
                dataRead = new Byte[9999];
                String responseData = String.Empty;
                Int32 bytes = netStream.Read(dataRead, 0, dataRead.Length);
                var package = Encoding.ASCII.GetString(dataRead, 0, bytes);
                
                ProcessResponse(package);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ServerConnectionLost();
            }            
        }

        private static void ProcessResponse(String package)
        {
            ChatProtocol chatPackage = new ChatProtocol(package);
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
                    ShowOnlineUsers(data);
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
                case 8:
                    ShowLiveChat(data);
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

        private static void ShowLiveChat(string data)
        {
            string[] message = data.Split('#');
            if (message.Length > 2)
            {
                if (message[1].Equals("END"))
                {
                    liveChatting = false;
                    liveChatUser = "";
                }
                else
                {
                    liveChatting = true;
                    liveChatUser = message[0];
                }
                if(!message[2].Equals(String.Empty))
                    Console.WriteLine("> " + message[2]);
            }
            else
            {
                Console.WriteLine("> " + data);
            }
        }

        private static void ShowOnlineUsers(string data)
        {
            if (data.Equals(String.Empty))
                Console.WriteLine("> No users online.");
            else
            {
                string[] users = data.Split('#');
                Console.WriteLine("> Online users:");
                foreach (var user in users)
                {
                    string[] userInfo = user.Split('_');
                    Console.WriteLine("- " + userInfo[0] + " | Friends: (" + userInfo[1] + ")" + " | Connections: (" + userInfo[2] + ") | Online for (" +userInfo[3]+ ") ");
                }
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

        private static void ShowMenu()
        {
            Console.WriteLine("[00] Logout\n" + "[01] Login\n" + "[02] Register & Login\n" + "[03] Online users\n" +
                "[04] Friend list\n" + "[05] Send friend request\n" + "[06] Pending friend requests\n" + "[07] Reply to friend requests\n" + "[08] Chat with friend");
            displayMenu = false;
        }

        private static void SendRequestToServer()
        {
            String message = Console.ReadLine();
            ChatProtocol request = MakeChatProtocolRequest(message);
            SendPackage(request);
        }

        private static ChatProtocol MakeChatProtocolRequest(string message)
        {
            ChatProtocol request; 
            if (liveChatting)
            {
                string[] chatModeMsg = message.Split('#');
                string recieverUserProfile = liveChatUser;
                string command = ChatData.CMD_LIVECHAT;
                string chatState = ChatData.LIVECHAT_CHAT;
                string msg = message;
                if(chatModeMsg.Length > 1)
                {
                    if(chatModeMsg[0].Length > 2)
                    {
                        command = new String(chatModeMsg[0].Take(2).ToArray());
                        recieverUserProfile = new String(chatModeMsg[0].Skip(2).Take(chatModeMsg[0].Length - 2).ToArray());
                    }
                    chatState = chatModeMsg[1];
                    msg = "";
                }
                if (!command.Equals(ChatData.CMD_LIVECHAT))
                    throw new Exception("Error: Close chat before using other commands");
                request = chatManager.CreateLiveChatRequestProtocol(recieverUserProfile, chatState, msg);
            }else
                request = chatManager.CreateRequestProtocolFromInput(message);
            return request;
        }

        private static void SendPackage(ChatProtocol request)
        {
            try
            {
                dataWrite = Encoding.ASCII.GetBytes(request.Package);
                netStream.Write(dataWrite, 0, dataWrite.Length);
                netStream.Flush(); ///OK?
            }
            catch (Exception ex)
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
