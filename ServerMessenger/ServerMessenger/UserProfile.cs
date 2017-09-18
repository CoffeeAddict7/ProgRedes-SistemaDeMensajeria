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

        public bool IsFriendWith(string userName)
        {
            return this.Friends.Any(friend => friend.UserName.Equals(UserName));
        }

        public void AddFriendRequest(UserProfile profile)
        {
            //VALIDATIONS
            this.PendingFriendRequest.Add(profile);
        }

        public int FriendsAmmount()
        {
            return Friends.Count;
        }



    }

}
