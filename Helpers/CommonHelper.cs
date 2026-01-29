using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TraderApp.Models;
using TraderApp.Config; // AESHelper yaha hai

public static class CommonHelper
{
    #region Events And Basic Helpers
    // Helper Function 
    public static event Action TradeCompleted;


    public static string GetLocalFilePath()
    {
        // Default domain fallback
        var domain = SessionManager.ServerListData
                    .FirstOrDefault(w => w.licenseId.ToString() == SessionManager.LicenseId)?
                    .serverDisplayName ?? "defaultDomain";

        // Default folder inside common data folder
        var folder = Path.Combine(CommonPaths.dataFolder, AESHelper.ToBase64UrlSafe(domain));

        // Default fileName using userId, fallback if null
        string userIdBase64 = !string.IsNullOrEmpty(SessionManager.UserId)
                              ? AESHelper.ToBase64UrlSafe(SessionManager.UserId)
                              : "defaultUser";

        var fileName = $"{userIdBase64}.dat";

        // Full path
        return Path.Combine(folder, fileName);
    }

    public static string GetLocalFolderPath()
    {
        // Default domain fallback
        var domain = SessionManager.ServerListData
                    .FirstOrDefault(w => w.licenseId.ToString() == SessionManager.LicenseId)?
                    .serverDisplayName ?? "defaultDomain";

        // Default folder inside common data folder
        return Path.Combine(CommonPaths.dataFolder, AESHelper.ToBase64UrlSafe(domain));

    }

    public static string ToReplaceSymbol(this string str)
    {
        if (string.IsNullOrEmpty(str)) return str;

        return str.Replace("▲ ", "").Replace("▼ ", "").Trim();
    }

    public static string ToReplaceUrl(this string str, string replaceWith = "api")
    {
        var domain = SessionManager.ServerListData
            .FirstOrDefault(w => w.licenseId.ToString() == SessionManager.LicenseId)?.primaryDomain;
        return string.IsNullOrEmpty(str) || string.IsNullOrEmpty(replaceWith) || domain == null
            ? str
            : str.Replace(replaceWith, replaceWith + "." + domain);
    }

    public static string ToWebSocketUrl(this string serverName, int port = 6011)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            throw new ArgumentException("Server name cannot be empty", nameof(serverName));

        // Ensure serverName does not contain protocol
        serverName = serverName.Replace("http://", "")
                               .Replace("https://", "")
                               .TrimEnd('/');

        return $"wss://skt.{serverName}:{port}/socket.io/?EIO=4&transport=websocket";
    }

    private const float BaseScreenWidth = 1920f;
    public static int GetScaled(float size)
    {
        float _scaleFactor;
        float currentWidth = Screen.PrimaryScreen.Bounds.Width;
        _scaleFactor = (currentWidth * size) / BaseScreenWidth;
        return (int)_scaleFactor;
    }

    // Helper to add Authorization header when calling other APIs
    public static void AddAuthHeader(this HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("Authorization");
        if (!string.IsNullOrEmpty(SessionManager.Token))
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + SessionManager.Token);
        }
    }

    public static async Task WaitForFileReadyAsync(string filePath, string type, int maxWaitMs = 5000)
    {
        int waited = 0;

        while (waited < maxWaitMs)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    // Try to open file safely even if another process is still writing
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs))
                    {
                        string encryptedContent = await reader.ReadToEndAsync();

                        if (!string.IsNullOrWhiteSpace(encryptedContent))
                        {
                            string decryptedContent = AESHelper.DecompressAndDecryptString(encryptedContent);

                            // Check if file actually contains a non-empty "History" array
                            if (decryptedContent.Contains($"\"{type}\""))
                            {
                                try
                                {
                                    var dict = Newtonsoft.Json.JsonConvert
                                        .DeserializeObject<Dictionary<string, object>>(decryptedContent);

                                    if (dict != null && dict.ContainsKey(type))
                                    {
                                        var historyJson = dict[type]?.ToString() ?? "";
                                        if (!string.IsNullOrWhiteSpace(historyJson) && historyJson != "[]")
                                        {
                                            // ✅ File has valid History data → ready to use
                                            break;
                                        }
                                    }
                                }
                                catch
                                {
                                    // If deserialization fails (partial write), keep waiting
                                }
                            }
                        }
                    }
                }
                catch (IOException)
                {
                    // File still writing — wait again
                }
                catch
                {
                    // Possibly half-written file or bad decrypt → retry
                }
            }

            await Task.Delay(100); // wait 300ms before retry
            waited += 100;
        }

        // Tiny delay to ensure OS flush
        await Task.Delay(100);
    }
    #endregion

    #region Data Saving And Encryption
    public static void SaveEncryptedData(string encryptedFolderName, string encryptedFileName, string encryptedContent)
    {
        try
        {
            string safeFolderPath = Path.Combine(CommonPaths.dataFolder, encryptedFolderName);

            if (!Directory.Exists(safeFolderPath))
                Directory.CreateDirectory(safeFolderPath);

            string safeFilePath = Path.Combine(safeFolderPath, $"{encryptedFileName}.dat");

            File.WriteAllText(safeFilePath, encryptedContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    #endregion

    #region Cache Loading Methods
    public static async Task<MarketWatchData> LoadCachedData(string filePath, Exception ex = null)
    {
        try
        {
            if (File.Exists(filePath))
            {
                string encryptedContent = File.ReadAllText(filePath);
                string decryptedContent = AESHelper.DecompressAndDecryptString(encryptedContent);

                // Deserialize the cached data
                var dataDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(decryptedContent);

                // Get specific data by key (e.g., "symbol", "user", etc.)
                if (dataDictionary != null && dataDictionary.ContainsKey("symbol"))
                {
                    return JsonConvert.DeserializeObject<MarketWatchData>(dataDictionary["symbol"].ToString());
                }
            }
            else
            {
                throw ex ?? new Exception("No cached data available.");
            }
        }
        catch (Exception innerEx)
        {
            Console.WriteLine($"⚠️ Failed to read local backup as well.\nDetails: {innerEx.Message}", "Error");
            throw;
        }

        return null;
    }

    public static async Task<List<ClientDetails>> LoadClientDataFromCacheAsync(string filePath, Exception ex = null)
    {
        try
        {
            if (File.Exists(filePath))
            {
                // Read the encrypted content from the file
                string encryptedContent = File.ReadAllText(filePath);
                string decryptedContent = AESHelper.DecompressAndDecryptString(encryptedContent);

                // Deserialize the cached data into a dictionary
                var dataDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(decryptedContent);

                // If the dictionary contains client details, retrieve it using the key "client"
                if (dataDictionary != null && dataDictionary.TryGetValue("client", out var clientObj))
                {
                    // Convert back to JSON string and deserialize to single ClientDetails
                    var clientJson = clientObj.ToString();
                    var singleClient = JsonConvert.DeserializeObject<ClientDetails>(clientJson);

                    return new List<ClientDetails> { singleClient };
                }
            }
            else
            {
                throw ex ?? new Exception("No cached client data available.");
            }
        }
        catch (Exception innerEx)
        {
            // Show an error message if something goes wrong
            Console.WriteLine($"⚠️ Failed to read local backup for client data.\nDetails: {innerEx.Message}", "Error");
            throw;
        }

        return null;
    }

    public static List<Position> LoadPositionDataFromCache(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                string encryptedContent = File.ReadAllText(filePath);
                string decryptedContent = AESHelper.DecompressAndDecryptString(encryptedContent);

                // Deserialize the cached dictionary
                var dataDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(decryptedContent);

                if (dataDictionary != null && dataDictionary.ContainsKey("position"))
                {
                    // Deserialize and return the position list
                    var positionJson = dataDictionary["position"].ToString();
                    return JsonConvert.DeserializeObject<List<Position>>(positionJson);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to read local backup for position data.\nDetails: {ex.Message}", "Error");
        }

        return null;
    }

    public static List<HistoryModel> LoadHistoryDataFromCache(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                string encryptedContent = File.ReadAllText(filePath);
                string decryptedContent = AESHelper.DecompressAndDecryptString(encryptedContent);

                // Deserialize the cached dictionary
                var dataDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(decryptedContent);

                if (dataDictionary != null && dataDictionary.ContainsKey("History"))
                {
                    // Deserialize and return the position list
                    var positionJson = dataDictionary["History"].ToString();
                    return JsonConvert.DeserializeObject<List<HistoryModel>>(positionJson);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to read local backup for History data.\nDetails: {ex.Message}", "Error");
        }

        return null;
    }

    public static List<PositionHistoryModel> LoadPositionHistoryDataFromCache(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                string encryptedContent = File.ReadAllText(filePath);
                string decryptedContent = AESHelper.DecompressAndDecryptString(encryptedContent);

                // Deserialize the cached dictionary
                var dataDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(decryptedContent);

                if (dataDictionary != null && dataDictionary.ContainsKey("PositionHistory"))
                {
                    // Deserialize and return the position history list
                    var positionJson = dataDictionary["PositionHistory"].ToString();
                    return JsonConvert.DeserializeObject<List<PositionHistoryModel>>(positionJson);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to read local backup for Position History data.\nDetails: {ex.Message}", "Error");
        }

        return null;
    }

    public static List<OrderModel> LoadOrderDataFromCache(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                string encryptedContent = File.ReadAllText(filePath);
                string decryptedContent = AESHelper.DecompressAndDecryptString(encryptedContent);

                // Deserialize the cached dictionary
                var dataDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(decryptedContent);

                if (dataDictionary != null && dataDictionary.ContainsKey("order"))
                {
                    // Deserialize and return the order list
                    var orderJson = dataDictionary["order"].ToString();
                    return JsonConvert.DeserializeObject<List<OrderModel>>(orderJson);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to read local backup for order data.\nDetails: {ex.Message}", "Error");
        }

        return null;
    }

    public static List<LoginInfo> LoadLoginDataFromCache(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                string encryptedContent = File.ReadAllText(filePath);
                string decryptedContent = AESHelper.DecompressAndDecryptString(encryptedContent);

                // Deserialize the cached dictionary (entire JSON structure)
                var dataDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(decryptedContent);

                if (dataDictionary != null && dataDictionary.ContainsKey("Login"))
                {
                    var loginData = dataDictionary["Login"];

                    // Case 1: If it's a string (JSON), try to deserialize it into List<LoginInfo>
                    if (loginData is string jsonString)
                    {
                        // If it's a JSON string, deserialize it into a list or single object
                        var list = JsonConvert.DeserializeObject<List<LoginInfo>>(jsonString);

                        // If it's a single LoginInfo object (not a list), wrap it in a List
                        if (list == null)
                        {
                            var single = JsonConvert.DeserializeObject<LoginInfo>(jsonString);
                            return single == null ? null : new List<LoginInfo> { single };
                        }
                        return list;
                    }

                    // Case 2: If it's a JToken (i.e., a JSON array or object), handle accordingly
                    else if (loginData is Newtonsoft.Json.Linq.JToken jToken)
                    {
                        // Case 2.1: If it's a JArray, convert it to List<LoginInfo>
                        if (jToken.Type == JTokenType.Array)
                        {
                            return jToken.ToObject<List<LoginInfo>>();
                        }
                        // Case 2.2: If it's a JObject (single LoginInfo), convert it to a List<LoginInfo>
                        else if (jToken.Type == JTokenType.Object)
                        {
                            var single = jToken.ToObject<LoginInfo>();
                            return single == null ? null : new List<LoginInfo> { single };
                        }
                    }

                    // Case 3: If it's already a List<LoginInfo> (directly deserialized), return it
                    else if (loginData is List<LoginInfo> loginInfoList)
                    {
                        return loginInfoList;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to read local backup for position data.\nDetails: {ex.Message}", "Error");
        }

        return null; // Return null if no data is found or an error occurs
    }
    #endregion

    #region Login And Trade Management
    public static void RaiseTradeCompleted()
    {
        TradeCompleted?.Invoke();
    }

    public static async Task StoreLoginDataAsync(string filePath, string password, bool lastLogin)
    {
        try
        {
            // Load existing login data (this will return a List<LoginInfo> or null if no data)
            var loginInfoList = LoadLoginDataFromCache(filePath);

            // If no login data exists, initialize an empty list
            if (loginInfoList == null)
            {
                loginInfoList = new List<LoginInfo>();
            }

            // Check if the user already exists in the list
            var existingUser = loginInfoList.FirstOrDefault(user => user.UserId == SessionManager.UserId && user.LicenseId == SessionManager.LicenseId);

            if (existingUser != null)
            {
                // Update the existing user's information
                existingUser.LicenseId = SessionManager.LicenseId;
                existingUser.Expiration = SessionManager.Expiration;
                existingUser.ServerListData = SessionManager.ServerListData;
                existingUser.Password = lastLogin ? password : string.Empty;  // Encrypt the password if necessary
                existingUser.LastLogin = true;
            }
            else
            {
                // Create a new login entry for the new user
                var newLoginInfo = new LoginInfo
                {
                    UserId = SessionManager.UserId,
                    Username = SessionManager.Username,
                    LicenseId = SessionManager.LicenseId,
                    Expiration = SessionManager.Expiration,
                    ServerListData = SessionManager.ServerListData,
                    Password = lastLogin ? password : string.Empty,  // Encrypt password if necessary
                    LastLogin = true
                };

                loginInfoList.Add(newLoginInfo);
            }

            // Set LastLogin = false for all other users except the current one
            foreach (var user in loginInfoList.Where(user => user.UserId != SessionManager.UserId || user.LicenseId != SessionManager.LicenseId))
            {
                user.LastLogin = false;
            }

            // Update the dictionary with the new list of login information
            var dataDictionary = new Dictionary<string, object>
            {
                { "Login", JsonConvert.SerializeObject(loginInfoList) }
            };

            // Serialize the dictionary to JSON
            string updatedJson = JsonConvert.SerializeObject(dataDictionary);

            // Encrypt the updated data
            string encryptedContent = AESHelper.CompressAndEncryptString(updatedJson);

            string decryptContent = AESHelper.DecompressAndDecryptString(encryptedContent);

            string reencryptContent = AESHelper.CompressAndEncryptString(decryptContent);

            // Save the encrypted content back to the file
            File.WriteAllText(filePath, reencryptContent);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to store login data.\nDetails: {ex.Message}", "Error");
        }
    }
    #endregion

    #region Utility Methods
    public static string GetLocalIPAddress()
    {
        try
        {
            string hostName = System.Net.Dns.GetHostName();
            var ip = System.Net.Dns.GetHostEntry(hostName)
                .AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return ip?.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }
    #endregion

    #region Esc To Close Application
    public interface IEscCloseControl
    {
        bool AllowEscClose { get; }
    }

    internal sealed class EscToCloseFilter : IMessageFilter
    {
        private const int WM_KEYDOWN = 0x0100;
        private readonly bool _ignoreMdiContainers;

        // List of UserControl types that should NOT trigger close
        private static readonly Type[] _excludedControls =
        {
            typeof(UCChartControl),
            typeof(UCDetailsControl),
            typeof(UCMarketWatchControl),
            typeof(Home),
            typeof(UCNavigationControl)
        };

        public EscToCloseFilter(bool ignoreMdiContainers = true)
        {
            _ignoreMdiContainers = ignoreMdiContainers;
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_KEYDOWN && ((Keys)m.WParam & Keys.KeyCode) == Keys.Escape)
            {
                var form = Form.ActiveForm;
                if (form == null) return false;

                // opt-out support via interface
                if (form is IEscCloseControl esc && esc.AllowEscClose == false)
                    return false;

                if (_ignoreMdiContainers && form.IsMdiContainer)
                    return false;

                // Check if the active form contains any excluded controls
                foreach (var ctrlType in _excludedControls)
                {
                    if (ContainsControlOfType(form, ctrlType))
                        return false;
                }

                try
                {
                    form.Close();
                    return true;
                }
                catch
                {
                    // Optional: log if needed
                    return false;
                }
            }
            return false;
        }

        private bool ContainsControlOfType(Control parent, Type controlType)
        {
            foreach (Control child in parent.Controls)
            {
                if (child.GetType() == controlType)
                    return true;

                // Recursive check in nested containers
                if (child.HasChildren && ContainsControlOfType(child, controlType))
                    return true;
            }
            return false;
        }
    }

    public static class AppWide
    {
        public static void EnableEscToClose(bool ignoreMdiContainers = true)
        {
            Application.AddMessageFilter(new EscToCloseFilter(ignoreMdiContainers));
        }
    }
    #endregion

    #region ExportExcel
    public static void ExportToExcel(DataGridView dgv, string title, string fileName)
    {
        try
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string folder = Path.Combine(userProfile, "Downloads");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fullPath = Path.Combine(folder, fileName + ".xlsx");

            int counter = 1;
            while (File.Exists(fullPath))
            {
                fullPath = Path.Combine(folder, $"{fileName} ({counter}).xlsx");
                counter++;
            }

            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Excel Workbook|*.xlsx";
            save.Title = "Save Excel File";
            save.InitialDirectory = folder;
            save.FileName = Path.GetFileName(fullPath);

            if (save.ShowDialog() != DialogResult.OK)
                return;

            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("Sheet1");
                int rowIndex = 1;

                // ---- TITLE ----
                var titleCell = sheet.Cell(rowIndex, 1);
                titleCell.Value = title;

                var titleRange = sheet.Range(rowIndex, 1, rowIndex, dgv.Columns.Count);
                titleRange.Merge();
                titleRange.Style.Font.Bold = true;
                titleRange.Style.Font.FontSize = 14;
                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                rowIndex += 2;

                // ---- COLUMN HEADERS ----
                for (int i = 0; i < dgv.Columns.Count; i++)
                {
                    var header = sheet.Cell(rowIndex, i + 1);
                    header.Value = dgv.Columns[i].HeaderText;

                    header.Style.Font.Bold = true;
                    header.Style.Fill.BackgroundColor = XLColor.LightGray;

                    var align = dgv.Columns[i].HeaderCell.Style.Alignment;
                    header.Style.Alignment.Horizontal =
                        ConvertAlign(align);
                }

                rowIndex++;

                // ---- DATA ROWS ----
                for (int r = 0; r < dgv.Rows.Count; r++)
                {
                    for (int c = 0; c < dgv.Columns.Count; c++)
                    {
                        var excelCell = sheet.Cell(rowIndex + r, c + 1);
                        var gridCell = dgv.Rows[r].Cells[c];

                        excelCell.Value = gridCell.Value?.ToString();

                        // Alignment
                        excelCell.Style.Alignment.Horizontal = ConvertAlign(gridCell.Style.Alignment);

                        // Font
                        if (gridCell.Style.Font != null)
                        {
                            excelCell.Style.Font.Bold = gridCell.Style.Font.Bold;
                            excelCell.Style.Font.Italic = gridCell.Style.Font.Italic;
                            excelCell.Style.Font.FontSize = gridCell.Style.Font.Size;
                        }

                        // Background color
                        if (gridCell.Style.BackColor != Color.Empty &&
                            gridCell.Style.BackColor != Color.Transparent)
                        {
                            excelCell.Style.Fill.BackgroundColor = XLColor.FromColor(gridCell.Style.BackColor);
                        }
                    }
                }

                sheet.Columns().AdjustToContents();
                workbook.SaveAs(save.FileName);
            }

            MessagePopup.ShowPopup(CommonMessages.ExportExcel, true);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
    private static XLAlignmentHorizontalValues ConvertAlign(DataGridViewContentAlignment align)
    {
        if (align == DataGridViewContentAlignment.MiddleRight)
            return XLAlignmentHorizontalValues.Right;

        if (align == DataGridViewContentAlignment.MiddleCenter)
            return XLAlignmentHorizontalValues.Center;

        return XLAlignmentHorizontalValues.Left;
    }

    #endregion ExportExcel

    #region ExportExcel Old Code
    //public static void ExportToExcel(DataGridView dgv, string title, string fileName)
    //{
    //    try
    //    {
    //        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    //        string folder = Path.Combine(userProfile, "Downloads");

    //        if (!Directory.Exists(folder))
    //            Directory.CreateDirectory(folder);

    //        string baseName = fileName;
    //        string fullPath = Path.Combine(folder, baseName + ".xlsx");

    //        int counter = 1;
    //        while (File.Exists(fullPath))
    //        {
    //            fullPath = Path.Combine(folder, $"{baseName} ({counter}).xlsx");
    //            counter++;
    //        }

    //        SaveFileDialog save = new SaveFileDialog();
    //        save.Filter = "Excel Workbook|*.xlsx";
    //        save.Title = "Save Excel File";
    //        save.InitialDirectory = folder;
    //        save.FileName = Path.GetFileName(fullPath);

    //        if (save.ShowDialog() != DialogResult.OK)
    //            return;

    //        Microsoft.Office.Interop.Excel.Application excelApp =
    //            new Microsoft.Office.Interop.Excel.Application();
    //        excelApp.Workbooks.Add(Type.Missing);

    //        Microsoft.Office.Interop.Excel.Worksheet sheet =
    //            (Microsoft.Office.Interop.Excel.Worksheet)excelApp.ActiveSheet;

    //        int rowIndex = 1;

    //        // ---- TITLE ----
    //        sheet.Cells[rowIndex, 1] = title;

    //        Microsoft.Office.Interop.Excel.Range titleRange =
    //            sheet.Range[sheet.Cells[rowIndex, 1],
    //                        sheet.Cells[rowIndex, dgv.Columns.Count]];
    //        titleRange.Merge();
    //        titleRange.Font.Size = 12;
    //        titleRange.Font.Bold = false;
    //        titleRange.HorizontalAlignment =
    //            Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;

    //        rowIndex += 2;

    //        // ---- COLUMN HEADERS ----
    //        for (int i = 0; i < dgv.Columns.Count; i++)
    //        {
    //            var cell = sheet.Cells[rowIndex, i + 1];
    //            cell.Value = dgv.Columns[i].HeaderText;

    //            cell.Font.Bold = true;
    //            cell.Interior.Color = Color.LightGray;

    //            // Replace switch with if-else
    //            var align = dgv.Columns[i].HeaderCell.Style.Alignment;
    //            if (align == DataGridViewContentAlignment.MiddleRight)
    //                cell.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignRight;
    //            else if (align == DataGridViewContentAlignment.MiddleCenter)
    //                cell.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;
    //            else
    //                cell.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignLeft;
    //        }

    //        rowIndex++;

    //        // ---- DATA ROWS ----
    //        for (int r = 0; r < dgv.Rows.Count; r++)
    //        {
    //            for (int c = 0; c < dgv.Columns.Count; c++)
    //            {
    //                var excelCell = sheet.Cells[r + rowIndex, c + 1];
    //                var gridCell = dgv.Rows[r].Cells[c];

    //                excelCell.Value = gridCell.Value?.ToString();

    //                // Replace switch with if-else
    //                var align = gridCell.Style.Alignment;
    //                if (align == DataGridViewContentAlignment.MiddleRight)
    //                    excelCell.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignRight;
    //                else if (align == DataGridViewContentAlignment.MiddleCenter)
    //                    excelCell.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;
    //                else
    //                    excelCell.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignLeft;

    //                // FONT
    //                if (gridCell.Style.Font != null)
    //                {
    //                    excelCell.Font.Bold = gridCell.Style.Font.Bold;
    //                    excelCell.Font.Italic = gridCell.Style.Font.Italic;
    //                    excelCell.Font.Size = gridCell.Style.Font.Size;
    //                }

    //                // BACKCOLOR
    //                if (gridCell.Style.BackColor != Color.Empty &&
    //                    gridCell.Style.BackColor != Color.Transparent)
    //                {
    //                    excelCell.Interior.Color = ColorTranslator.ToOle(gridCell.Style.BackColor);
    //                }
    //            }
    //        }

    //        // ---- Auto Fit ----
    //        sheet.Columns.AutoFit();

    //        sheet.SaveAs(save.FileName);
    //        excelApp.Quit();

    //        MessagePopup.ShowPopup(CommonMessages.ExportExcel, true);
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine("Error: " + ex.Message);
    //    }
    //}

    #endregion ExportExcel Old Code

    #region AmountFormatter
    public static string FormatAmount(decimal amount)
    {
        return FormatAmountInternal(amount);
    }

    public static string FormatAmount(double amount)
    {
        return FormatAmountInternal((decimal)amount); // Convert double to decimal for precise formatting
    }

    /// <summary>
    /// Internal method to handle formatting
    /// </summary>
    private static string FormatAmountInternal(decimal amount)
    {
        NumberFormatInfo nfi = new NumberFormatInfo()
        {
            NumberGroupSeparator = " ",
            NumberDecimalDigits = 2,
            NumberDecimalSeparator = "."
        };

        // "#,0.00" pattern with comma replaced by space
        //return amount.ToString("#,0.00", nfi).Replace(",", " ");

        // Format with comma pattern but output uses space separator
        return amount.ToString("#,0.00", nfi);
    }
    #endregion AmountFormatter
}
