using System.Windows.Forms;

namespace TraderApp.UI.Usercontrol
{
    public partial class MarketWatchControl : UserControl
    {
        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.tlpMainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.tlpHeaderControls = new System.Windows.Forms.TableLayoutPanel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.txtsearchsymbol = new System.Windows.Forms.TextBox();
            this.cmbfontsize = new System.Windows.Forms.ComboBox();
            this.btnSaveSymbol = new System.Windows.Forms.Button();
            this.date_timeLabel = new System.Windows.Forms.Label();
            this.dgvMarketWatchGrid = new System.Windows.Forms.DataGridView();
            this.tlpMainLayout.SuspendLayout();
            this.pnlHeader.SuspendLayout();
            this.tlpHeaderControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMarketWatchGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // tlpMainLayout
            // 
            this.tlpMainLayout.ColumnCount = 1;
            this.tlpMainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMainLayout.Controls.Add(this.pnlHeader, 0, 0);
            this.tlpMainLayout.Controls.Add(this.dgvMarketWatchGrid, 0, 1);
            this.tlpMainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpMainLayout.Location = new System.Drawing.Point(0, 0);
            this.tlpMainLayout.Margin = new System.Windows.Forms.Padding(0);
            this.tlpMainLayout.Name = "tlpMainLayout";
            this.tlpMainLayout.RowCount = 2;
            this.tlpMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F)); // Reduced height to 32px
            this.tlpMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMainLayout.Size = new System.Drawing.Size(800, 500);
            this.tlpMainLayout.TabIndex = 0;
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.White;
            this.pnlHeader.Controls.Add(this.tlpHeaderControls);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Margin = new System.Windows.Forms.Padding(0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(800, 32);
            this.pnlHeader.TabIndex = 1;
            // 
            // tlpHeaderControls
            // 
            this.tlpHeaderControls.ColumnCount = 6;
            this.tlpHeaderControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F)); // Image (Tighter)
            this.tlpHeaderControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));    // Time Label
            this.tlpHeaderControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F)); // Search (Reduced width)
            this.tlpHeaderControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));  // Combo (Reduced width)
            this.tlpHeaderControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));    // Button
            this.tlpHeaderControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F)); // Spacer
            this.tlpHeaderControls.Controls.Add(this.pictureBox1, 0, 0);
            this.tlpHeaderControls.Controls.Add(this.date_timeLabel, 1, 0);
            this.tlpHeaderControls.Controls.Add(this.txtsearchsymbol, 2, 0);
            this.tlpHeaderControls.Controls.Add(this.cmbfontsize, 3, 0);
            this.tlpHeaderControls.Controls.Add(this.btnSaveSymbol, 4, 0);
            this.tlpHeaderControls.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpHeaderControls.Location = new System.Drawing.Point(0, 0);
            this.tlpHeaderControls.Name = "tlpHeaderControls";
            this.tlpHeaderControls.RowCount = 1;
            this.tlpHeaderControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpHeaderControls.Size = new System.Drawing.Size(800, 32);
            this.tlpHeaderControls.TabIndex = 0;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.pictureBox1.Image = global::TraderApp.Properties.Resources.stop_watch;
            this.pictureBox1.Location = new System.Drawing.Point(3, 5);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this.pictureBox1.Size = new System.Drawing.Size(22, 22);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            // 
            // txtsearchsymbol
            // 
            this.txtsearchsymbol.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtsearchsymbol.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtsearchsymbol.Location = new System.Drawing.Point(125, 5);
            this.txtsearchsymbol.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.txtsearchsymbol.Name = "txtsearchsymbol";
            this.txtsearchsymbol.Size = new System.Drawing.Size(124, 21);
            this.txtsearchsymbol.TabIndex = 4;
            this.txtsearchsymbol.TextChanged += new System.EventHandler(this.txtsearchsymbol_TextChanged);
            this.txtsearchsymbol.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtsearchsymbol_KeyDown);
            // 
            // cmbfontsize
            // 
            this.cmbfontsize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbfontsize.BackColor = System.Drawing.SystemColors.Window;
            this.cmbfontsize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbfontsize.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbfontsize.FormattingEnabled = true;
            this.cmbfontsize.Items.AddRange(new object[] {
            "10",
            "12",
            "14",
            "16",
            "18",
            "20",
            "22",
            "24",
            "26",
            "28",
            "30"});
            this.cmbfontsize.Location = new System.Drawing.Point(255, 5);
            this.cmbfontsize.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.cmbfontsize.Name = "cmbfontsize";
            this.cmbfontsize.Size = new System.Drawing.Size(44, 21);
            this.cmbfontsize.TabIndex = 2;
            this.cmbfontsize.SelectedIndexChanged += new System.EventHandler(this.cmbfontsize_SelectedIndexChanged);
            // 
            // btnSaveSymbol
            // 
            this.btnSaveSymbol.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnSaveSymbol.AutoSize = true;
            this.btnSaveSymbol.FlatAppearance.BorderSize = 0;
            this.btnSaveSymbol.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
            this.btnSaveSymbol.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSaveSymbol.ForeColor = System.Drawing.Color.White;
            this.btnSaveSymbol.Location = new System.Drawing.Point(305, 3);
            this.btnSaveSymbol.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.btnSaveSymbol.Name = "btnSaveSymbol";
            this.btnSaveSymbol.Size = new System.Drawing.Size(65, 25);
            this.btnSaveSymbol.TabIndex = 1;
            this.btnSaveSymbol.Text = "Save";
            this.btnSaveSymbol.UseVisualStyleBackColor = false;
            this.btnSaveSymbol.Click += new System.EventHandler(this.saveSymbol_Click);
            // 
            // date_timeLabel
            // 
            this.date_timeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.date_timeLabel.AutoSize = true;
            this.date_timeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.date_timeLabel.Location = new System.Drawing.Point(33, 8);
            this.date_timeLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.date_timeLabel.Name = "date_timeLabel";
            this.date_timeLabel.Size = new System.Drawing.Size(86, 15);
            this.date_timeLabel.TabIndex = 0;
            this.date_timeLabel.Text = "00:00:00 AM";
            this.date_timeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // dgvMarketWatchGrid
            // 
            this.dgvMarketWatchGrid.AllowUserToAddRows = false;
            this.dgvMarketWatchGrid.AllowUserToDeleteRows = false;
            this.dgvMarketWatchGrid.AllowUserToResizeRows = false;
            this.dgvMarketWatchGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvMarketWatchGrid.BackgroundColor = System.Drawing.Color.White;
            this.dgvMarketWatchGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.CornflowerBlue;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.CornflowerBlue;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvMarketWatchGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvMarketWatchGrid.ColumnHeadersHeight = 29;
            this.dgvMarketWatchGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvMarketWatchGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvMarketWatchGrid.EnableHeadersVisualStyles = false;
            this.dgvMarketWatchGrid.Location = new System.Drawing.Point(0, 32);
            this.dgvMarketWatchGrid.Margin = new System.Windows.Forms.Padding(0);
            this.dgvMarketWatchGrid.MultiSelect = false;
            this.dgvMarketWatchGrid.Name = "dgvMarketWatchGrid";
            this.dgvMarketWatchGrid.RowHeadersVisible = false;
            this.dgvMarketWatchGrid.RowHeadersWidth = 51;
            this.dgvMarketWatchGrid.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dgvMarketWatchGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvMarketWatchGrid.ShowCellToolTips = false;
            this.dgvMarketWatchGrid.Size = new System.Drawing.Size(800, 468);
            this.dgvMarketWatchGrid.TabIndex = 2;
            //this.dgvMarketWatchGrid.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgvMarketWatchGrid_CellMouseDoubleClick);
            this.dgvMarketWatchGrid.Scroll += new System.Windows.Forms.ScrollEventHandler(this.dgvMarketWatchGrid_Scroll);
            this.dgvMarketWatchGrid.Resize += new System.EventHandler(this.dgvMarketWatchGrid_Resize);
            // 
            // MarketWatchUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tlpMainLayout);
            this.Name = "MarketWatchUserControl";
            this.Size = new System.Drawing.Size(800, 500);
            this.tlpMainLayout.ResumeLayout(false);
            this.pnlHeader.ResumeLayout(false);
            this.tlpHeaderControls.ResumeLayout(false);
            this.tlpHeaderControls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMarketWatchGrid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlpMainLayout;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.TableLayoutPanel tlpHeaderControls;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox txtsearchsymbol;
        private System.Windows.Forms.ComboBox cmbfontsize;
        private System.Windows.Forms.Button btnSaveSymbol;
        private System.Windows.Forms.Label date_timeLabel;
        public System.Windows.Forms.DataGridView dgvMarketWatchGrid;
    }
}
