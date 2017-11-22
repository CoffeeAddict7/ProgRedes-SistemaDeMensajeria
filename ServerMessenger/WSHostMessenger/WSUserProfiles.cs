using MessengerDomain;
using Persistence;
using System;
using System.ServiceModel;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Configuration;

namespace WSHostMessenger
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class WSUserProfiles : IWSUserProfile
    {
        private IUserRepository UserRepository;
        public WSUserProfiles()
        {
            ActivateRemotingUserRepository();
        }

        private void ActivateRemotingUserRepository()
        {
            ConfigurationManager.RefreshSection("appSettings");
            string remotingIp = ConfigurationManager.AppSettings["RemotingIp"];
            TcpClientChannel channel = new TcpClientChannel();
            ChannelServices.RegisterChannel(channel, false);
            UserRepository = (IUserRepository)Activator.GetObject(typeof(IUserRepository), "tcp://"+remotingIp+":7777/Users");
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
