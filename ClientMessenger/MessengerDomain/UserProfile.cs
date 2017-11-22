using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MessengerDomain
{
    [Serializable]
    [DataContract]
    public class UserProfile
    {
        [DataMember]
        public string UserName { get; set; }
        [DataMember]      
        public string Password { get; set; }
        [DataMember]
        public int NumberOfConnections { get; set; }
        public DateTime SessionBegin { get; set; }

        static readonly object _lockFriends = new object();
        static readonly object _lockPendingFriendRequest = new object();
        static readonly object _lockPendingMessages = new object();
        static readonly object _lockLiveChatProfile = new object();
        static readonly object _lockLoginParams = new object();

        private ICollection<UserProfile> Friends { get; set; }
        private ICollection<UserProfile> PendingFriendRequest { get; set; }        
        private List<KeyValuePair<UserProfile, Tuple<DateTime,string>>> PendingMessages { get; set; }   
        private Tuple<UserProfile, bool> LiveChatProfile { get; set; }

        public UserProfile(string userName, string password)
        {
            ValidateAttributesLength(userName, password);
            InitializeCollections();
            this.UserName = userName;
            this.Password = password;
            this.NumberOfConnections = 1;
            this.SessionBegin = DateTime.Now;
        }

        private void InitializeCollections()
        {
            this.Friends = new List<UserProfile>();
            this.PendingFriendRequest = new List<UserProfile>();
            this.PendingMessages = new List<KeyValuePair<UserProfile, Tuple<DateTime, string>>>();
            this.LiveChatProfile = null;
        }

        public void AddPendingMessage(UserProfile sender, string msg)
        {
            var chatLog = new Tuple<DateTime, string>(DateTime.Now, msg);
            var chatMsg = new KeyValuePair<UserProfile, Tuple<DateTime, string>>(sender, chatLog);
            lock (_lockPendingMessages)
                PendingMessages.Add(chatMsg);
        }
        public List<UserProfile> GetProfilesOfPendingMessages()
        {
            List<UserProfile> pendingMsgs;
            lock(_lockPendingMessages)
                pendingMsgs = PendingMessages.Select(kvp => kvp.Key).Distinct().ToList();
            return pendingMsgs;
        }
        public ICollection<UserProfile> GetFriends()
        {
            ICollection<UserProfile> friends;
            lock (_lockFriends)
                friends = this.Friends;
            return friends;
        }
        public ICollection<UserProfile> GetPendingFriendRequest()
        {
            ICollection<UserProfile> pendingFRs;
            lock (_lockPendingFriendRequest)
                pendingFRs = this.PendingFriendRequest;
            return pendingFRs;
        }
        public UserProfile GetLiveChatProfile()
        {
            UserProfile liveChatProf;
            lock (_lockLiveChatProfile)
                liveChatProf = this.LiveChatProfile.Item1;
            return liveChatProf;
        }

        public List<Tuple<DateTime,string>> GetPendingMessagesOfFriend(UserProfile sender)
        {
            if (!IsFriendWith(sender))
                throw new Exception("Error: This user is not your friend");
            List<KeyValuePair<UserProfile, Tuple<DateTime, string>>> chatLogs;
            List<Tuple<DateTime, string>> pendingMsgs;
            lock (_lockPendingMessages)
            {
                chatLogs = PendingMessages.FindAll(log => log.Key.UserName.Equals(sender.UserName));
                pendingMsgs = chatLogs.Select(kvp => kvp.Value).ToList();
            }
            return pendingMsgs;
        }
        public void RemovePendingMessagesOfFriend(UserProfile friend)
        {
            lock (_lockPendingMessages)
                PendingMessages.RemoveAll(kvp => kvp.Key.UserName.Equals(friend.UserName));
        }
        public bool IsOnLiveChat()
        {
            bool isLiveChatting;
            lock (_lockLiveChatProfile)            
                isLiveChatting = HasLiveChatProfileSet() && LiveChatProfile.Item2;
            return isLiveChatting;          
        }
        public UserProfile PendingLiveChatProfile()
        {
            UserProfile pendingLiveChatProf;
            lock (_lockLiveChatProfile)            
                pendingLiveChatProf = (HasLiveChatProfileSet()) ? LiveChatProfile.Item1 : null;
            return pendingLiveChatProf;            
        }
        public bool HasLiveChatProfileSet()
        {
            bool liveChatProfSet;
            lock (_lockLiveChatProfile)
                liveChatProfSet = LiveChatProfile != null;
            return liveChatProfSet;
        }
        public void SetLiveChatProfile(UserProfile profile, bool establish)
        {
           lock (_lockLiveChatProfile)
                LiveChatProfile = new Tuple<UserProfile, bool>(profile, establish); 
        }
        public void UnSetLiveChatProfile()
        {
            lock (_lockLiveChatProfile)
                LiveChatProfile = null;
        }
        private void ValidateAttributesLength(string userName, string password)
        {
            if(!ValidAttributeLength(userName) || !ValidAttributeLength(password))
                throw new Exception("Error: login parameters must have at least 4 letters");
        }

        private bool ValidAttributeLength(string attribute)
        {
            return attribute != null && (attribute.Length >= ChatData.PROFILE_ATTRIBUTES_MIN_LENGTH);
        }
        public void NewConnectionMade()
        {
            this.NumberOfConnections++;
            this.SessionBegin = DateTime.Now;
        }
        public string GetSessionDuration()
        {
            TimeSpan timespan = DateTime.Now - SessionBegin;
            return timespan.Hours + ":" + timespan.Minutes + ":" + timespan.Seconds;
        }

        public bool IsFriendWith(UserProfile profile)
        {
            bool isFriendWith;
            lock (_lockFriends)
                isFriendWith = this.Friends.Any(friend => friend.UserName.Equals(profile.UserName));
            return isFriendWith;
        }

        public void UpdateProfileInfo(string newUserName, string newPassword)
        {
            lock (_lockLoginParams)
            {
                this.UserName = newUserName;
                this.Password = newPassword;
            }
        }

        public bool IsFriendWith(string username)
        {
            bool isFriendWith;
            lock (_lockFriends)
                isFriendWith = this.Friends.Any(friend => friend.UserName.Equals(username));
            return isFriendWith;
        }
        public void AddFriend(UserProfile profile)
        {
            lock (_lockFriends)
                this.Friends.Add(profile);
        }

        public bool IsFriendRequestedBy(string username)
        {
            bool isFriendReq;
            lock (_lockPendingFriendRequest)
                isFriendReq = this.PendingFriendRequest.Any(pending => pending.UserName.Equals(username));
            return isFriendReq;
        }

        public void AddFriendRequest(UserProfile profile)
        {
            if (IsFriendWith(profile.UserName))
                throw new Exception("Error: ("+ profile.UserName + ") is already in your friend list");

            if(IsFriendRequestedBy(profile.UserName))
                throw new Exception("Error: already sent a friend request to (" +this.UserName+ ")");

            lock (_lockPendingFriendRequest)
                PendingFriendRequest.Add(profile);
        }
        public void ReplyFriendRequest(UserProfile profile, bool accept)
        {
            if (IsFriendWith(profile.UserName))
                throw new Exception("Error: (" + profile.UserName + ") is already in your friend list");

            if (!IsFriendRequestedBy(profile.UserName))
                throw new Exception("Error: ("+profile.UserName+") not in your friend requests");

            if (accept)            
                AddFriendAndRemoveFriendRequest(profile);
            else            
               lock (_lockPendingFriendRequest)
                    PendingFriendRequest.Remove(profile);                         
        }

        public void AcceptFriendRequest(UserProfile profile)
        {
            ReplyFriendRequest(profile, true);
        }

        private void AddFriendAndRemoveFriendRequest(UserProfile pendingFriend)
        {
            lock (_lockFriends)
                Friends.Add(pendingFriend);
            lock (_lockPendingFriendRequest)
                PendingFriendRequest.Remove(pendingFriend);
        }
       
        public int FriendsAmmount()
        {
            int numOffriends;
            lock(_lockFriends)
                numOffriends = Friends.Count;
            return numOffriends;
        }

        public void RemoveInfoRelatedToUser(UserProfile profile)
        {
            lock (_lockFriends)
                Friends.Remove(profile);
            lock (_lockPendingFriendRequest)
                PendingFriendRequest.Remove(profile);
            lock (_lockLiveChatProfile)            
                if (HasLiveChatProfileSet() && LiveChatProfile.Item1.UserName.Equals(profile.UserName))
                    UnSetLiveChatProfile();            
        }

    }
}
