using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClientDesktop.HelperClass
{
    public static class ThemeManager
    {
        #region Enums
        public enum ThemeType
        {
            Dark,
            Light
        }
        #endregion

        #region Theme Configuration
        // Current theme
        public static ThemeType CurrentTheme { get; private set; } = ThemeType.Light;

        // Common fonts
        public static Font CommonFont = new Font("Microsoft Sans Serif", 10f, FontStyle.Regular);
        public static Font CommonFontForLogin = new Font("Microsoft Sans Serif", 15f, FontStyle.Regular, GraphicsUnit.Pixel);
        public static Font CommonBoldFont = new Font("Microsoft Sans Serif", 10f, FontStyle.Bold);
        public static Font TitleBoldFont = new Font("Microsoft Sans Serif", 12f, FontStyle.Bold);
        public static Font TradeFont = new Font("Microsoft Sans Serif", 20f, FontStyle.Regular);
        public static Font TitleFont = new Font("Microsoft Sans Serif", 12f, FontStyle.Regular);
        public static Font InvoiceCommonFont = new Font("Microsoft Sans Serif", 9f, FontStyle.Regular);

        // Common colors (auto updated on theme change)
        public static Color White { get; private set; } = ColorTranslator.FromHtml("#FFFFFF");
        public static Color Black { get; private set; } = ColorTranslator.FromHtml("#212529");
        public static Color Red { get; private set; } = ColorTranslator.FromHtml("#dc3545");
        public static Color LightRed { get; private set; } = ColorTranslator.FromHtml("#ffc0c0");
        public static Color Blue { get; private set; } = ColorTranslator.FromHtml("#0077fe");
        public static Color Green { get; private set; } = ColorTranslator.FromHtml("#28A745");
        public static Color Gray { get; private set; } = ColorTranslator.FromHtml("#c1c1c1");
        public static Color Yellow { get; private set; } = ColorTranslator.FromHtml("#f2c810");
        public static Color SkyBlue { get; private set; } = ColorTranslator.FromHtml("#38acff"); //Color.FromArgb(163, 189, 217);
        public static Color DockPanelSelectedHeaderBackColor { get; private set; } = Color.FromArgb(153, 180, 209);
        public static Color DockPanelHeaderBackColor { get; private set; } = Color.FromArgb(240, 248, 255);
        public static Color GridRowBackColor { get; private set; } = Color.FromArgb(235, 235, 235);
        public static Color Reportback { get; private set; } = White; //ColorTranslator.FromHtml("#5f7991"); // Previous Use color 
        public static Color SelectionBackColor { get; private set; } = ColorTranslator.FromHtml("#007acc");
        public static Color InvoiceBackColor { get; private set; } = ColorTranslator.FromHtml("#EFECC8");

        // Notify event when theme changes
        public static event Action ThemeChanged;
        #endregion

        #region Theme Application For Controls
        // 🔹 Apply theme to a DataGridView
        public static void ApplyTheme(DataGridView grid)
        {
            if (grid == null) return;

            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = White;

            grid.RowTemplate.Height = CommonHelper.GetScaled(35);
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            // Header Style
            grid.ColumnHeadersDefaultCellStyle.BackColor = White;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Black;
            grid.ColumnHeadersDefaultCellStyle.Font = CommonFont;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = White;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Black;

            // Row Styles
            grid.RowsDefaultCellStyle.BackColor = White;
            grid.RowsDefaultCellStyle.ForeColor = Black;
            grid.RowsDefaultCellStyle.Font = CommonFont;
            grid.AlternatingRowsDefaultCellStyle.BackColor = GridRowBackColor;

            // Selection Styles
            grid.DefaultCellStyle.SelectionBackColor = SelectionBackColor;
            grid.DefaultCellStyle.SelectionForeColor = White;
        }

        // 🔹 Apply theme to a Button
        public static void ApplyTheme(Button button)
        {
            if (button == null) return;

            button.ForeColor = White;
            button.Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Bold);
            button.BackColor = SkyBlue;
        }

        // 🔹 Apply theme to a Panel
        public static void ApplyTheme(Panel panel)
        {
            if (panel == null) return;

            panel.BackColor = White;
        }

        // 🔹 Apply theme to an entire Form (background + grids inside)
        public static void ApplyTheme(Form form)
        {
            if (form == null) return;

            if (form is LoginPage)
            {
                form.Font = CommonFontForLogin;
                foreach (Control ctrl in form.Controls)
                    ApplyThemeRecursive(ctrl, true);
            }
            else
            {
                form.Font = CommonFont;

                foreach (Control ctrl in form.Controls)
                    ApplyThemeRecursive(ctrl);
            }

            form.BackColor = White;
            form.Icon = Properties.Resources.ClientDesktop_Logo;
        }

        // 🔹 Apply theme to an entire UserControl (background + grids inside)
        public static void ApplyTheme(UserControl userControl)
        {
            if (userControl == null) return;

            userControl.Font = CommonFont;

            foreach (Control ctrl in userControl.Controls)
                ApplyThemeRecursive(ctrl);
        }
        #endregion

        #region Recursive Theme Application
        // Recursive apply for nested controls (grids inside panels, etc.)
        private static void ApplyThemeRecursive(Control ctrl, bool isFromLogin = false)
        {
            if (isFromLogin)
            {
                if (ctrl is DataGridView grid)
                {
                    ApplyTheme(grid);
                }
                else
                {
                    ctrl.Font = CommonFontForLogin;

                    foreach (Control child in ctrl.Controls)
                        ApplyThemeRecursive(child, true);
                }
            }
            else
            {
                if (ctrl is DataGridView grid)
                {
                    ApplyTheme(grid);
                }
                else
                {
                    ctrl.Font = CommonFont;

                    foreach (Control child in ctrl.Controls)
                        ApplyThemeRecursive(child);
                }
            }
        }
        #endregion

        #region Layout Adjustment
        public static void AdjustLoginSize(Form child, Form parent)
        {
            if (parent == null) return;

            // ✅ Keep fixed clean layout (no distortion)
            child.Size = new Size(CommonHelper.GetScaled(600), CommonHelper.GetScaled(350));
            child.MaximumSize = child.Size;
            child.MinimumSize = child.Size;

            // ✅ Center inside parent
            child.StartPosition = FormStartPosition.Manual;
            child.Location = new Point(
                parent.Left + (parent.Width - child.Width) / 2,
                parent.Top + (parent.Height - child.Height) / 2
            );
        }
        #endregion
    }
}
