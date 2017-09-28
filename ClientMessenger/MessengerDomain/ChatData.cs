using System;
using System.Collections.Generic;
using System.Net;

namespace MessengerDomain
{
    public class ChatData
    {
        public static IPEndPoint SERVER_IP_END_POINT = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6500);
        public static int MAX_ACTIVE_CONN = 100;
        public static int PROTOCOL_FIXED_BYTES = 9;
        public static string REQUEST_HEADER = "REQ";
        public static string RESPONSE_HEADER = "RES";
        public static string RESPONSE_OK = "OK";
        public static string RESPONSE_ERROR = "ERROR";
        public static int PROFILE_ATTRIBUTES_MIN_LENGTH = 4;
        public static string FRIEND_REQUEST_YES_REPLY = "YES";
        public static string FRIEND_REQUEST_NO_REPLY = "NO";
        public static ICollection<string> acceptedFriendRequestReply = new List<String> { FRIEND_REQUEST_YES_REPLY, FRIEND_REQUEST_NO_REPLY};
    }
}
