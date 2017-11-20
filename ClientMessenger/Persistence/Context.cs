using MessengerDomain;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Linq;

namespace Persistence
{
    public class Context
    {
        public static Context instance;

        private List<UserProfile> storedUserProfiles;
        private Dictionary<Socket, UserProfile> authorizedClients;
        private Dictionary<Socket, Thread> activeClientThreads;
        private List<KeyValuePair<Socket, Socket>> clientAndMessenger;

        static readonly object _lockAuthorizedClients = new object();
        static readonly object _lockClientAndMessenger = new object();
        static readonly object _lockStoredProfiles = new object();

        private Context()
       {
            LoadPersistenceStructures();
            LoadUserProfiles();
            activeClientThreads = new Dictionary<Socket, Thread>();
            clientAndMessenger = new List<KeyValuePair<Socket, Socket>>();
        }

        public static Context GetInstance()
        {
            if (instance == null)
                instance = new Context();
            return instance;
        }

        private void LoadPersistenceStructures()
        {
            storedUserProfiles = new List<UserProfile>();
            authorizedClients = new Dictionary<Socket, UserProfile>();
        }


        private void LoadUserProfiles()
        {
            UserProfile luis = new UserProfile("LUIS", "PEPE");
            UserProfile jose = new UserProfile("JOSE", "PEPE");
            luis.AddFriend(jose);
            jose.AddFriend(luis);
            storedUserProfiles.Add(luis);
            storedUserProfiles.Add(jose);
        }

        public void AddClientMessenger(Socket clientSocket, Socket messengerSocket)
        {
            clientAndMessenger.Add(new KeyValuePair<Socket, Socket>(clientSocket, messengerSocket));
        }

        public void AddActiveClient(Socket client, Thread thread)
        {
            activeClientThreads.Add(client, thread);
        }
        public List<UserProfile> GetUserProfiles()
        {
            return storedUserProfiles;
        }
        public Dictionary<Socket, UserProfile> GetAuthorizedClients()
        {
            return authorizedClients;
        }

        public Dictionary<Socket, Thread> GetActiveClients()
        {
            return activeClientThreads;
        }

        public void RemoveAllActiveClients()
        {
            activeClientThreads = new Dictionary<Socket, Thread>();
        }

        public void RemoveActiveClient(Socket client)
        {
            activeClientThreads.Remove(client);
        }

        public void RemoveClientAuthorization(Socket client)
        {
            lock (_lockAuthorizedClients) authorizedClients.Remove(client);
        }

        public void RemoveClientMessenger(Socket client, Socket messenger)
        {
            lock (_lockClientAndMessenger) clientAndMessenger.Remove(new KeyValuePair<Socket, Socket>(client, messenger));
        }

        public Socket GetClientMessenger(Socket client)
        {
            lock (_lockClientAndMessenger) return clientAndMessenger.Find(kvp => kvp.Key.RemoteEndPoint.Equals(client.RemoteEndPoint)).Value;
        }

        public Socket GetClientFromProfile(UserProfile profile)
        {
            lock (_lockAuthorizedClients) return authorizedClients.First(auth => ProfilesAreEquals(auth.Value, profile)).Key;
        }

        public bool ProfilesAreEquals(UserProfile prof1, UserProfile prof2)
        {
           return prof1.UserName.Equals(prof2.UserName);
        }

        public UserProfile GetUserByName(string username)
        {
            lock (_lockStoredProfiles) return storedUserProfiles.First(prof => prof.UserName.Equals(username));
        }

        public UserProfile GetProfileFromClient(Socket client)
        {
            lock (_lockAuthorizedClients) return authorizedClients.First(auth => ClientsAreEquals(auth.Key, client)).Value;
        }

        private bool ClientsAreEquals(Socket client1, Socket client2)
        {
            return client1.RemoteEndPoint.Equals(client2.RemoteEndPoint);
        }

        public void AddUserProfile(UserProfile profile)
        {
            lock (_lockStoredProfiles) storedUserProfiles.Add(profile);
        }

        public void AddClientAuthorization(Socket client, UserProfile profile)
        {
            lock (_lockAuthorizedClients) authorizedClients.Add(client, profile);
        }
        internal void DeleteUserProfile(UserProfile toDelete)
        {
            lock (_lockStoredProfiles)
            {
                storedUserProfiles.Remove(toDelete);
                foreach (UserProfile user in storedUserProfiles)
                {
                    user.RemoveInfoRelatedToUser(toDelete);
                }
            }
        }
        internal void ModifyUserProfile(UserProfile toModify, string newUserName, string newPassword)
        {
            lock (_lockStoredProfiles)            
                storedUserProfiles.Find(prof => ProfilesAreEquals(prof, toModify)).UpdateProfileInfo(newUserName, newPassword);            
        }
    }
}
