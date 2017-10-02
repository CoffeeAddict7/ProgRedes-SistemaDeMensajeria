using MessengerDomain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;

namespace ServerMessenger
{
    public class Server
    {
        private static bool acceptingConnections;
        private static Dictionary<Socket, Thread> activeClientThreads;
        private static Socket tcpServer;
        private static ProtocolManager chatManager;
        private static List<UserProfile> storedUserProfiles;
        private static Dictionary<Socket,UserProfile> authorizedClients;
        private static List<KeyValuePair<Socket, Socket>> clientAndMessanger;

        static void Main(string[] args)
        {
            try
            {
                LoadPersistenceStructures();
                LoadUserProfiles();
                StartServer();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Read();

            }
         
        }

        private static void LoadPersistenceStructures()
        {
            storedUserProfiles = new List<UserProfile>();
            authorizedClients = new Dictionary<Socket, UserProfile>();
        }

        private static void LoadUserProfiles()
        {
            UserProfile luis = new UserProfile("LUIS", "PEPE");
            UserProfile jose = new UserProfile("JOSE", "PEPE");
            luis.AddFriend(jose);
            jose.AddFriend(luis);
            storedUserProfiles.Add(luis);
            storedUserProfiles.Add(jose);
        }
        static readonly object _lock = new object();

        private static void StartServer()
        {        
            InitializeServerConfiguration();
            
            while (acceptingConnections) { 
                try
                {
                    Socket clientSocket = tcpServer.Accept();
                    Socket clientSocketMessenger = tcpServer.Accept();
                    
                    var thread = new Thread(() => ClientHandler(clientSocket));
                    thread.Start();

                    activeClientThreads.Add(clientSocket, thread);
                    clientAndMessanger.Add(new KeyValuePair<Socket, Socket>(clientSocket, clientSocketMessenger));
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
                acceptingConnections = true;
                activeClientThreads = new Dictionary<Socket, Thread>();
                clientAndMessanger = new List<KeyValuePair<Socket, Socket>>();
                chatManager = new ProtocolManager();
                tcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                tcpServer.Bind(ChatData.SERVER_IP_END_POINT);
                tcpServer.Listen(ChatData.MAX_ACTIVE_CONN);
                Console.WriteLine("Start waiting for clients");
            }
            catch (Exception)
            {
                acceptingConnections = false;
                Console.WriteLine("Error -> Instance of the server already running"); Console.Read();
            }
        }

        private static void CloseServerConnection()
        {
            acceptingConnections = false;
            foreach (KeyValuePair<Socket, Thread> entry in activeClientThreads)
            {
                ChatProtocol closeConnectionResponse = chatManager.CreateResponseOkProtocol("99");
                NotifyClientWithPackage(entry.Key, closeConnectionResponse.Package);
                authorizedClients.Remove(entry.Key);
                chatManager.EndConnection(entry.Key);
            }
            activeClientThreads = new Dictionary<Socket, Thread>();
        }

        private static void ClientHandler(Socket client)
        {
            NetworkStream stream = new NetworkStream(client);
            StreamReader reader = new StreamReader(stream);
            try
            {
                while (acceptingConnections && !ServerExecuteCommand())
                {
                    var sb = new StringBuilder();
                    int packageLength = chatManager.ReadFixedBytesFromPackage(client, reader, ref sb);
                    chatManager.ReadPayloadBytesFromPackage(client, reader, ref sb, packageLength);
                    
                    var package = sb.ToString();
                    ChatProtocol chatMsg = new ChatProtocol(package);
                    ProcessMessage(client, chatMsg);
                                             
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client disconnected!");
                authorizedClients.Remove(client);
                activeClientThreads.Remove(client);
            }
            stream.Close();
            //chatManager.EndConnection(client);
        }

        private static bool ServerExecuteCommand()
        {
            if(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.X)
            {
                Console.WriteLine("X Pressed");
                acceptingConnections = false;
                return true;
            }
            return false;
        }

        private static void ProcessMessage(Socket client, ChatProtocol chatMsg)
        {
            try
            {
                ValidateClientHeader(chatMsg);
                ProcessByProtocolCommmand(client, chatMsg);
                Console.WriteLine("Response to: [" + client.RemoteEndPoint + "] of CMD [" + chatMsg.Command + "]");
            }
            catch (Exception ex)
            {
                ChatProtocol response = chatManager.CreateResponseErrorProtocol(chatMsg.Command, ex.Message);
                NotifyClientWithPackage(client, response.Package);
                Console.WriteLine("Response to: [" + client.RemoteEndPoint + "] of CMD [" + chatMsg.Command + "] With ERROR");
            }            
        }

        private static void ProcessByProtocolCommmand(Socket client, ChatProtocol chatMsg)
        {
            Console.WriteLine("Receive: CMD ["+ chatMsg.Command + "] Requested by ["+ client.RemoteEndPoint+ "]");
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
                default:
                    throw new Exception("Error: Unidentified command");
            }
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
            UserProfile senderProf = authorizedClients.First(auth => ClientsAreEquals(auth.Key, client)).Value;
            UserProfile recieverProf = storedUserProfiles.First(prof => prof.UserName.Equals(chatProfile));

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
                {
                    // EndOnlineChat(client, senderProf, recieverProf)
                }else                
                    EndOfflineChat(client, senderProf, recieverProf);                
            }
        
        }

        private static void OnlineChat(Socket client, string chatMessage, UserProfile senderProf, UserProfile recieverProf)
        {
            Socket recieverClient = authorizedClients.First(auth => ProfilesAreEquals(auth.Value, recieverProf)).Key;
            ValidateChatModeInformation(client, senderProf, recieverProf);

            if (recieverProf.HasLiveChatProfileSet()) 
            {
                if (ProfilesAreEquals(senderProf, recieverProf.PendingLiveChatProfile()))
                {
                    if (recieverProf.IsOnLiveChat())
                    {
                        SearchAndSendLiveChatMessage(recieverClient, senderProf.UserName, chatMessage);
                        ChatProtocol senderResponse = chatManager.CreateLiveChatResponseProtocol(recieverProf.UserName, ChatData.LIVECHAT_CHAT, "");
                        NotifyClientWithPackage(client, senderResponse.Package);                     
                    }
                    else                    
                        BeginLiveChatBetweenClients(client, senderProf, recieverProf, recieverClient);                    
                }
                else
                {
                    senderProf.SetLiveChatProfile(recieverProf, false);
                    ChatProtocol senderResponse = chatManager.CreateLiveChatResponseProtocol(recieverProf.UserName, ChatData.LIVECHAT_CHAT, recieverProf.UserName + " is chatting with other user. Wait or cancel chat mode");
                    NotifyClientWithPackage(client, senderResponse.Package);
                }                                                  
            }
            else
            {
                senderProf.SetLiveChatProfile(recieverProf, false);
                ChatProtocol senderResponse = chatManager.CreateLiveChatResponseProtocol(recieverProf.UserName, ChatData.LIVECHAT_CHAT, "Waiting for: " + recieverProf.UserName + " to chat with you...");
                NotifyClientWithPackage(client, senderResponse.Package);
            }            
        }
        private static void SearchAndSendLiveChatMessage(Socket recieverClient, string sender, string message)
        {
            foreach (var el in clientAndMessanger)
            {
                var socketMessenger = el.Value;
                if (el.Key.RemoteEndPoint.Equals(recieverClient.RemoteEndPoint))
                {
                    ChatProtocol response = chatManager.CreateLiveChatResponseProtocol(sender, ChatData.LIVECHAT_CHAT, sender + ": " + message);
                    NotifyClientWithPackage(socketMessenger, response.Package);
                }
            }
        }


        private static void BeginLiveChatBetweenClients(Socket client, UserProfile senderProf, UserProfile recieverProf, Socket recieverClient)
        {
            senderProf.SetLiveChatProfile(recieverProf, true);
            recieverProf.SetLiveChatProfile(senderProf, true);

            Socket recieverMessenger = clientAndMessanger.Find(kvp => kvp.Key.RemoteEndPoint.Equals(recieverClient.RemoteEndPoint)).Value;

            SearchAndSendLiveChatMessage(recieverClient,senderProf.UserName, "Begin chat...");

            ChatProtocol senderResponse = chatManager.CreateLiveChatResponseProtocol(recieverProf.UserName, ChatData.LIVECHAT_CHAT, "Begin chat...");
            NotifyClientWithPackage(client, senderResponse.Package);

            //     ChatProtocol recieverResponse = chatManager.CreateLiveChatResponseProtocol(senderProf.UserName, ChatData.LIVECHAT_CHAT, "Begin chat...");
            //    NotifyClientWithPackage(recieverMessenger, recieverResponse.Package);
            //    ChatProtocol recieverResponse = chatManager.CreateLiveChatResponseProtocol(senderProf.UserName, ChatData.LIVECHAT_CHAT, "Begin chat...");
            // NotifyClientWithPackage(recieverClient, recieverResponse.Package);
        }

        private static void EndOfflineChat(Socket client, UserProfile senderProf, UserProfile recieverProf)
        {
            senderProf.UnSetLiveChatProfile();
            ChatProtocol response = chatManager.CreateLiveChatResponseProtocol(recieverProf.UserName, ChatData.LIVECHAT_END, "Chat ended.");
            NotifyClientWithPackage(client, response.Package);
        }

        private static void OfflineChat(Socket client, string chatMessage, UserProfile senderProf, UserProfile recieverProf)
        {
            if (!senderProf.IsFriendWith(recieverProf))
                throw new Exception("Error: The user is not friend with you");

            if (!senderProf.HasLiveChatProfileSet())//Primera vez que hace CHAT
            {
                senderProf.SetLiveChatProfile(recieverProf, false);
                ChatProtocol response = chatManager.CreateLiveChatResponseProtocol(recieverProf.UserName, ChatData.LIVECHAT_CHAT, "Begin chat... He will recieve your messages when he connects");
                NotifyClientWithPackage(client, response.Package);
            }
            else
            {
                if (ProfilesAreEquals(senderProf.PendingLiveChatProfile(), recieverProf))
                {
                    recieverProf.AddPendingMessage(senderProf, chatMessage);
                    ChatProtocol response = chatManager.CreateLiveChatResponseProtocol(recieverProf.UserName, ChatData.LIVECHAT_CHAT, "");
                    NotifyClientWithPackage(client, response.Package);
                }
                else
                    throw new Exception("Error: You can only enter chat mode with one friend");
            }
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

        private static void SetLiveChatProfileAndSendMessage(Socket client, UserProfile sender, UserProfile reciever, ChatProtocol chatMsg, string payload)
        {
            sender.SetLiveChatProfile(reciever, false);
            ChatProtocol response = chatManager.CreateResponseOkProtocol(chatMsg.Command, payload);
            NotifyClientWithPackage(client, response.Package);
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

            UserProfile clientProfile = authorizedClients.First(authCli => ClientsAreEquals(authCli.Key, client)).Value;
            UserProfile profileToReply = storedUserProfiles.First(prof => prof.UserName.Equals(username));

            bool accept = (MessageIsFriendRequestReply(reply, ChatData.FRIEND_REQUEST_YES_REPLY)) ? true : false;
            clientProfile.ReplyFriendRequest(profileToReply, accept);
            if (accept)
                profileToReply.AddFriend(clientProfile);
            ChatProtocol response = chatManager.CreateResponseOkProtocol(protocol.Command);
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
            ChatProtocol response = chatManager.CreateResponseOkProtocol(protocol.Command, payload);
            NotifyClientWithPackage(client, response.Package);
        }

        private static string GeneratePendingFriendRequestsPayload(Socket client)
        {
            string pendingFriendRequests = "";
            UserProfile profile = authorizedClients.First(cli => ClientsAreEquals(cli.Key, client)).Value;
            foreach (var prof in profile.PendingFriendRequest)
            {
                pendingFriendRequests += prof.UserName;
                if (!ProfilesAreEquals(profile.PendingFriendRequest.Last(), prof))
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
            UserProfile sender, reciever;
            string userNameRequest = protocol.Payload;
            ValidateFriendRequestInformation(client, userNameRequest);

            authorizedClients.TryGetValue(client, out sender);
            reciever = storedUserProfiles.First(prof => prof.UserName.Equals(userNameRequest));
            ApplyFriendRequest(sender, reciever);

            ChatProtocol responseProtocol = chatManager.CreateResponseOkProtocol(protocol.Command);
            NotifyClientWithPackage(client, responseProtocol.Package);
        }

        private static void ValidateFriendRequestInformation(Socket client, string userNameRequest)
        {
            if (!authorizedClients.ContainsKey(client))
                throw new Exception("Error: To send friend request login first");
            if (!storedUserProfiles.Exists(us => us.UserName.Equals(userNameRequest)))
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
            ChatProtocol responseProtocol = chatManager.CreateResponseOkProtocol(protocol.Command, payload);
            NotifyClientWithPackage(client, responseProtocol.Package);
        }

        private static void ValidateFriendListInformation(Socket client, ChatProtocol protocol)
        {
            if (!authorizedClients.ContainsKey(client))
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
            UserProfile profile = authorizedClients.First(cli => ClientsAreEquals(cli.Key, client)).Value;
            foreach (var prof in profile.Friends)
            {
                friendlist += prof.UserName + "_" + prof.FriendsAmmount();               
                if (!ProfilesAreEquals(profile.Friends.Last(), prof))
                    friendlist += "#";
            }
            return friendlist;
        }

        private static void ProcessConnectedUsersRequest(Socket client, ChatProtocol protocol)
        {
            ValidateOnlineUsersInformation(client, protocol);
            string payload = GenerateConnectedUsersPayload(client);
            ChatProtocol responseProtocol = chatManager.CreateResponseOkProtocol(protocol.Command, payload);
            NotifyClientWithPackage(client, responseProtocol.Package);
        }

        private static void ValidateOnlineUsersInformation(Socket client, ChatProtocol protocol)
        {
            if (!authorizedClients.ContainsKey(client))
                throw new Exception("Error: To see online users you must be logged in");
            if (!protocol.Payload.Equals(String.Empty))
                throw new Exception("Error: Online users command must be written alone");
        }

        private static string GenerateConnectedUsersPayload(Socket client)
        {
            string connectedUsers = "";
            UserProfile clientProf, profile, lastProfile;
            var onlineProf = authorizedClients.Select(d => d.Value).ToList();
            clientProf = authorizedClients.First(cli => ClientsAreEquals(cli.Key, client)).Value;
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
            authorizedClients.Remove(client);
            ChatProtocol response = chatManager.CreateResponseOkProtocol(protocol.Command);
            NotifyClientWithPackage(client, response.Package);
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
            ChatProtocol response = chatManager.CreateResponseOkProtocol(protocol.Command);
            NotifyClientWithPackage(client, response.Package);           
        }

        private static void CreateProfileAndLogin(Socket client, string user, string password)
        {
            UserProfile profile = new UserProfile(user, password);
            storedUserProfiles.Add(profile);
            authorizedClients.Add(client, profile);
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
            return new Tuple<string, string>(user, password);
        }

        private static void ValidateLoginInformation(Socket client, string user, string password, ChatProtocol protocol)
        {
            LoginVerification(client, user);            
            LoginClientAsUserProfile(client, user, password);
            ChatProtocol response = chatManager.CreateResponseOkProtocol(protocol.Command);
            NotifyClientWithPackage(client, response.Package);
        }


        private static void NotifyClientWithPackage(Socket client, string resPackage)
        {
            try
            {
                lock (_lock)
                {
                    Byte[] responseBuffer = Encoding.ASCII.GetBytes(resPackage);
                    NetworkStream stream = new NetworkStream(client);
                    stream.Write(responseBuffer, 0, responseBuffer.Length);
                    stream.Flush(); //OK?
                }               

            }catch(Exception ex)
            {
                Console.WriteLine("Couldn't send response to -> " + client.RemoteEndPoint);
            }
           
        }

        private static bool ClientIsConnected(Socket client)
        {
            return authorizedClients.Any(cli => ClientsAreEquals(cli.Key, client));
        }
        private static bool ClientsAreEquals(Socket client1, Socket client2)
        {
            return client1.RemoteEndPoint.Equals(client2.RemoteEndPoint);
        }

        private static void LoginClientAsUserProfile(Socket client, string user, string password)
        {
            var profile = ExtractProfileFromAttributes(user, password);
            authorizedClients.Add(client, profile);
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
            foreach(var loggedClient in authorizedClients)
            {
                var clientProfile = loggedClient.Value;
                if (clientProfile.UserName.Equals(user))
                    return true;
            }
            return false;
        }

        private static bool ProfileUserNameExists(string user)
        {
            return storedUserProfiles.Exists(us => us.UserName.Equals(user));
        }

        private static UserProfile ExtractProfileFromAttributes(string user, string password)
        {
           var profile = storedUserProfiles.Find(prof => prof.UserName.Equals(user) && prof.Password.Equals(password));
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

