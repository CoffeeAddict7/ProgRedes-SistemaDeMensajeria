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
        private static bool acceptingConnections = true;

        private static Dictionary<Socket, Thread> activeClientThreads;
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
            storedUserProfiles.Add(luis);           
        }

        private static void StartServer()
        {
            InitializeServerConfiguration();

            while (acceptingConnections) //Cambiar
            {
                try
                {
                    var clientSocket = tcpServer.Accept();
                    var thread = new Thread(() => ClientHandler(clientSocket));
                    thread.Start();      
                    //Checkear si ya lo ingrese
                    activeClientThreads.Add(clientSocket, thread);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            CloseServerConnection();
        }

        private static void InitializeServerConfiguration()
        {
            activeClientThreads = new Dictionary<Socket, Thread>();
            chatManager = new ProtocolManager();
            tcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpServer.Bind(ChatData.SERVER_IP_END_POINT);
            tcpServer.Listen(ChatData.MAX_ACTIVE_CONN);
            Console.WriteLine("Start waiting for clients");
        }

        private static void CloseServerConnection()
        {
            foreach (KeyValuePair<Socket, Thread> entry in activeClientThreads)
            {
                EndConnection(entry.Key);
                //CERRAR THREAD y borrarlos de los dos diccionarios
            }
            tcpServer.Close();
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
            using (var reader = new StreamReader(stream))
            {
                try
                {
                    while (true)
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
                    authorizedClients.Remove(client);
                    activeClientThreads.Remove(client);
                    Console.WriteLine("Client " + client.RemoteEndPoint + " disconnected!");
                }
            }
            stream.Close();
            EndConnection(client);
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
                int packageSize = ChatData.PROTOCOL_FIXED_BYTES + ex.Message.Length;
                string errorMsg = ChatData.RESPONSE_HEADER + chatMsg.Command + chatManager.BuildProtocolHeaderLength(packageSize) + ex.Message;
                NotifyClientWithPackage(client, errorMsg);
                Console.WriteLine("Response to: [" + client.RemoteEndPoint + "] of CMD [" + chatMsg.Command + "] With ERROR");
            }
            
        }

        private static void ProcessByProtocolCommmand(Socket client, ChatProtocol chatMsg)
        {
            Console.WriteLine("Receive: CMD ["+ chatMsg.Command + "] Requested by ["+ client.RemoteEndPoint+ "]");
            switch (chatMsg.GetCommandNumber())
            {
                case 0:
                    ProcessLogoutRequest(client, chatMsg.Payload);
                    break;
                case 1:
                    ProcessLoginRequest(client, chatMsg.Payload);
                    break;
                case 2:
                    ProcessRegisterRequest(client, chatMsg.Payload);
                    break;
                case 3:
                    ProcessConnectedUsersRequest(client, chatMsg);
                    break;
                default:
                    throw new Exception("Error: Unidentified command");
            }
        }

        private static void ProcessConnectedUsersRequest(Socket client, ChatProtocol protocol)
        {
            if (!authorizedClients.ContainsKey(client))
                throw new Exception("Error: To see online users you must be logged in");
            string payload = GenerateConnectedUsersPayload(client);
            ChatProtocol responseProtocol = chatManager.CreateResponseProtocol(protocol.Command, payload);
            NotifyClientWithPackage(client, responseProtocol.Package);
        }
        private static string GenerateConnectedUsersPayload(Socket client)
        {
            string connectedUsers = "";
            var onlineProf = authorizedClients.Select(d => d.Value).ToList();
            UserProfile clientProf, profile, lastProfile;
            authorizedClients.TryGetValue(client, out clientProf);
            lastProfile = onlineProf.Last();
            for(int index = 0; index < onlineProf.Count; index++)
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

        private static void ProcessLogoutRequest(Socket client, string payload)
        {
            LogoutVerification(client, payload);
            authorizedClients.Remove(client);
            string resPackage = "RES000011OK";
            NotifyClientWithPackage(client, resPackage);
        }

        private static void LogoutVerification(Socket client, string payload)
        {
            if (!ClientIsConnected(client))
                throw new Exception("Error: Client is not logged in ");
            if (!payload.Equals(String.Empty))
                throw new Exception("Error: Logout command must be written alone");
        }

        private static void ProcessRegisterRequest(Socket client, string payload)
        {
            string errorMsg = "Wrong register parameters separated by '#'";
            var profileAccessInfo = ExtractUserProfileAccessInfo(payload, errorMsg);
            ValidateRegisterInformation(client, profileAccessInfo.Item1, profileAccessInfo.Item2);
        }

        private static void ValidateRegisterInformation(Socket client, string user, string password)
        {
            if (!ProfileUserNameExists(user))
            {
                CreateProfileAndLogin(client, user, password);
                string resPackage = "RES020011OK";
                NotifyClientWithPackage(client, resPackage);
            }
            else
                throw new Exception("Error: Profile username already registered");
        }

        private static void CreateProfileAndLogin(Socket client, string user, string password)
        {
            UserProfile profile = new UserProfile(user, password);
            storedUserProfiles.Add(profile);
            authorizedClients.Add(client, profile);
        }

        private static void ProcessLoginRequest(Socket client, string payload)
        {
            string errorMsg = "Wrong login parameters separated by '#' ";
            var profileAccessInfo = ExtractUserProfileAccessInfo(payload, errorMsg);
            ValidateLoginInformation(client, profileAccessInfo.Item1, profileAccessInfo.Item2);

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

        private static void ValidateLoginInformation(Socket client, string user, string password)
        {
            LoginVerification(client, user);            
            LoginClientAsUserProfile(client, user, password);
            string resPackage = "RES010011OK";
            NotifyClientWithPackage(client, resPackage);                               
        }

        private static void NotifyClientWithPackage(Socket client, string resPackage)
        {            
            Byte[] responseBuffer = Encoding.ASCII.GetBytes(resPackage);
            NetworkStream stream = new NetworkStream(client);
            stream.Write(responseBuffer, 0, responseBuffer.Length);
        }

        private static bool ClientIsConnected(Socket client)
        {
           foreach(var cli in authorizedClients)
            {
                var connectedClient = cli.Key;
                if (connectedClient.RemoteEndPoint.Equals(client.RemoteEndPoint))
                    return true;
            }
            return false;
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

