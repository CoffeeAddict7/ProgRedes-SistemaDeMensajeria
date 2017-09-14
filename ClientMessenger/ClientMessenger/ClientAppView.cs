using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientMessenger
{
    public partial class ClientAppView : Form
    {
        public ClientAppView()
        {
            InitializeComponent();
            UserControl us = new MainWindows();
            this.Controls.Add(us);

        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            Client client = new ClientMessenger.Client();
            try
            {
                string userName = this.textUserName.Text;
                string password = this.textPassword.Text;
                int packageSize = 9 + userName.Count() + password.Count() + 1;
                string protocolPayloadSize = "0000";
                if (packageSize < 10)
                    protocolPayloadSize = "000" + packageSize;
                else if (packageSize < 100)
                    protocolPayloadSize = "00" + packageSize;
                else if (packageSize < 1000)
                    protocolPayloadSize = "0" + packageSize;
                else 
                    protocolPayloadSize = packageSize.ToString();
                //Check if > 10000 ? error msg

                string message = "REQ" + "01" + protocolPayloadSize + userName + "#" + password;

            }
            catch(Exception ex) { }
        }
    }
}
