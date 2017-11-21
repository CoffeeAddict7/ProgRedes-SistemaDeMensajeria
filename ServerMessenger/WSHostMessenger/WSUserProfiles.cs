using MessengerDomain;
using Persistence;
using System;
using System.ServiceModel;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using ServerMSQM;

namespace WSHostMessenger
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class WSUserProfiles : IWSUserProfile
    {
        private IUserRepository UserRepository;
        private LogManager serverLog;
        public WSUserProfiles()
        {
            ActivateRemotingUserRepository();
            serverLog = new LogManager("127.0.0.1");
        }

        private void ActivateRemotingUserRepository()
        {
            TcpClientChannel channel = new TcpClientChannel();
            ChannelServices.RegisterChannel(channel, false);
            UserRepository = (IUserRepository)Activator.GetObject(typeof(IUserRepository), "tcp://127.0.0.1:7777/Users");
        }

        public UserProfile GetUserProfile(string username)
        {
            try
            {
               return UserRepository.GetUserProfile(username);
            }
            catch (Exception ex)
            {
                throw new FaultException("Error: User not found");
            }
        }

        public void DeleteUserProfile(string username)
        {
            try
            {
                UserRepository.DeleteUserProfile(username);
                serverLog.SendMessage("Delete user", username, "Deletion successfully");
            }
            catch (Exception ex)
            {
                serverLog.SendMessage("Delete user", username, ex.Message);
                throw new FaultException(ex.Message);
            }
        }

        public void CreateUserProfile(string username, string password)
        {
            try
            {
                UserRepository.CreateUserProfile(username, password);
                serverLog.SendMessage("Create user", username, "Registration successfully");
            }
            catch (Exception ex)
            {
                serverLog.SendMessage("Create user", username, ex.Message);
                throw new FaultException(ex.Message);
            }
        }

        public void ModifyUserProfile(string profile, string newUserName, string newPassword)
        {
            try
            {
                UserRepository.ModifyUserProfile(profile, newUserName, newPassword);
                serverLog.SendMessage("Modify user", profile, "Name/Password modified successfully to {"+newUserName +"}/{****}");
            }
            catch (Exception ex)
            {
                serverLog.SendMessage("Modify user", profile, ex.Message);
                throw new FaultException(ex.Message);
            }
        }

    }
}
