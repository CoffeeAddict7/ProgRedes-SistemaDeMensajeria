using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Configuration;

namespace WSHostMessenger
{
    class Service
    {        
        static void Main(string[] args)
        {
            ServiceHost messengerServiceHost = null;
            try
            {
                ConfigurationManager.RefreshSection("appSettings");
                string serviceIp = ConfigurationManager.AppSettings["ServiceIp"];

                Uri httpBaseAddress = new Uri("http://"+ serviceIp + ":8719/MessengerService");
     
                messengerServiceHost = new ServiceHost(typeof(WSHostMessenger.WSUserProfiles), httpBaseAddress);

                messengerServiceHost.AddServiceEndpoint(typeof(WSHostMessenger.IWSUserProfile), new WSHttpBinding(), "");

                ServiceMetadataBehavior serviceBehavior = new ServiceMetadataBehavior();
                serviceBehavior.HttpGetEnabled = true;
                messengerServiceHost.Description.Behaviors.Add(serviceBehavior);

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
