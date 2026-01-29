using ClientDesktop.HelperClass;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopClient
{
    public partial class DisclaimerForm : Form
    {
        #region Constructor
        public DisclaimerForm()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
            ApplyResponsiveLayout();
        }
        #endregion Constructor

        #region Responsive Layout

        private void ApplyResponsiveLayout()
        {
            this.Width = CommonHelper.GetScaled(this.Width);
            ScaleControlsRecursive(this);
        }

        private void ScaleControlsRecursive(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                // Scale Dimensions
                ctrl.Left = CommonHelper.GetScaled(ctrl.Left);
                ctrl.Top = CommonHelper.GetScaled(ctrl.Top);
                ctrl.Width = CommonHelper.GetScaled(ctrl.Width);
                ctrl.Height = CommonHelper.GetScaled(ctrl.Height);

                if (ctrl.Font != null)
                {
                    float originalSize = ctrl.Font.Size;
                    float scaledSize = CommonHelper.GetScaled(originalSize);

                    if (scaledSize < 9f) scaledSize = 9f;

                    ctrl.Font = new Font(ctrl.Font.FontFamily, scaledSize, ctrl.Font.Style, ctrl.Font.Unit);
                }

                if (ctrl.HasChildren)
                {
                    ScaleControlsRecursive(ctrl);
                }
            }
        }

        #endregion Responsive Layout

        #region Form Events

        private void DisclaimerForm_Load(object sender, EventArgs e)
        {
            int contentWidth = contentPanel.ClientSize.Width - contentPanel.Padding.Left - contentPanel.Padding.Right;
            disclaimer.MaximumSize = new Size(contentWidth, 0);

            this.PerformLayout();

            int neededHeight = titleBar.Height +
                               contentPanel.Padding.Top +
                               disclaimer.PreferredHeight +
                               contentPanel.Padding.Bottom +
                               acknowledgeButton.Height +
                               15;

            int maxHeight = CommonHelper.GetScaled(550);
            this.Height = Math.Min(neededHeight, maxHeight);

            Rectangle screen = Screen.FromControl(this).WorkingArea;
            if (this.Height > screen.Height)
            {
                this.Height = screen.Height - 40;
            }

            this.CenterToScreen();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void AcknowledgeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        #endregion Form Events

        #region Native Drag Support

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        #endregion Native Drag Support
    }
}