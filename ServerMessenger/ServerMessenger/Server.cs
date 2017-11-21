using MessengerDomain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;
using System.Configuration;
using System.Net;
using System.Runtime;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using Persistence;
using ServerMSQM;

namespace ServerMessenger
{
    public sealed class Server
    {
        private static bool acceptingConnections;
        private static Socket tcpServer;
        private static ProtocolManager protManager;
        private static string clientFilesDirectory;
        private static Context context;
        private static LogManager serverLog;
        static readonly object _lockClientResponse = new object();

        static void Main(string[] args)
        {
            try
            {
                StartServer();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Read();
            }         
        }

        private static void StartServer()
        {        
            InitializeServerConfiguration();

            Thread serverCommandReader = new Thread(() => ReadServerCommands());
            serverCommandReader.Start();

            while (acceptingConnections) { 
                try
                {
                    Socket clientSocket = tcpServer.Accept();
                    Socket clientSocketMessenger = tcpServer.Accept();
                    var thread = new Thread(() => ClientHandler(clientSocket));
                    thread.Start();
                    context.AddActiveClient(clientSocket, thread);
                    context.AddClientMessenger(clientSocket, clientSocketMessenger);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            tcpServer.Close();
            Console.Read();
        }

        private static void InitializeServerConfiguration()
        {
            try
            {
                RegisterRemotingUserRepository();
                serverLog = new LogManager();
                context = Context.GetInstance();
                InitializeServerAttributes();
                EstablishConnection();
            }
            catch (Exception)
            {
                acceptingConnections = false;
                Console.WriteLine("Error -> Instance of the server already running"); Console.Read();
            }
        }

        private static void RegisterRemotingUserRepository()
        {
            TcpServerChannel channel = new TcpServerChannel(7777);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(UserRepository), "Users", WellKnownObjectMode.SingleCall);
        }

        private static void EstablishConnection()
        {
            tcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ConfigurationManager.RefreshSection("appSettings");
            string ip = ConfigurationManager.AppSettings["Ip"];
            int port = Int32.Parse(ConfigurationManager.AppSettings["Port"]);
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            tcpServer.Bind(serverEndPoint);
            tcpServer.Listen(ChatData.MAX_ACTIVE_CONN);
            Console.WriteLine("Start waiting for clients");
            Console.WriteLine("Connected at: " + serverEndPoint.ToString());
        }

        private static void InitializeServerAttributes()
        {
            acceptingConnections = true;
            protManager = new ProtocolManager();
            clientFilesDirectory = "UserFiles";
            Directory.CreateDirectory(clientFilesDirectory);
        }

        private static void ReadServerCommands()
        {
            while (acceptingConnections)
            {
                string command = Console.ReadLine();
                ApplyServerCommand(command);
            }            
        }

        private static void ApplyServerCommand(string command)
        {
            switch (command)
            {
                case "EXECUTE":
                    ExecuteCommand();
                    break;
                case "USERS":
                    UsersCommand();
                    break;
                case "PEAK LOGS":
                    PeakLogsCommand();
                    break;
                case "CLEAN LOGS":
                    CleanLogsCommand();
                    break;
                default:
                    Console.WriteLine("> Wrong command");
                    break;
            }   
        }

        private static void CleanLogsCommand()
        {
            serverLog.RemoveAllMessages();
            Console.WriteLine("> Removed all server logs");
        }

        private static void PeakLogsCommand()
        {
            List<string> messagesLog = serverLog.GetAllMessages();
            foreach (var msg in messagesLog)            
                Console.WriteLine(msg);            
        }

        private static void ExecuteCommand()
        {
            acceptingConnections = false;
            Console.WriteLine("> Server will close on next client request");
        }

        private static void UsersCommand()
        {
            if (context.GetUserProfiles().Count > 0)
            {
                foreach (var prof in context.GetUserProfiles())
                    Console.WriteLine("- " + prof.UserName + " friends: (" + prof.FriendsAmmount() + ") connections: (" + prof.NumberOfConnections + ")");
            }
            else
                Console.WriteLine("> No user profiles registered");
        }

        private static void CloseServerConnection()
        {
            acceptingConnections = false;
            foreach (KeyValuePair<Socket, Thread> entry in context.GetActiveClients())
            {
                ChatProtocol closeConnectionResponse = protManager.CreateResponseOkProtocol("99");
                EndClientConnection(entry.Key);
            }
            context.RemoveAllActiveClients();
        }

        private static void ClientHandler(Socket client)
        {
            NetworkStream stream = new NetworkStream(client);
            StreamReader reader = new StreamReader(stream);
            try
            {
                while (acceptingConnections)
                {
                    var sb = new StringBuilder();
                    int packageLength = protManager.ReadFixedBytesFromPackage(client, reader, ref sb);
                    protManager.ReadPayloadBytesFromPackage(client, reader, ref sb, packageLength);
                    var package = sb.ToString();
                    ChatProtocol chatMsg = new ChatProtocol(package);
                    ProcessMessage(client, chatMsg);
                }
                CloseServerConnection();
            }
            catch (Exception) { }
            Console.WriteLine("Client disconnected!");
            stream.Close();
            EndClientConnection(client);
            context.RemoveActiveClient(client);
        }

        private static void EndClientConnection(Socket client)
        {
            NotifyIfHasChattingFriend(client);
            Socket clientMessenger = GetClientMessengerSocket(client);
            context.RemoveClientAuthorization(client);
            context.RemoveClientMessenger(client, clientMessenger);        
            protManager.EndConnection(client);
            protManager.EndConnection(clientMessenger);
        }

        private static void NotifyIfHasChattingFriend(Socket client)
        {
            try
            {
                UserProfile profile = GetProfileConnectedToClient(client);
                if (profile.HasLiveChatProfileSet())
                {
                    UserProfile chattingFriend = profile.GetLiveChatProfile();
                    if (profile.IsOnLiveChat())
                    {
                        Socket clientReciever = GetAuthorizedClientFromProfile(chattingFriend);
                        SearchAndSendLiveChatMessage(clientReciever, ChatData.LIVECHAT_END, profile.UserName, ChatData.ENDED_LIVECHAT);
                        chattingFriend.UnSetLiveChatProfile();
                    }
                    profile.UnSetLiveChatProfile();
                }
            }
            catch (Exception) { }            
        }

        private static Socket GetClientMessengerSocket(Socket client)
        {
            return context.GetClientMessenger(client);
        }

        private static void ProcessMessage(Socket client, ChatProtocol chatMsg)
        {
            Console.WriteLine("Receive: CMD [" + chatMsg.Command + "] Requested by [" + client.RemoteEndPoint + "]");
            try
            {
                ValidateClientHeader(chatMsg);
                ProcessByProtocolCommmand(client, chatMsg);
            }
            catch (Exception ex)
            {
                ChatProtocol response = protManager.CreateResponseErrorProtocol(chatMsg.Command, ex.Message);
                NotifyClientWithPackage(client, response.Package);
            }
            Console.WriteLine("Response to: [" + client.RemoteEndPoint + "] of CMD [" + chatMsg.Command + "]");
        }

        private static void ProcessByProtocolCommmand(Socket client, ChatProtocol chatMsg)
        {
            switch (chatMsg.GetCommandNumber())
            {
                case 0:
                    ProcessLogoutRequest(client, chatMsg);
                    break;
                case 1:
                    ProcessLoginRequest(client, chatMsg);
                    break;
                case 2:
                    ProcessRegisterRequest(client, chatMsg);
                    break;
                case 3:
                    ProcessConnectedUsersRequest(client, chatMsg);
                    break;
                case 4:
                    ProcessFriendListRequest(client, chatMsg);
                    break;
                case 5:
                    ProcessSendFriendRequest(client, chatMsg);
                    break;
                case 6:
                    ProcessPendingFriendRequestsView(client, chatMsg);
                    break;
                case 7:
                    ProcessReplyToFriendRequest(client, chatMsg);
                    break;
                case 8:
                    ProcessChatModeRequest(client, chatMsg);
                    break;
                case 9:
                    ProcessUnseenMessagesRequest(client, chatMsg);
                    break;
                case 10:
                    ProcessUploadFileRequest(client, chatMsg);
                    break;
                case 11:
                    ProcessViewAndDownloadFileRequest(client, chatMsg);
                    break;
                default:
                    throw new Exception("Error: Unidentified command");
            }
        }

        private static void ProcessViewAndDownloadFileRequest(Socket client, ChatProtocol chatMsg)
        {
            ValidateClientConnected(client, "view or download files");
            if (chatMsg.Payload.Equals(String.Empty))
            {
                ChatProtocol response = protManager.CreateResponseOkProtocol(chatMsg.Command, GenerateStoredFilesList());
                NotifyClientWithPackage(client, response.Package);
            }
            else
            {
                string selectedStoredFile = Path.Combine(clientFilesDirectory, chatMsg.Payload);
                if (!File.Exists(selectedStoredFile))
                    throw new Exception("Error: Selected file name doesnt exists");

                string responsePayload = ChatData.FILE_TO_DOWNLOAD + "#" + chatMsg.Payload + "#" + File.ReadAllBytes(selectedStoredFile).Length;
                ChatProtocol response = protManager.CreateResponseOkProtocol(chatMsg.Command, responsePayload);
                NotifyClientWithPackage(client, response.Package);
                using (NetworkStream netStream = new NetworkStream(client))
                {
                    using (Stream source = File.OpenRead(selectedStoredFile))
                    {
                        byte[] buffer = new byte[2048];
                        int bytesRead;
                        while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            netStream.Write(buffer, 0, bytesRead);
                        }
                    }
                }
                Console.WriteLine("File Sent!");
            }

        }

        private static string GenerateStoredFilesList()
        {
            string filesUploaded = ChatData.FILES_FOR_DOWNLOAD;
            var storedFiles = Directory.EnumerateFiles(clientFilesDirectory);
            if (storedFiles.Count() != 0)
                filesUploaded += "#";
            foreach (string file in storedFiles)
            {
                filesUploaded += Path.GetFileName(file);
                if (!storedFiles.Last().Equals(file))
                    filesUploaded += "#";
            }

            return filesUploaded;
        }

        private static void ProcessUploadFileRequest(Socket client, ChatProtocol chatMsg)
        {
            ValidateClientConnected(client, "upload files");

            var fileNameAndLength = chatMsg.Payload.Split('#');
            string fileName = fileNameAndLength[0];
            int fileBytes = Int32.Parse(fileNameAndLength[1]);
            string storagePath = Path.Combine(clientFilesDirectory, fileName);
            if (File.Exists(storagePath))
                throw new Exception("Error: Already exists file uploaded with that name");

            NotifyClientInstantlyWithOkResponse(client, chatMsg);
            UploadFileToServerDirectory(client, fileBytes, storagePath);
            ChatProtocol responseAfterUpload = protManager.CreateResponseOkProtocol(chatMsg.Command, "File upload completed!");
            NotifyClientWithPackage(client, responseAfterUpload.Package);
        }

        private static void ValidateClientConnected(Socket client, string command)
        {
            if (!ClientIsConnected(client))
                throw new Exception("Error: To "+ command +", login first");
        }

        private static void NotifyClientInstantlyWithOkResponse(Socket client, ChatProtocol chatMsg)
        {
            Socket messengerClient = GetClientMessengerSocket(client);
            ChatProtocol responseForSend = protManager.CreateResponseOkProtocol(chatMsg.Command);
            NotifyClientWithPackage(messengerClient, responseForSend.Package);
        }

        private static void UploadFileToServerDirectory(Socket client, int fileBytes, string storagePath)
        {
            using (NetworkStream netStreamClient = new NetworkStream(client))
            {
                using (Stream dest = File.OpenWrite(storagePath))
                {
                    byte[] buffer = new byte[fileBytes];
                    int bytesToRead = fileBytes;
                    int localRead = 0, recieved = 0;
                    while (bytesToRead > 0)
                    {
                        localRead = netStreamClient.Read(buffer, recieved, bytesToRead);
                        recieved += localRead;
                        bytesToRead -= localRead;
                    }
                    dest.Write(buffer, 0, fileBytes);
                    Console.WriteLine("File upload completed!");
                }
            }
        }

        private static void ProcessUnseenMessagesRequest(Socket client, ChatProtocol chatMsg)
        {
            if(!ClientIsConnected(client))
                throw new Exception("Error: To read pending messages, login first");

            UserProfile profile = GetProfileConnectedToClient(client);
            string profileForReading = chatMsg.Payload;
            if (!profileForReading.Equals(String.Empty))
            {
                if (!ProfileUserNameExists(profileForReading))
                    throw new Exception("Error: Profile doesn't exists");
                RespondWithPendingMessagesFromProfile(client, profile, profileForReading);
            }
            else
                RespondWithProfilesOfPendingMessages(client, chatMsg, profile);            
        }

        private static void RespondWithPendingMessagesFromProfile(Socket client, UserProfile profile, string profileForReading)
        {
            UserProfile friendWithMessages = GetStoredUserProfile(profileForReading);
            var messages = profile.GetPendingMessagesOfFriend(friendWithMessages);

            if (messages.Count == 0)
                throw new Exception("Error: That user didnt left you messages");

            var responseMessage = "";
            foreach (var chatLog in messages)
            {
                responseMessage += "[" + chatLog.Item1 + "] " + chatLog.Item2;
                if (!messages.Last().Equals(chatLog))
                    responseMessage += "#";
            }

            ChatProtocol response = protManager.CreateUnseenMessagesResponseProtocol(ChatData.PENDING_MSGS_PROFILE_MSGS, responseMessage);
            NotifyClientWithPackage(client, response.Package);
            profile.RemovePendingMessagesOfFriend(friendWithMessages);
        }

        private static void RespondWithProfilesOfPendingMessages(Socket client, ChatProtocol chatMsg, UserProfile profile)
        {
            var profilesWithMessages = profile.GetProfilesOfPendingMessages().Select(p => p.UserName);
            string payload = "";
            foreach(var msg in profilesWithMessages)
            {
                payload += msg;
                if (!profilesWithMessages.Last().Equals(msg))
                    payload += "#";
            }
            ChatProtocol response = protManager.CreateUnseenMessagesResponseProtocol(ChatData.PENDING_MSGS_USERS, payload);
            NotifyClientWithPackage(client, response.Package);
        }

        private static void ProcessChatModeRequest(Socket client, ChatProtocol chatMsg)
        {
            string chatProfile = "";
            string chatType = "";
            string chatMessage = "";
            string[] msgInfo = chatMsg.Payload.Split('#');
            if (msgInfo.Length >= 2)
            {
                chatProfile = msgInfo[0];
                chatType = msgInfo[1];
                if(msgInfo.Length.Equals(3))
                    chatMessage = msgInfo[2];
            }
            CheckLiveChatMessageInfo(client, chatProfile, chatType);
            UserProfile senderProf = GetProfileConnectedToClient(client);
            UserProfile recieverProf = GetStoredUserProfile(chatProfile);

            if (chatType.Equals(ChatData.LIVECHAT_CHAT))
            {
                if (ProfileIsConnectedToAClient(chatProfile))                
                    OnlineChat(client, chatMessage, senderProf, recieverProf);                
                else                
                    OfflineChat(client, chatMessage, senderProf, recieverProf);                
            }
            else
            {
                if (ProfileIsConnectedToAClient(chatProfile))
                    EndOnlineChat(client, senderProf, recieverProf);
                else
                    EndOfflineChat(client, senderProf, recieverProf);                
            }
        
        }

        private static void EndOnlineChat(Socket client, UserProfile senderProf, UserProfile recieverProf)
        {
            Socket recieverClient = GetAuthorizedClientFromProfile(recieverProf);
            senderProf.UnSetLiveChatProfile();
            ChatProtocol response = protManager.CreateLiveChatResponseProtocol(recieverProf.UserName, ChatData.LIVECHAT_END, ChatData.ENDED_LIVECHAT);
            NotifyClientWithPackage(client, response.Package);
            if (ProfilesAreEquals(recieverProf.PendingLiveChatProfile(), senderProf)){
                recieverProf.UnSetLiveChatProfile();
                SearchAndSendLiveChatMessage(recieverClient, ChatData.LIVECHAT_END, senderProf.UserName, ChatData.ENDED_LIVECHAT);
            }
            serverLog.SendMessage("End online chat", senderProf.UserName, "Success. Online chat ended with "+recieverProf.UserName);            
        }

        private static Socket GetAuthorizedClientFromProfile(UserProfile profile)
        {
            return context.GetClientFromProfile(profile);
        }

        private static void OnlineChat(Socket client, string chatMessage, UserProfile senderProf, UserProfile recieverProf)
        {
            Socket recieverClient = GetAuthorizedClientFromProfile(recieverProf);
            ValidateChatModeInformation(client, senderProf, recieverProf);

            if (recieverProf.HasLiveChatProfileSet()) 
            {
                if (ProfilesAreEquals(senderProf, recieverProf.PendingLiveChatProfile()))
                {
                    if (recieverProf.IsOnLiveChat())                    
                        ApplyLiveChatMessage(client, chatMessage, senderProf, recieverProf, recieverClient);                    
                    else                    
                        BeginLiveChatBetweenClients(client, senderProf, recieverProf, recieverClient);                    
                }
                else                
                    ApplyWaitingOrCancelOnBusyClient(client, senderProf, recieverProf);                
            }
            else            
                SetLiveChatProfileAndWaitForClient(client, senderProf, recieverProf);            
        }

        private static void ApplyWaitingOrCancelOnBusyClient(Socket client, UserProfile senderProf, UserProfile recieverProf)
        {
            senderProf.SetLiveChatProfile(recieverProf, false);
            ChatProtocol senderResponse = protManager.CreateLiveChatResponseProtocol(recieverProf.UserName, ChatData.LIVECHAT_CHAT, recieverProf.UserName + " is chatting with other user. Wait or cancel chat mode");
            NotifyClientWithPackage(client, senderResponse.Package);
            serverLog.SendMessage("Live chat", senderProf.UserName, "Waiting for " + recieverProf.UserName + " to accept live chat");
        }

        private static void SetLiveChatProfileAndWaitForClient(Socket client, UserProfile senderProf, UserProfile recieverProf)
        {
            senderProf.SetLiveChatProfile(recieverProf, false);
            ChatProtocol senderResponse = protManager.CreateLiveChatResponseProtocol(recieverProf.UserName, ChatData.LIVECHAT_CHAT, "Waiting for: " + recieverProf.UserName + " to chat with you...");
            NotifyClientWithPackage(client, senderResponse.Package);
            serverLog.SendMessage("Live chat", senderProf.UserName, "Waiting for " + recieverProf.UserName + " to accept live chat");
        }

        private static void ApplyLiveChatMessage(Socket client, string chatMessage, UserProfile senderProf, UserProfile recieverProf, Socket recieverClient)
        {
            SearchAndSendLiveChatMessage(recieverClient, ChatData.LIVECHAT_CHAT, senderProf.UserName, chatMessage);
            ChatProtocol senderResponse = protManager.CreateLiveChatResponseProtocol(recieverProf.UserName, ChatData.LIVECHAT_CHAT, "");
            NotifyClientWithPackage(client, senderResponse.Package);
            serverLog.SendMessage("Live chat", senderProf.UserName, "Live message sent to " + recieverProf.UserName);
        }

        private static void SearchAndSendLiveChatMessage(Socket recieverClient, string messageType, string sender, string message)
        {
            Socket messengerClient = GetClientMessengerSocket(recieverClient);
            ChatProtocol response = protManager.CreateLiveChatResponseProtocol(sender, messageType, message);
            NotifyClientWithPackage(messengerClient, response.Package);
        }

        private static void BeginLiveChatBetweenClients(Socket client, UserProfile senderProf, UserProfile recieverProf, Socket recieverClient)
        {
            senderProf.SetLiveChatProfile(recieverProf, true);
            recieverProf.SetLiveChatProfile(senderProf, true);

            Socket recieverMessenger = GetClientMessengerSocket(recieverClient);
            SearchAndSendLiveChatMessage(recieverClient, ChatData.LIVECHAT_CHAT, senderProf.UserName, ChatData.BEGIN_LIVECHAT);
            ChatProtocol senderResponse = protManager.CreateLiveChatResponseProtocol(recieverProf.UserName, ChatData.LIVECHAT_CHAT, ChatData.BEGIN_LIVECHAT);
            NotifyClientWithPackage(client, senderResponse.Package);
            serverLog.SendMessage("Live chat", senderProf.UserName, "Success. Begin live chat with " + recieverProf.UserName);
        }

        private static void EndOfflineChat(Socket client, UserProfile senderProf, UserProfile recieverProf)
        {
            senderProf.UnSetLiveChatProfile();
            ChatProtocol response = protManager.CreateLiveChatResponseProtocol(recieverProf.UserName, ChatData.LIVECHAT_END, ChatData.ENDED_LIVECHAT);
            NotifyClientWithPackage(client, response.Package);
            serverLog.SendMessage("End offline chat", senderProf.UserName, "Success. Offline chat ended with " + recieverProf.UserName);
        }

        private static void OfflineChat(Socket client, string chatMessage, UserProfile senderProf, UserProfile recieverProf)
        {
            if (!senderProf.IsFriendWith(recieverProf))
                throw new Exception("Error: The user is not friend with you");

            if (!senderProf.HasLiveChatProfileSet())            
                BeginOfflineChat(client, senderProf, recieverProf);            
            else
            {
                if (ProfilesAreEquals(senderProf.PendingLiveChatProfile(), recieverProf))                
                    SaveChatLogForOfflineUser(client, chatMessage, senderProf, recieverProf);                
                else
                    throw new Exception("Error: You can only enter chat mode with one friend");
            }
        }

        private static void SaveChatLogForOfflineUser(Socket client, string chatMessage, UserProfile senderProf, UserProfile recieverProf)
        {
            recieverProf.AddPendingMessage(senderProf, chatMessage);
            ChatProtocol response = protManager.CreateLiveChatResponseProtocol(recieverProf.UserName, ChatData.LIVECHAT_CHAT, "");
            NotifyClientWithPackage(client, response.Package);
            serverLog.SendMessage("Offline chat", senderProf.UserName, "Success. Added pending message to " + recieverProf.UserName);
        }

        private static void BeginOfflineChat(Socket client, UserProfile senderProf, UserProfile recieverProf)
        {
            senderProf.SetLiveChatProfile(recieverProf, false);
            ChatProtocol response = protManager.CreateLiveChatResponseProtocol(recieverProf.UserName, ChatData.LIVECHAT_CHAT, "Begin chat... He will recieve your messages when he connects");
            NotifyClientWithPackage(client, response.Package);
            serverLog.SendMessage("Offline chat", senderProf.UserName, "Success. Begin offline chat with " + recieverProf.UserName);
        }

        private static void CheckLiveChatMessageInfo(Socket client, string chatProfile, string chatType)
        {
            if (!ClientIsConnected(client))
                throw new Exception("Error: To enter chat mode you must be logged in");
            if (!ProfileUserNameExists(chatProfile))
                throw new Exception("Error: Username doesnt exists");
            if (!chatType.Equals(ChatData.LIVECHAT_END) && !chatType.Equals(ChatData.LIVECHAT_CHAT))
                throw new Exception("Error: Wrong chat mode state. Must be CHAT or END");
        }   

        private static void ValidateChatModeInformation(Socket client, UserProfile sender, UserProfile reciever)
        {
            if (!sender.IsFriendWith(reciever))
                throw new Exception("Error: The user is not friend with you");
            if (sender.HasLiveChatProfileSet() && !ProfilesAreEquals(sender.PendingLiveChatProfile(), reciever))
                throw new Exception("Error: You can only enter chat mode with one friend");
        }

        private static void ProcessReplyToFriendRequest(Socket client, ChatProtocol protocol)
        {
            Tuple<string, string> clientMessage = ExtractFriendRequestReply(protocol.Payload);
            string username = clientMessage.Item1;
            string reply = clientMessage.Item2;
            ValidateFriendRequestReplyInformation(client, username, reply);

            UserProfile clientProfile = GetProfileConnectedToClient(client);
            UserProfile profileToReply = GetStoredUserProfile(username);

            bool accept = (MessageIsFriendRequestReply(reply, ChatData.FRIEND_REQUEST_YES_REPLY)) ? true : false;
            clientProfile.ReplyFriendRequest(profileToReply, accept);
            if (accept)
            {
                profileToReply.AddFriend(clientProfile);
                serverLog.SendMessage("Friend Request reply", clientProfile.UserName, "Accepted friend request from " + profileToReply.UserName);
            }
            else
            {
                serverLog.SendMessage("Friend Request reply",clientProfile.UserName, "Declined friend request from " + profileToReply.UserName);
            }
            ChatProtocol response = protManager.CreateResponseOkProtocol(protocol.Command);
            NotifyClientWithPackage(client, response.Package);

        }

        private static void ValidateFriendRequestReplyInformation(Socket client, string username, string reply)
        {
            if (!ClientIsConnected(client))
                throw new Exception("Error: To reply to a friend request you must be logged in");
            if (!ProfileUserNameExists(username))
                throw new Exception("Error: Profile username doesn't exists");
            if (!ChatData.acceptedFriendRequestReply.Any(rep => MessageIsFriendRequestReply(rep, reply)))
                throw new Exception("Error: Invalid friend request reply");
        }

        private static bool MessageIsFriendRequestReply(string s1, string s2)
        {
            return s1.ToUpper().Equals(s2.ToUpper());
        }

        private static Tuple<string, string> ExtractFriendRequestReply(string payload)
        {
            var clientMessage = payload.Split('#');
            if (clientMessage.Length != 2)
                throw new Exception("Error: Wrong friend request reply format");

            string user = clientMessage[0];
            string reply = clientMessage[1];
            return new Tuple<string, string>(user, reply);
        }

        private static void ProcessPendingFriendRequestsView(Socket client, ChatProtocol protocol)
        {
            ValidatePendingFRViewInformation(client, protocol);
            string payload = GeneratePendingFriendRequestsPayload(client);
            ChatProtocol response = protManager.CreateResponseOkProtocol(protocol.Command, payload);
            NotifyClientWithPackage(client, response.Package);
            UserProfile profile = GetProfileConnectedToClient(client);
            serverLog.SendMessage("Pending friend requests view", profile.UserName, "Server response with user pending friend requests");
        }

        private static string GeneratePendingFriendRequestsPayload(Socket client)
        {
            string pendingFriendRequests = "";
            UserProfile profile = GetProfileConnectedToClient(client);
            foreach (var prof in profile.GetPendingFriendRequest())
            {
                pendingFriendRequests += prof.UserName;
                if (!ProfilesAreEquals(profile.GetPendingFriendRequest().Last(), prof))
                    pendingFriendRequests += "#";
            }
            return pendingFriendRequests;
        }

        private static bool ProfilesAreEquals(UserProfile profile1, UserProfile profile2)
        {
            return profile1.UserName.Equals(profile2.UserName);
        }

        private static void ValidatePendingFRViewInformation(Socket client, ChatProtocol chatMsg)
        {
            if (!ClientIsConnected(client))
                throw new Exception("Error: To visualize pending requests login first");
            if (!EmptyProtocolPayload(chatMsg))
                throw new Exception("Error: Pending friend requests command must be written alone");
        }

        private static void ProcessSendFriendRequest(Socket client, ChatProtocol protocol)
        {
            string userNameRequest = protocol.Payload;
            ValidateFriendRequestInformation(client, userNameRequest);

            UserProfile sender = GetProfileConnectedToClient(client);
            UserProfile reciever = GetStoredUserProfile(userNameRequest);
            ApplyFriendRequest(sender, reciever);
            ChatProtocol responseProtocol;
            if (sender.IsFriendWith(reciever))
            {
                responseProtocol = protManager.CreateResponseOkProtocol(protocol.Command, "Now friends with " + reciever.UserName);
                serverLog.SendMessage("Send friend request", sender.UserName, "Success. Now friends with " + reciever.UserName);
            }
            else
            {
                responseProtocol = protManager.CreateResponseOkProtocol(protocol.Command);
                serverLog.SendMessage("Send friend request", sender.UserName, "Success. Friend request sent to " + reciever.UserName);
            }
            NotifyClientWithPackage(client, responseProtocol.Package);
            
        }
        private static UserProfile GetStoredUserProfile(string username)
        {
            return context.GetUserByName(username);
        }

        private static void ValidateFriendRequestInformation(Socket client, string userNameRequest)
        {
            if (!ClientIsConnected(client))
                throw new Exception("Error: To send friend request login first"); 
            if (!ProfileUserNameExists(userNameRequest))
                throw new Exception("Error: User profile not registered");
        }

        private static void ApplyFriendRequest(UserProfile sender, UserProfile reciever)
        {
            ValidateFriendRequestApplication(sender, reciever);

            if (sender.IsFriendRequestedBy(reciever.UserName))
            {
                sender.AcceptFriendRequest(reciever);
                reciever.AddFriendRequest(sender);
                reciever.AcceptFriendRequest(sender);
            }
            else
            {
                reciever.AddFriendRequest(sender);
            }
        }

        private static void ValidateFriendRequestApplication(UserProfile sender, UserProfile reciever)
        {
            if (!DistinctUserProfiles(sender, reciever))
                throw new Exception("Error: Can't send friend request to yourself");
            if (sender.IsFriendWith(reciever.UserName))
                throw new Exception("Error: Already friends with " + reciever.UserName);
        }

        private static void ProcessFriendListRequest(Socket client, ChatProtocol protocol)
        {
            ValidateFriendListInformation(client, protocol);
            string payload = GenerateFriendListPayload(client);
            ChatProtocol responseProtocol = protManager.CreateResponseOkProtocol(protocol.Command, payload);
            NotifyClientWithPackage(client, responseProtocol.Package);
            UserProfile profile = GetProfileConnectedToClient(client);
            serverLog.SendMessage("Friend list view", profile.UserName, "Server response with user friend list");
        }

        private static void ValidateFriendListInformation(Socket client, ChatProtocol protocol)
        { 
            if (!ClientIsConnected(client))
                throw new Exception("Error: To see friend list login first");
            if (!EmptyProtocolPayload(protocol))
                throw new Exception("Error: Friend list command must be written alone");
        }

        private static bool EmptyProtocolPayload(ChatProtocol protocol)
        {
            return protocol.Payload.Equals(String.Empty);
        }

        private static string GenerateFriendListPayload(Socket client)
        {
            string friendlist = "";
            UserProfile profile = GetProfileConnectedToClient(client);
            foreach (var prof in profile.GetFriends())
            {
                friendlist += prof.UserName + "_" + prof.FriendsAmmount();               
                if (!ProfilesAreEquals(profile.GetFriends().Last(), prof))
                    friendlist += "#";
            }
            return friendlist;
        }

        private static void ProcessConnectedUsersRequest(Socket client, ChatProtocol protocol)
        {
            ValidateOnlineUsersInformation(client, protocol);
            string payload = GenerateConnectedUsersPayload(client);
            ChatProtocol responseProtocol = protManager.CreateResponseOkProtocol(protocol.Command, payload);
            NotifyClientWithPackage(client, responseProtocol.Package);
            UserProfile profile = GetProfileConnectedToClient(client);
            serverLog.SendMessage("Connected users", profile.UserName, "Server response with online users");
        }

        private static void ValidateOnlineUsersInformation(Socket client, ChatProtocol protocol)
        {
            if (!context.GetAuthorizedClients().ContainsKey(client))
                throw new Exception("Error: To see online users you must be logged in");
            if (!protocol.Payload.Equals(String.Empty))
                throw new Exception("Error: Online users command must be written alone");
        }
        private static UserProfile GetProfileConnectedToClient(Socket client)
        {
            return context.GetProfileFromClient(client);
        }

        private static string GenerateConnectedUsersPayload(Socket client)
        {
            string connectedUsers = "";
            UserProfile clientProf, profile, lastProfile;
            List<UserProfile> onlineProf;            
            onlineProf = context.GetAuthorizedClients().Select(d => d.Value).ToList();
            clientProf = GetProfileConnectedToClient(client);
            lastProfile = onlineProf.Last();
            for (int index = 0; index < onlineProf.Count; index++)
            {
                profile = onlineProf[index];
                if (DistinctUserProfiles(clientProf, profile))
                {
                    connectedUsers += profile.UserName + "_" + profile.FriendsAmmount() + "_" + profile.NumberOfConnections + "_" + profile.GetSessionDuration();
                    if (DistinctUserProfiles(profile, lastProfile) && !(IndexIsPenultimate(onlineProf.IndexOf(lastProfile), index) && !DistinctUserProfiles(clientProf, lastProfile)))
                        connectedUsers += "#";
                }
            }
            return connectedUsers;
        }

        private static bool IndexIsPenultimate(int listSize, int index)
        {
            return (index == listSize - 1);
        }

        private static bool DistinctUserProfiles(UserProfile clientProfile, UserProfile loggedProf)
        {
            return !loggedProf.UserName.Equals(clientProfile.UserName);
        }

        private static void ProcessLogoutRequest(Socket client, ChatProtocol protocol)
        {
            LogoutVerification(client, protocol);
            UserProfile toLogout = context.GetProfileFromClient(client);
            context.RemoveClientAuthorization(client);
            ChatProtocol response = protManager.CreateResponseOkProtocol(protocol.Command);
            NotifyClientWithPackage(client, response.Package);
            serverLog.SendMessage("User logout", toLogout.UserName, "Logged out successfully");
        }

        private static void LogoutVerification(Socket client, ChatProtocol protocol)
        {
            if (!ClientIsConnected(client))
                throw new Exception("Error: Client is not logged in");
            if (!EmptyProtocolPayload(protocol))
                throw new Exception("Error: Logout command must be written alone");
        }

        private static void ProcessRegisterRequest(Socket client, ChatProtocol protocol)
        {
            if (ClientIsConnected(client))
                throw new Exception("Error: Logout before registering a new user");

            string errorMsg = "Wrong register parameters separated by '#'";
            var profileAccessInfo = ExtractUserProfileAccessInfo(protocol.Payload, errorMsg);
            ValidateRegisterInformation(client, profileAccessInfo.Item1, profileAccessInfo.Item2, protocol);
        }

        private static void ValidateRegisterInformation(Socket client, string user, string password, ChatProtocol protocol)
        {
            if (ProfileUserNameExists(user))
                throw new Exception("Error: Profile username already registered");

            CreateProfileAndLogin(client, user, password);
            ChatProtocol response = protManager.CreateResponseOkProtocol(protocol.Command);
            NotifyClientWithPackage(client, response.Package);
            serverLog.SendMessage("User registration", user, "Registration successfully");    
        }

        private static void CreateProfileAndLogin(Socket client, string user, string password)
        {
            UserProfile profile = new UserProfile(user, password);
            context.AddUserProfile(profile);
            context.AddClientAuthorization(client, profile);
        }

        private static void ProcessLoginRequest(Socket client, ChatProtocol protocol)
        {
            string errorMsg = "Wrong login parameters separated by '#' ";
            var profileAccessInfo = ExtractUserProfileAccessInfo(protocol.Payload, errorMsg);
            ValidateLoginInformation(client, profileAccessInfo.Item1, profileAccessInfo.Item2, protocol);
        }
        private static Tuple<string,string> ExtractUserProfileAccessInfo(string payload, string errorMsg)
        {
            var userProfileAttributes = payload.Split('#');
            if (userProfileAttributes.Length != 2)
                throw new Exception(errorMsg);
            string user = userProfileAttributes[0];
            string password = userProfileAttributes[1];
            ValidateRegisterParameter(user);
            ValidateRegisterParameter(password);
            return new Tuple<string, string>(user, password);
        }

        private static void ValidateRegisterParameter(string param)
        {
            if (param.Contains('$') || param.Contains('_'))
                throw new Exception("Error: Names and passwords can't contain the specified symbol");
        }

        private static void ValidateLoginInformation(Socket client, string user, string password, ChatProtocol protocol)
        {
            LoginVerification(client, user);            
            LoginClientAsUserProfile(client, user, password);
            ChatProtocol response = protManager.CreateResponseOkProtocol(protocol.Command);
            NotifyClientWithPackage(client, response.Package);
            serverLog.SendMessage("User login", user, "Logged in successfully");
        }

        private static void NotifyClientWithPackage(Socket client, string resPackage)
        {
            try
            {
                lock (_lockClientResponse)
                {
                    Byte[] responseBuffer = Encoding.ASCII.GetBytes(resPackage);
                    NetworkStream stream = new NetworkStream(client);
                    stream.Write(responseBuffer, 0, responseBuffer.Length);
                    stream.Flush();
                }               

            }catch(Exception)
            {
                Console.WriteLine("Couldn't send response to -> " + client.RemoteEndPoint);
            }           
        }

        private static bool ClientIsConnected(Socket client)
        {
            return context.GetAuthorizedClients().ContainsKey(client);
        }

        private static void LoginClientAsUserProfile(Socket client, string user, string password)
        {
            var profile = ExtractProfileFromAttributes(user, password);
            context.AddClientAuthorization(client, profile);
            profile.NewConnectionMade();
        }

        private static void LoginVerification(Socket client, string user)
        {
            if (ClientIsConnected(client))
                throw new Exception("Error: Client already logged in");
            if (!ProfileUserNameExists(user))
                throw new Exception("Error: Profile username nonexistent");
            if (ProfileIsConnectedToAClient(user))
                throw new Exception("Error: Profile already logged in");
        }

        private static bool ProfileIsConnectedToAClient(string user)
        {
            return context.GetAuthorizedClients().Any(auth => auth.Value.UserName.Equals(user));
        }

        private static bool ProfileUserNameExists(string user)
        {
           return context.GetUserProfiles().Exists(us => us.UserName.Equals(user));
        }

        private static UserProfile ExtractProfileFromAttributes(string user, string password)
        {
           UserProfile profile = context.GetUserProfiles().Find(prof => prof.UserName.Equals(user) && prof.Password.Equals(password));
           if (profile == null)
               throw new Exception("Error: Incorrect profile password");
           return profile;            
        }

        private static void ValidateClientHeader(ChatProtocol chatMsg)
        {
            if (!chatMsg.Header.Equals(ChatData.REQUEST_HEADER))
                throw new Exception("Error: Wrong client header");
        }
    }
}

