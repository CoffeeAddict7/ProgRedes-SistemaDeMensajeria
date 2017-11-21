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
            lock (_lockPendingMessages) PendingMessages.Add(chatMsg);
        }
        public List<UserProfile> GetProfilesOfPendingMessages()
        {
            lock(_lockPendingMessages) return PendingMessages.Select(kvp => kvp.Key).Distinct().ToList();
        }
        public ICollection<UserProfile> GetFriends()
        {
           lock (_lockFriends) return this.Friends;
        }
        public ICollection<UserProfile> GetPendingFriendRequest()
        {
            lock (_lockPendingFriendRequest) return this.PendingFriendRequest;
        }
        public UserProfile GetLiveChatProfile()
        {
            lock (_lockLiveChatProfile) return this.LiveChatProfile.Item1;
        }

        public List<Tuple<DateTime,string>> GetPendingMessagesOfFriend(UserProfile sender)
        {
            if (!IsFriendWith(sender))
                throw new Exception("Error: This user is not your friend");
            List<KeyValuePair<UserProfile, Tuple<DateTime, string>>> chatLogs;
            lock(_lockPendingMessages) chatLogs = PendingMessages.FindAll(log => log.Key.UserName.Equals(sender.UserName));
            return chatLogs.Select(kvp => kvp.Value).ToList();      
        }
        public void RemovePendingMessagesOfFriend(UserProfile friend)
        {
            lock (_lockPendingMessages) PendingMessages.RemoveAll(kvp => kvp.Key.UserName.Equals(friend.UserName));
        }
        public bool IsOnLiveChat()
        {
            lock (_lockLiveChatProfile)
            {
                return HasLiveChatProfileSet() && LiveChatProfile.Item2;
            }            
        }
        public UserProfile PendingLiveChatProfile()
        {
            lock (_lockLiveChatProfile)
            {
                return (HasLiveChatProfileSet()) ? LiveChatProfile.Item1 : null;
            }            
        }
        public bool HasLiveChatProfileSet()
        {
           lock (_lockLiveChatProfile) return LiveChatProfile != null;
        }
        public void SetLiveChatProfile(UserProfile profile, bool establish)
        {
           lock (_lockLiveChatProfile) LiveChatProfile = new Tuple<UserProfile, bool>(profile, establish); 
        }
        public void UnSetLiveChatProfile()
        {
            lock (_lockLiveChatProfile) LiveChatProfile = null;
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
            lock (_lockFriends) return this.Friends.Any(friend => friend.UserName.Equals(profile.UserName));
        }

        public void UpdateProfileInfo(string newUserName, string newPassword)
        {
            this.UserName = newUserName;
            this.Password = newPassword;
        }

        public bool IsFriendWith(string username)
        {
            lock (_lockFriends) return this.Friends.Any(friend => friend.UserName.Equals(username));
        }
        public void AddFriend(UserProfile profile)
        {
            lock (_lockFriends) this.Friends.Add(profile);
        }

        public bool IsFriendRequestedBy(string username)
        {
           lock (_lockPendingFriendRequest) return this.PendingFriendRequest.Any(pending => pending.UserName.Equals(username));
        }

        public void AddFriendRequest(UserProfile profile)
        {
            if (IsFriendWith(profile.UserName))
                throw new Exception("Error: ("+ profile.UserName + ") is already in your friend list");

            if(IsFriendRequestedBy(profile.UserName))
                throw new Exception("Error: already sent a friend request to (" +this.UserName+ ")");

            lock (_lockPendingFriendRequest) PendingFriendRequest.Add(profile);
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
            {
               lock (_lockPendingFriendRequest) PendingFriendRequest.Remove(profile);
            }                
        }

        public void AcceptFriendRequest(UserProfile profile)
        {
            ReplyFriendRequest(profile, true);
        }

        private void AddFriendAndRemoveFriendRequest(UserProfile pendingFriend)
        {
            lock (_lockFriends) Friends.Add(pendingFriend);
            lock (_lockPendingFriendRequest) PendingFriendRequest.Remove(pendingFriend);
        }
       
        public int FriendsAmmount()
        {
            lock(_lockFriends) return Friends.Count;
        }

        public void RemoveInfoRelatedToUser(UserProfile profile)
        {
            lock (_lockFriends) Friends.Remove(profile);
            lock (_lockPendingFriendRequest) PendingFriendRequest.Remove(profile);
            lock (_lockLiveChatProfile)
            {
                if (HasLiveChatProfileSet() && LiveChatProfile.Item1.UserName.Equals(profile.UserName))
                    UnSetLiveChatProfile();
            }
        }

    }
}
