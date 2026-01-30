using System.Drawing;
using System.Windows.Forms;
using TraderApps.UI.Theme;

namespace DesktopClient
{
    partial class DisclaimerForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.titleBar = new System.Windows.Forms.Panel();
            this.titleLabel = new System.Windows.Forms.Label();
            this.closeButton = new System.Windows.Forms.Button();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.contentPanel = new System.Windows.Forms.Panel();
            this.disclaimer = new System.Windows.Forms.Label();
            this.acknowledgeButton = new System.Windows.Forms.Button();

            this.titleBar.SuspendLayout();
            this.mainPanel.SuspendLayout();
            this.contentPanel.SuspendLayout();
            this.SuspendLayout();

            // 
            // titleBar
            // 
            this.titleBar.BackColor = ThemeManager.SkyBlue;
            this.titleBar.Controls.Add(this.titleLabel);
            this.titleBar.Controls.Add(this.closeButton);
            this.titleBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.titleBar.Height = 40;
            this.titleBar.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TitleBar_MouseDown);

            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            // BOLD TITLE
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.ForeColor = ThemeManager.White;
            this.titleLabel.Location = new System.Drawing.Point(10, 8); // Reduced left margin
            this.titleLabel.Padding = new System.Windows.Forms.Padding(2);
            this.titleLabel.Text = "Disclaimer";

            // 
            // closeButton
            // 
            this.closeButton.BackColor = System.Drawing.Color.Transparent;
            this.closeButton.Dock = System.Windows.Forms.DockStyle.Right;
            this.closeButton.FlatAppearance.BorderSize = 0;
            this.closeButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(232, 17, 35);
            this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.closeButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.closeButton.ForeColor = ThemeManager.Black;
            this.closeButton.TabStop = false;
            this.closeButton.Text = "X";
            this.closeButton.Width = 45;
            this.closeButton.Click += new System.EventHandler(this.CloseButton_Click);

            // 
            // mainPanel
            // 
            this.mainPanel.BackColor = ThemeManager.White;
            this.mainPanel.Controls.Add(this.contentPanel);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Padding = new System.Windows.Forms.Padding(0);
            this.mainPanel.AutoScroll = true;

            // 
            // contentPanel
            // 
            this.contentPanel.AutoSize = true;
            this.contentPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.contentPanel.BackColor = System.Drawing.Color.White;
            this.contentPanel.Controls.Add(this.disclaimer);
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Top;
            // REDUCED PADDING: 20px on all sides (was 40px)
            this.contentPanel.Padding = new System.Windows.Forms.Padding(20);

            // 
            // disclaimer
            // 
            this.disclaimer.AutoSize = true;
            this.disclaimer.Dock = System.Windows.Forms.DockStyle.Top;
            this.disclaimer.Font = ThemeManager.CommonFont;
            this.disclaimer.Location = new System.Drawing.Point(20, 20); // Matches new padding
            this.disclaimer.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.disclaimer.Text = "This application is developed solely for educational purposes. It is intended " +
                                 "to provide users with a basic understanding of financial instruments, trading " +
                                 "concepts, and market strategies. No real money is involved, and this app " +
                                 "does not support or facilitate any real-world trading or financial transactions.\r\n\r\n" +
                                 "All data, prices, and simulations presented within the app are purely " +
                                 "fictitious or for demonstration purposes and may not reflect real market " +
                                 "conditions. Users are advised not to rely on the content for actual " +
                                 "investment decisions.\r\n\r\n" +
                                 "This software is completely free and provided as a learning tool. We do not " +
                                 "promote or encourage any specific financial product, instrument, or service. " +
                                 "Use of the app is entirely at the user's discretion, and we are not " +
                                 "responsible for any financial decisions made outside this platform.";

            // 
            // acknowledgeButton
            // 
            this.acknowledgeButton.BackColor = ThemeManager.SkyBlue;
            this.acknowledgeButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.acknowledgeButton.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.acknowledgeButton.FlatAppearance.BorderSize = 0;
            this.acknowledgeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            // BOLD BUTTON TEXT
            this.acknowledgeButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.acknowledgeButton.ForeColor = ThemeManager.White;
            this.acknowledgeButton.Height = 40; // Reduced Height (was 50)
            this.acknowledgeButton.Margin = new System.Windows.Forms.Padding(20, 5, 20, 15); // Adjusted margins
            this.acknowledgeButton.Text = "I Acknowledge";
            this.acknowledgeButton.Click += new System.EventHandler(this.AcknowledgeButton_Click);

            // 
            // DisclaimerForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.acknowledgeButton);
            this.Controls.Add(this.titleBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Padding = new System.Windows.Forms.Padding(1);
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Disclaimer";
            this.Width = 600;
            this.Load += new System.EventHandler(this.DisclaimerForm_Load);

            this.titleBar.ResumeLayout(false);
            this.titleBar.PerformLayout();
            this.mainPanel.ResumeLayout(false);
            this.mainPanel.PerformLayout();
            this.contentPanel.ResumeLayout(false);
            this.contentPanel.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel titleBar;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.Panel contentPanel;
        private System.Windows.Forms.Label disclaimer;
        private System.Windows.Forms.Button acknowledgeButton;
    }
}