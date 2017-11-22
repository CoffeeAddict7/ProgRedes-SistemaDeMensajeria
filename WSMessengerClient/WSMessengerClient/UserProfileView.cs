using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WSMessengerClient
{
    public partial class UserProfileView : Form
    {
        ServiceUserRepository.WSUserProfileClient service;
        public UserProfileView(ServiceUserRepository.WSUserProfileClient myServ)
        {
            service = myServ;
            InitializeComponent();
            this.panelMainMenu.Controls.Add(new CreateUserProfile(service));
            try
            {
                string path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
                this.Icon = new Icon(Path.Combine(path,"user.ico"));
            }
            catch (Exception) { }
        }


        private void stripCreate_Click(object sender, EventArgs e)
        {
            this.panelMainMenu.Controls.Clear();
            UserControl createProfile = new CreateUserProfile(service);
            this.panelMainMenu.Controls.Add(createProfile);
        }
        private void stripDelete_Click(object sender, EventArgs e)
        {
            this.panelMainMenu.Controls.Clear();
            UserControl deleteProfile = new DeleteUserProfile(service);
            this.panelMainMenu.Controls.Add(deleteProfile);
        }

        private void stripModify_Click(object sender, EventArgs e)
        {
            this.panelMainMenu.Controls.Clear();
            UserControl modifyProfile = new ModifyUserProfile(service);
            this.panelMainMenu.Controls.Add(modifyProfile);
        }

    }
}
