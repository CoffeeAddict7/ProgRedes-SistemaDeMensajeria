namespace WSMessengerClient
{
    partial class DeleteUserProfile
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
            this.btnDelete = new System.Windows.Forms.Button();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.txtNumConnections = new System.Windows.Forms.TextBox();
            this.lblUserName = new System.Windows.Forms.Label();
            this.lblNumConnections = new System.Windows.Forms.Label();
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
            this.groupBoxUserInfo.Controls.Add(this.btnDelete);
            this.groupBoxUserInfo.Controls.Add(this.txtUsername);
            this.groupBoxUserInfo.Controls.Add(this.txtNumConnections);
            this.groupBoxUserInfo.Controls.Add(this.lblUserName);
            this.groupBoxUserInfo.Controls.Add(this.lblNumConnections);
            this.groupBoxUserInfo.ForeColor = System.Drawing.Color.Maroon;
            this.groupBoxUserInfo.Location = new System.Drawing.Point(19, 117);
            this.groupBoxUserInfo.Name = "groupBoxUserInfo";
            this.groupBoxUserInfo.Size = new System.Drawing.Size(512, 292);
            this.groupBoxUserInfo.TabIndex = 9;
            this.groupBoxUserInfo.TabStop = false;
            this.groupBoxUserInfo.Text = "User info";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(239, 178);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(252, 31);
            this.txtPassword.TabIndex = 10;
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.ForeColor = System.Drawing.Color.Black;
            this.lblPassword.Location = new System.Drawing.Point(34, 178);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(106, 25);
            this.lblPassword.TabIndex = 9;
            this.lblPassword.Text = "Password";
            // 
            // btnDelete
            // 
            this.btnDelete.ForeColor = System.Drawing.Color.Black;
            this.btnDelete.Location = new System.Drawing.Point(372, 226);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(119, 50);
            this.btnDelete.TabIndex = 8;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(239, 129);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(252, 31);
            this.txtUsername.TabIndex = 7;
            // 
            // txtNumConnections
            // 
            this.txtNumConnections.Location = new System.Drawing.Point(239, 81);
            this.txtNumConnections.Name = "txtNumConnections";
            this.txtNumConnections.Size = new System.Drawing.Size(100, 31);
            this.txtNumConnections.TabIndex = 6;
            // 
            // lblUserName
            // 
            this.lblUserName.AutoSize = true;
            this.lblUserName.ForeColor = System.Drawing.Color.Black;
            this.lblUserName.Location = new System.Drawing.Point(29, 129);
            this.lblUserName.Name = "lblUserName";
            this.lblUserName.Size = new System.Drawing.Size(110, 25);
            this.lblUserName.TabIndex = 5;
            this.lblUserName.Text = "Username";
            // 
            // lblNumConnections
            // 
            this.lblNumConnections.AutoSize = true;
            this.lblNumConnections.ForeColor = System.Drawing.Color.Black;
            this.lblNumConnections.Location = new System.Drawing.Point(29, 81);
            this.lblNumConnections.Name = "lblNumConnections";
            this.lblNumConnections.Size = new System.Drawing.Size(191, 25);
            this.lblNumConnections.TabIndex = 4;
            this.lblNumConnections.Text = "Connections made";
            // 
            // btnSearch
            // 
            this.btnSearch.Location = new System.Drawing.Point(428, 12);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(103, 49);
            this.btnSearch.TabIndex = 8;
            this.btnSearch.Text = "Search";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // txtUserProfile
            // 
            this.txtUserProfile.Location = new System.Drawing.Point(144, 22);
            this.txtUserProfile.Name = "txtUserProfile";
            this.txtUserProfile.Size = new System.Drawing.Size(261, 31);
            this.txtUserProfile.TabIndex = 7;
            // 
            // lblUserProfile
            // 
            this.lblUserProfile.AutoSize = true;
            this.lblUserProfile.Location = new System.Drawing.Point(14, 22);
            this.lblUserProfile.Name = "lblUserProfile";
            this.lblUserProfile.Size = new System.Drawing.Size(124, 25);
            this.lblUserProfile.TabIndex = 6;
            this.lblUserProfile.Text = "User Profile";
            // 
            // DeleteUserProfile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxUserInfo);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.txtUserProfile);
            this.Controls.Add(this.lblUserProfile);
            this.Name = "DeleteUserProfile";
            this.Size = new System.Drawing.Size(699, 640);
            this.groupBoxUserInfo.ResumeLayout(false);
            this.groupBoxUserInfo.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxUserInfo;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.TextBox txtNumConnections;
        private System.Windows.Forms.Label lblUserName;
        private System.Windows.Forms.Label lblNumConnections;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.TextBox txtUserProfile;
        private System.Windows.Forms.Label lblUserProfile;
    }
}
