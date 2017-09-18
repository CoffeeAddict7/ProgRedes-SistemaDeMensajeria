using MessengerDomain;
using ClientMessenger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerMessenger
{
    public class Server
    {
        private static Boolean acceptingConnections = true;

        private static Dictionary<Socket, Thread> activeClientThreads;
        private static Socket tcpServer;

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
            tcpServer.Bind(ChatData.SERVER_IP_END_POINT);
            tcpServer.Listen(ChatData.MAX_ACTIVE_CONN);
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
                var sb = new StringBuilder();
                int packageLength = ReadFixedBytesFromPackage(client, reader, ref sb);
                ReadPayloadBytesFromPackage(client, reader, ref sb, packageLength);

                var payloadLength = packageLength - ChatData.PROTOCOL_FIXED_BYTES;
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
            ValidateClientHeader(chatMsg);
            try
            {
                ProcessByProtocolCommmand(client, chatMsg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); //Devolver mensaje al cliente
            }
            
        }

        private static void ProcessByProtocolCommmand(Socket client, ChatProtocol chatMsg)
        {
            switch (chatMsg.Command)
            {
                case 1:
                    Console.WriteLine("Loggin request by " + client.RemoteEndPoint);
                    ProcessLoginRequest(client, chatMsg.Payload);
                    break;
                default:
                    throw new Exception("Error: Unidentified command");
            }
        }

        private static void ProcessLoginRequest(Socket client, string payload)
        {
            var userProfileAttributes = payload.Split('#');
            if (userProfileAttributes.Length != 2)
                throw new Exception("Wrong login parameters separated by '#' ");

            string user = userProfileAttributes[0];
            string password = userProfileAttributes[1];
            ValidateLoginInformation(client, user, password);

        }

        private static void ValidateLoginInformation(Socket client, string user, string password)
        {
            if (!ClientIsConnected(client))
            {
                LoginClientAsUserProfile(client, user, password);
                Console.WriteLine("Success");//Response message with menu
            }
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
            if (ProfileUserNameExists(user))
            {
                var profile = ExtractProfileFromAttributes(user, password);
                if (!ProfileIsConnectedToAClient(user))
                {
                    authorizedClients.Add(client, profile);
                    profile.NewConnectionMade();
                    Console.WriteLine("Number of connections -> " + profile.NumberOfConnections);
                    //Responder al cliente menu
                }
                else
                {
                    throw new Exception("Error: Profile already logged in");
                }
            }
            else
            {
                CreateProfileAndLogin(client, user, password);
            }
        }

        private static void CreateProfileAndLogin(Socket client, string user, string password)
        {
            UserProfile profile = new UserProfile(user, password);
            storedUserProfiles.Add(profile);
            authorizedClients.Add(client,profile);
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
            else
                return profile;            
        }

        private static void ValidateClientHeader(ChatProtocol chatMsg)
        {
            if (!chatMsg.Header.Equals(ChatData.REQUEST_HEADER))
                Console.WriteLine("Wrong client header");
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

