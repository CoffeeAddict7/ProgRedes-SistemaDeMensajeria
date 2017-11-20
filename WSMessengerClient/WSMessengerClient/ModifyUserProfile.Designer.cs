namespace WSMessengerClient
{
    partial class ModifyUserProfile
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBoxUserInfo = new System.Windows.Forms.GroupBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.btnModify = new System.Windows.Forms.Button();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.lblUserName = new System.Windows.Forms.Label();
            this.btnSearch = new System.Windows.Forms.Button();
            this.txtUserProfile = new System.Windows.Forms.TextBox();
            this.lblUserProfile = new System.Windows.Forms.Label();
            this.groupBoxUserInfo.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxUserInfo
            // 
            this.groupBoxUserInfo.Controls.Add(this.txtPassword);
            this.groupBoxUserInfo.Controls.Add(this.lblPassword);
            this.groupBoxUserInfo.Controls.Add(this.btnModify);
            this.groupBoxUserInfo.Controls.Add(this.txtUsername);
            this.groupBoxUserInfo.Controls.Add(this.lblUserName);
            this.groupBoxUserInfo.ForeColor = System.Drawing.Color.Maroon;
            this.groupBoxUserInfo.Location = new System.Drawing.Point(17, 112);
            this.groupBoxUserInfo.Name = "groupBoxUserInfo";
            this.groupBoxUserInfo.Size = new System.Drawing.Size(512, 246);
            this.groupBoxUserInfo.TabIndex = 13;
            this.groupBoxUserInfo.TabStop = false;
            this.groupBoxUserInfo.Text = "User info";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(236, 114);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(252, 31);
            this.txtPassword.TabIndex = 10;
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.ForeColor = System.Drawing.Color.Black;
            this.lblPassword.Location = new System.Drawing.Point(31, 114);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(106, 25);
            this.lblPassword.TabIndex = 9;
            this.lblPassword.Text = "Password";
            // 
            // btnModify
            // 
            this.btnModify.ForeColor = System.Drawing.Color.Black;
            this.btnModify.Location = new System.Drawing.Point(369, 162);
            this.btnModify.Name = "btnModify";
            this.btnModify.Size = new System.Drawing.Size(119, 50);
            this.btnModify.TabIndex = 8;
            this.btnModify.Text = "Modify";
            this.btnModify.UseVisualStyleBackColor = true;
            this.btnModify.Click += new System.EventHandler(this.btnModify_Click);
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(236, 65);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(252, 31);
            this.txtUsername.TabIndex = 7;
            // 
            // lblUserName
            // 
            this.lblUserName.AutoSize = true;
            this.lblUserName.ForeColor = System.Drawing.Color.Black;
            this.lblUserName.Location = new System.Drawing.Point(26, 65);
            this.lblUserName.Name = "lblUserName";
            this.lblUserName.Size = new System.Drawing.Size(110, 25);
            this.lblUserName.TabIndex = 5;
            this.lblUserName.Text = "Username";
            // 
            // btnSearch
            // 
            this.btnSearch.Location = new System.Drawing.Point(426, 7);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(103, 49);
            this.btnSearch.TabIndex = 12;
            this.btnSearch.Text = "Search";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // txtUserProfile
            // 
            this.txtUserProfile.Location = new System.Drawing.Point(142, 17);
            this.txtUserProfile.Name = "txtUserProfile";
            this.txtUserProfile.Size = new System.Drawing.Size(261, 31);
            this.txtUserProfile.TabIndex = 11;
            // 
            // lblUserProfile
            // 
            this.lblUserProfile.AutoSize = true;
            this.lblUserProfile.Location = new System.Drawing.Point(12, 17);
            this.lblUserProfile.Name = "lblUserProfile";
            this.lblUserProfile.Size = new System.Drawing.Size(124, 25);
            this.lblUserProfile.TabIndex = 10;
            this.lblUserProfile.Text = "User Profile";
            // 
            // ModifyUserProfile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxUserInfo);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.txtUserProfile);
            this.Controls.Add(this.lblUserProfile);
            this.Name = "ModifyUserProfile";
            this.Size = new System.Drawing.Size(836, 827);
            this.groupBoxUserInfo.ResumeLayout(false);
            this.groupBoxUserInfo.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxUserInfo;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.Button btnModify;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label lblUserName;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.TextBox txtUserProfile;
        private System.Windows.Forms.Label lblUserProfile;
    }
}
