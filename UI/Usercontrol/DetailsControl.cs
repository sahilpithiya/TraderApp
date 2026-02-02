using ClientDesktop.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TraderApp.Services;
using TraderApps.Config;
using TraderApps.Helpers;
using TraderApps.UI.Theme;

namespace TraderApp.UI.Usercontrol
{
    public partial class DetailsControl : UserControl
    {
        #region  Variables / Property Declaration

        private Panel mainContainer;

        // Service Instance
        private readonly DetailService _detailService;

        private ContextMenuStrip contextMenu;
        private DataGridView historyDataGrid;
        private DataGridView journalDataGrid;

        private string currentActiveTab = "Position";
        private List<string[]> _journalCache = new List<string[]>();

        private Timer _priceUpdateTimer;

        // UI Statics for Footer Calculation
        private static double creditAmountForHistoryFooter, balanceForHistoryFooter;
        private static decimal totalProfit, totalComm;

        // Data Models
        List<HistoryModel> _originalHistory;
        List<PositionHistoryModel> _originalPositionHistory;
        private List<HistoryModel> _History;
        private List<PositionHistoryModel> _PositionHistory;

        private Task _historyLoadingTask;
        private bool _ignoreEvents = false;
        private TabPage _cachedHistoryTab;

        // Combo Lists
        private List<string> _symbols = new List<string>();
        private List<string> _excecution = new List<string>();
        Label lbl = new Label();

        public System.Windows.Forms.ComboBox SymbolCombo => this.Cmbsoymbol;
        public System.Windows.Forms.ComboBox ExecutionCombo => this.CmbExecution;
        public System.Windows.Forms.ComboBox TypeCombo => this.Cmbtype;
        public System.Windows.Forms.ComboBox EntryCombo => this.Cmbentry;
        public System.Windows.Forms.ComboBox DaysCombo => this.Cmbselectdays;
        public DateTimePicker FromDate => this.Fromdate;
        public DateTimePicker ToDate => this.Todate;
        public System.Windows.Forms.Button RequestButton => this.btnrequest;
        System.Windows.Forms.Button ButtonOk = new System.Windows.Forms.Button();

        #endregion Variables / Property Declaration

        #region Constructor and Initialization

        public DetailsControl()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(FilterPanel);
            ThemeManager.ApplyTheme(btnrequest);
            ThemeManager.ApplyTheme(ButtonOk);
            this.AutoScroll = true;

            _cachedHistoryTab = tabHistory;

            mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            if (this.Controls.Contains(FilterPanel))
            {
                this.Controls.Remove(FilterPanel);
            }
            mainContainer.Controls.Add(FilterPanel);
            this.Controls.Add(mainContainer);

            // Initialize Service
            _detailService = new DetailService();

            this.tabControlDetails.SelectedIndexChanged += TabControlDetails_SelectedIndexChanged;
            this.DaysCombo.SelectedIndexChanged += DaysCombo_SelectedIndexChanged;
            this.FromDate.ValueChanged += Date_ValueChanged;
            this.ToDate.ValueChanged += Date_ValueChanged;
            this.SymbolCombo.SelectedIndexChanged += ComboFilter_SelectedIndexChanged;
            this.ExecutionCombo.SelectedIndexChanged += ComboFilter_SelectedIndexChanged;
            this.TypeCombo.SelectedIndexChanged += ComboFilter_SelectedIndexChanged;
            this.EntryCombo.SelectedIndexChanged += ComboFilter_SelectedIndexChanged;

            FileLogger.OnLogReceived = this.ReceiveJournalLog;
        }

        public void EnableFullAccess()
        {
            if (!tabControlDetails.TabPages.Contains(_cachedHistoryTab))
            {
                tabControlDetails.TabPages.Insert(0, _cachedHistoryTab);
            }
        }

        public void SetupPreLoginMode()
        {
            if (tabControlDetails.TabPages.Contains(_cachedHistoryTab))
            {
                tabControlDetails.TabPages.Remove(_cachedHistoryTab);
            }
            ShowJournalTab();
        }

        public void LoadData()
        {
            if (tabControlDetails.SelectedTab == tabHistory)
                ShowHistoryTab();
            else
                ShowJournalTab();
        }

        private void TabControlDetails_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControlDetails.SelectedTab == tabHistory)
            {
                //FileLogger.Log("UI", "Switched to History Tab");
                ShowHistoryTab();
            }
            else if (tabControlDetails.SelectedTab == tabJournal)
            {
                //FileLogger.Log("UI", "Switched to Journal Tab");
                ShowJournalTab();
            }
        }

        private async void ShowHistoryTab()
        {
            currentActiveTab = "History";
            if (journalDataGrid != null) journalDataGrid.Visible = false;

            FilterPanel.Visible = true;

            if (historyDataGrid == null)
            {
                InitComboBox(DaysCombo);
                this.OnDeals_Click(null, EventArgs.Empty);
                await TriggerHistoryLoad();
            }
            else
            {
                historyDataGrid.Visible = true;
                historyDataGrid.BringToFront();
                FilterPanel.SendToBack();
            }
        }

        private void ShowJournalTab()
        {
            currentActiveTab = "Journal";

            // Hide History Grid if exists
            if (historyDataGrid != null)
                historyDataGrid.Visible = false;

            FilterPanel.Visible = false;

            if (journalDataGrid != null && !mainContainer.Controls.Contains(journalDataGrid))
            {
                mainContainer.Controls.Add(journalDataGrid);
            }

            if (journalDataGrid == null)
            {
                CreateJournalGrid();
            }
            else
            {
                journalDataGrid.Visible = true;
                journalDataGrid.BringToFront();
            }
        }

        private void CreateJournalGrid()
        {
            journalDataGrid = CreateNewDataGridView();
            mainContainer.Controls.Add(journalDataGrid);

            journalDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "Time", DataPropertyName = "Time", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = CommonHelper.GetScaled(200) });
            journalDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Source", HeaderText = "Source", DataPropertyName = "Source", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = CommonHelper.GetScaled(150) });
            journalDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Message", HeaderText = "Message", DataPropertyName = "Message", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = CommonHelper.GetScaled(400) });

            if (_journalCache.Count == 0)
            {
                try
                {
                    string logDir = Path.Combine(AppConfig.AppDataPath, "Logs");
                    string logFile = Path.Combine(logDir, DateTime.Now.ToString("yyyyMMdd") + ".log");

                    if (File.Exists(logFile))
                    {
                        var lines = File.ReadAllLines(logFile);
                        Array.Reverse(lines);

                        foreach (var line in lines)
                        {
                            var parts = line.Split('\t');
                            if (parts.Length == 3) _journalCache.Add(parts);
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine("Error loading logs: " + ex.Message); }
            }

            foreach (var log in _journalCache)
            {
                journalDataGrid.Rows.Add(log[0], log[1], log[2]);
            }

            if (_journalCache.Count == 0) FileLogger.Log("System", "Journal Initialized");
            journalDataGrid.Visible = true;
        }

        #endregion Constructor and Initialization

        private async void LoadHistoryView()
        {
            currentActiveTab = "History";
            InitComboBox(DaysCombo);
            this.OnDeals_Click(null, EventArgs.Empty);
            await TriggerHistoryLoad();
        }

        private Task TriggerHistoryLoad()
        {
            FileLogger.Log("History", "Background loading of history data started");
            return _historyLoadingTask = Task.Run(async () =>
            {
                string domain = SessionManager.ServerListData
                    .FirstOrDefault(w => w.licenseId.ToString() == SessionManager.LicenseId)?
                    .serverDisplayName;

                _History = await _detailService.GetDealsOrOrdersDataAsync(SessionManager.UserId, SessionManager.LicenseId, domain);
                _PositionHistory = await _detailService.GetPositionHistoryDataAsync(SessionManager.UserId, SessionManager.LicenseId, domain);
            });
        }

        private void InitializeContextMenu()
        {
            contextMenu = new ContextMenuStrip();
            AddMenuItem("Deals", OnDeals_Click, isCheckable: true);
            contextMenu.Items.Add(new ToolStripSeparator());
            AddMenuItem("Orders", OnOrders_Click, isCheckable: true);
            contextMenu.Items.Add(new ToolStripSeparator());
            AddMenuItem("Position", OnPosition_Click, isCheckable: true);

            // Assign to DataGridView
            historyDataGrid.ContextMenuStrip = contextMenu;
        }

        private void ReceiveJournalLog(string time, string source, string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ReceiveJournalLog(time, source, message)));
                return;
            }
            _journalCache.Insert(0, new string[] { time, source, message });
            if (journalDataGrid != null && !journalDataGrid.IsDisposed)
            {
                journalDataGrid.Rows.Insert(0, time, source, message);
            }
        }

        private void AddMenuItem(string text, EventHandler onClick, bool isCheckable = false, bool isChecked = false)
        {
            var item = new ToolStripMenuItem(text, null, onClick)
            {
                CheckOnClick = isCheckable,
                Checked = isChecked
            };
            contextMenu.Items.Add(item);
        }

        private async void OnDeals_Click(object sender, EventArgs e) => HandleHistoryViewSelection(sender, "Deals", "Deals");
        private async void OnOrders_Click(object sender, EventArgs e) => HandleHistoryViewSelection(sender, "Orders", "Orders");
        private async void OnPosition_Click(object sender, EventArgs e) => HandleHistoryViewSelection(sender, "Position", "Position");

        private void HandleHistoryViewSelection(object sender, string viewType, string menuItemText)
        {
            if (contextMenu != null && sender is ToolStripMenuItem clickedItem)
            {
                foreach (ToolStripItem item in contextMenu.Items)
                {
                    if (item is ToolStripMenuItem menuItem && menuItem.CheckOnClick && menuItem != clickedItem)
                    {
                        menuItem.Checked = false;
                    }
                }
                clickedItem.Checked = true;
            }
            SetupHistoryView(viewType, menuItemText);
        }

        private void SetupHistoryView(string viewType, string contextMenuItemText)
        {
            FilterPanel.Visible = true;
            lblHistory.Visible = true;
            lblHistory.Text = viewType;
            lblHistory.TextAlign = ContentAlignment.MiddleCenter;
            lblHistory.ForeColor = ThemeManager.Black;
            lblHistory.Font = ThemeManager.CommonBoldFont;
            bool isPosition = viewType == "Position";

            Cmbsoymbol.Visible = true;
            CmbExecution.Visible = !isPosition;
            Cmbtype.Visible = !isPosition;
            Cmbentry.Visible = !isPosition;
            Cmbselectdays.Visible = true;
            btnrequest.Visible = true;
            ButtonOk.Visible = false;

            ResetPanel();
            FilterPanel.Dock = DockStyle.Top;

            if (historyDataGrid != null && mainContainer.Controls.Contains(historyDataGrid))
            {
                mainContainer.Controls.Remove(historyDataGrid);
                historyDataGrid.Dispose();
                historyDataGrid = null;
            }

            if (journalDataGrid != null) journalDataGrid.Visible = false;

            historyDataGrid = CreateHistoryDataGrid(viewType);

            mainContainer.Controls.Clear();
            mainContainer.Controls.Add(historyDataGrid);
            mainContainer.Controls.Add(FilterPanel);
            mainContainer.Padding = new Padding(0, 0, 0, 25);

            InitializeContextMenu();
            var menuItem = contextMenu.Items?.OfType<ToolStripMenuItem>()?
                .FirstOrDefault(s => s != null && !string.IsNullOrEmpty(s.Text) && s.Text == contextMenuItemText);
            if (menuItem != null)
                menuItem.Checked = true;
            menuItem.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            menuItem.Image = menuItem.Checked ? Properties.Resources.m_checkmark : null;

            mainContainer.PerformLayout();
            SetupFilters(historyDataGrid);
            InitializeComboBoxes(viewType);

            historyDataGrid.Resize += HistoryDataGrid_Resize;
            SetupFilters(historyDataGrid);
            historyDataGrid.DataSource = AddFooterRowToDataTable(null);

            RequestButton.Click -= RequestButton_ClickAsync;
            RequestButton.Click += RequestButton_ClickAsync;

            historyDataGrid.CellPainting -= historyDataGrid_CellPainting;
            historyDataGrid.CellPainting += historyDataGrid_CellPainting;
            historyDataGrid.CellClick -= historyDataGrid_CellClick;
            historyDataGrid.CellClick += historyDataGrid_CellClick;
            historyDataGrid.ColumnHeaderMouseClick += historyDataGrid_ColumnHeaderMouseClick;

            // Log View Change
            //FileLogger.Log("UI", $"View changed to {viewType}");
        }

        private DataGridView CreateHistoryDataGrid(string viewType)
        {
            historyDataGrid = CreateNewDataGridView();
            historyDataGrid.AutoGenerateColumns = false;
            historyDataGrid.Dock = DockStyle.Fill;
            historyDataGrid.Columns.Clear();

            switch (viewType)
            {
                case "Deals":
                case "Orders":
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "Time", DataPropertyName = "Time", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(160) });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DealId", HeaderText = "Deal Id", DataPropertyName = "Deal Id", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(140) });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PositionId", HeaderText = "Position Id", DataPropertyName = "Position Id", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(140) });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Symbol", HeaderText = "Symbol", DataPropertyName = "Symbol", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(200) });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Execution", HeaderText = "Execution", DataPropertyName = "Execution", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(140) });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type", DataPropertyName = "Type", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(100) });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Entry", HeaderText = "Entry", DataPropertyName = "Entry", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(140) });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Volume", HeaderText = "Volume", DataPropertyName = "Volume", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(140), DefaultCellStyle = CreateDefaultCellStyle() });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "Price", DataPropertyName = "Price", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(190), DefaultCellStyle = CreateDefaultCellStyle() });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Comm", HeaderText = "Comm.", DataPropertyName = "Comm", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(140), DefaultCellStyle = CreateDefaultCellStyle() });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Profit", HeaderText = "Profit", DataPropertyName = "Profit", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(140), DefaultCellStyle = CreateDefaultCellStyle() });
                    // Comment Column Removed
                    break;
                case "Position":
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "Time", DataPropertyName = "Time", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(180) });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastOutTime", HeaderText = "Last Out Time", DataPropertyName = "LastOutTime", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(180) });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PositionId", HeaderText = "PositionId", DataPropertyName = "PositionId", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(130) });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type", DataPropertyName = "Type", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(100) });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Volume", HeaderText = "Volume", DataPropertyName = "Volume", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(120), HeaderCell = CreateHeaderCell(), DefaultCellStyle = CreateDefaultCellStyle() });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Symbol", HeaderText = "Symbol", DataPropertyName = "Symbol", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(200) });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "Price", DataPropertyName = "Price", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(120), DefaultCellStyle = CreateDefaultCellStyle() });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Comm", HeaderText = "Comm.", DataPropertyName = "Comm", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(300), HeaderCell = CreateHeaderCell(), DefaultCellStyle = CreateDefaultCellStyle() });
                    historyDataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Profit", HeaderText = "Profit", DataPropertyName = "Profit", SortMode = DataGridViewColumnSortMode.Programmatic, MinimumWidth = CommonHelper.GetScaled(120), DefaultCellStyle = CreateDefaultCellStyle() });
                    // Comment Column Removed
                    break;
            }
            historyDataGrid.Visible = true;
            return historyDataGrid;
        }

        private async Task ReloadDataFromService(string viewType)
        {
            string domain = SessionManager.ServerListData
                .FirstOrDefault(w => w.licenseId.ToString() == SessionManager.LicenseId)?
                .serverDisplayName;

            if (viewType != "Position")
            {
                _History = await _detailService.GetDealsOrOrdersDataAsync(SessionManager.UserId, SessionManager.LicenseId, domain);
            }
            else
            {
                _PositionHistory = await _detailService.GetPositionHistoryDataAsync(SessionManager.UserId, SessionManager.LicenseId, domain);
            }
        }

        private DataTable AddFooterRowToDataTable(DataTable dt)
        {
            if (dt == null) dt = new DataTable();
            if (!dt.Columns.Contains("Symbol")) dt.Columns.Add("Symbol", typeof(string));

            if (dt.Rows.Count > 0)
            {
                var lastRow = dt.Rows[dt.Rows.Count - 1];
                if (Convert.ToString(lastRow["Symbol"]) == "—") dt.Rows.Remove(lastRow);
            }

            DataRow footer = dt.NewRow();
            foreach (DataColumn col in dt.Columns) footer[col.ColumnName] = DBNull.Value;
            footer["Symbol"] = "—";
            dt.Rows.Add(footer);
            return dt;
        }

        private async void RequestButton_ClickAsync(object sender, EventArgs e)
        {
            try
            {
                // LOG REQUEST START
                FileLogger.Log("History", $"Requesting Data for {lblHistory.Text} (From: {FromDate.Value.ToShortDateString()} To: {ToDate.Value.ToShortDateString()})");

                btnrequest.Enabled = false;
                if (_historyLoadingTask != null) await _historyLoadingTask;

                if (historyDataGrid != null)
                {
                    await ReloadDataFromService(lblHistory.Text);

                    // LOG SUCCESS
                    int recordCount = (lblHistory.Text != "Position") ? (_History?.Count ?? 0) : (_PositionHistory?.Count ?? 0);
                    FileLogger.Log("History", $"Data Loaded Successfully. Total Records: {recordCount}");

                    if (lblHistory.Text != "Position")
                        BindHistoryDataToGrid(_History, lblHistory.Text);
                    else
                        BindPositionHistoryDataToGrid(_PositionHistory);

                    SymbolCombo.Enabled = true;
                    if (lblHistory.Text != "Position")
                    {
                        ExecutionCombo.Enabled = true;
                        TypeCombo.Enabled = true;
                        EntryCombo.Enabled = true;
                    }

                    GetHistoryTotalPnlCommission(lblHistory.Text, _originalPositionHistory, _originalHistory);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log("History", $"Error loading data: {ex.Message}");
                Console.WriteLine("Error loading history: " + ex.Message);
            }
            finally
            {
                btnrequest.Enabled = true;
            }
        }

        private void GetHistoryTotalPnlCommission(string viewType, List<PositionHistoryModel> positionHistoryModel, List<HistoryModel> historyModel)
        {
            totalProfit = 0m;
            totalComm = 0m;

            if (positionHistoryModel != null && string.Equals(viewType, "Position", StringComparison.OrdinalIgnoreCase))
            {
                totalProfit = (decimal)(positionHistoryModel?.Sum(h => h.Pnl) ?? 0d);
                return;
            }

            if (historyModel == null) return;

            foreach (var h in historyModel)
            {
                if (string.Equals(h.orderType, "Bill", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(h.symbolName, "Credit", StringComparison.OrdinalIgnoreCase)) continue;

                totalProfit += h.pnl;
                totalComm += h.uplineCommission;
            }
        }

        private void historyDataGrid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            var grid = (DataGridView)sender;
            if (grid.Rows.Count == 0 || e.RowIndex < 0) return;

            if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
            {
                string columnName = historyDataGrid.Columns[e.ColumnIndex].Name;
                bool isCopyColumn = columnName == "DealId" || columnName == "PositionId";

                if (isCopyColumn)
                {
                    var cell = historyDataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    string cellValue = cell.Value?.ToString();
                    e.PaintBackground(e.CellBounds, true);
                    e.PaintContent(e.CellBounds);
                    bool hasValue = !string.IsNullOrEmpty(cellValue);
                    if (hasValue && Properties.Resources.copy != null)
                    {
                        var img = Properties.Resources.copy;
                        int imgSize = 16;
                        int padding = 4;
                        Rectangle imgRect = new Rectangle(
                            e.CellBounds.Right - imgSize - padding,
                            e.CellBounds.Top + (e.CellBounds.Height - imgSize) / 2,
                            imgSize,
                            imgSize
                        );
                        e.Graphics.DrawImage(img, imgRect);
                    }
                    ControlPaint.DrawBorder(e.Graphics, e.CellBounds, Color.LightGray, 0, ButtonBorderStyle.None, Color.LightGray, 1, ButtonBorderStyle.Solid, Color.LightGray, 0, ButtonBorderStyle.None, Color.LightGray, 0, ButtonBorderStyle.None);
                    e.Handled = true;
                }
            }

            var row = grid.Rows[e.RowIndex];
            bool isFooter = Convert.ToString(row.Cells["Symbol"].Value) == "—";
            if (!isFooter) return;

            bool hasRealData = grid.Rows.Cast<DataGridViewRow>().Any(r => Convert.ToString(r.Cells["Symbol"].Value) != "—");
            int startColIndex = grid.Columns["Time"].Index;

            // Adjust End Column logic since Comment is removed
            // If data exists, merge up to Price. If no data, merge up to the last column (Profit)
            int endColIndex = hasRealData ? grid.Columns["Price"].Index : grid.Columns["Profit"].Index;

            grid.Rows[e.RowIndex].DefaultCellStyle.SelectionBackColor = ThemeManager.Gray;
            grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = ThemeManager.Gray;
            grid.Rows[e.RowIndex].DefaultCellStyle.SelectionForeColor = ThemeManager.Black;

            if (e.ColumnIndex >= startColIndex && e.ColumnIndex <= endColIndex)
            {
                e.Handled = true;
                Rectangle mergedRect = e.CellBounds;
                for (int c = startColIndex; c <= endColIndex; c++)
                    mergedRect = Rectangle.Union(mergedRect, grid.GetCellDisplayRectangle(c, e.RowIndex, true));

                mergedRect = new Rectangle(mergedRect.Left, mergedRect.Top, grid.DisplayRectangle.Width, hasRealData ? mergedRect.Height : mergedRect.Height * 2);

                using (SolidBrush bg = new SolidBrush(hasRealData ? ThemeManager.Gray : ThemeManager.White))
                    e.Graphics.FillRectangle(bg, mergedRect);

                string text;
                if (hasRealData)
                {
                    var clientDetail = SessionManager.ClientListData.Where(q => q.ClientId == SessionManager.UserId).FirstOrDefault();

                    creditAmountForHistoryFooter = clientDetail.CreditAmount;

                    balanceForHistoryFooter = CalculateUplineBalance(clientDetail?.UplineAmount, clientDetail?.UplineCommission, clientDetail?.RealtimeCommission == true);

                    var footer = grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => Convert.ToString(r.Cells["Symbol"].Value) == "—");
                    if (footer != null)
                    {
                        footer.Cells["Profit"].Value = CommonHelper.FormatAmount(totalProfit);
                        footer.Cells["Comm"].Value = CommonHelper.FormatAmount(totalComm);
                        footer.Cells["Profit"].Style.Font = ThemeManager.CommonBoldFont;
                        footer.Cells["Comm"].Style.Font = ThemeManager.CommonBoldFont;
                    }

                    text = $"Profit: {(lblHistory.Text.Equals("Position") || (totalProfit + totalComm) > 0 ? CommonHelper.FormatAmount((totalProfit + totalComm)) : CommonHelper.FormatAmount(0.00m)):N2}   " +
                           $"Credit: {CommonHelper.FormatAmount(creditAmountForHistoryFooter):N2}   " +
                           $"Balance: {CommonHelper.FormatAmount(balanceForHistoryFooter):N2} INR";
                }
                else
                {
                    text = "No data available";
                }

                using (Brush brush = new SolidBrush(ThemeManager.Black))
                using (StringFormat sf = new StringFormat { Alignment = hasRealData ? StringAlignment.Near : StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    e.Graphics.DrawString(text, hasRealData ? ThemeManager.CommonBoldFont : ThemeManager.TitleBoldFont, brush, mergedRect, sf);
                }
            }
        }

        public static double CalculateUplineBalance(double? uplineAmount, double? uplineCommission, bool realtimeCommission)
        {
            double value = 0d;

            if (uplineAmount != null && uplineCommission != null)
                value = uplineAmount.Value +
                        (realtimeCommission ? uplineCommission.Value : 0d);
            else if (uplineAmount != null)
                value = uplineAmount.Value;
            else if (uplineCommission != null && realtimeCommission)
                value = uplineCommission.Value;

            return value;
        }


        private void historyDataGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
            {
                string columnName = historyDataGrid.Columns[e.ColumnIndex].Name;
                if (columnName == "DealId" || columnName == "PositionId")
                {
                    var cellValue = historyDataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
                    if (string.IsNullOrEmpty(cellValue)) return;
                    var cellRect = historyDataGrid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                    int iconAreaStart = cellRect.Right - 24;
                    Point mousePos = historyDataGrid.PointToClient(Cursor.Position);
                    if (mousePos.X > iconAreaStart) Clipboard.SetText(cellValue);
                }
            }
        }

        private void historyDataGrid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (!(historyDataGrid?.DataSource is DataTable dt)) return;
            string columnName = historyDataGrid.Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrEmpty(columnName) || !dt.Columns.Contains(columnName)) return;

            var sortInfo = historyDataGrid.Tag as Tuple<string, bool>;
            bool sortAscending = true;
            if (sortInfo != null && sortInfo.Item1 == columnName) sortAscending = !sortInfo.Item2;
            historyDataGrid.Tag = new Tuple<string, bool>(columnName, sortAscending);

            if (dt.Rows.Count > 0)
            {
                var lastRow = dt.Rows[dt.Rows.Count - 1];
                if (Convert.ToString(lastRow["Symbol"]) == "—") dt.Rows.Remove(lastRow);
            }

            DataTable sorted;
            if (columnName == "Time")
            {
                var rows = dt.AsEnumerable().Where(r => Convert.ToString(r["Symbol"]) != "—");
                if (sortAscending) rows = rows.OrderBy(r => Convert.ToDateTime(r["Time"]));
                else rows = rows.OrderByDescending(r => Convert.ToDateTime(r["Time"]));
                sorted = rows.CopyToDataTable();
            }
            else
            {
                DataView view = dt.DefaultView;
                view.Sort = $"{columnName} {(sortAscending ? "ASC" : "DESC")}";
                sorted = view.ToTable();
            }

            DataRow footer = sorted.NewRow();
            foreach (DataColumn col in sorted.Columns) footer[col.ColumnName] = DBNull.Value;
            footer["Symbol"] = "—";
            sorted.Rows.Add(footer);

            historyDataGrid.DataSource = sorted;
            historyDataGrid.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = sortAscending ? SortOrder.Ascending : SortOrder.Descending;
        }

        #region History Deals & Order Handling

        private DataTable CreateHistoryDataTable(List<HistoryModel> historyList)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Time", typeof(string));
            dt.Columns.Add("Deal Id", typeof(string));
            dt.Columns.Add("Position Id", typeof(string));
            dt.Columns.Add("Symbol", typeof(string));
            dt.Columns.Add("Execution", typeof(string));
            dt.Columns.Add("Type", typeof(string));
            dt.Columns.Add("Entry", typeof(string));
            dt.Columns.Add("Volume", typeof(decimal));
            dt.Columns.Add("Price", typeof(decimal));
            dt.Columns.Add("Comm", typeof(string));
            dt.Columns.Add("Profit", typeof(string));
            // Comment Column Removed

            foreach (var h in historyList)
            {
                DateTime istTime = GMTTime.ConvertUtcToIst(h.createdOn);
                string displayTime = istTime.ToString("dd/MM/yy HH:mm").Replace("-", "/");
                dt.Rows.Add(
                    displayTime,
                    h.refId ?? "",
                    h.positionId ?? "",
                    h.symbolName ?? "",
                    h.orderType ?? "",
                    h.side ?? "",
                    h.dealType ?? "",
                    h.volume,
                    h.price.ToString("F2") ?? "",
                    h.uplineCommission,
                    CommonHelper.FormatAmount(h.pnl)
                );
            }
            return dt;
        }

        private void BindHistoryDataToGrid(List<HistoryModel> historyList, string viewType)
        {
            if (historyList == null || historyList.Count == 0)
            {
                historyDataGrid.DataSource = AddFooterRowToDataTable(null);
                return;
            }

            HashSet<string> orderTypeFilter;
            HashSet<string> symbolNameFilter;
            DateTime from = FromDate.Value.Date;
            DateTime to = ToDate.Value.Date.AddDays(1).AddTicks(-1);

            if (viewType == "Deals")
            {
                orderTypeFilter = new HashSet<string> { "BuyLimit", "SellLimit", "BuyStop", "SellStop" };
                symbolNameFilter = new HashSet<string> { "Credit", "Balance" };
            }
            else
            {
                orderTypeFilter = new HashSet<string>();
                symbolNameFilter = new HashSet<string>();
            }

            var filteredData = historyList
                 .Where(h =>
                 {
                     if (orderTypeFilter.Contains(h.orderType)) return false;
                     if (symbolNameFilter.Contains(h.symbolName)) return false;
                     var istTime = GMTTime.ConvertUtcToIst(h.createdOn);
                     return istTime >= from && istTime <= to;
                 })
                .OrderBy(s => s.createdOn)
                .ToList();

            _originalHistory = filteredData.ToList();
            historyDataGrid.DataSource = null;

            if (filteredData.Count > 0)
            {
                DataTable dt = CreateHistoryDataTable(filteredData);
                dt = AddFooterRowToDataTable(dt);
                historyDataGrid.DataSource = dt;
                LoadSymbolsToHistoryComboBox(filteredData);
                LoadExceptionToHistoryComboBox(filteredData);
            }
            else
            {
                historyDataGrid.DataSource = AddFooterRowToDataTable(null);
            }
        }

        #endregion

        #region History Position Handling

        private DataTable CreatePositionHistoryDataTable(List<PositionHistoryModel> positionHistoryList)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Time", typeof(string));
            dt.Columns.Add("LastOutTime", typeof(string));
            dt.Columns.Add("PositionId", typeof(string));
            dt.Columns.Add("Type", typeof(string));
            dt.Columns.Add("Volume", typeof(string));
            dt.Columns.Add("Symbol", typeof(string));
            dt.Columns.Add("Price", typeof(decimal));
            dt.Columns.Add("Comm", typeof(string));
            dt.Columns.Add("Profit", typeof(string));
            // Comment Column Removed

            foreach (var h in positionHistoryList)
            {
                string displayTime = GMTTime.ConvertUtcToIst(h.UpdatedAt).ToString("dd/MM/yy HH:mm:ss").Replace("-", "/");
                string lastOutTime = "--";
                if (h.LastOutAt != null) lastOutTime = GMTTime.ConvertUtcToIst(h.LastOutAt.Value).ToString("dd/MM/yy HH:mm:ss").Replace("-", "/");

                dt.Rows.Add(
                    displayTime,
                    lastOutTime,
                    h.RefId ?? "",
                    h.Side.ToLower() == "bid" ? "Sell" : "Buy",
                    h.OutVolume + "/" + h.InVolume,
                    h.SymbolName ?? "",
                    h.AveragePrice.ToString("F" + h.SymbolDigit.ToString()) ?? "",
                    "",
                    CommonHelper.FormatAmount(h.Pnl)
                );
            }
            return dt;
        }

        private void BindPositionHistoryDataToGrid(List<PositionHistoryModel> positionHistoryList)
        {
            if (positionHistoryList == null || positionHistoryList.Count == 0)
            {
                historyDataGrid.DataSource = AddFooterRowToDataTable(null);
                return;
            }

            _originalPositionHistory = positionHistoryList.Where(h => h.LastOutAt == null || GMTTime.ConvertUtcToIst(h.UpdatedAt) >= FromDate.Value.Date &&
                  GMTTime.ConvertUtcToIst(h.UpdatedAt) <= ToDate.Value.Date.AddDays(1).AddTicks(-1)).OrderBy(s => s.UpdatedAt).ToList();

            historyDataGrid.DataSource = null;

            if (_originalPositionHistory.Count > 0)
            {
                DataTable dt = CreatePositionHistoryDataTable(_originalPositionHistory);
                dt = AddFooterRowToDataTable(dt);
                historyDataGrid.DataSource = dt;
                LoadSymbolsToPositionHistoryComboBox(_originalPositionHistory);
            }
            else
            {
                historyDataGrid.DataSource = AddFooterRowToDataTable(null);
            }
        }

        #endregion

        #region History Filter

        private void InitializeComboBoxes(string viewType)
        {
            SymbolCombo.Enabled = false;
            if (viewType != "Position")
            {
                ExecutionCombo.Enabled = false;
                TypeCombo.Enabled = false;
                EntryCombo.Enabled = false;
                InitComboBox(ExecutionCombo);
                InitComboBox(TypeCombo);
                InitComboBox(EntryCombo);
            }
            InitComboBox(SymbolCombo);
        }

        private void InitComboBox(System.Windows.Forms.ComboBox comboBox)
        {
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.DrawMode = DrawMode.OwnerDrawFixed;
            comboBox.DrawItem += ComboBox_DrawItem;
            comboBox.SelectedIndex = 0;
        }

        public void LoadSymbolsToHistoryComboBox(List<HistoryModel> historyList)
        {
            try
            {
                if (historyList == null) return;
                var selectedSymbol = SymbolCombo.SelectedItem?.ToString();
                _symbols = historyList.Where(h => !string.IsNullOrEmpty(h.symbolName)).Select(h => h.symbolName).Distinct().OrderBy(s => s).ToList();
                _symbols.Insert(0, "ALL");
                SymbolCombo.DataSource = null;
                SymbolCombo.DataSource = _symbols;
                if (!string.IsNullOrEmpty(selectedSymbol) && _symbols.Contains(selectedSymbol)) SymbolCombo.SelectedItem = selectedSymbol;
                else SymbolCombo.SelectedIndex = 0;
            }
            catch (Exception ex) { Console.WriteLine("Error loading symbols: " + ex.Message); }
        }

        public void LoadSymbolsToPositionHistoryComboBox(List<PositionHistoryModel> positionHistoryList)
        {
            try
            {
                if (positionHistoryList == null) return;
                var selectedSymbol = SymbolCombo.SelectedItem?.ToString();
                _symbols = positionHistoryList.Where(h => !string.IsNullOrEmpty(h.SymbolName)).Select(h => h.SymbolName).Distinct().OrderBy(s => s).ToList();
                _symbols.Insert(0, "ALL");
                SymbolCombo.DataSource = null;
                SymbolCombo.DataSource = _symbols;
                if (!string.IsNullOrEmpty(selectedSymbol) && _symbols.Contains(selectedSymbol)) SymbolCombo.SelectedItem = selectedSymbol;
                else SymbolCombo.SelectedIndex = 0;
            }
            catch (Exception ex) { Console.WriteLine("Error loading symbols: " + ex.Message); }
        }

        public void LoadExceptionToHistoryComboBox(List<HistoryModel> historyList)
        {
            try
            {
                if (historyList == null) return;
                var selectedExcecution = ExecutionCombo.SelectedItem?.ToString();
                _excecution = historyList.Where(h => !string.IsNullOrEmpty(h.orderType)).Select(h => h.orderType).Distinct().OrderBy(s => s).ToList();
                _excecution.Insert(0, "ALL");
                ExecutionCombo.DataSource = null;
                ExecutionCombo.DataSource = _excecution;
                if (!string.IsNullOrEmpty(selectedExcecution) && _excecution.Contains(selectedExcecution)) ExecutionCombo.SelectedItem = selectedExcecution;
                else ExecutionCombo.SelectedIndex = 0;
            }
            catch (Exception ex) { Console.WriteLine("Error loading symbols: " + ex.Message); }
        }

        private void SetupFilters(DataGridView dgv)
        {
            if (dgv == null || dgv.Columns.Count == 0) return;

            var controlsToRemove = FilterPanel.Controls.Cast<Control>().Where(c => c != lblHistory && c != ButtonOk).ToList();
            foreach (var ctrl in controlsToRemove) FilterPanel.Controls.Remove(ctrl);

            int topPosition = CommonHelper.GetScaled(10);
            int height = CommonHelper.GetScaled(22);
            int iconSize = CommonHelper.GetScaled(28);

            #region Set Label History (Left side)
            lblHistory.Location = new Point(CommonHelper.GetScaled(10), topPosition + CommonHelper.GetScaled(2));
            lblHistory.Size = new Size(CommonHelper.GetScaled(100), height);
            lblHistory.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            lblHistory.TextAlign = ContentAlignment.MiddleLeft;
            FilterPanel.Controls.Add(lblHistory);
            #endregion

            if (lblHistory.Text != "Position")
            {
                #region Deals/Orders Layout
                if (dgv.Columns.Contains("Symbol"))
                {
                    int colSymbol = dgv.Columns["Symbol"].Index;
                    Rectangle rectSymbol = dgv.GetCellDisplayRectangle(colSymbol, -1, true);
                    Cmbsoymbol.Location = new Point(rectSymbol.X, topPosition);
                    Cmbsoymbol.Size = new Size(Math.Max(rectSymbol.Width - CommonHelper.GetScaled(5), CommonHelper.GetScaled(120)), height);
                    Cmbsoymbol.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    FilterPanel.Controls.Add(Cmbsoymbol);
                }
                if (dgv.Columns.Contains("Execution"))
                {
                    int colExecution = dgv.Columns["Execution"].Index;
                    Rectangle rectExecution = dgv.GetCellDisplayRectangle(colExecution, -1, true);
                    CmbExecution.Location = new Point(rectExecution.X, topPosition);
                    CmbExecution.Size = new Size(Math.Max(rectExecution.Width - CommonHelper.GetScaled(5), CommonHelper.GetScaled(90)), height);
                    CmbExecution.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    FilterPanel.Controls.Add(CmbExecution);
                }
                if (dgv.Columns.Contains("Type"))
                {
                    int colType = dgv.Columns["Type"].Index;
                    Rectangle rectType = dgv.GetCellDisplayRectangle(colType, -1, true);
                    Cmbtype.Location = new Point(rectType.X, topPosition);
                    Cmbtype.Size = new Size(Math.Max(rectType.Width - CommonHelper.GetScaled(5), CommonHelper.GetScaled(80)), height);
                    Cmbtype.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    FilterPanel.Controls.Add(Cmbtype);
                }
                if (dgv.Columns.Contains("Entry"))
                {
                    int colEntry = dgv.Columns["Entry"].Index;
                    Rectangle rectEntry = dgv.GetCellDisplayRectangle(colEntry, -1, true);
                    Cmbentry.Location = new Point(rectEntry.X, topPosition);
                    Cmbentry.Size = new Size(Math.Max(rectEntry.Width - CommonHelper.GetScaled(5), CommonHelper.GetScaled(70)), height);
                    Cmbentry.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    FilterPanel.Controls.Add(Cmbentry);
                }

                int rightX = FilterPanel.Width - CommonHelper.GetScaled(8);
                btnrequest.Location = new Point(rightX - CommonHelper.GetScaled(130), topPosition - CommonHelper.GetScaled(2));
                btnrequest.Size = new Size(CommonHelper.GetScaled(136), Cmbsoymbol.Height + CommonHelper.GetScaled(2));
                btnrequest.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                FilterPanel.Controls.Add(btnrequest);
                rightX = btnrequest.Left - CommonHelper.GetScaled(8);

                Todate.Location = new Point(rightX - CommonHelper.GetScaled(150), topPosition);
                Todate.Size = new Size(CommonHelper.GetScaled(150), height);
                Todate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                FilterPanel.Controls.Add(Todate);
                rightX = Todate.Left - CommonHelper.GetScaled(8);

                Fromdate.Location = new Point(rightX - CommonHelper.GetScaled(150), topPosition);
                Fromdate.Size = new Size(CommonHelper.GetScaled(150), height);
                Fromdate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                FilterPanel.Controls.Add(Fromdate);
                rightX = Fromdate.Left - CommonHelper.GetScaled(8);

                Cmbselectdays.Location = new Point(rightX - CommonHelper.GetScaled(190), topPosition);
                Cmbselectdays.Size = new Size(CommonHelper.GetScaled(190), height);
                Cmbselectdays.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                FilterPanel.Controls.Add(Cmbselectdays);
                #endregion
            }
            else
            {
                #region Position Layout
                if (dgv.Columns.Contains("Symbol"))
                {
                    int colSymbol = dgv.Columns["Symbol"].Index;
                    Rectangle rectSymbol = dgv.GetCellDisplayRectangle(colSymbol, -1, true);
                    Cmbsoymbol.Location = new Point(rectSymbol.X, topPosition);
                    Cmbsoymbol.Size = new Size(Math.Max(rectSymbol.Width - CommonHelper.GetScaled(5), CommonHelper.GetScaled(200)), height);
                    Cmbsoymbol.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    FilterPanel.Controls.Add(Cmbsoymbol);
                }

                int rightX = FilterPanel.Width - CommonHelper.GetScaled(8);
                btnrequest.Location = new Point(rightX - CommonHelper.GetScaled(140), topPosition - CommonHelper.GetScaled(2));
                btnrequest.Size = new Size(CommonHelper.GetScaled(146), Cmbsoymbol.Height + CommonHelper.GetScaled(2));
                btnrequest.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                FilterPanel.Controls.Add(btnrequest);
                rightX = btnrequest.Left - CommonHelper.GetScaled(8);

                Todate.Location = new Point(rightX - CommonHelper.GetScaled(150), topPosition);
                Todate.Size = new Size(CommonHelper.GetScaled(150), height);
                Todate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                FilterPanel.Controls.Add(Todate);
                rightX = Todate.Left - CommonHelper.GetScaled(8);

                Fromdate.Location = new Point(rightX - CommonHelper.GetScaled(150), topPosition);
                Fromdate.Size = new Size(CommonHelper.GetScaled(150), height);
                Fromdate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                FilterPanel.Controls.Add(Fromdate);
                rightX = Fromdate.Left - CommonHelper.GetScaled(8);

                Cmbselectdays.Location = new Point(rightX - CommonHelper.GetScaled(190), topPosition);
                Cmbselectdays.Size = new Size(CommonHelper.GetScaled(190), height);
                Cmbselectdays.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                FilterPanel.Controls.Add(Cmbselectdays);
                #endregion
            }
        }

        private void HistoryDataGrid_Resize(object sender, EventArgs e)
        {
            if (currentActiveTab == "History" && historyDataGrid != null && historyDataGrid.Visible && historyDataGrid.Columns.Count > 0)
            {
                SetupFilters(historyDataGrid);
            }
        }

        private void DaysCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_ignoreEvents) return;
            _ignoreEvents = true;

            ToDate.Value = DateTime.Today.AddDays(1);
            string selected = DaysCombo.SelectedItem?.ToString();
            DateTime toDate = DateTime.Today.Date;
            DateTime fromDate = toDate.Date;

            if (selected == "Today") { fromDate = toDate; FromDate.Value = fromDate; }
            else if (selected == "Last  3 Days") { fromDate = toDate.AddDays(-2); FromDate.Value = fromDate; }
            else if (selected == "Last Week")
            {
                int diff = (7 + (toDate.DayOfWeek - DayOfWeek.Sunday)) % 7;
                DateTime lastSunday = toDate.AddDays(-diff);
                fromDate = lastSunday;
                FromDate.Value = fromDate;
            }
            else if (selected == "Last Month") { fromDate = new DateTime(toDate.Year, toDate.Month, 1); FromDate.Value = fromDate; }
            else if (selected == "Last 3 Months") { fromDate = new DateTime(toDate.AddMonths(-2).Year, toDate.AddMonths(-2).Month, 1); FromDate.Value = fromDate; }
            else if (selected == "Last 6 Months") { fromDate = new DateTime(toDate.AddMonths(-5).Year, toDate.AddMonths(-5).Month, 1); FromDate.Value = fromDate; }
            else if (selected == "All History") { fromDate = new DateTime(1970, 1, 1); FromDate.Value = fromDate; }
            else { DaysCombo.SelectedIndex = -1; }
            _ignoreEvents = false;
        }

        private void Date_ValueChanged(object sender, EventArgs e)
        {
            if (_ignoreEvents) return;
            _ignoreEvents = true;

            DateTime from = FromDate.Value.Date;
            DateTime to = ToDate.Value.Date;
            string match = null;

            if (from == DateTime.Today && to == DateTime.Today.AddDays(1)) match = "Today";
            else if (from == DateTime.Today.AddDays(-2) && to == DateTime.Today.AddDays(1)) match = "Last  3 Days";
            else
            {
                DateTime today = DateTime.Today;
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Sunday)) % 7;
                DateTime ls = today.AddDays(-diff);
                if (from == ls && to == DateTime.Today.AddDays(1)) match = "Last Week";
            }

            DateTime firstThisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            if (match == null && from == firstThisMonth && to == DateTime.Today.AddDays(1)) match = "Last Month";
            DateTime start3 = new DateTime(DateTime.Today.AddMonths(-2).Year, DateTime.Today.AddMonths(-2).Month, 1);
            if (match == null && from == start3 && to == DateTime.Today.AddDays(1)) match = "Last 3 Months";
            DateTime start6 = new DateTime(DateTime.Today.AddMonths(-5).Year, DateTime.Today.AddMonths(-5).Month, 1);
            if (match == null && from == start6 && to == DateTime.Today.AddDays(1)) match = "Last 6 Months";
            if (match == null && from == new DateTime(1970, 1, 1) && to == DateTime.Today.AddDays(1)) match = "All History";

            DaysCombo.SelectedItem = match;
            if (match == null) DaysCombo.SelectedIndex = -1;
            _ignoreEvents = false;
        }

        private void ComboFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lblHistory.Text != "Position")
            {
                if (_originalHistory == null || !_originalHistory.Any()) return;

                string selectedSymbol = Cmbsoymbol.SelectedItem?.ToString();
                string selectedExec = CmbExecution.SelectedItem?.ToString();
                string selectedType = Cmbtype.SelectedItem?.ToString();
                string selectedEntry = Cmbentry.SelectedItem?.ToString();

                var filtered = _originalHistory.AsEnumerable();

                if (!string.IsNullOrEmpty(selectedSymbol) && selectedSymbol != "ALL") filtered = filtered.Where(x => x.symbolName == selectedSymbol);
                if (!string.IsNullOrEmpty(selectedExec) && selectedExec != "ALL") filtered = filtered.Where(x => x.orderType == selectedExec);
                if (!string.IsNullOrEmpty(selectedType) && selectedType != "ALL") filtered = filtered.Where(x => x.side == selectedType);
                if (!string.IsNullOrEmpty(selectedEntry) && selectedEntry != "ALL") filtered = filtered.Where(x => x.dealType == selectedEntry);

                var filterList = filtered.ToList();
                GetHistoryTotalPnlCommission(lblHistory.Text, null, filterList);

                if (filterList.Count > 0)
                {
                    DataTable dt = CreateHistoryDataTable(filterList);
                    dt = AddFooterRowToDataTable(dt);
                    historyDataGrid.DataSource = dt;
                }
                else historyDataGrid.DataSource = AddFooterRowToDataTable(null);
            }
            else
            {
                if (_originalPositionHistory == null || !_originalPositionHistory.Any()) return;
                string selectedSymbol = Cmbsoymbol.SelectedItem?.ToString();
                var filtered = _originalPositionHistory.AsEnumerable();
                if (!string.IsNullOrEmpty(selectedSymbol) && selectedSymbol != "ALL") filtered = filtered.Where(x => x.SymbolName == selectedSymbol);

                var filterList = filtered.ToList();
                GetHistoryTotalPnlCommission(lblHistory.Text, filterList, null);

                if (filterList.Count > 0)
                {
                    DataTable dt = CreatePositionHistoryDataTable(filterList);
                    dt = AddFooterRowToDataTable(dt);
                    historyDataGrid.DataSource = dt;
                }
                else historyDataGrid.DataSource = AddFooterRowToDataTable(null);
            }
        }

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            ComboBox comboBox = sender as ComboBox;
            string text = comboBox.Items[e.Index].ToString();
            bool isDroppedDown = comboBox.DroppedDown;
            if (isDroppedDown && (e.State & DrawItemState.Selected) == DrawItemState.Selected) e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
            else e.Graphics.FillRectangle(new SolidBrush(comboBox.BackColor), e.Bounds);
            Font font = comboBox.Font;
            if (text == "ALL" && !isDroppedDown) font = ThemeManager.CommonBoldFont;
            Brush textBrush = (isDroppedDown && (e.State & DrawItemState.Selected) == DrawItemState.Selected) ? SystemBrushes.HighlightText : new SolidBrush(comboBox.ForeColor);
            e.Graphics.DrawString(text, font, textBrush, e.Bounds.X + 2, e.Bounds.Y + 2);
        }

        #endregion

        #region UI Helpers

        private void ResetPanel()
        {
            Control filterPanelToKeep = null;
            foreach (Control ctrl in mainContainer.Controls)
            {
                if (ctrl.Name == "FilterPanel") { filterPanelToKeep = ctrl; break; }
            }
            mainContainer.Controls.Clear();
            if (filterPanelToKeep != null) mainContainer.Controls.Add(filterPanelToKeep);
            if (!this.Controls.Contains(tabControlDetails)) this.Controls.Add(tabControlDetails);
            tabControlDetails.BringToFront();
        }

        private DataGridView CreateNewDataGridView()
        {
            DataGridView newDataGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ScrollBars = ScrollBars.Both,
                RowHeadersVisible = false,
                AllowUserToResizeRows = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                ColumnHeadersHeight = CommonHelper.GetScaled(35)
            };

            typeof(DataGridView).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty, null, newDataGrid, new object[] { true });
            ThemeManager.ApplyTheme(newDataGrid);
            return newDataGrid;
        }

        private DataGridViewColumnHeaderCell CreateHeaderCell(DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleRight)
        {
            return new DataGridViewColumnHeaderCell { Style = new DataGridViewCellStyle { Alignment = alignment } };
        }

        private DataGridViewCellStyle CreateDefaultCellStyle(DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleRight)
        {
            return new DataGridViewCellStyle { Alignment = alignment };
        }

        #endregion UI Helpers

        #region Disposal
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _priceUpdateTimer?.Stop();
                _priceUpdateTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion 
    }
}