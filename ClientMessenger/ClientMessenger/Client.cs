using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientMessenger
{
    class Client
    {
        private static Socket tcpClient;
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ClientAppView());
            ConnectToServer();
        }

        private static bool loggedIn = true;

        private static void ConnectToServer()
        {
            InitializeClientConfiguration();

            //Make register & login || login and then proceed to while loop            

            NetworkStream netStream = new NetworkStream(tcpClient);
            
            while (loggedIn)
            {
                string message = Console.ReadLine();
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                NetworkStream stream = new NetworkStream(tcpClient);
                stream.Write(data, 0, data.Length);
                Console.WriteLine("Sent: {0}", message);

                data = new Byte[256];                
                String responseData = String.Empty;
                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);
                            
                //Ask for commands
                //Send Message
                //Receive response
                //Process Response
                stream.Close();
                tcpClient.Close();
            }     
        }

        private static void InitializeClientConfiguration()
        {
            tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var clientEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            tcpClient.Bind(clientEndPoint);
            Console.WriteLine("Connecting to server...");
            tcpClient.Connect(ServerMessenger.Server.SERVER_IP_END_POINT);
            Console.WriteLine("Connected to server");
        }
    }
}
