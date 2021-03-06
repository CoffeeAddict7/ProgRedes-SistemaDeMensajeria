﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Configuration;

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
        public static string LIVECHAT_END = "END";
        public static string LIVECHAT_CHAT = "CHAT";
        public static string CMD_LIVECHAT = "08";
        public static string CMD_UNSEEN_MSGS = "09";
        public static string CMD_UPLOAD_FILE = "10";
        public static string BEGIN_LIVECHAT = "Begin chat...";
        public static string ENDED_LIVECHAT = "Chat ended.";
        public static string UNSEEN_MESSAGES = "Those where your friend unseen messages";
        public static string FILES_FOR_DOWNLOAD = "FILES";
        public static string FILE_TO_DOWNLOAD = "FILE";
        public static string PENDING_MSGS_USERS = "USERS";
        public static string PENDING_MSGS_PROFILE_MSGS = "PROFILE";
        public static ICollection<string> acceptedFriendRequestReply = new List<String> { FRIEND_REQUEST_YES_REPLY, FRIEND_REQUEST_NO_REPLY};


        public static string GetRequestOperationFromCommand(string cmd)
        {
            string operation;
            switch (cmd)
            {
                case "00":
                    operation = "Logout";
                    break;
                case "01":
                    operation = "Login";
                    break;
                case "02":
                    operation = "Register";
                    break;
                case "03":
                    operation = "Connected users";
                    break;
                case "04":
                    operation = "Friend list";
                    break;
                case "05":
                    operation = "Send friend request";
                    break;
                case "06":
                    operation = "Pending friend requests";
                    break;
                case "07":
                    operation = "Friend request reply";
                    break;
                case "08":
                    operation = "Chat mode";
                    break;
                case "09":
                    operation = "Unseen messages";
                    break;
                case "10":
                    operation = "Upload file";
                    break;
                case "11":
                    operation = "View/Download files";
                    break;
                default:
                    operation = "Unidentified";
                    break;
            }
            return operation;
        }




    }
}
