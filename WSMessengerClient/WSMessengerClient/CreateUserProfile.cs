using System;
using System.Windows.Forms;

namespace WSMessengerClient
{
    public partial class CreateUserProfile : UserControl
    {
        ServiceUserRepository.WSUserProfileClient service;

        public CreateUserProfile(ServiceUserRepository.WSUserProfileClient myServ)
        {
            InitializeComponent();
            service = myServ;
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            var username = txtUserName.Text;
            var password = txtPassword.Text;

            try
            {
                if (CommonBehavior.UserProfileValidInfo(username, password))
                {
                    service.CreateUserProfile(username, password);
                    MessageBox.Show("User created successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
