using MessengerDomain;
using ServerMessenger;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ClientMessenger
{
    public class Client
    {
        private static Socket tcpClient;
        private static Socket tcpMessageClient;
        private static NetworkStream netStream;
        private static NetworkStream netStreamMessenger;
        private static Byte[] dataRead;
        private static Byte[] dataWrite;
        private static Byte[] dataReadMessenger;
        private static ProtocolManager chatManager;
        private static string serverErrorMsg = "Server disconnected. Unable to establish connection";
        private static bool liveChatting = false;
        private static string liveChatUser = "";
        private static string fileToSend;
        private static string filesDownloadDirectory = "MessengerFiles";

        static void Main()
        {
            ConnectToServer();
        }

        private static bool connected = true;

        private static void ConnectToServer()
        {
            BeginInteractionWithServer();
            ShowMenu();

            Thread chatMessenger = new Thread(() => ProcessChatMessenger());
            chatMessenger.Start();

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
            CloseSocketsAndStreams();
        }

        private static void CloseSocketsAndStreams()
        {
            netStream.Close();
            netStreamMessenger.Close();
            tcpClient.Close();
            tcpMessageClient.Close();
            Console.Read();
        }

        private static void BeginInteractionWithServer()
        {
            try
            {
                chatManager = new ProtocolManager();
                InitializeClientConfiguration();
                netStream = new NetworkStream(tcpClient);
                netStreamMessenger = new NetworkStream(tcpMessageClient);
                fileToSend = String.Empty;
                Directory.CreateDirectory(filesDownloadDirectory);
            }
            catch (Exception)
            {
                Console.WriteLine(serverErrorMsg);
            }
        }

        private static void ProcessChatMessenger()
        {
            try
            {
                while (connected)
                    ReceiveMessagesFromServer();
            }
            catch (Exception) { }                        
        }

        private static void ReceiveMessagesFromServer()
        {
            try
            {
                ChatProtocol protocol = ReadFromMessengerClientAndGetProtocol();            
                string[] packageState = protocol.Payload.Split('$');
                string[] messageInfo = packageState[1].Split('#');
                if (messageInfo.Length >= 2)
                    ShowLiveChatMessage(messageInfo);
                else
                {
                    if (ChatData.CMD_UPLOAD_FILE.Equals(protocol.Command) && !fileToSend.Equals(String.Empty))
                        SendFile(fileToSend);
                }      
            }
            catch (Exception)
            {
                ServerConnectionLost();
            }
        }

        private static void ShowLiveChatMessage(string[] messageInfo)
        {
            string user = messageInfo[0];
            string chatType = messageInfo[1];
            string chatMessage = messageInfo[2];
            if (chatMessage.Equals(ChatData.BEGIN_LIVECHAT) || chatMessage.Equals(ChatData.ENDED_LIVECHAT))
                Console.WriteLine("> " + chatMessage);
            else
                Console.WriteLine(user + "> " + chatMessage);

            if (chatType.Equals(ChatData.LIVECHAT_END))
            {
                liveChatting = false;
                liveChatUser = "";
            }
        }

        private static ChatProtocol ReadFromMessengerClientAndGetProtocol()
        {
            dataReadMessenger = new Byte[9999];
            String responseData = String.Empty;
            Int32 bytes = netStreamMessenger.Read(dataReadMessenger, 0, dataReadMessenger.Length);
            var package = Encoding.ASCII.GetString(dataReadMessenger, 0, bytes);
            return new ChatProtocol(package);    
        }

        private static void ServerConnectionLost()
        {
            throw new Exception(serverErrorMsg);
        }

        private static void ReceiveResponseFromServer()
        {
            StreamReader reader = new StreamReader(netStream);
            try
            {               
                dataRead = new Byte[9999];
                String responseData = String.Empty;
                Int32 bytes = netStream.Read(dataRead, 0, dataRead.Length);
                var package = Encoding.ASCII.GetString(dataRead, 0, bytes);                
                ProcessResponse(package);                
            }
            catch (Exception)
            {
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
            string responseData = typeAndData[1];

            if (responseType.Equals(ChatData.RESPONSE_ERROR))
            {
                Console.WriteLine("> " + responseData);
                if (chatPackage.Command.Equals(ChatData.CMD_UPLOAD_FILE))
                    fileToSend = String.Empty;
                if (chatPackage.Command.Equals(ChatData.CMD_LIVECHAT))
                {
                    liveChatUser = String.Empty;
                    liveChatting = false;
                }
            }
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
                    if (!data.Equals(String.Empty))
                        Console.WriteLine("> " + data);
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
                case 9:
                    ShowPendingMessages(data);
                    break;
                case 10:
                    fileToSend = String.Empty;
                    Console.WriteLine("> " + data);                                          
                    break;
                case 11:
                    ProcessFilesResponse(data);
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

        private static void ProcessFilesResponse(string data)
        {
            string[] messageInfo = data.Split('#');
            string type = messageInfo[0];
            if (type.Equals(ChatData.FILES_FOR_DOWNLOAD))
            {
                ShowFilesAvailablesForDownload(messageInfo);
            }
            else
            {
                string storagePath = Path.Combine(filesDownloadDirectory, messageInfo[1]);
                int fileBytes = Int32.Parse(messageInfo[2]);
                using (Stream dest = File.OpenWrite(storagePath))
                {
                    byte[] buffer = new byte[fileBytes];
                    int bytesToRead = fileBytes;
                    int localRead = 0, recieved = 0;
                    while (bytesToRead > 0)
                    {
                        localRead = netStream.Read(buffer, recieved, bytesToRead);
                        recieved += localRead;
                        bytesToRead -= localRead;
                    }
                    dest.Write(buffer, 0, fileBytes);
                    Console.WriteLine("File download completed!");                    
                }
                
            }
        }

        private static void ShowFilesAvailablesForDownload(string[] messageInfo)
        {
            if (messageInfo[0].Length == 1)
                Console.WriteLine("> No files uploaded");
            else
                Console.WriteLine("> Uploaded files:");

            for (int i = 1; i < messageInfo.Length; i++)
                Console.WriteLine("\t" + messageInfo[i]);
        }

        private static void ShowPendingMessages(string data)
        {
            string[] messageInfo = data.Split('#');
            string type = messageInfo[0];
            if (type.Equals(ChatData.PENDING_MSGS_USERS))
                ShowUserList(messageInfo[1], "You have no pending messages to read", "Friends that left you messages:");
            else
            {
                for (int i = 1; i < messageInfo.Length; i++)                
                    Console.WriteLine(messageInfo[i]);
                
                Console.WriteLine(ChatData.UNSEEN_MESSAGES);
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
                Console.WriteLine("> " + data);            
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
                    Console.WriteLine("- " + friendInfo[0] + " has (" + friendInfo[1] + ") friends");
                }
            }           
        }

        private static void ShowMenu()
        {
            Console.WriteLine("[00] Logout\n" + "[01] Login\n" + "[02] Register & Login\n" + "[03] Online users\n" +
                "[04] Friend list\n" + "[05] Send friend request\n" + "[06] Pending friend requests\n" +
                "[07] Reply to friend requests\n" + "[08] Chat with friend\n" + "[09] Unread messages\n" +
                "[10] Upload file\n" + "[11] View and Download Files");
        }

        private static void SendRequestToServer()
        {                       
            String message = Console.ReadLine();
            ChatProtocol request = MakeChatProtocolRequest(message);
            if (request.Command.Equals(ChatData.CMD_UPLOAD_FILE))
                SendFileRequestToServer(request);
            else
                SendPackage(request);
        }

        private static void SendFileRequestToServer(ChatProtocol request)
        {
            if (!File.Exists(request.Payload))
                throw new Exception("Error: Invalid file path for upload");

            using(FileStream fileStream = new FileStream(request.Payload, FileMode.Open, FileAccess.Read))
            {
                var name = Path.GetFileName(request.Payload);
                var length = fileStream.Length;
                var command = request.Command;
                var packageName = chatManager.CreateRequestProtocol(command, name + "#" + length);
                SendPackage(packageName);
            }
            fileToSend = request.Payload;  
        }

        private static void SendFile(string path)
        {
            try
            {
                using (Stream source = File.OpenRead(path))
                {
                    byte[] buffer = new byte[2048];
                    int bytesRead;
                    while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        netStream.Write(buffer, 0, bytesRead);
                    }
                }
            }
            catch (Exception)
            {
                ServerConnectionLost();
            }
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
                }else
                {
                    if (msg.Equals(String.Empty) || msg.Contains("#") || msg.Contains("_"))
                        throw new Exception("> Error: chat messages cant be empty or contain # or _");
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
                netStream.Flush(); 
            }
            catch (Exception)
            {
                ServerConnectionLost();
            }
        }

        private static void InitializeClientConfiguration()
        {
            ConfigurationManager.RefreshSection("appSettings");
            string ServerIp = ConfigurationManager.AppSettings["Ip"];
            int ServerPort = Int32.Parse(ConfigurationManager.AppSettings["Port"]);
            string ClientIp = ConfigurationManager.AppSettings["ClientIp"];
            ConnectNetworkClient(ServerIp, ServerPort, ClientIp);
            ConnectMessageClient(ServerIp, ServerPort, ClientIp);
            Console.WriteLine("Connected to server");
        }

        private static void ConnectMessageClient(string ServerIp, int ServerPort, string ClientIp)
        {
            tcpMessageClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var messageEndPoint = new IPEndPoint(IPAddress.Parse(ClientIp), 0);
            tcpMessageClient.Bind(messageEndPoint);
            tcpMessageClient.Connect(new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort));
        }

        private static void ConnectNetworkClient(string ServerIp, int ServerPort, string ClientIp)
        {
            tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var clientEndPoint = new IPEndPoint(IPAddress.Parse(ClientIp), 0);
            tcpClient.Bind(clientEndPoint);
            tcpClient.Connect(new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort));
        }
    }
}
