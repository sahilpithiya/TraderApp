using ClientDesktop.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TraderApp.Properties;
using TraderApp.Utils.Network;
using TraderApps.Config;
using TraderApps.Helpers;
using TraderApps.UI.Theme;

namespace TraderApp.UI.Usercontrol
{
    public partial class MarketWatchControl : UserControl
    {
        #region Variables And Fields

        private SignalRManager _signalRManager;
        private ContextMenuStrip contextMenu;
        private BindingList<MarketWatchSymbols> _bindingList;
        private SortOrder _symbolNameSortOrder = SortOrder.None;
        private ConcurrentQueue<MarketWatchSymbols> _tickQueue;
        private HashSet<string> _currentVisibleSymbols = new HashSet<string>();
        private Timer _scrollDebounceTimer;
        private bool _isUpdatingSymbols = false;
        private bool _updatePending = false;

        int dragRow = -1;
        Label dragLabel = null;

        private List<MarketWatchSymbols> removedRows = new List<MarketWatchSymbols>();
        private ContextMenuStrip suggestionMenu;

        #endregion

        #region Initialization

        public MarketWatchControl()
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.Dpi;
            ThemeManager.ApplyTheme(this);
            ThemeManager.ApplyTheme(dgvMarketWatchGrid);
            ThemeManager.ApplyTheme(btnSaveSymbol);
            date_timeLabel.Font = ThemeManager.CommonBoldFont;
            _tickQueue = new ConcurrentQueue<MarketWatchSymbols>();

            // Call Socket Action 
            //SocketManager.OnPositionUpdated += SocketManager_OnPositionUpdated;
            //SocketManager.OnSocketReconnected += SocketManager_OnReconnected;

            // Set up timer for time updates
            var timer = new Timer();
            timer.Interval = 1000; // 1 second
            timer.Tick += (s, e) =>
            {
                date_timeLabel.Text = DateTime.Now.ToString("HH:mm:ss tt");
            };
            timer.Start();

            _scrollDebounceTimer = new Timer();
            _scrollDebounceTimer.Interval = 200;
            _scrollDebounceTimer.Tick += (s, e) =>
            {
                _scrollDebounceTimer.Stop();
                _ = UpdateVisibleSymbolsAsync();
            };

            LoadInitData();
            SetPlaceholder(txtsearchsymbol, "Search Symbol");
        }
        #endregion

        #region Load Init Data

        private async void LoadInitData()
        {
            try
            {
                var marketWatchData = await LoadMarketWatchDataFromApiAsync();

                if (marketWatchData?.symbols == null || marketWatchData.symbols.Count == 0)
                    return;

                removedRows.Clear();

                SessionManager.SymbolNameList = marketWatchData.symbols.ToList();

                // Process API symbols
                var visibleSymbols = ProcessApiSymbols(marketWatchData.symbols);

                _bindingList = new BindingList<MarketWatchSymbols>(visibleSymbols);
                dgvMarketWatchGrid.DataSource = _bindingList;

                SetupGrid(marketWatchData);

                await InitSignalRAsync();
            }
            catch (Exception ex)
            {
                Console.Write("Error loading market watch data: " + ex.Message);
            }
        }

        private async Task<MarketWatchData> LoadMarketWatchDataFromApiAsync()
        {
            string domain = SessionManager.ServerListData?
                .FirstOrDefault(w => w.licenseId.ToString() == SessionManager.LicenseId)
                ?.serverDisplayName;

            string folder = Path.Combine(AppConfig.dataFolder, AESHelper.ToBase64UrlSafe(domain));
            string fileName = $"{AESHelper.ToBase64UrlSafe(SessionManager.UserId)}.dat";
            string filePath = Path.Combine(folder, fileName);

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    client.AddAuthHeader();

                    var response = await client.GetAsync(AppConfig.MarketWatchInitDataUrl.ToReplaceUrl());

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var apiResponse = JsonConvert.DeserializeObject<MarketWatchApiResponse>(jsonString);

                        if (apiResponse?.isSuccess == true && apiResponse.data != null)
                        {
                            // Serialize and encrypt the fresh data
                            string responseJson = JsonConvert.SerializeObject(apiResponse.data);
                            string encrypted = AESHelper.CompressAndEncryptString(responseJson);

                            // Load existing data (if any)
                            var existingData = File.Exists(filePath)
                                ? JsonConvert.DeserializeObject<Dictionary<string, object>>(AESHelper.DecompressAndDecryptString(File.ReadAllText(filePath)))
                                : new Dictionary<string, object>();

                            // Add/update the new symbol/user data
                            existingData["symbol"] = apiResponse.data;  // Replace "symbol" with the relevant key if needed

                            // Serialize the dictionary and save it
                            string updatedJson = JsonConvert.SerializeObject(existingData);
                            string encryptedUpdatedJson = AESHelper.CompressAndEncryptString(updatedJson);

                            CommonHelper.SaveEncryptedData(folder, AESHelper.ToBase64UrlSafe(SessionManager.UserId), encryptedUpdatedJson);

                            return apiResponse.data;
                        }
                        else
                        {
                            // Handle case where API responds but data is invalid
                            return await CommonHelper.LoadCachedData(filePath);
                        }
                    }
                    else
                    {
                        // Handle API failure and fallback to local file
                        return await CommonHelper.LoadCachedData(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                // Final fallback: try local file before showing error
                return await CommonHelper.LoadCachedData(filePath, ex);
            }
        }

        private List<MarketWatchSymbols> ProcessApiSymbols(List<MarketWatchApiSymbol> apiSymbols)
        {
            var validSymbols = apiSymbols.Where(s => s.symbolStatus);

            var visibleSymbols = new List<MarketWatchSymbols>();

            foreach (var apiSymbol in validSymbols.OrderBy(s => s.displayPosition))
            {
                var symbolModel = CreateMarketWatchSymbol(apiSymbol);

                if (!apiSymbol.symbolHide)
                {
                    visibleSymbols.Add(symbolModel);
                }
                else
                {
                    if (!removedRows.Any(r => r.SymbolName == symbolModel.SymbolName))
                    {
                        removedRows.Add(symbolModel);
                    }
                }
            }

            return visibleSymbols;
        }

        private MarketWatchSymbols CreateMarketWatchSymbol(MarketWatchApiSymbol apiSymbol)
        {
            return new MarketWatchSymbols
            {
                SymbolId = apiSymbol.symbolId,
                symbolDigit = apiSymbol.symbolDigits,
                SymbolName = apiSymbol.symbolName,
                Bid = decimal.Parse((apiSymbol.symbolBook?.bid ?? 0).ToString($"F{apiSymbol.symbolDigits}")),
                Ask = decimal.Parse((apiSymbol.symbolBook?.ask ?? 0).ToString($"F{apiSymbol.symbolDigits}")),
                LTP = decimal.Parse((apiSymbol.symbolBook?.ltp ?? 0).ToString($"F{apiSymbol.symbolDigits}")),
                High = decimal.Parse((apiSymbol.symbolBook?.high ?? 0).ToString($"F{apiSymbol.symbolDigits}")),
                Low = decimal.Parse((apiSymbol.symbolBook?.low ?? 0).ToString($"F{apiSymbol.symbolDigits}")),
                BuyVolume = decimal.Parse((apiSymbol.symbolBook?.buyVolume ?? 0).ToString($"F{apiSymbol.symbolDigits}")),
                SellVolume = decimal.Parse((apiSymbol.symbolBook?.sellVolume ?? 0).ToString($"F{apiSymbol.symbolDigits}")),
                Open = decimal.Parse((apiSymbol.symbolBook?.open ?? 0).ToString($"F{apiSymbol.symbolDigits}")),
                PreviousClose = decimal.Parse((apiSymbol.symbolBook?.previousClose ?? 0).ToString($"F{apiSymbol.symbolDigits}")),
                UpdateTime = apiSymbol.symbolBook?.updateTime ?? 0,
                Spread = GetSpread(apiSymbol.symbolBook?.bid ?? 0, apiSymbol.symbolBook?.bid ?? 0, apiSymbol.symbolDigits),
                DailyChangePercent = GetDailyChangePercent(apiSymbol.symbolBook?.bid ?? 0, apiSymbol.symbolBook?.previousClose ?? 0),
                DailyChangeValue = GetDailyChangeValue(apiSymbol.symbolBook?.bid ?? 0, apiSymbol.symbolBook?.previousClose ?? 0)
            };
        }

        private void SetupGrid(MarketWatchData marketWatchData)
        {
            // Double buffering (flicker fix)
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.SetProperty,
                null, dgvMarketWatchGrid, new object[] { true });

            if (marketWatchData != null)
            {
                cmbfontsize.SelectedItem = !string.IsNullOrEmpty(marketWatchData.fontSize.ToString()) && !marketWatchData.fontSize.Equals(0) ? marketWatchData.fontSize.ToString() : "10";
                ApplyColumnVisibility(marketWatchData.displayColumnNames != null ? marketWatchData.displayColumnNames as string : null);
            }

            InitializeContextMenu();

            //Date Format
            if (dgvMarketWatchGrid.Columns["UpdateDateTime"] != null)
                dgvMarketWatchGrid.Columns["UpdateDateTime"].DefaultCellStyle.Format = "HH:mm:ss";

            dgvMarketWatchGrid.Columns["DailyChangePercent"].DefaultCellStyle.Format = "0.00'%'";

            // Alignments (Right for numbers)
            foreach (DataGridViewColumn col in dgvMarketWatchGrid.Columns)
            {
                if (col.Name != "SymbolName" && col.Name != "DragHandle")
                {
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            // Add a drag handle column if not already added
            if (!dgvMarketWatchGrid.Columns.Contains("DragHandle"))
            {
                DataGridViewImageColumn dragCol = new DataGridViewImageColumn();
                dragCol.Name = "DragHandle";
                dragCol.HeaderText = "";
                dragCol.MinimumWidth = 30;
                dragCol.Width = 30;
                dragCol.ReadOnly = true;
                dragCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dragCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
                dgvMarketWatchGrid.Columns.Insert(0, dragCol);
            }

            // Set handle symbol for each row
            dgvMarketWatchGrid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex == dgvMarketWatchGrid.Columns["DragHandle"].Index)
                {
                    // Check if last row
                    bool isLastRow = e.RowIndex == dgvMarketWatchGrid.Rows.Count - 1;

                    if (isLastRow)
                    {
                        e.Value = Properties.Resources.add;
                    }
                    else
                    {
                        e.Value = Properties.Resources.drag_drop;
                    }

                    e.CellStyle.NullValue = null;
                }

                bool isLastRowGeneral = e.RowIndex == dgvMarketWatchGrid.Rows.Count - 1;
                if (isLastRowGeneral && e.ColumnIndex >= 0)
                {
                    e.CellStyle.SelectionBackColor = ThemeManager.White;

                    if (e.ColumnIndex != dgvMarketWatchGrid.Columns["DragHandle"].Index &&
                        e.ColumnIndex != dgvMarketWatchGrid.Columns["SymbolName"].Index)
                    {
                        e.Value = "";
                    }
                }
            };

            dgvMarketWatchGrid.CellMouseDown += dgvMarketWatchGrid_MouseDown;
            dgvMarketWatchGrid.MouseMove += dgvMarketWatchGrid_MouseMove;
            dgvMarketWatchGrid.MouseUp += dgvMarketWatchGrid_MouseUp;
            dgvMarketWatchGrid.ColumnHeaderMouseClick += DgvMarketWatchGrid_ColumnHeaderMouseClick;

            EnsureEmptyRow();
            dgvMarketWatchGrid.EditingControlShowing += dgvMarketWatchGrid_EditingControlShowing;
            dgvMarketWatchGrid.CellBeginEdit += dgvMarketWatchGrid_CellBeginEdit;
            dgvMarketWatchGrid.CellPainting += dgvMarketWatchGrid_CellPainting;
        }

        #endregion

        #region Grid Features And Events

        private void InitializeContextMenu()
        {
            contextMenu = new ContextMenuStrip();

            //// Specification
            //ToolStripMenuItem specificationItem = new ToolStripMenuItem("Specification", Properties.Resources.m_details, OnSpecification_Click);
            //contextMenu.Items.Add(specificationItem);

            //// Chart window
            //ToolStripMenuItem chartWindowItem = new ToolStripMenuItem("Chart window", Properties.Resources.m_tickchart, OnChartWindow_Click);
            //contextMenu.Items.Add(chartWindowItem);

            // 🔹 Separator
            ToolStripSeparator separator1 = new ToolStripSeparator();
            contextMenu.Items.Add(separator1);

            // Hide
            ToolStripMenuItem hideItem = new ToolStripMenuItem("Hide", Properties.Resources.m_hide, OnHide_Click);
            contextMenu.Items.Add(hideItem);

            // Hide All
            ToolStripMenuItem hideAllItem = new ToolStripMenuItem("Hide All", Resources.m_hideall, OnHideAll_Click);
            contextMenu.Items.Add(hideAllItem);

            // Show All
            ToolStripMenuItem showAllItem = new ToolStripMenuItem("Show All", Resources.m_showall, OnShowAll_Click);
            contextMenu.Items.Add(showAllItem);

            //// Symbol
            //ToolStripMenuItem symbolAllItem = new ToolStripMenuItem("Symbol", Resources.symbol, OnSymbol_Click);
            //contextMenu.Items.Add(symbolAllItem);

            // 🔹 Separator (2nd one, if needed)
            ToolStripSeparator separator2 = new ToolStripSeparator();
            contextMenu.Items.Add(separator2);

            // Column submenu
            ToolStripMenuItem columnMenu = new ToolStripMenuItem("Column", Resources.m_column);

            // Dynamically add all columns from the DataGridView to the context menu
            foreach (DataGridViewColumn column in dgvMarketWatchGrid.Columns)
            {
                // Skip the columns that you don't want to display in the menu (e.g., certain default columns)
                string[] excludedColumn = { "SymbolName", "Bid", "Ask", "Low" };
                if (excludedColumn.Contains(column.Name)) continue;

                string text = string.Empty;

                switch (column.HeaderText)
                {
                    case "High":
                        text = "High/Low";
                        break;
                    case "DCP":
                        text = "Daily Change %";
                        break;
                    case "DCV":
                        text = "Daily Change Value";
                        break;
                    default:
                        text = column.HeaderText;
                        break;
                }

                // Create a new ToolStripMenuItem for each column
                ToolStripMenuItem columnItem = new ToolStripMenuItem(text, null, OnColumn_Click)
                {
                    // Set the name of the menu item to match the column name for easy identification
                    Name = column.Name,
                    Checked = column.Visible,  // Initially set the checkbox based on column visibility
                    DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                    Image = column.Visible ? Properties.Resources.m_checkmark : null
                };

                // Add the column item to the column menu
                columnMenu.DropDownItems.Add(columnItem);
            }

            contextMenu.Items.Add(columnMenu);

            // Assign to DataGridView
            dgvMarketWatchGrid.ContextMenuStrip = contextMenu;
            contextMenu.Opening += (s, e) =>
            {
                if (dgvMarketWatchGrid.SelectedRows.Count == 0)
                {
                    e.Cancel = true;
                    return;
                }

                var selectedRow = dgvMarketWatchGrid.SelectedRows[0];
                if (selectedRow.DataBoundItem is MarketWatchSymbols symbol)
                {
                    if (string.IsNullOrEmpty(symbol.SymbolName))
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                else
                {
                    e.Cancel = true;
                }
            };
        }

        private void cmbfontsize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbfontsize.SelectedItem != null)
            {
                // Convert selected item to integer (assuming it's a number like 10, 12, 14, etc.)
                int fontSize;
                if (int.TryParse(cmbfontsize.SelectedItem.ToString(), out fontSize))
                {
                    // Set the font size for the DataGridView and its column headers
                    dgvMarketWatchGrid.RowsDefaultCellStyle.Font = new Font("Microsoft Sans Serif", fontSize, FontStyle.Regular);
                    //dgvMarketWatchGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft Sans Serif", fontSize, FontStyle.Regular);

                    // Adjust column based on font size
                    AdjustGridDimensions(fontSize);
                }
            }
        }

        private void ApplyColumnVisibility(string apiFields)
        {
            if (string.IsNullOrEmpty(apiFields)) return;

            var fields = new HashSet<string>(apiFields.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim().ToLowerInvariant()));

            // Map API fields -> Grid columns
            var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "ltp", new[] { "LTP" } },
                { "hl", new[] { "High", "Low" } },
                { "open", new[] { "Open" } },
                { "close", new[] { "PreviousClose" } },
                { "time", new[] { "UpdateDateTime" } },
                { "spread", new[] { "Spread" } },
                { "dailychangepercentage", new[] { "DailyChangePercent" } },
                { "dailychangevalue", new[] { "DailyChangeValue" } },
            };

            foreach (DataGridViewColumn col in dgvMarketWatchGrid.Columns)
            {
                string[] excludedColumn = { "DragHandle", "SymbolName", "Bid", "Ask" };
                if (excludedColumn.Contains(col.Name)) continue;
                col.Visible = false;
            }

            // Show only those columns present in API response
            foreach (var field in fields)
            {
                if (map.TryGetValue(field, out var gridCols))
                {
                    foreach (var colName in gridCols)
                    {
                        if (dgvMarketWatchGrid.Columns.Contains(colName))
                            dgvMarketWatchGrid.Columns[colName].Visible = true;
                    }
                }
            }
        }

        private void AdjustGridDimensions(int newFontSize)
        {
            if (dgvMarketWatchGrid.ColumnCount > 0)
            {
                // Default font size is 12, so we calculate the scale factor
                float scaleFactor = (float)newFontSize / 10;

                // Adjust each column's width based on the font size
                dgvMarketWatchGrid.Columns["SymbolName"].MinimumWidth = Math.Max(CommonHelper.GetScaled(200), (int)(CommonHelper.GetScaled(200) * scaleFactor));
                dgvMarketWatchGrid.Columns["Bid"].MinimumWidth = Math.Max(CommonHelper.GetScaled(80), (int)(CommonHelper.GetScaled(100) * scaleFactor));
                dgvMarketWatchGrid.Columns["Ask"].MinimumWidth = Math.Max(CommonHelper.GetScaled(80), (int)(CommonHelper.GetScaled(100) * scaleFactor));
                dgvMarketWatchGrid.Columns["LTP"].MinimumWidth = Math.Max(CommonHelper.GetScaled(80), (int)(CommonHelper.GetScaled(100) * scaleFactor));
                dgvMarketWatchGrid.Columns["High"].MinimumWidth = Math.Max(CommonHelper.GetScaled(80), (int)(CommonHelper.GetScaled(100) * scaleFactor));
                dgvMarketWatchGrid.Columns["Low"].MinimumWidth = Math.Max(CommonHelper.GetScaled(80), (int)(CommonHelper.GetScaled(100) * scaleFactor));
                dgvMarketWatchGrid.Columns["Open"].MinimumWidth = Math.Max(CommonHelper.GetScaled(80), (int)(CommonHelper.GetScaled(100) * scaleFactor));
                dgvMarketWatchGrid.Columns["PreviousClose"].MinimumWidth = Math.Max(CommonHelper.GetScaled(80), (int)(CommonHelper.GetScaled(100) * scaleFactor));
                dgvMarketWatchGrid.Columns["UpdateDateTime"].MinimumWidth = Math.Max(CommonHelper.GetScaled(80), (int)(CommonHelper.GetScaled(100) * scaleFactor));
                dgvMarketWatchGrid.Columns["Spread"].MinimumWidth = Math.Max(CommonHelper.GetScaled(80), (int)(CommonHelper.GetScaled(100) * scaleFactor));
                dgvMarketWatchGrid.Columns["DailyChangePercent"].MinimumWidth = Math.Max(CommonHelper.GetScaled(70), (int)(CommonHelper.GetScaled(90) * scaleFactor));
                dgvMarketWatchGrid.Columns["DailyChangeValue"].MinimumWidth = Math.Max(CommonHelper.GetScaled(80), (int)(CommonHelper.GetScaled(100) * scaleFactor));

                //// Adjust row height based on the font size
                //int newHeaderHeight = (int)(newFontSize * 2); // Header height is typically a little larger
                //newHeaderHeight = Math.Max(newHeaderHeight, 28); // Ensure header height does not go below 28px
                dgvMarketWatchGrid.ColumnHeadersHeight = CommonHelper.GetScaled(35);//newHeaderHeight;

                int newRowHeight = (int)(newFontSize * 2); // Increase height based on font size
                newRowHeight = Math.Max(newRowHeight, CommonHelper.GetScaled(30)); // Ensure row height does not go below 22px
                foreach (DataGridViewRow row in dgvMarketWatchGrid.Rows)
                {
                    row.Height = newRowHeight; // Apply the same height to each row
                }
            }
        }

        private void OnColumn_Click(object sender, EventArgs e)
        {
            // Get the clicked ToolStripMenuItem
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;

            if (menuItem != null)
            {
                // Retrieve the column name from the menu item
                string columnName = menuItem.Name; // Since we set Name as column's Name

                // Check if the column exists in the DataGridView
                if (dgvMarketWatchGrid.Columns.Contains(columnName))
                {
                    // Toggle the visibility of the column
                    bool newVisibility = !dgvMarketWatchGrid.Columns[columnName].Visible;
                    dgvMarketWatchGrid.Columns[columnName].Visible = newVisibility;

                    if (columnName == "High")
                    {
                        dgvMarketWatchGrid.Columns["Low"].Visible = newVisibility;
                    }

                    // Update the checkbox state based on the column's visibility
                    menuItem.Checked = newVisibility;
                    menuItem.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                    menuItem.Image = newVisibility ? Properties.Resources.m_checkmark : null;
                }
            }
        }

        #endregion

        #region Symbol Search Feature

        private async void txtsearchsymbol_TextChanged(object sender, EventArgs e)
        {

            if (txtsearchsymbol.Text.Equals("Search Symbol") || _bindingList == null || dgvMarketWatchGrid.Rows.Count == 0) return;

            var keywords = txtsearchsymbol.Text
                .Split(',')
                .Select(k => k.Trim().ToLowerInvariant())
                .Where(k => k.Length > 0)
                .ToArray();

            dgvMarketWatchGrid.SuspendLayout();

            int safeIndex = dgvMarketWatchGrid.Rows.Count - 1;

            if (dgvMarketWatchGrid.CurrentCell != null)
            {
                var currentRow = dgvMarketWatchGrid.CurrentCell.RowIndex;
                var currentSymbol = dgvMarketWatchGrid.Rows[currentRow].DataBoundItem as MarketWatchSymbols;

                bool match = currentSymbol != null && (keywords.Length == 0 ||
                             keywords.Any(k => currentSymbol.SymbolName.ToLowerInvariant().Contains(k)));

                if (!match)
                {
                    if (safeIndex >= 0)
                        dgvMarketWatchGrid.CurrentCell = dgvMarketWatchGrid.Rows[safeIndex].Cells["SymbolName"];
                    else
                        dgvMarketWatchGrid.CurrentCell = null;
                }
            }

            foreach (DataGridViewRow row in dgvMarketWatchGrid.Rows)
            {
                var symbol = row.DataBoundItem as MarketWatchSymbols;
                bool match = (keywords.Length == 0) ||
                             (symbol != null && (string.IsNullOrEmpty(symbol.SymbolName) || keywords.Any(k => (symbol.SymbolName ?? "").ToLowerInvariant().Contains(k))));

                row.Visible = match;
            }

            dgvMarketWatchGrid.ResumeLayout();
            await Task.Delay(500);
            dgvMarketWatchGrid_Scroll(this, new ScrollEventArgs(ScrollEventType.ThumbPosition, 0));
        }

        private void txtsearchsymbol_KeyDown(object sender, KeyEventArgs e)
        {
            // Check if Ctrl + Backspace is pressed
            if (e.Control && e.KeyCode == Keys.Back)
            {
                txtsearchsymbol.Clear();
                txtsearchsymbol.Focus();
                e.SuppressKeyPress = true; // Prevent default backspace behavior 
            }
            else if (e.KeyCode == Keys.Back)
            {
                txtsearchsymbol.Clear();
                txtsearchsymbol.Focus();
                e.SuppressKeyPress = true;
            }
            else
            {

            }
        }

        private void SetPlaceholder(TextBox txt, string placeholder)
        {
            // Default placeholder state
            txt.Text = placeholder;

            // 🔸 When user focuses on the textbox
            txt.Enter += (s, e) =>
            {
                if (txt.Text == placeholder)
                {
                    txt.Text = "";
                }
            };

            // 🔸 When textbox loses focus
            txt.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt.Text))
                {
                    txt.Text = placeholder;
                }
            };
        }

        #endregion

        #region Symbol Hide And Show Feature

        private async void OnHide_Click(object sender, EventArgs e)
        {
            if (dgvMarketWatchGrid.SelectedRows.Count == 0) return;

            var selectedRow = dgvMarketWatchGrid.SelectedRows[0];
            if (selectedRow.DataBoundItem is MarketWatchSymbols symbol)
            {
                await ProcessHideOperation(new List<int> { symbol.SymbolId }, "Hide");
            }
        }

        private async void OnHideAll_Click(object sender, EventArgs e)
        {
            try
            {
                var visibleSymbols = _bindingList
                    .Where(symbol => !string.IsNullOrWhiteSpace(symbol.SymbolName))
                    .ToList();

                if (visibleSymbols.Count == 0)
                {
                    //MessagePopup.ShowPopup(CommonMessages.NoSymbolHide);
                    return;
                }

                var ids = visibleSymbols.Select(s => s.SymbolId).ToList();
                await ProcessHideOperation(ids, "Hide All");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error hiding symbols: {ex.Message}");
            }
        }

        private async Task ProcessHideOperation(List<int> symbolIds, string operationName)
        {
            if (symbolIds == null || symbolIds.Count == 0) return;

            var hideSymbol = await CallHideApiAsync(symbolIds);
            if (hideSymbol == null) return;

            // Process successful hide operation
            if (hideSymbol?.data?.symbolId != null)
            {
                var symbolsToRemove = _bindingList
                    .Where(symbol => hideSymbol.data.symbolId.Contains(symbol.SymbolId))
                    .ToList();

                foreach (var symbol in symbolsToRemove)
                {
                    if (!removedRows.Contains(symbol))
                    {
                        removedRows.Add(symbol);
                    }
                    _bindingList.Remove(symbol);
                }

                EnsureEmptyRow();
                await Task.Delay(100);
                await UpdateVisibleSymbolsAsync();
            }

            // Show success message
            if (!string.IsNullOrEmpty(hideSymbol.successMessage))
            {
                //MessagePopup.ShowPopup($"{hideSymbol.successMessage}", hideSymbol?.data?.symbolId != null ? true : false);
            }
        }

        private async Task<HideSymbolResponse> CallHideApiAsync(List<int> symbolIds)
        {
            if (symbolIds == null || symbolIds.Count == 0)
                return null;

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    client.AddAuthHeader();

                    var payload = new { symbolId = symbolIds };
                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PutAsync(AppConfig.MarketWatchHideApiUrl.ToReplaceUrl(), content);
                    var respBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Hide API HTTP Error: {response.StatusCode} - {respBody}");
                        return null;
                    }

                    var apiResp = JsonConvert.DeserializeObject<HideSymbolResponse>(respBody);

                    if (apiResp != null && apiResp.isSuccess)
                    {
                        return apiResp;
                    }
                    else
                    {
                        string err = apiResp?.exception ?? "Unknown error";
                        Console.WriteLine($"Hide failed: {err}", "Error");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hide API exception: {ex.Message}");
                return null;
            }
        }

        private async void OnShowAll_Click(object sender, EventArgs e)
        {
            try
            {
                if (removedRows.Count == 0)
                {
                    //MessagePopup.ShowPopup(CommonMessages.NoHiddenSymbolShow);
                    return;
                }

                // All hidden symbols ko wapas add karein
                var restoredCount = 0;
                var symbolsToRestore = removedRows.ToList();

                foreach (var symbol in symbolsToRestore)
                {
                    // Check if symbol already exists in binding list
                    var existing = _bindingList.FirstOrDefault(x =>
                        x.SymbolName?.ToReplaceSymbol() ==
                        symbol.SymbolName?.ToReplaceSymbol());

                    if (existing == null)
                    {
                        _bindingList.Insert(_bindingList.Count - 1, symbol);
                        restoredCount++;
                    }
                }

                // Restore ho gaye symbols ko removedRows se hatao
                removedRows.Clear();
                EnsureEmptyRow();

                await Task.Delay(100);
                await UpdateVisibleSymbolsAsync();
                //MessagePopup.ShowPopup($"{restoredCount} {CommonMessages.HiddenSymbolRestored}", true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing symbols: {ex.Message}");
            }
        }

        #endregion

        #region Hidden Symbol Add Feature

        private void EnsureEmptyRow()
        {
            if (_bindingList == null) return;

            // Always ensure last row is empty and there's exactly one empty row
            var last = _bindingList.LastOrDefault();
            if (last == null || !string.IsNullOrWhiteSpace(last.SymbolName))
            {
                _bindingList.Add(new MarketWatchSymbols { SymbolName = "" });
            }

            // Remove multiple empty rows if any
            var emptyRows = _bindingList.Where(x => string.IsNullOrWhiteSpace(x.SymbolName)).ToList();
            if (emptyRows.Count > 1)
            {
                for (int i = 0; i < emptyRows.Count - 1; i++)
                {
                    _bindingList.Remove(emptyRows[i]);
                }
            }

            cmbfontsize_SelectedIndexChanged(null, EventArgs.Empty);
        }

        private void dgvMarketWatchGrid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            // allow edit only in SymbolName column of last row
            if (e.ColumnIndex != dgvMarketWatchGrid.Columns["SymbolName"].Index)
            {
                e.Cancel = true;
                return;
            }

            if (e.RowIndex != dgvMarketWatchGrid.Rows.Count - 1)
            {
                e.Cancel = true;
            }
        }

        private void dgvMarketWatchGrid_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dgvMarketWatchGrid.CurrentCell.ColumnIndex == dgvMarketWatchGrid.Columns["SymbolName"].Index &&
                dgvMarketWatchGrid.CurrentCell.RowIndex == dgvMarketWatchGrid.Rows.Count - 1)
            {
                var txt = e.Control as TextBox;
                if (txt != null)
                {
                    txt.TextChanged -= SymbolEditing_TextChanged;
                    txt.TextChanged += SymbolEditing_TextChanged;
                    txt.Enter -= Txt_Enter;
                    txt.Enter += Txt_Enter;
                    txt.Leave -= Txt_Leave;
                    txt.Leave += Txt_Leave;
                }
            }
        }

        private void SymbolEditing_TextChanged(object sender, EventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt == null) return;

            string searchText = txt.Text.Trim();
            DisposeSuggestionDropdown();

            if (string.IsNullOrWhiteSpace(searchText))
                return;

            // Match only from removedRows
            var matches = removedRows
                .Where(s => s.SymbolName.ToReplaceSymbol()
                .IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(10)
                .ToList();

            if (matches.Count == 0) return;

            suggestionMenu = new ContextMenuStrip
            {
                AutoClose = false
            };

            foreach (var symbol in matches)
            {
                string symbolName = symbol.SymbolName.ToReplaceSymbol();
                var item = new ToolStripMenuItem(symbolName);
                item.Click += async (s, e2) =>
                {
                    // Clear the current editing cell first
                    int currentRowIndex = dgvMarketWatchGrid.CurrentCell.RowIndex;

                    // End edit mode and clear the cell
                    dgvMarketWatchGrid.EndEdit();
                    _bindingList[currentRowIndex].SymbolName = "";

                    // Add symbol at second last position (just before the blank row)
                    _bindingList.Insert(_bindingList.Count - 1, symbol);
                    removedRows.Remove(symbol);

                    // Ensure blank row stays at the end
                    EnsureEmptyRow();
                    DisposeSuggestionDropdown();

                    await Task.Delay(100);
                    await UpdateVisibleSymbolsAsync();

                    // Move focus to the newly added row
                    if (_bindingList.Count > 1)
                    {
                        dgvMarketWatchGrid.CurrentCell = dgvMarketWatchGrid.Rows[_bindingList.Count - 2].Cells["SymbolName"];
                    }
                };
                suggestionMenu.Items.Add(item);
            }

            var cellRect = dgvMarketWatchGrid.GetCellDisplayRectangle(
                dgvMarketWatchGrid.CurrentCell.ColumnIndex,
                dgvMarketWatchGrid.CurrentCell.RowIndex, true);

            var screenPos = dgvMarketWatchGrid.PointToScreen(
                new Point(cellRect.X, cellRect.Y + cellRect.Height));

            suggestionMenu.Show(screenPos);

            BeginInvoke((Action)(() =>
            {
                txt.Focus();
            }));

        }

        private async void Txt_Leave(object sender, EventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt == null) return;

            await Task.Delay(150);

            if (!txt.Focused)
                DisposeSuggestionDropdown();
        }

        private void Txt_Enter(object sender, EventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt == null) return;
            if (!string.IsNullOrEmpty(txt.Text))
            {
                SymbolEditing_TextChanged(sender, e);
            }
        }

        private void DisposeSuggestionDropdown()
        {
            if (suggestionMenu != null)
            {
                suggestionMenu.Close();
                suggestionMenu.Dispose();
                suggestionMenu = null;
            }
        }

        #endregion

        #region Movable Row Feature

        private void MoveRow(int fromIndex, int toIndex)
        {
            // Ensure valid indices
            if (fromIndex < 0 || toIndex < 0 || fromIndex >= _bindingList.Count || toIndex >= _bindingList.Count)
                return;

            // Swap the rows in the BindingList
            var rowToMove = _bindingList[fromIndex];
            _bindingList.RemoveAt(fromIndex);
            _bindingList.Insert(toIndex, rowToMove);

            // Optional: Update the row positions in the DataGridView
            dgvMarketWatchGrid.Refresh();
            cmbfontsize_SelectedIndexChanged(null, EventArgs.Empty);
        }

        // Cell Mouse Down Event
        private void dgvMarketWatchGrid_MouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0 || e.RowIndex == dgvMarketWatchGrid.Rows.Count - 1 || dgvMarketWatchGrid[1, e.RowIndex].Value == null) return;

            if (e.ColumnIndex == dgvMarketWatchGrid.Columns["DragHandle"].Index)
            {
                dragRow = e.RowIndex;
                if (dragLabel == null) dragLabel = new Label();
                dragLabel.Text = dgvMarketWatchGrid[1, e.RowIndex].Value.ToString().ToReplaceSymbol() ?? "";
                dragLabel.Parent = dgvMarketWatchGrid;
                dragLabel.Location = e.Location;
                dragLabel.AutoSize = true;
                dragLabel.TextAlign = ContentAlignment.MiddleCenter;
                dragLabel.BackColor = ThemeManager.Black;
                dragLabel.ForeColor = ThemeManager.White;
            }
            else
            {
                dragRow = -1;
            }
        }

        // Mouse Move Event
        private void dgvMarketWatchGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && dragLabel != null)
            {
                dragLabel.Location = e.Location;
                dgvMarketWatchGrid.ClearSelection();
            }
        }

        // Mouse Up Event (when the user releases the mouse)
        private void dgvMarketWatchGrid_MouseUp(object sender, MouseEventArgs e)
        {
            var hit = dgvMarketWatchGrid.HitTest(e.X, e.Y);
            int dropRow = -1;
            if (hit.Type != DataGridViewHitTestType.None)
            {
                dropRow = hit.RowIndex;

                if (dropRow == dgvMarketWatchGrid.Rows.Count - 1)
                {
                    dropRow = -1;
                }

                if (dragRow >= 0 && dropRow >= 0 && dragRow != dropRow)
                {
                    // Move the row
                    MoveRow(dragRow, dropRow);
                }
            }

            if (dragLabel != null)
            {
                dragLabel.Dispose();
                dragLabel = null;
            }

            txtsearchsymbol_TextChanged(this, EventArgs.Empty);
        }

        #endregion

        #region Sorting Feature

        private void DgvMarketWatchGrid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == dgvMarketWatchGrid.Columns["SymbolName"].Index)
            {
                _symbolNameSortOrder = _symbolNameSortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
                SortSymbolNameColumn();
            }
        }

        private void SortSymbolNameColumn()
        {
            if (_bindingList == null || _bindingList.Count <= 1) return;

            var items = new List<MarketWatchSymbols>();
            MarketWatchSymbols emptyItem = null;

            // Single pass to separate items
            foreach (var item in _bindingList)
            {
                if (string.IsNullOrWhiteSpace(item.SymbolName))
                    emptyItem = item;
                else
                    items.Add(item);
            }

            // Fast sort with precomputed values
            var comparison = _symbolNameSortOrder == SortOrder.Ascending
                ? (Comparison<MarketWatchSymbols>)((x, y) => string.Compare(
                    x.SymbolName.ToReplaceSymbol(),
                    y.SymbolName.ToReplaceSymbol(),
                    StringComparison.OrdinalIgnoreCase))
                : (x, y) => string.Compare(
                    y.SymbolName.ToReplaceSymbol(),
                    x.SymbolName.ToReplaceSymbol(),
                    StringComparison.OrdinalIgnoreCase);

            items.Sort(comparison);

            // Efficient rebuild
            _bindingList.RaiseListChangedEvents = false;
            _bindingList.Clear();

            foreach (var item in items)
                _bindingList.Add(item);

            if (emptyItem != null)
                _bindingList.Add(emptyItem);

            _bindingList.RaiseListChangedEvents = true;
            _bindingList.ResetBindings();

            dgvMarketWatchGrid.Columns["SymbolName"].HeaderCell.SortGlyphDirection = _symbolNameSortOrder;

            // Quick search refresh if needed
            if (txtsearchsymbol.TextLength > 0 && txtsearchsymbol.Text != "Search Symbol")
                txtsearchsymbol_TextChanged(this, EventArgs.Empty);
            cmbfontsize_SelectedIndexChanged(null, EventArgs.Empty);
            dgvMarketWatchGrid_Scroll(this, new ScrollEventArgs(ScrollEventType.ThumbPosition, 0));
        }

        #endregion

        #region Save Client Watch Profile

        private async void saveSymbol_Click(object sender, EventArgs e)
        {
            await SaveClientWatchProfileAsync();
        }

        private async Task SaveClientWatchProfileAsync()
        {
            try
            {
                btnSaveSymbol.Enabled = false;
                if (_bindingList == null || _bindingList.Count <= 1)
                {
                    //MessagePopup.ShowPopup(CommonMessages.NoSymbolSave);
                    return;
                }

                // 🔹 Font size from combo
                int fontSize = 12;
                if (cmbfontsize.SelectedItem != null)
                    int.TryParse(cmbfontsize.SelectedItem.ToString(), out fontSize);

                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "LTP", "ltp" },
                    { "High", "hl" },
                    { "Low", "hl" },
                    { "Open", "open" },
                    { "PreviousClose", "close" },
                    { "UpdateDateTime", "time" },
                    { "Spread", "spread" },
                    { "DailyChangePercent", "dailyChangePercentage" },
                    { "DailyChangeValue", "dailyChangeValue" },
                };

                var apiFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (DataGridViewColumn col in dgvMarketWatchGrid.Columns)
                {
                    if (!col.Visible)
                        continue;

                    if (map.TryGetValue(col.Name, out var apiName))
                    {
                        apiFields.Add(apiName);
                    }
                }

                string displayColumns = string.Join(",", apiFields);

                // 🔹 Prepare symbolsConfig
                var symbolsConfig = new List<object>();
                int position = 1;

                foreach (var row in _bindingList)
                {
                    if (string.IsNullOrWhiteSpace(row.SymbolName))
                        continue;

                    symbolsConfig.Add(new
                    {
                        symbolId = row.SymbolId,
                        symbolHide = false,
                        displayPosition = position++
                    });
                }

                // 🔹 Final payload
                var payload = new
                {
                    fontSize = fontSize,
                    displayColumnNames = displayColumns,
                    symbolsConfig = symbolsConfig
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    client.AddAuthHeader();

                    var response = await client.PostAsync(AppConfig.MarketWatchSaveClientProfileUrl.ToReplaceUrl(), content);
                    var respBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Save Profile HTTP Error: {respBody}");
                        return;
                    }

                    var apiResp = JsonConvert.DeserializeObject<HideSymbolResponse>(respBody);

                    if (apiResp?.isSuccess == true)
                    {
                        //MessagePopup.ShowPopup(apiResp.successMessage ?? CommonMessages.ProfileSaved, true);
                    }
                    else
                    {
                        //MessagePopup.ShowPopup(CommonMessages.ProfileFailedToSaved);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while saving profile: {ex.Message}");
            }
            finally
            {
                btnSaveSymbol.Enabled = true;
            }
        }

        #endregion

        #region SignalR
        private async Task InitSignalRAsync()
        {
            try
            {
                _signalRManager = new SignalRManager(AppConfig.MarketWatchSignalRUrl.ToReplaceUrl("sglr"));

                _signalRManager.OnMessageReceived += (data) =>
                {
                    var symbolData = ParsePipeMessage(data);
                    if (_tickQueue != null && symbolData != null && !string.IsNullOrEmpty(symbolData.SymbolName))
                    {
                        _tickQueue.Enqueue(symbolData);
                        if (this.IsHandleCreated && !this.IsDisposed)
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                RefreshGrid();
                            }));
                        }
                    }
                };

                _signalRManager.OnReconnected += async () =>
                {
                    _currentVisibleSymbols.Clear();
                    await UpdateVisibleSymbolsAsync();
                };

                await _signalRManager.StartAsync();
                await UpdateVisibleSymbolsAsync();
            }
            catch { }
        }

        private MarketWatchSymbols ParsePipeMessage(string data)
        {
            try
            {
                var parts = data.Split('|');
                if (parts.Length < 12) return null;  // ignore bad messages

                bool TryParseDecimal(string val, out decimal result) =>
                    decimal.TryParse(val, System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture, out result);

                bool TryParseLong(string val, out long result) =>
                    long.TryParse(val, out result);

                int symbolDigit = 0;

                if (_bindingList != null)
                {
                    var existing = _bindingList.FirstOrDefault(s =>
                        s.SymbolName?.ToReplaceSymbol() == parts[0]);
                    if (existing != null) symbolDigit = existing.symbolDigit;
                }

                // Helper function to format double with symbolDigit
                decimal FormatDecimal(decimal value, int digits) =>
                    decimal.Parse((value).ToString($"F{digits}", System.Globalization.CultureInfo.InvariantCulture));

                return new MarketWatchSymbols
                {
                    SymbolName = parts[0],
                    Bid = TryParseDecimal(parts[2], out var bid) ? FormatDecimal(bid, symbolDigit) : 0,
                    Ask = TryParseDecimal(parts[3], out var ask) ? FormatDecimal(ask, symbolDigit) : 0,
                    LTP = TryParseDecimal(parts[4], out var ltp) ? FormatDecimal(ltp, symbolDigit) : 0,
                    High = TryParseDecimal(parts[5], out var high) ? FormatDecimal(high, symbolDigit) : 0,
                    Low = TryParseDecimal(parts[6], out var low) ? FormatDecimal(low, symbolDigit) : 0,
                    UpdateTime = TryParseLong(parts[7], out var time) ? time : 0,
                    BuyVolume = TryParseDecimal(parts[8], out var buy) ? FormatDecimal(buy, symbolDigit) : 0,
                    SellVolume = TryParseDecimal(parts[9], out var sell) ? FormatDecimal(sell, symbolDigit) : 0,
                    Open = TryParseDecimal(parts[10], out var open) ? FormatDecimal(open, symbolDigit) : 0,
                    PreviousClose = TryParseDecimal(parts[11], out var close) ? FormatDecimal(close, symbolDigit) : 0,
                    Spread = GetSpread(ask, bid, symbolDigit),
                    DailyChangePercent = GetDailyChangePercent(bid, close),
                    DailyChangeValue = FormatDecimal(GetDailyChangeValue(bid, close), symbolDigit)
                };
            }
            catch { return null; }
        }

        private void dgvMarketWatchGrid_Scroll(object sender, ScrollEventArgs e)
        {
            _scrollDebounceTimer.Stop();

            if (e.Type == ScrollEventType.EndScroll || e.Type == ScrollEventType.ThumbPosition)
            {
                _scrollDebounceTimer.Interval = 50;
            }
            else
            {
                _scrollDebounceTimer.Interval = 250;
            }

            _scrollDebounceTimer.Start();
        }

        private void dgvMarketWatchGrid_Resize(object sender, EventArgs e)
        {
            _ = UpdateVisibleSymbolsAsync();
            cmbfontsize_SelectedIndexChanged(null, EventArgs.Empty);
        }

        private async Task UpdateVisibleSymbolsAsync()
        {
            if (_isUpdatingSymbols)
            {
                _updatePending = true;
                return;
            }

            _isUpdatingSymbols = true;

            try
            {
                if (!this.Visible || !this.Enabled || dgvMarketWatchGrid.RowCount == 0) return;

                HashSet<string> newVisible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int firstVisibleIndex = dgvMarketWatchGrid.FirstDisplayedScrollingRowIndex;
                int visibleCount = dgvMarketWatchGrid.DisplayedRowCount(true);

                if (firstVisibleIndex >= 0 && visibleCount > 0)
                {
                    newVisible.UnionWith(
                        dgvMarketWatchGrid.Rows
                            .OfType<DataGridViewRow>()
                            .Skip(firstVisibleIndex - 5)
                            .Where(row => row.Visible)
                            .Take(visibleCount + 5)
                            .Select(row => row.Cells["SymbolName"]?.Value?.ToString())
                            .Where(symbolName => !string.IsNullOrWhiteSpace(symbolName))
                            .Select(symbolName => symbolName.ToReplaceSymbol())
                    );
                }

                if (!newVisible.SetEquals(_currentVisibleSymbols))
                {
                    var toRemove = _currentVisibleSymbols.Except(newVisible).ToArray();
                    var toAdd = newVisible.Except(_currentVisibleSymbols).ToArray();

                    if (toAdd.Length > 0)
                    {
                        foreach (var s in toAdd)
                            await _signalRManager.SafeInvokeAsync("GetLastTickBySymbol", s);

                        await _signalRManager.SafeInvokeAsync("AddToGroup", toAdd);
                    }

                    foreach (var sym in toRemove)
                        await _signalRManager.SafeInvokeAsync("RemoveFromGroup", new[] { sym });

                    _currentVisibleSymbols = newVisible;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update Error: {ex.Message}");
            }
            finally
            {
                _isUpdatingSymbols = false;
                if (_updatePending)
                {
                    _updatePending = false;
                    _ = UpdateVisibleSymbolsAsync();
                }
            }
        }

        #endregion

        #region Update Grid

        private void RefreshGrid()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(RefreshGrid));
                return;
            }

            if (_bindingList == null || _tickQueue == null) return;

            while (_tickQueue.TryDequeue(out var newTick))
            {
                string rawSymbol = newTick.SymbolName;
                //MarketDataHub.Instance.PublishTick(rawSymbol, newTick);
                var existing = _bindingList.FirstOrDefault(x => (x.SymbolName ?? string.Empty).ToReplaceSymbol() == (newTick.SymbolName ?? string.Empty));
                if (existing != null)
                {
                    int rowIndex = _bindingList.IndexOf(existing);
                    if (rowIndex >= 0 && rowIndex < dgvMarketWatchGrid.Rows.Count)
                    {
                        var row = dgvMarketWatchGrid.Rows[rowIndex];

                        if (newTick.Bid != existing.Bid)
                        {
                            string symbolChange = newTick.Bid > existing.Bid ? "▲ " : "▼ ";

                            string currentValue = row.Cells["SymbolName"].Value?.ToString().ToReplaceSymbol() ?? "";
                            row.Cells["SymbolName"].Value = symbolChange + currentValue;

                            var color = newTick.Bid > existing.Bid ? ThemeManager.Blue : ThemeManager.Red;
                            row.Cells["Bid"].Style.ForeColor = color;
                            row.Cells["UpdateDateTime"].Style.ForeColor = color;

                            //if (row.Cells["Bid"].Selected)
                            //{
                            //    row.Cells["Bid"].Style.SelectionForeColor = color;
                            //    row.Cells["UpdateDateTime"].Style.SelectionForeColor = color;
                            //}
                        }

                        if (newTick.Ask != existing.Ask)
                        {
                            var color = newTick.Ask > existing.Ask ? ThemeManager.Blue : ThemeManager.Red;
                            row.Cells["Ask"].Style.ForeColor = color;
                            //if (row.Cells["Ask"].Selected)
                            //{
                            //    row.Cells["Ask"].Style.SelectionForeColor = color;
                            //}
                        }

                        if (newTick.LTP != existing.LTP)
                        {
                            var color = newTick.LTP > existing.LTP ? ThemeManager.Blue : ThemeManager.Red;
                            row.Cells["LTP"].Style.ForeColor = color;
                            //if (row.Cells["LTP"].Selected)
                            //{
                            //    row.Cells["LTP"].Style.SelectionForeColor = color;
                            //}
                        }

                        //row.Cells["DailyChangePercent"].Value = newTick.DailyChangePercent.ToString("F2") + "%";
                        if (newTick.DailyChangePercent != existing.DailyChangePercent)
                        {
                            var color = newTick.DailyChangePercent > 0 ? ThemeManager.Blue : ThemeManager.Red;
                            row.Cells["DailyChangePercent"].Style.ForeColor = color;
                            row.Cells["DailyChangeValue"].Style.ForeColor = color;
                        }

                        if (newTick.Spread != existing.Spread)
                        {
                            var color = newTick.Spread > 0 ? ThemeManager.Blue : ThemeManager.Red;
                            row.Cells["Spread"].Style.ForeColor = color;
                        }

                        // Update values
                        existing.Bid = newTick.Bid;
                        existing.Ask = newTick.Ask;
                        existing.LTP = newTick.LTP;
                        existing.High = newTick.High;
                        existing.Low = newTick.Low;
                        existing.UpdateTime = newTick.UpdateTime;
                        existing.BuyVolume = newTick.BuyVolume;
                        existing.SellVolume = newTick.SellVolume;
                        existing.Open = newTick.Open;
                        existing.PreviousClose = newTick.PreviousClose;
                        existing.Spread = newTick.Spread;
                        existing.DailyChangePercent = newTick.DailyChangePercent;
                        existing.DailyChangeValue = newTick.DailyChangeValue;
                    }
                }
                else if (!removedRows.Any(s => s.SymbolName.ToReplaceSymbol().Equals(newTick.SymbolName, StringComparison.OrdinalIgnoreCase)))
                {
                    _bindingList.Add(newTick);
                }
            }
        }

        private void dgvMarketWatchGrid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // Only handle SymbolName column and valid rows
            if (e.RowIndex < 0 || e.ColumnIndex != dgvMarketWatchGrid.Columns["SymbolName"].Index)
                return;

            var row = dgvMarketWatchGrid.Rows[e.RowIndex];
            var cellValue = row.Cells["SymbolName"].Value?.ToString();

            if (string.IsNullOrEmpty(cellValue)) return;

            // Check if cell contains arrow
            bool hasUpArrow = cellValue.StartsWith("▲");
            bool hasDownArrow = cellValue.StartsWith("▼");

            if (hasUpArrow || hasDownArrow)
            {
                e.PaintBackground(e.CellBounds, true);

                // Determine arrow color
                Color arrowColor = hasUpArrow ? ThemeManager.Blue : ThemeManager.Red;
                string arrowText = cellValue.Substring(0, 2); // Arrow + space
                string symbolText = cellValue.Substring(2);

                // Use TextRenderer for better alignment
                TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding;

                // Draw arrow with colored text
                Color textColor = ThemeManager.Black;

                if (row.Cells["SymbolName"].Selected)
                {
                    textColor = ThemeManager.White;
                }

                // Measure arrow text
                Size arrowSize = TextRenderer.MeasureText(e.Graphics, arrowText, e.CellStyle.Font, Size.Empty, flags);

                // Draw arrow (colored)
                TextRenderer.DrawText(e.Graphics, arrowText, e.CellStyle.Font,
                    new Rectangle(e.CellBounds.Left, e.CellBounds.Top, arrowSize.Width, e.CellBounds.Height),
                    arrowColor, flags);

                // Draw symbol text (normal color)
                TextRenderer.DrawText(e.Graphics, symbolText, e.CellStyle.Font,
                    new Rectangle(e.CellBounds.Left + arrowSize.Width, e.CellBounds.Top,
                                 e.CellBounds.Width - arrowSize.Width, e.CellBounds.Height),
                    textColor, flags);

                e.Handled = true;
            }
        }

        #endregion

        #region Helper

        private decimal GetSpread(decimal ask, decimal bid, int symbolDigit)
        {
            decimal spread = 0;
            try
            {
                decimal multiplier = (decimal)Math.Pow(10, symbolDigit);
                spread = (ask * multiplier) - (bid * multiplier);
                spread = Math.Round(spread, 2);
            }
            catch { spread = 0; }

            return spread;
        }

        private decimal GetDailyChangePercent(decimal bid, decimal close)
        {
            decimal dcp = 0;
            if (bid != 0)
            {
                dcp = (100 * (bid - close)) / bid;
                dcp = Math.Round(dcp, 2);
            }

            return dcp;
        }

        private decimal GetDailyChangeValue(decimal bid, decimal close)
        {
            return bid - close;
        }

        #endregion

        #region Cleanup
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (_signalRManager != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            await _signalRManager.StopAsync();
                            await _signalRManager.DisposeAsync();
                        });
                    }

                    suggestionMenu?.Dispose();
                    contextMenu?.Dispose();
                }
                catch { }
            }

            base.Dispose(disposing);
        }
        #endregion
    }
}
