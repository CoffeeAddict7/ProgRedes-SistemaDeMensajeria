using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessengerDomain;

namespace Persistence
{
    public class UserRepository : MarshalByRefObject, IUserRepository 
    {
        private Context context;

        public UserRepository()
        {
            context = Context.GetInstance();
        }

        public void CreateUserProfile(string username, string password)
        {
            if (UsernameAlreadyRegistered(username))
                throw new Exception("Error: Username already registered");
            UserProfile toRegister = new UserProfile(username, password);
            context.AddUserProfile(toRegister);
        }

        private bool UsernameAlreadyRegistered(string username)
        {
            return context.GetUserProfiles().Exists(us => us.UserName.Equals(username));
        }

        public void DeleteUserProfile(string username)
        {
            if (!UsernameAlreadyRegistered(username))
                throw new Exception("Error: Username not registered");
            UserProfile toDelete = context.GetUserByName(username);
            if (ProfileIsOnUse(toDelete))
                throw new Exception("Error: Cannot delete profile while is logged in");
            context.DeleteUserProfile(toDelete); 
        }

        private bool ProfileIsOnUse(UserProfile toDelete)
        {
            return context.GetAuthorizedClients().Any(kvp => context.ProfilesAreEquals(kvp.Value, toDelete));
        }

        public UserProfile GetUserProfile(string username)
        {
            return context.GetUserByName(username);
        }

        public void ModifyUserProfile(string profile, string newUserName, string newPassword)
        {
            if (!UsernameAlreadyRegistered(profile))
                throw new Exception("Error: The username to modify doesn't exits");
            if (UsernameAlreadyRegistered(newUserName) && !profile.Equals(newUserName))
                throw new Exception("Error: The new username is already registered");
            UserProfile toModify = context.GetUserByName(profile);
            if (ProfileIsOnUse(toModify))
                throw new Exception("Error: Cannot update profile while is logged in");            
            context.ModifyUserProfile(toModify, newUserName, newPassword);                 
        }
    }
}
