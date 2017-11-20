using System;
using System.Windows.Forms;

namespace WSMessengerClient
{
    public partial class ModifyUserProfile : UserControl
    {
        ServiceUserRepository.WSUserProfileClient service;
        public ModifyUserProfile(ServiceUserRepository.WSUserProfileClient myServ)
        {
            InitializeComponent();
            service = myServ;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            var userToFind = txtUserProfile.Text;
            try
            {
                var result = service.GetUserProfile(userToFind);
                txtUsername.Text = result.UserName;
                txtPassword.Text = result.Password;
            }
            catch (Exception ex)
            {
                txtUsername.Text = String.Empty;
                txtPassword.Text = String.Empty;
                MessageBox.Show(ex.Message);
            }
        }

        private void btnModify_Click(object sender, EventArgs e)
        {
            var userToModify = txtUserProfile.Text;
            var newUserName = txtUsername.Text;
            var newPassword = txtPassword.Text;
            try
            {
                if(CommonBehavior.UserProfileValidInfo(newUserName, newPassword))
                {
                    service.ModifyUserProfile(userToModify, newUserName, newPassword);
                    MessageBox.Show("User info modified!");
                }
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
