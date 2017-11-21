using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Messaging;

namespace ServerMSQM
{
    public class LogManager
    {
        private static string queueName;
        MessageQueue msgQueue;

        public LogManager()
        {
            queueName = @".\private$\networkMessenger";
            InitMessageQueue();
        }

        public LogManager(string remoteIp)
        {
            queueName = @"FormatName:DIRECT=TCP:"+remoteIp+@"\private$\networkMessenger";
        }
        private void InitMessageQueue()
        {
            if (!MessageQueue.Exists(queueName))
            {
                msgQueue = MessageQueue.Create(queueName);
            }
        }
        public void SendMessage(string operation, string user, string detail)
        {
            using (MessageQueue myQueue = new MessageQueue(queueName))
            {
                string messageData = "Operation: " + operation + " - User: " + user + " - Details: " + detail;
                Message msg = new Message(messageData);
                myQueue.Send(msg);
            }
        }
        public string ReadMessage()
        {
            using (msgQueue = new MessageQueue(queueName))
            {
                msgQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(string) });
                string msgLog;
                try
                {
                    Message msg = msgQueue.Receive(TimeSpan.Zero);
                    msgLog = (string)msg.Body;
                }
                catch(MessageQueueException)
                {
                    msgLog = String.Empty;
                }   
                return msgLog;
            }
        }

        public List<string> GetAllMessages()
        {
            using (msgQueue = new MessageQueue(queueName))
            {
                msgQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(string) });
                Message[] storedLogs = msgQueue.GetAllMessages();
                return storedLogs.Select(msg => msg.Body).Cast<string>().ToList();
            }
        }

        public void RemoveAllMessages()
        {
            using (msgQueue = new MessageQueue(queueName))
            {
                msgQueue.Purge();
            }
        }
    }
}
