using System.Windows.Forms;

namespace TraderApps.UI.Forms
{
    partial class Home
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Home));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.trade = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.panelsDropdown = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripDropDownUserButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.changePasswordToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.disconnectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dockPanel = new WeifenLuo.WinFormsUI.Docking.DockPanel();
            this.topPanel = new System.Windows.Forms.Panel();
            this.middlePanel = new System.Windows.Forms.Panel();
            this.toolStrip1.SuspendLayout();
            this.topPanel.SuspendLayout();
            this.middlePanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.BackColor = System.Drawing.Color.White;
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.trade,
            this.toolStripSeparator6,
            this.panelsDropdown,
            this.toolStripDropDownUserButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Padding = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.toolStrip1.Size = new System.Drawing.Size(1496, 34);
            this.toolStrip1.TabIndex = 1;
            // 
            // trade
            // 
            this.trade.Image = global::TraderApp.Properties.Resources.tradesnew;
            this.trade.Name = "trade";
            this.trade.Size = new System.Drawing.Size(78, 29);
            this.trade.Text = "Trade";
            this.trade.Visible = false;
            this.trade.Click += new System.EventHandler(this.trade_Click);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(6, 34);
            // 
            // panelsDropdown
            // 
            this.panelsDropdown.Name = "panelsDropdown";
            this.panelsDropdown.Size = new System.Drawing.Size(96, 29);
            this.panelsDropdown.Text = "Window";
            this.panelsDropdown.Visible = false;
            // 
            // toolStripDropDownUserButton
            // 
            this.toolStripDropDownUserButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripDropDownUserButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.changePasswordToolStripMenuItem,
            this.disconnectToolStripMenuItem});
            this.toolStripDropDownUserButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.toolStripDropDownUserButton.Image = global::TraderApp.Properties.Resources.user;
            this.toolStripDropDownUserButton.ImageTransparentColor = System.Drawing.Color.White;
            this.toolStripDropDownUserButton.Name = "toolStripDropDownUserButton";
            this.toolStripDropDownUserButton.Size = new System.Drawing.Size(38, 29);
            // 
            // changePasswordToolStripMenuItem
            // 
            this.changePasswordToolStripMenuItem.Image = global::TraderApp.Properties.Resources.change_password;
            this.changePasswordToolStripMenuItem.Name = "changePasswordToolStripMenuItem";
            this.changePasswordToolStripMenuItem.Size = new System.Drawing.Size(275, 34);
            this.changePasswordToolStripMenuItem.Text = "Change Password";
            this.changePasswordToolStripMenuItem.Visible = false;
            this.changePasswordToolStripMenuItem.Click += new System.EventHandler(this.changePasswordToolStripMenuItem_Click);
            // 
            // disconnectToolStripMenuItem
            // 
            this.disconnectToolStripMenuItem.Image = global::TraderApp.Properties.Resources.connected;
            this.disconnectToolStripMenuItem.Name = "disconnectToolStripMenuItem";
            this.disconnectToolStripMenuItem.Size = new System.Drawing.Size(275, 34);
            this.disconnectToolStripMenuItem.Text = "Connect";
            this.disconnectToolStripMenuItem.Click += new System.EventHandler(this.disconnectToolStripMenuItem_Click);
            // 
            // dockPanel
            // 
            this.dockPanel.AutoSize = true;
            this.dockPanel.BackColor = System.Drawing.Color.White;
            this.dockPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dockPanel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.World);
            this.dockPanel.Location = new System.Drawing.Point(0, 0);
            this.dockPanel.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.dockPanel.Name = "dockPanel";
            this.dockPanel.Size = new System.Drawing.Size(1496, 1015);
            this.dockPanel.TabIndex = 2;
            // 
            // topPanel
            // 
            this.topPanel.AutoSize = true;
            this.topPanel.BackColor = System.Drawing.Color.White;
            this.topPanel.Controls.Add(this.toolStrip1);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.topPanel.Location = new System.Drawing.Point(0, 0);
            this.topPanel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.topPanel.Name = "topPanel";
            this.topPanel.Size = new System.Drawing.Size(1496, 34);
            this.topPanel.TabIndex = 5;
            // 
            // middlePanel
            // 
            this.middlePanel.AutoSize = true;
            this.middlePanel.Controls.Add(this.dockPanel);
            this.middlePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.middlePanel.Location = new System.Drawing.Point(0, 34);
            this.middlePanel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.middlePanel.Name = "middlePanel";
            this.middlePanel.Size = new System.Drawing.Size(1496, 1015);
            this.middlePanel.TabIndex = 6;
            // 
            // Home
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1496, 1049);
            this.Controls.Add(this.middlePanel);
            this.Controls.Add(this.topPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.Name = "Home";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Home_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.topPanel.ResumeLayout(false);
            this.topPanel.PerformLayout();
            this.middlePanel.ResumeLayout(false);
            this.middlePanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStripDropDownButton panelsDropdown;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton trade;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;

        private WeifenLuo.WinFormsUI.Docking.DockPanel dockPanel;
        private Panel topPanel;
        private Panel middlePanel;
        private ToolStripDropDownButton toolStripDropDownUserButton;
        private ToolStripMenuItem disconnectToolStripMenuItem;
        private ToolStripMenuItem changePasswordToolStripMenuItem;
    }
}
