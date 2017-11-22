using System.Messaging;

namespace ServerMSMQ
{
    public class AppLog
    {
        private static MessageQueue msgQueue;
        public AppLog(string remoteIp)
        {
            string queueName = @"FormatName:DIRECT=TCP:"+remoteIp+@"\private$\networkMessenger";
            msgQueue = new MessageQueue(queueName);
        }

        public void SendDetailedMessage(string operation, string user, string detail)
        {
            string messageData = "Operation: " + operation + " - User: " + user + " - Details: " + detail;
            Message msg = new Message(messageData);
            msgQueue.Send(msg);
        }
        public void SendErrorMessage(string operation, string user, string error)
        {
            string messageData = "Operation: " + operation + " - User: " + user + " - " + error;
            Message msg = new Message(messageData);
            msgQueue.Send(msg);
        }
    }
}
