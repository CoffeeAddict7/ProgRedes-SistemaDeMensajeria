using System;
using System.Windows.Forms;

namespace WSMessengerClient
{
    public partial class DeleteUserProfile : UserControl
    {        
        ServiceUserRepository.WSUserProfileClient service;
        public DeleteUserProfile(ServiceUserRepository.WSUserProfileClient myServ)
        {
            InitializeComponent();
            txtNumConnections.ReadOnly = true;
            txtUsername.ReadOnly = true;
            service = myServ;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var userToDelete = txtUserProfile.Text;
            var confirmResult = MessageBox.Show("Are you sure to delete the selected user?",
                                        "Confirm Deletion!",
                                        MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {
                try
                {
                    service.DeleteUserProfile(userToDelete);
                    MessageBox.Show("User '" + userToDelete + "' deleted successfully");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }            
            CleanFieldsView();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            var userToFind = txtUserProfile.Text;
            try
            {
                var result = service.GetUserProfile(userToFind);
                txtNumConnections.Text = result.NumberOfConnections.ToString();
                txtUsername.Text = result.UserName;
            }
            catch (Exception ex)
            {
                CleanFieldsView();
                MessageBox.Show(ex.Message);
            }
        }

        private void CleanFieldsView()
        {
            txtNumConnections.Text = String.Empty;
            txtUsername.Text = String.Empty;
            txtUserProfile.Text = String.Empty;
        }
    }
}
