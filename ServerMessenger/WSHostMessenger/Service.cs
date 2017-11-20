using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using Persistence;
namespace WSHostMessenger
{
    class Service
    {
        
        static void Main(string[] args)
        {
            ServiceHost messengerServiceHost = null;
            try
            {
                //Base Address for MessengerService
                Uri httpBaseAddress = new Uri("http://localhost:8719/MessengerService");
     
                //Instantiate ServiceHost
                messengerServiceHost = new ServiceHost(typeof(WSHostMessenger.WSUserProfiles),
                    httpBaseAddress);

                //Add Endpoint to Host
                messengerServiceHost.AddServiceEndpoint(typeof(WSHostMessenger.IWSUserProfile),
                                                        new WSHttpBinding(), "");

                //Metadata Exchange
                ServiceMetadataBehavior serviceBehavior = new ServiceMetadataBehavior();
                serviceBehavior.HttpGetEnabled = true;
                messengerServiceHost.Description.Behaviors.Add(serviceBehavior);

                //Open
                messengerServiceHost.Open();
                Console.WriteLine("Service is live now at: {0}", httpBaseAddress);
                Console.ReadKey();
               
            }
            catch (Exception ex)
            {
                messengerServiceHost = null;
                Console.WriteLine("There is an issue with MessengerService " + ex.Message);
                Console.ReadKey();
            }
        }
    }
}
