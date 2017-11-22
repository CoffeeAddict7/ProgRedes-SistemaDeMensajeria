using System;
using System.Messaging;

namespace ServerMSMQ
{
    class LogView
    {
        private static string queueName;
        private static MessageQueue msgQueue;

        static void Main(string[] args)
        {
            queueName = @".\private$\networkMessenger";
            InitMessageQueue();
            Console.WriteLine("> Server aplication log online");
            Console.WriteLine("> Receiving messages ...");
                    
            msgQueue.PeekCompleted += new PeekCompletedEventHandler(MessageHasBeenReceived);                        
            msgQueue.BeginPeek();
            Console.ReadKey();
        }
        private static void InitMessageQueue()
        {
            if (!MessageQueue.Exists(queueName))            
                msgQueue = MessageQueue.Create(queueName);            
            else            
                msgQueue = new MessageQueue(queueName);
            msgQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(String) });
        }
        private static void MessageHasBeenReceived(object sender, PeekCompletedEventArgs e)
        {
            var msg = msgQueue.EndPeek(e.AsyncResult);            
            Console.WriteLine((string)msg.Body);            
            msgQueue.ReceiveById(msg.Id);
            msgQueue.BeginPeek();
        }

    }
}
