using System;
using System.Windows.Forms;

namespace WSMessengerClient
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ServiceUserRepository.WSUserProfileClient myServ = new ServiceUserRepository.WSUserProfileClient();
            Application.Run(new UserProfileView(myServ)); 
        }
    }
}
