namespace WSMessengerClient
{
    partial class UserProfileView
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        { 
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.stripCreate = new System.Windows.Forms.ToolStripLabel();
            this.stripModify = new System.Windows.Forms.ToolStripLabel();
            this.stripDelete = new System.Windows.Forms.ToolStripLabel();
            this.panelMainMenu = new System.Windows.Forms.Panel();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stripCreate,
            this.stripModify,
            this.stripDelete});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(676, 35);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // stripCreate
            // 
            this.stripCreate.Name = "stripCreate";
            this.stripCreate.Size = new System.Drawing.Size(84, 32);
            this.stripCreate.Text = "Create";
            this.stripCreate.Click += new System.EventHandler(this.stripCreate_Click);
            // 
            // stripModify
            // 
            this.stripModify.Name = "stripModify";
            this.stripModify.Size = new System.Drawing.Size(91, 32);
            this.stripModify.Text = "Modify";
            this.stripModify.Click += new System.EventHandler(this.stripModify_Click);
            // 
            // stripDelete
            // 
            this.stripDelete.Name = "stripDelete";
            this.stripDelete.Size = new System.Drawing.Size(85, 32);
            this.stripDelete.Text = "Delete";
            this.stripDelete.Click += new System.EventHandler(this.stripDelete_Click);
            // 
            // panelMainMenu
            // 
            this.panelMainMenu.Location = new System.Drawing.Point(12, 57);
            this.panelMainMenu.Name = "panelMainMenu";
            this.panelMainMenu.Size = new System.Drawing.Size(652, 538);
            this.panelMainMenu.TabIndex = 4;
            // 
            // UserProfileView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(676, 629);
            this.Controls.Add(this.panelMainMenu);
            this.Controls.Add(this.toolStrip1);
            this.Name = "UserProfileView";
            this.Text = "User profile";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel stripDelete;
        private System.Windows.Forms.ToolStripLabel stripModify;
        private System.Windows.Forms.ToolStripLabel stripCreate;
        private System.Windows.Forms.Panel panelMainMenu;
    }
}

