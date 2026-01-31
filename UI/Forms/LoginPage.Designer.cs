namespace TraderApps.Forms
{
    partial class LoginPage
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
            this.cmbLogin = new System.Windows.Forms.ComboBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.cancleButton = new System.Windows.Forms.Button();
            this.accountPictureBox = new System.Windows.Forms.PictureBox();
            this.messageLabel = new System.Windows.Forms.Label();
            this.serverLabel = new System.Windows.Forms.Label();
            this.cmbServerName = new System.Windows.Forms.ComboBox();
            this.loginLabel = new System.Windows.Forms.Label();
            this.passwordLabel = new System.Windows.Forms.Label();
            this.txtpassword = new System.Windows.Forms.TextBox();
            this.loginButton = new System.Windows.Forms.Button();
            this.eyePictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.accountPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.eyePictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // cmbLogin
            // 
            this.cmbLogin.FormattingEnabled = true;
            this.cmbLogin.Location = new System.Drawing.Point(155, 112);
            this.cmbLogin.Name = "cmbLogin";
            this.cmbLogin.Size = new System.Drawing.Size(147, 24);
            this.cmbLogin.TabIndex = 1;
            this.cmbLogin.Enter += new System.EventHandler(this.cmbLogin_Enter);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(344, 154);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(119, 20);
            this.checkBox1.TabIndex = 21;
            this.checkBox1.Text = "Remember Me";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // cancleButton
            // 
            this.cancleButton.BackColor = System.Drawing.Color.Red;
            this.cancleButton.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.cancleButton.Location = new System.Drawing.Point(291, 202);
            this.cancleButton.Name = "cancleButton";
            this.cancleButton.Size = new System.Drawing.Size(130, 40);
            this.cancleButton.TabIndex = 17;
            this.cancleButton.Text = "Cancel";
            this.cancleButton.UseVisualStyleBackColor = false;
            this.cancleButton.Click += new System.EventHandler(this.cancleButton_Click);
            // 
            // accountPictureBox
            // 
            this.accountPictureBox.Image = global::TraderApp.Properties.Resources.loginnew;
            this.accountPictureBox.Location = new System.Drawing.Point(25, 24);
            this.accountPictureBox.Name = "accountPictureBox";
            this.accountPictureBox.Size = new System.Drawing.Size(41, 32);
            this.accountPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.accountPictureBox.TabIndex = 13;
            this.accountPictureBox.TabStop = false;
            // 
            // messageLabel
            // 
            this.messageLabel.AutoSize = true;
            this.messageLabel.Location = new System.Drawing.Point(72, 24);
            this.messageLabel.MaximumSize = new System.Drawing.Size(400, 0);
            this.messageLabel.Name = "messageLabel";
            this.messageLabel.Size = new System.Drawing.Size(397, 32);
            this.messageLabel.TabIndex = 10;
            this.messageLabel.Text = "Please enter your Valid Credentials with valid Server Selection for Login.";
            // 
            // serverLabel
            // 
            this.serverLabel.AutoSize = true;
            this.serverLabel.Location = new System.Drawing.Point(72, 80);
            this.serverLabel.Name = "serverLabel";
            this.serverLabel.Size = new System.Drawing.Size(50, 16);
            this.serverLabel.TabIndex = 14;
            this.serverLabel.Text = "Server:";
            // 
            // cmbServerName
            // 
            this.cmbServerName.Location = new System.Drawing.Point(155, 77);
            this.cmbServerName.Name = "cmbServerName";
            this.cmbServerName.Size = new System.Drawing.Size(284, 24);
            this.cmbServerName.TabIndex = 0;
            // 
            // loginLabel
            // 
            this.loginLabel.AutoSize = true;
            this.loginLabel.Location = new System.Drawing.Point(72, 115);
            this.loginLabel.Name = "loginLabel";
            this.loginLabel.Size = new System.Drawing.Size(43, 16);
            this.loginLabel.TabIndex = 18;
            this.loginLabel.Text = "Login:";
            // 
            // passwordLabel
            // 
            this.passwordLabel.AutoSize = true;
            this.passwordLabel.Location = new System.Drawing.Point(72, 155);
            this.passwordLabel.Name = "passwordLabel";
            this.passwordLabel.Size = new System.Drawing.Size(70, 16);
            this.passwordLabel.TabIndex = 20;
            this.passwordLabel.Text = "Password:";
            // 
            // txtpassword
            // 
            this.txtpassword.Location = new System.Drawing.Point(155, 152);
            this.txtpassword.Name = "txtpassword";
            this.txtpassword.PasswordChar = '*';
            this.txtpassword.Size = new System.Drawing.Size(147, 22);
            this.txtpassword.TabIndex = 2;
            // 
            // loginButton
            // 
            this.loginButton.BackColor = System.Drawing.Color.DodgerBlue;
            this.loginButton.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.loginButton.Location = new System.Drawing.Point(155, 202);
            this.loginButton.Name = "loginButton";
            this.loginButton.Size = new System.Drawing.Size(130, 40);
            this.loginButton.TabIndex = 16;
            this.loginButton.Text = "Login";
            this.loginButton.UseVisualStyleBackColor = false;
            this.loginButton.Click += new System.EventHandler(this.loginButton_Click);
            // 
            // eyePictureBox
            // 
            this.eyePictureBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.eyePictureBox.Image = global::TraderApp.Properties.Resources.eye_close;
            this.eyePictureBox.Location = new System.Drawing.Point(306, 152);
            this.eyePictureBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.eyePictureBox.Name = "eyePictureBox";
            this.eyePictureBox.Size = new System.Drawing.Size(30, 22);
            this.eyePictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.eyePictureBox.TabIndex = 19;
            this.eyePictureBox.TabStop = false;
            this.eyePictureBox.Click += new System.EventHandler(this.eyePictureBox_Click);
            // 
            // LoginPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(542, 303);
            this.Controls.Add(this.cmbLogin);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.cancleButton);
            this.Controls.Add(this.accountPictureBox);
            this.Controls.Add(this.messageLabel);
            this.Controls.Add(this.serverLabel);
            this.Controls.Add(this.cmbServerName);
            this.Controls.Add(this.loginLabel);
            this.Controls.Add(this.passwordLabel);
            this.Controls.Add(this.txtpassword);
            this.Controls.Add(this.loginButton);
            this.Controls.Add(this.eyePictureBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "LoginPage";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Login";
            this.Load += new System.EventHandler(this.LoginPage_Load);
            ((System.ComponentModel.ISupportInitialize)(this.accountPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.eyePictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbLogin;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Button cancleButton;
        private System.Windows.Forms.PictureBox accountPictureBox;
        private System.Windows.Forms.Label messageLabel;
        private System.Windows.Forms.Label serverLabel;
        private System.Windows.Forms.ComboBox cmbServerName;
        private System.Windows.Forms.Label loginLabel;
        private System.Windows.Forms.Label passwordLabel;
        public System.Windows.Forms.TextBox txtpassword;
        private System.Windows.Forms.Button loginButton;
        private System.Windows.Forms.PictureBox eyePictureBox;
    }
}