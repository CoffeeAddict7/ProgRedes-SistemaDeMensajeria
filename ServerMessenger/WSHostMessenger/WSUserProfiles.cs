using MessengerDomain;
using Persistence;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace WSHostMessenger
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class WSUserProfiles : IWSUserProfile
    {
        private IUserRepository UserRepository;
        public WSUserProfiles()
        {
            UserRepository = new UserRepository();
            //Activate remoting
        }

        public UserProfile GetUserProfile(string username)
        {
            try
            {
               return UserRepository.GetUserProfile(username);
            }
            catch (Exception)
            {
                throw new FaultException("Error: User not found");
            }
        }

        public void DeleteUserProfile(string username)
        {
            try
            {
                UserRepository.DeleteUserProfile(username);
            }
            catch (Exception ex)
            {
                throw new FaultException(ex.Message);
            }
        }

        public void CreateUserProfile(string username, string password)
        {
            try
            {
                UserRepository.CreateUserProfile(username, password);
            }
            catch (Exception ex)
            {
                throw new FaultException(ex.Message);
            }
        }

        public void ModifyUserProfile(string profile, string newUserName, string newPassword)
        {
            try
            {
                UserRepository.ModifyUserProfile(profile, newUserName, newPassword);
            }
            catch (Exception ex)
            {
                throw new FaultException(ex.Message);
            }
        }

    }
}
