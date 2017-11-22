using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessengerDomain;

namespace Persistence
{
    public interface IUserRepository
    {
        UserProfile GetUserProfile(string username);
        void CreateUserProfile(string username, string password);
        void ModifyUserProfile(string profile, string newUserName, string newPassword);
        void DeleteUserProfile(string username);
    }
}
