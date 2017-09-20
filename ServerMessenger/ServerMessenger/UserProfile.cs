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
        public ICollection<UserProfile> Friends { get; set; }
        public ICollection<UserProfile> PendingFriendRequest { get; set; }        
        public ICollection<Tuple<DateTime, string>> PendingMessages { get; set; }
        public IDictionary<UserProfile, Tuple<string, DateTime>> ChatLog { get; set; }

        public UserProfile(string userName, string password)
        {
            ValidateAttributesLength(userName, password);
            this.Friends = new List<UserProfile>();
            this.PendingFriendRequest = new List<UserProfile>();
            this.PendingMessages = new List<Tuple<DateTime, string>>();
            this.ChatLog = new Dictionary<UserProfile, Tuple<string, DateTime>>();
            this.UserName = userName;
            this.Password = password;
            this.NumberOfConnections = 1;
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
        }

        public bool IsFriendWith(string username)
        {
            return this.Friends.Any(friend => friend.UserName.Equals(username));
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
        public void AcceptFriendRequest(UserProfile profile)
        {
            if (IsFriendWith(profile.UserName))
                throw new Exception("Error: (" + profile.UserName + ") is already in your friend list");

            if (!IsFriendRequestedBy(profile.UserName))
                throw new Exception("Error: ("+profile.UserName+") not in your friend requests");

            var pendingFriend = PendingFriendRequest.First(prof => prof.UserName.Equals(profile.UserName));
            AddFriendAndRemoveFriendRequest(pendingFriend);
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
