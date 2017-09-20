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
        public static int PROFILE_ATTRIBUTES_MIN_LENGTH = 4;
   
    }
}
