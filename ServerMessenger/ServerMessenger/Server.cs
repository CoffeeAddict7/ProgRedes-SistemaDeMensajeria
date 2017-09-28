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
        private static Dictionary<Socket, Thread> activeClientThreads; //Close threads and sockets if server makes shutdown command
        private static Socket tcpServer;
        private static ProtocolManager chatManager;
        private static List<UserProfile> storedUserProfiles;
        private static Dictionary<Socket,UserProfile> authorizedClients;

        static void Main(string[] args)
        {
            LoadPersistenceStructures();
            LoadUserProfiles();
            StartServer();
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
            storedUserProfiles.Add(luis);
            storedUserProfiles.Add(jose);
        }

        private static void StartServer()
        {        
            InitializeServerConfiguration();
            
            while (acceptingConnections) { 
                try
                {
                    var clientSocket = tcpServer.Accept();
                    var thread = new Thread(() => ClientHandler(clientSocket));
                    thread.Start();
                    activeClientThreads.Add(clientSocket, thread);
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
                EndConnection(entry.Key);
            }
            activeClientThreads = new Dictionary<Socket, Thread>();
        }

        private static int ReadFixedBytesFromPackage(Socket client ,StreamReader reader, ref StringBuilder sb)
        {
            var buffer = new char[10000];
            int received = 0, localReceived = 0, bytesLeftToRead = 0, packageLength = ChatData.PROTOCOL_FIXED_BYTES;
            while(received != ChatData.PROTOCOL_FIXED_BYTES)
            {
                bytesLeftToRead = ChatData.PROTOCOL_FIXED_BYTES - received;
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
            int payloadLength = packageLength - ChatData.PROTOCOL_FIXED_BYTES;
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
            var reader = new StreamReader(stream);
            try
            {
                while (acceptingConnections && !ServerExecuteCommand())
                {
                    var sb = new StringBuilder();
                    int packageLength = ReadFixedBytesFromPackage(client, reader, ref sb);
                    ReadPayloadBytesFromPackage(client, reader, ref sb, packageLength);
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
            CloseServerConnection();
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
                default:
                    throw new Exception("Error: Unidentified command");
            }
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
                    connectedUsers += profile.UserName;
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
            Byte[] responseBuffer = Encoding.ASCII.GetBytes(resPackage);
            NetworkStream stream = new NetworkStream(client);
            stream.Write(responseBuffer, 0, responseBuffer.Length);
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
            Console.WriteLine("Number of connections -> " + profile.NumberOfConnections);
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

        private static void EndConnection(Socket client)
        {
            try
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (Exception) { }
        }

        private static void AppendBufferToStringBuilder(ref StringBuilder sb, char[] payloadBuffer, int payloadLength)
        {
            var bufferCopy = new char[payloadLength];
            Array.Copy(payloadBuffer, bufferCopy, payloadLength);
            sb.Append(bufferCopy);
        }
    }
}

