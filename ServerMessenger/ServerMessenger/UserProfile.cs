using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMessenger
{
    public class UserProfile
    {
        public string UserName { get; }
        private string Password;
        public ICollection<UserProfile> Friends { get; }
        public ICollection<UserProfile> PendingFriendRequest { get; }
        

        public UserProfile(string userName, string password)
        {
            this.Friends = new List<UserProfile>();
            this.PendingFriendRequest = new List<UserProfile>();
            this.UserName = userName;
            this.Password = password;
        }
        
        public bool IsFriendWith(string userName)
        {
            return this.Friends.Any(friend => friend.UserName.Equals(UserName));
        }

        public int FriendsAmmount()
        {
            return Friends.Count;
        }


    }

}
