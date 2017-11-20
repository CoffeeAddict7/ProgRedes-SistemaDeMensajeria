using MessengerDomain;
using System.Collections.Generic;
using System.ServiceModel;

namespace WSHostMessenger
{
    [ServiceContract]
    public interface IWSUserProfile
    {
        [OperationContract]
        UserProfile GetUserProfile(string username);

        [OperationContract]
        void CreateUserProfile(string username, string password);

        [OperationContract]
        void ModifyUserProfile(string profile, string newUserName, string newPassword);

        [OperationContract]
        void DeleteUserProfile(string username);

    }
}
