namespace TraderApp.UI.Usercontrol
{
    partial class DetailsControl
    {
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.tabControlDetails = new System.Windows.Forms.TabControl();
            this.tabHistory = new System.Windows.Forms.TabPage();
            this.tabJournal = new System.Windows.Forms.TabPage();
            this.FilterPanel = new System.Windows.Forms.Panel();
            this.btnrequest = new System.Windows.Forms.Button();
            this.Todate = new System.Windows.Forms.DateTimePicker();
            this.Fromdate = new System.Windows.Forms.DateTimePicker();
            this.Cmbselectdays = new System.Windows.Forms.ComboBox();
            this.Cmbentry = new System.Windows.Forms.ComboBox();
            this.Cmbtype = new System.Windows.Forms.ComboBox();
            this.CmbExecution = new System.Windows.Forms.ComboBox();
            this.Cmbsoymbol = new System.Windows.Forms.ComboBox();
            this.lblHistory = new System.Windows.Forms.Label();
            this.FilterPanel.SuspendLayout();
            this.tabControlDetails.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlDetails
            // 
            this.tabControlDetails.Controls.Add(this.tabHistory);
            this.tabControlDetails.Controls.Add(this.tabJournal);
            this.tabControlDetails.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tabControlDetails.ItemSize = new System.Drawing.Size(80, 25);
            this.tabControlDetails.Location = new System.Drawing.Point(0, 119);
            this.tabControlDetails.Name = "tabControlDetails";
            this.tabControlDetails.SelectedIndex = 0;
            this.tabControlDetails.Size = new System.Drawing.Size(1012, 27);
            this.tabControlDetails.TabIndex = 14;
            // 
            // tabHistory
            // 
            this.tabHistory.Location = new System.Drawing.Point(4, 29);
            this.tabHistory.Name = "tabHistory";
            this.tabHistory.Padding = new System.Windows.Forms.Padding(3);
            this.tabHistory.Size = new System.Drawing.Size(1004, 0);
            this.tabHistory.TabIndex = 0;
            this.tabHistory.Text = "History";
            this.tabHistory.UseVisualStyleBackColor = true;
            // 
            // tabJournal
            // 
            this.tabJournal.Location = new System.Drawing.Point(4, 29);
            this.tabJournal.Name = "tabJournal";
            this.tabJournal.Padding = new System.Windows.Forms.Padding(3);
            this.tabJournal.Size = new System.Drawing.Size(1004, 0);
            this.tabJournal.TabIndex = 1;
            this.tabJournal.Text = "Journal";
            this.tabJournal.UseVisualStyleBackColor = true;
            // 
            // FilterPanel
            // 
            this.FilterPanel.AutoSize = true;
            this.FilterPanel.BackColor = System.Drawing.Color.CornflowerBlue;
            this.FilterPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.FilterPanel.Controls.Add(this.btnrequest);
            this.FilterPanel.Controls.Add(this.Todate);
            this.FilterPanel.Controls.Add(this.Fromdate);
            this.FilterPanel.Controls.Add(this.Cmbselectdays);
            this.FilterPanel.Controls.Add(this.Cmbentry);
            this.FilterPanel.Controls.Add(this.Cmbtype);
            this.FilterPanel.Controls.Add(this.CmbExecution);
            this.FilterPanel.Controls.Add(this.Cmbsoymbol);
            this.FilterPanel.Controls.Add(this.lblHistory);
            this.FilterPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.FilterPanel.Location = new System.Drawing.Point(0, 0);
            this.FilterPanel.Name = "FilterPanel";
            this.FilterPanel.Size = new System.Drawing.Size(1012, 43);
            this.FilterPanel.TabIndex = 13;
            // 
            // btnrequest
            // 
            this.btnrequest.BackColor = System.Drawing.Color.CornflowerBlue;
            this.btnrequest.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            this.btnrequest.ForeColor = System.Drawing.SystemColors.Window;
            this.btnrequest.Location = new System.Drawing.Point(1867, 4);
            this.btnrequest.Name = "btnrequest";
            this.btnrequest.Size = new System.Drawing.Size(90, 30);
            this.btnrequest.TabIndex = 14;
            this.btnrequest.Text = "Request";
            this.btnrequest.UseVisualStyleBackColor = false;
            // 
            // Todate
            // 
            this.Todate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.Todate.Location = new System.Drawing.Point(0, 0);
            this.Todate.Name = "Todate";
            this.Todate.Size = new System.Drawing.Size(127, 22);
            this.Todate.TabIndex = 13;
            // 
            // Fromdate
            // 
            this.Fromdate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.Fromdate.Location = new System.Drawing.Point(1045, 10);
            this.Fromdate.Name = "Fromdate";
            this.Fromdate.Size = new System.Drawing.Size(123, 22);
            this.Fromdate.TabIndex = 12;
            // 
            // Cmbselectdays
            // 
            this.Cmbselectdays.BackColor = System.Drawing.Color.White;
            this.Cmbselectdays.FormattingEnabled = true;
            this.Cmbselectdays.Items.AddRange(new object[] {
            "Today",
            "Last  3 Days",
            "Last Week",
            "Last Month",
            "Last 3 Months",
            "Last 6 Months",
            "All History"});
            this.Cmbselectdays.Location = new System.Drawing.Point(549, 9);
            this.Cmbselectdays.Name = "Cmbselectdays";
            this.Cmbselectdays.Size = new System.Drawing.Size(188, 24);
            this.Cmbselectdays.TabIndex = 11;
            // 
            // Cmbentry
            // 
            this.Cmbentry.FormattingEnabled = true;
            this.Cmbentry.Items.AddRange(new object[] {
            "ALL",
            "IN",
            "OUT",
            "OUT_IN"});
            this.Cmbentry.Location = new System.Drawing.Point(456, 9);
            this.Cmbentry.Name = "Cmbentry";
            this.Cmbentry.Size = new System.Drawing.Size(87, 24);
            this.Cmbentry.TabIndex = 10;
            // 
            // Cmbtype
            // 
            this.Cmbtype.FormattingEnabled = true;
            this.Cmbtype.Items.AddRange(new object[] {
            "ALL",
            "Buy",
            "Sell",
            "Expired",
            "Cancelled"});
            this.Cmbtype.Location = new System.Drawing.Point(377, 9);
            this.Cmbtype.Name = "Cmbtype";
            this.Cmbtype.Size = new System.Drawing.Size(73, 24);
            this.Cmbtype.TabIndex = 9;
            // 
            // CmbExecution
            // 
            this.CmbExecution.FormattingEnabled = true;
            this.CmbExecution.Items.AddRange(new object[] {
            "ALL",
            "Market",
            "Bill",
            "SellLimit",
            "BuyLimit"});
            this.CmbExecution.Location = new System.Drawing.Point(253, 9);
            this.CmbExecution.Name = "CmbExecution";
            this.CmbExecution.Size = new System.Drawing.Size(118, 24);
            this.CmbExecution.TabIndex = 8;
            // 
            // Cmbsoymbol
            // 
            this.Cmbsoymbol.FormattingEnabled = true;
            this.Cmbsoymbol.Items.AddRange(new object[] {
            "ALL"});
            this.Cmbsoymbol.Location = new System.Drawing.Point(87, 9);
            this.Cmbsoymbol.Name = "Cmbsoymbol";
            this.Cmbsoymbol.Size = new System.Drawing.Size(160, 24);
            this.Cmbsoymbol.TabIndex = 7;
            // 
            // lblHistory
            // 
            this.lblHistory.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold);
            this.lblHistory.ForeColor = System.Drawing.Color.White;
            this.lblHistory.Location = new System.Drawing.Point(4, 4);
            this.lblHistory.Name = "lblHistory";
            this.lblHistory.Size = new System.Drawing.Size(77, 35);
            this.lblHistory.TabIndex = 0;
            this.lblHistory.Text = "Deals";
            this.lblHistory.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // DetailsControl
            // 
            this.AutoSize = true;
            this.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Controls.Add(this.tabControlDetails);
            this.Controls.Add(this.FilterPanel);
            this.Name = "DetailsControl";
            this.Size = new System.Drawing.Size(1012, 146);
            this.FilterPanel.ResumeLayout(false);
            this.tabControlDetails.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlDetails;
        private System.Windows.Forms.TabPage tabHistory;
        private System.Windows.Forms.TabPage tabJournal;
        private System.Windows.Forms.Panel FilterPanel;
        private System.Windows.Forms.Button btnrequest;
        private System.Windows.Forms.DateTimePicker Todate;
        private System.Windows.Forms.DateTimePicker Fromdate;
        private System.Windows.Forms.ComboBox Cmbselectdays;
        private System.Windows.Forms.ComboBox Cmbentry;
        private System.Windows.Forms.ComboBox Cmbtype;
        private System.Windows.Forms.ComboBox CmbExecution;
        private System.Windows.Forms.ComboBox Cmbsoymbol;
        private System.Windows.Forms.Label lblHistory;
    }
}