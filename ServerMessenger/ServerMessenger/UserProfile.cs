using MessengerDomain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMessenger
{
    public class UserProfile
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public int NumberOfConnections { get; set; }
        public DateTime SessionBegin { get; set; }
        public ICollection<UserProfile> Friends { get; set; }
        public ICollection<UserProfile> PendingFriendRequest { get; set; }        
    //    public ICollection<Tuple<DateTime, string>> PendingMessages { get; set; }
        public List<KeyValuePair<UserProfile, Tuple<DateTime,string>>> PendingMessages { get; set; }   
        public Tuple<UserProfile, bool> LiveChatProfile { get; set; }

        public UserProfile(string userName, string password)
        {
            ValidateAttributesLength(userName, password);
            this.Friends = new List<UserProfile>();
            this.PendingFriendRequest = new List<UserProfile>();
            //      this.PendingMessages = new List<Tuple<DateTime, string>>();
            this.PendingMessages = new List<KeyValuePair<UserProfile, Tuple<DateTime, string>>>();
            this.UserName = userName;
            this.Password = password;
            this.NumberOfConnections = 1;
            this.SessionBegin = DateTime.Now;
            this.LiveChatProfile = null;
        }
        
        public void AddPendingMessage(UserProfile sender, string msg)
        {
            var chatLog = new Tuple<DateTime, string>(DateTime.Now, msg);
            var chatMsg = new KeyValuePair<UserProfile, Tuple<DateTime, string>>(sender, chatLog);
            PendingMessages.Add(chatMsg);
        }
        public List<Tuple<DateTime,string>> GetPendingMessagesOfFriend(UserProfile sender)
        {
            if (!IsFriendWith(sender))
                throw new Exception("Error: This user is not your friend");

            var chatLogs = PendingMessages.FindAll(log => log.Key.UserName.Equals(sender));
            return chatLogs.Select(kvp => kvp.Value).ToList();      
        }

        public bool IsOnLiveChat()
        {
            return HasLiveChatProfileSet() && LiveChatProfile.Item2;
        }
        public UserProfile PendingLiveChatProfile()
        {
            return (HasLiveChatProfileSet()) ? LiveChatProfile.Item1 : null;
        }
        public bool HasLiveChatProfileSet()
        {
            return LiveChatProfile != null;
        }
        public void SetLiveChatProfile(UserProfile profile, bool establish)
        {
            LiveChatProfile = new Tuple<UserProfile, bool>(profile, establish); 
        }
        public void UnSetLiveChatProfile()
        {
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
            return this.Friends.Any(friend => friend.UserName.Equals(profile.UserName));
        }
        public bool IsFriendWith(string username)
        {
            return this.Friends.Any(friend => friend.UserName.Equals(username));
        }
        public void AddFriend(UserProfile profile)
        {
            this.Friends.Add(profile);
        }

        public bool IsFriendRequestedBy(string username)
        {
            return this.PendingFriendRequest.Any(pending => pending.UserName.Equals(username));
        }

        public void AddFriendRequest(UserProfile profile)
        {
            if (IsFriendWith(profile.UserName))
                throw new Exception("Error: ("+ profile.UserName + ") is already in your friend list");

            if(IsFriendRequestedBy(profile.UserName))
                throw new Exception("Error: already sent a friend request to (" +this.UserName+ ")");

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
                PendingFriendRequest.Remove(profile);
        }

        public void AcceptFriendRequest(UserProfile profile)
        {
            ReplyFriendRequest(profile, true);
        }

        private void AddFriendAndRemoveFriendRequest(UserProfile pendingFriend)
        {
            Friends.Add(pendingFriend);
            PendingFriendRequest.Remove(pendingFriend);
        }

        public int FriendsAmmount()
        {
            return Friends.Count;
        }
    }
}
